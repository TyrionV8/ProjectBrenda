using Godot;

namespace HardwareDefence.View;

/// <summary>
/// The bug route: a wide copper trace with solder pads on the corners, painted in art pixels
/// (in tiles, so the LEDs along it light it), and a glitching breach where bugs come in.
/// </summary>
public partial class PathTraceView : Node2D
{
	const int Px = PixelWorld.Scale;
	static readonly Color CopperEdge = new("5c3b10");
	static readonly Color[] Copper = { new("ffd98a"), new("e8ab4c"), new("c4842e"), new("8a5a1a") };

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

	/// <summary>Distance from q to the route and where across the trace it is (negative = top/left side).</summary>
	(float Distance, float Across) Measure(Vector2 q, Vector2[] pts)
	{
		var best = float.MaxValue;
		var across = 0f;
		for (var i = 1; i < pts.Length; i++)
		{
			var a = pts[i - 1];
			var b = pts[i];
			var c = Geometry2D.GetClosestPointToSegment(q, a, b);
			var d = q.DistanceTo(c);
			if (d >= best)
				continue;
			best = d;
			var horizontal = Mathf.Abs(b.Y - a.Y) < 0.01f;
			var atEnd = c.IsEqualApprox(a) || c.IsEqualApprox(b);
			across = atEnd ? Mathf.Sign((q - c).X + (q - c).Y) * d : horizontal ? q.Y - a.Y : q.X - a.X;
		}
		return (best, across);
	}

	PixelCanvas Paint()
	{
		var canvas = new PixelCanvas(BoardCells.X * BoardView.CellPx, BoardCells.Y * BoardView.CellPx);
		var pts = new Vector2[_points.Length];
		for (var i = 0; i < pts.Length; i++)
			pts[i] = _points[i] / Px;
		const float half = 4.5f;
		for (var y = 0; y < canvas.Height; y++)
			for (var x = 0; x < canvas.Width; x++)
			{
				var q = new Vector2(x + 0.5f, y + 0.5f);
				var (d, across) = Measure(q, pts);
				if (d <= half + 1)
				{
					if (d <= half)
					{
						var t = Mathf.Clamp((across / half + 1) / 2, 0, 0.999f);
						canvas.Set(x, y, Copper[(int)(t * Copper.Length)]);
						if (d < 1)
							canvas.Plot(x, y, new Color(0.25f, 0.15f, 0.05f, 0.55f));
					}
					else
						canvas.Set(x, y, CopperEdge);
				}
				else if (Measure(q - new Vector2(1, 2), pts).Distance <= half + 1)
					canvas.Set(x, y, new Color(0, 0, 0, 0.4f));
			}
		// Solder pads on the corners.
		for (var i = 1; i < pts.Length - 1; i++)
		{
			var p = pts[i];
			canvas.Disc(p.X + 1, p.Y + 1.5f, 7.3f, new Color(0, 0, 0, 0.35f));
			canvas.Disc(p.X, p.Y, 7, CopperEdge);
			canvas.Disc(p.X, p.Y, 6, new Color("d9a441"));
			canvas.Disc(p.X - 0.5f, p.Y - 0.5f, 4.6f, new Color("f0c060"));
			canvas.Disc(p.X, p.Y, 4, new Color("8f6220"));
			canvas.Disc(p.X, p.Y, 1.7f, new Color("2a1a08"));
		}
		return canvas;
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
		_breach.DrawCircle(_entry, 17, new Color(0.05f, 0, 0.02f, 0.85f));
		_breach.DrawArc(_entry, 19, 0, Mathf.Tau, 32, new Color(1, 0.25f, 0.45f, 0.9f), Px);
	}
}
