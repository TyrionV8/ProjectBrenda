using Godot;

namespace HardwareDefence.View;

/// <summary>
/// The bug route: a thick copper trace with chamfered bends, raised rails and a recessed lane
/// marked with chevrons toward the CPU, painted in art pixels (in tiles, so the LEDs along it
/// light it), and a glitching breach where bugs come in.
/// </summary>
public partial class PathTraceView : Node2D
{
	const int Px = PixelWorld.Scale;

	Vector2[] _points = System.Array.Empty<Vector2>();
	Vector2 _entry;
	float _breachAngle;
	PointLight2D _breachLight;
	Sprite2D _breachGlow;
	Node2D _breach;
	float _t;

	/// <summary>Board size in cells (the trace is painted over the whole board).</summary>
	public Vector2I BoardCells = new(Data.Grid.Cols, Data.Grid.Rows);

	public void Setup(Vector2[] points, Vector2 entry)
	{
		foreach (var child in GetChildren())
			child.QueueFree();
		_breach = null;
		_points = points;
		_entry = entry;
		if (points.Length < 2)
			return;
		var firstDir = (points[1] - points[0]).Normalized();
		_breachAngle = firstDir.Angle();
		AddChild(Paint().ToTiles());

		_breach = new Node2D { Material = FxUtil.Unshaded };
		_breach.Draw += DrawBreach;
		AddChild(_breach);
		_breachGlow = FxUtil.GlowSprite(new Color(1f, 0.15f, 0.35f), 1.6f);
		_breachGlow.Position = entry;
		AddChild(_breachGlow);
		_breachLight = FxUtil.Light(new Color(1f, 0.2f, 0.35f), 1.2f, 0.8f);
		_breachLight.Position = entry;
		AddChild(_breachLight);
		AddChild(new CpuParticles2D
		{
			Position = entry, Amount = 18, Lifetime = 1.1f, Texture = FxUtil.PixelTexture, Material = FxUtil.Additive,
			EmissionShape = CpuParticles2D.EmissionShapeEnum.Sphere, EmissionSphereRadius = 14, Direction = firstDir, Spread = 50,
			Gravity = Vector2.Zero, InitialVelocityMin = 10, InitialVelocityMax = 35, ScaleAmountMin = 0.375f, ScaleAmountMax = 0.75f,
			ColorRamp = new Gradient { Colors = new[] { new Color(1, 0.3f, 0.5f), new Color(1, 0.1f, 0.3f, 0) } },
		});
	}

	/// <summary>
	/// Distance from q to the route and where across the trace it is (negative = top/left).
	/// Distances to the corners use an octagonal measure, so bends get 45 degree chamfers like
	/// real PCB traces.
	/// </summary>
	static (float Distance, float Across) Measure(Vector2 q, Vector2[] pts)
	{
		var best = float.MaxValue;
		var across = 0f;
		for (var i = 1; i < pts.Length; i++)
		{
			var a = pts[i - 1];
			var b = pts[i];
			var horizontal = Mathf.Abs(b.Y - a.Y) < 0.01f;
			float d, s;
			var t = horizontal ? (q.X - a.X) / (b.X - a.X) : (q.Y - a.Y) / (b.Y - a.Y);
			if (t >= 0 && t <= 1)
			{
				s = horizontal ? q.Y - a.Y : q.X - a.X;
				d = Mathf.Abs(s);
			}
			else
			{
				var end = t < 0 ? a : b;
				var dx = Mathf.Abs(q.X - end.X);
				var dy = Mathf.Abs(q.Y - end.Y);
				d = Mathf.Max(Mathf.Max(dx, dy), (dx + dy) * 0.7071f);
				s = Mathf.Sign(q.X - end.X + q.Y - end.Y) * d;
			}
			if (d < best)
			{
				best = d;
				across = s;
			}
		}
		return (best, across);
	}

	// The trace, in art pixels from its middle: a 3 px recessed lane where the bugs walk,
	// raised rails on both sides lit from the top-left, and a dark rim.
	const float Lane = 3.5f;
	const float Half = 7.5f;
	static readonly Color Rim = new("4a2f0c");
	static readonly Color RailLight = new("f2c25e");
	static readonly Color RailEdgeLight = new("ffe6a6");
	static readonly Color RailDark = new("b37428");
	static readonly Color RailEdgeDark = new("85561a");
	static readonly Color LaneColor = new("74491b");
	static readonly Color LaneShade = new("4c3010");
	static readonly Color LaneLit = new("9a6a2a");
	static readonly Color Chevron = new("c99245");

	PixelCanvas Paint()
	{
		var canvas = new PixelCanvas(BoardCells.X * BoardView.CellPx, BoardCells.Y * BoardView.CellPx);
		var pts = new Vector2[_points.Length];
		for (var i = 0; i < pts.Length; i++)
			pts[i] = _points[i] / Px;
		for (var y = 0; y < canvas.Height; y++)
			for (var x = 0; x < canvas.Width; x++)
			{
				var q = new Vector2(x + 0.5f, y + 0.5f);
				var (d, s) = Measure(q, pts);
				if (d > Half)
				{
					if (Measure(q - new Vector2(1, 2), pts).Distance <= Half)
						canvas.Set(x, y, new Color(0, 0, 0, 0.45f));
					continue;
				}
				Color c;
				if (d > Half - 1)
					c = Rim;
				else if (d > Lane)
				{
					var outer = d > Half - 2;
					c = s < 0 ? (outer ? RailEdgeLight : RailLight) : (outer ? RailEdgeDark : RailDark);
					// Fine etching along the rails.
					if ((x + y) % 5 == 0)
						c = c.Darkened(0.06f);
				}
				else
				{
					// Recessed: the top/left inner edge is in shadow, the bottom/right one catches light.
					c = s < -(Lane - 1) ? LaneShade : s > Lane - 1 ? LaneLit : LaneColor;
					if (((x >> 1) + (y >> 1)) % 3 == 0)
						c = c.Lightened(0.05f);
				}
				canvas.Set(x, y, c);
			}
		PaintChevrons(canvas, pts);
		// A via in the middle of every bend.
		for (var i = 1; i < pts.Length - 1; i++)
		{
			canvas.Disc(pts[i].X, pts[i].Y, 2.6f, new Color("d9a441"));
			canvas.Disc(pts[i].X, pts[i].Y, 1.4f, new Color("1a1006"));
		}
		return canvas;
	}

	/// <summary>Static chevrons along the lane, one per cell, pointing toward the CPU.</summary>
	static void PaintChevrons(PixelCanvas canvas, Vector2[] pts)
	{
		for (var i = 1; i < pts.Length; i++)
		{
			var a = pts[i - 1];
			var b = pts[i];
			var length = a.DistanceTo(b);
			if (length < 1)
				continue;
			var dir = (b - a) / length;
			var side = dir.Orthogonal();
			for (var d = BoardView.CellPx; d <= length - BoardView.CellPx * 0.75f; d += BoardView.CellPx)
			{
				// The tip sits just ahead of the cell centre; the arms trail back.
				var tip = a + dir * (d + 1.5f);
				for (var k = 0; k <= 2; k++)
					foreach (var sgn in new[] { -1, 1 })
					{
						var p = tip - dir * k + side * sgn * (k + 0.5f);
						canvas.Plot(Mathf.FloorToInt(p.X), Mathf.FloorToInt(p.Y), Chevron);
						var back = p - dir;
						canvas.Plot(Mathf.FloorToInt(back.X), Mathf.FloorToInt(back.Y), new Color(Chevron, 0.45f));
					}
			}
		}
	}

	public override void _Process(double delta)
	{
		if (_breach == null)
			return;
		_t += (float)delta;
		var pulse = 0.75f + 0.25f * Mathf.Sin(_t * 5) + ((_t % 2.3f) < 0.08f ? 0.3f : 0);
		_breachGlow.Modulate = new Color(_breachGlow.Modulate, pulse);
		_breachLight.Energy = pulse;
		_breach.QueueRedraw();
	}

	void DrawBreach()
	{
		var rng = new RandomNumberGenerator { Seed = (ulong)Mathf.FloorToInt(_t * 14) };
		var along = Vector2.FromAngle(_breachAngle);
		var side = along.Orthogonal();
		for (var i = 0; i < 7; i++)
		{
			var off = side * rng.RandfRange(-24, 24) + along * rng.RandfRange(-6, 10);
			var size = new Vector2(rng.RandiRange(1, 5), rng.RandiRange(1, 2)) * Px;
			_breach.DrawRect(new Rect2(PixelWorld.Snap(_entry + off - size / 2), size), new Color(1, 0.2f, 0.4f, rng.RandfRange(0.3f, 0.9f)));
		}
		_breach.DrawCircle(_entry, 22, new Color(0.05f, 0, 0.02f, 0.85f));
		_breach.DrawArc(_entry, 24, 0, Mathf.Tau, 32, new Color(1, 0.25f, 0.45f, 0.9f), Px);
	}
}
