using Godot;

namespace HardwareDefence.View;

/// <summary>
/// The bug route drawn as a wide copper trace with data pulses flowing to the CPU,
/// solder pads on the corners and a glitching breach where bugs come in.
/// </summary>
public partial class PathTraceView : Node2D
{
	static readonly Color CopperEdge = new("5c3b10");

	Vector2[] _points = System.Array.Empty<Vector2>();
	Vector2 _entry;
	float _breachAngle;
	Line2D _flow;
	PointLight2D _breachLight;
	Sprite2D _breachGlow;
	Node2D _breach;
	float _t;

	public void Setup(Vector2[] points, Vector2 entry)
	{
		foreach (var child in GetChildren())
			child.QueueFree();
		_points = points;
		_entry = entry;
		if (points.Length < 2)
			return;
		var firstDir = (points[1] - points[0]).Normalized();
		_breachAngle = firstDir.Angle();

		AddLine(points, 36, new Color(0, 0, 0, 0.4f), new Vector2(4, 5));
		AddLine(points, 32, CopperEdge);
		var copper = AddLine(points, 26, Colors.White);
		copper.Texture = BevelTexture();
		copper.TextureMode = Line2D.LineTextureMode.Tile;
		AddLine(points, 8, new Color(0.25f, 0.15f, 0.05f, 0.55f));

		_flow = AddLine(points, 22, Colors.White);
		_flow.Texture = BevelTexture();
		_flow.TextureMode = Line2D.LineTextureMode.Stretch;
		var mat = new ShaderMaterial { Shader = GD.Load<Shader>("res://assets/shaders/trace_flow.gdshader") };
		mat.SetShaderParameter("line_length", Length(points));
		_flow.Material = mat;

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
			Gravity = Vector2.Zero, InitialVelocityMin = 10, InitialVelocityMax = 35, ScaleAmountMin = 0.2f, ScaleAmountMax = 0.5f,
			ColorRamp = new Gradient { Colors = new[] { new Color(1, 0.3f, 0.5f), new Color(1, 0.1f, 0.3f, 0) } },
		});
		QueueRedraw();
	}

	/// <summary>Make pulses race faster (used while a wave is running).</summary>
	public void SetIntensity(float value)
	{
		if (_flow?.Material is not ShaderMaterial mat)
			return;
		mat.SetShaderParameter("intensity", value);
		mat.SetShaderParameter("speed", 70 + 80 * value);
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

	public override void _Draw()
	{
		// Solder pads on the corners.
		for (var i = 1; i < _points.Length - 1; i++)
		{
			var p = _points[i];
			DrawCircle(p + new Vector2(3, 4), 22, new Color(0, 0, 0, 0.35f));
			DrawCircle(p, 21, CopperEdge);
			DrawCircle(p, 18, new Color("d9a441"));
			DrawCircle(p, 12, new Color("8f6220"));
			DrawCircle(p, 5, new Color("2a1a08"));
		}
	}

	void DrawBreach()
	{
		var rng = new RandomNumberGenerator { Seed = (ulong)Mathf.FloorToInt(_t * 14) };
		var side = Vector2.FromAngle(_breachAngle).Orthogonal();
		for (var i = 0; i < 7; i++)
		{
			var off = side * rng.RandfRange(-24, 24) + Vector2.FromAngle(_breachAngle) * rng.RandfRange(-6, 10);
			var size = new Vector2(rng.RandfRange(4, 14), rng.RandfRange(2, 6));
			_breach.DrawRect(new Rect2(_entry + off - size / 2, size), new Color(1, 0.2f, 0.4f, rng.RandfRange(0.3f, 0.9f)));
		}
		_breach.DrawCircle(_entry, 17, new Color(0.05f, 0, 0.02f, 0.85f));
		_breach.DrawArc(_entry, 19, 0, Mathf.Tau, 32, new Color(1, 0.25f, 0.45f, 0.9f), 2.5f, true);
	}

	Line2D AddLine(Vector2[] points, float width, Color color, Vector2 offset = default)
	{
		var line = new Line2D
		{
			Points = points, Position = offset, Width = width, DefaultColor = color, JointMode = Line2D.LineJointMode.Round,
			BeginCapMode = Line2D.LineCapMode.Round, EndCapMode = Line2D.LineCapMode.Round, Antialiased = true,
		};
		AddChild(line);
		return line;
	}

	static float Length(Vector2[] points)
	{
		var total = 0f;
		for (var i = 1; i < points.Length; i++)
			total += points[i - 1].DistanceTo(points[i]);
		return total;
	}

	/// <summary>Vertical gradient across the trace width so it reads as a bevelled metal strip.</summary>
	static Texture2D BevelTexture() => new GradientTexture2D
	{
		Width = 4, Height = 32, FillFrom = Vector2.Zero, FillTo = new Vector2(0, 1),
		Gradient = new Gradient
		{
			Offsets = new[] { 0f, 0.18f, 0.45f, 0.82f, 1f },
			Colors = new[] { new Color("8a5a1a"), new Color("ffd98a"), new Color("e3a345"), new Color("b97a28"), CopperEdge },
		},
	};
}
