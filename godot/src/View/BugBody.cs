using Brenda.Data;
using Godot;

namespace Brenda.View;

/// <summary>Procedural top-down bug, facing +X. Legs follow a tripod gait driven by <see cref="Gait"/>.</summary>
public partial class BugBody : Node2D
{
	static readonly Shader FlashShader = GD.Load<Shader>("res://assets/shaders/flash.gdshader");

	public readonly EnemyDef Def;
	public float Gait;
	public bool Chewing;
	Node2D _eyes;
	float _bite;

	public BugBody(EnemyDef def) => Def = def;

	public override void _Ready()
	{
		Material = new ShaderMaterial { Shader = FlashShader };
		_eyes = new Node2D { Material = FxUtil.Additive };
		_eyes.Draw += DrawEyes;
		AddChild(_eyes);
	}

	public void SetFlash(float value) => ((ShaderMaterial)Material).SetShaderParameter("flash", value);

	public void SetFrozen(float value) => ((ShaderMaterial)Material).SetShaderParameter("frozen", value);

	public void Animate(float deltaGait, float delta)
	{
		Gait += deltaGait;
		_bite = Chewing ? _bite + delta * 14 : 0;
		QueueRedraw();
		_eyes.QueueRedraw();
	}

	public override void _Draw()
	{
		var r = Def.Radius;
		var shadow = new Vector2(3, 5).Rotated(-GlobalRotation);
		DrawColoredPolygon(FxUtil.Ellipse(shadow, new Vector2(r * 1.15f, r * 0.85f)), new Color(0, 0, 0, 0.35f));
		// Dark rim so bugs read clearly on top of the bright copper.
		DrawColoredPolygon(FxUtil.Ellipse(new Vector2(-r * 0.15f, 0), new Vector2(r * 1.2f, r * 0.95f)), new Color(0, 0, 0, 0.45f));
		switch (Def.Style)
		{
			case "spider": Spider(r); break;
			case "slim": Slim(r); break;
			case "armored": Armored(r); break;
			case "boss": Boss(r); break;
			default: Beetle(r); break;
		}
		if (Chewing)
			Mandibles(r);
	}

	void Ellipse(Vector2 c, Vector2 radii, Color color) => DrawColoredPolygon(FxUtil.Ellipse(c, radii), color);

	void Beetle(float r)
	{
		Legs(3, r * 0.55f, r * 1.25f, 2 + r * 0.08f, 0.35f);
		Antennae(r);
		Ellipse(new Vector2(-r * 0.25f, 0), new Vector2(r * 0.95f, r * 0.8f), Def.Accent);
		Ellipse(new Vector2(-r * 0.3f, 0), new Vector2(r * 0.85f, r * 0.7f), Def.Shell);
		DrawLine(new Vector2(r * 0.35f, 0), new Vector2(-r * 1.1f, 0), Def.Accent, Mathf.Max(r * 0.08f, 1.2f));
		Ellipse(new Vector2(-r * 0.45f, -r * 0.3f), new Vector2(r * 0.35f, r * 0.18f), new Color(1, 1, 1, 0.35f));
		Ellipse(new Vector2(r * 0.55f, 0), new Vector2(r * 0.4f, r * 0.42f), Def.Accent.Darkened(0.3f));
		Ellipse(new Vector2(r * 0.85f, 0), new Vector2(r * 0.3f, r * 0.3f), Def.Leg);
	}

	void Spider(float r)
	{
		Legs(4, r * 0.25f, r * 1.7f, 1.8f + r * 0.05f, 0.5f);
		Ellipse(new Vector2(-r * 0.45f, 0), new Vector2(r * 0.85f, r * 0.75f), Def.Accent);
		Ellipse(new Vector2(-r * 0.5f, -r * 0.05f), new Vector2(r * 0.75f, r * 0.65f), Def.Shell);
		var m = new Vector2(-r * 0.5f, 0);
		var mark = Def.Eye.Darkened(0.2f);
		DrawColoredPolygon(new[] { m + new Vector2(-r * 0.3f, -r * 0.18f), m + new Vector2(-r * 0.3f, r * 0.18f), m }, mark);
		DrawColoredPolygon(new[] { m + new Vector2(r * 0.3f, -r * 0.18f), m, m + new Vector2(r * 0.3f, r * 0.18f) }, mark);
		Ellipse(new Vector2(-r * 0.7f, -r * 0.3f), new Vector2(r * 0.25f, r * 0.14f), new Color(1, 1, 1, 0.3f));
		Ellipse(new Vector2(r * 0.45f, 0), new Vector2(r * 0.45f, r * 0.42f), Def.Leg.Lightened(0.15f));
	}

	void Slim(float r)
	{
		Legs(3, r * 0.6f, r * 1.3f, 1.6f, 0.6f, -0.5f);
		Antennae(r * 1.3f);
		Ellipse(new Vector2(-r * 0.6f, 0), new Vector2(r * 1.1f, r * 0.45f), Def.Accent);
		Ellipse(new Vector2(-r * 0.65f, 0), new Vector2(r * 1f, r * 0.36f), Def.Shell);
		for (var i = 0; i < 3; i++)
		{
			var x = -r * (0.2f + i * 0.4f);
			DrawLine(new Vector2(x, -r * 0.36f), new Vector2(x, r * 0.36f), Def.Accent, 1.2f);
		}
		Ellipse(new Vector2(r * 0.45f, 0), new Vector2(r * 0.35f, r * 0.32f), Def.Accent.Darkened(0.2f));
		Ellipse(new Vector2(r * 0.75f, 0), new Vector2(r * 0.25f, r * 0.25f), Def.Leg);
	}

	void Armored(float r)
	{
		Legs(3, r * 0.6f, r * 1.15f, 2.6f + r * 0.06f, 0.25f);
		Antennae(r * 0.8f);
		Ellipse(new Vector2(-r * 0.2f, 0), new Vector2(r * 1f, r * 0.88f), Def.Accent.Darkened(0.3f));
		for (var i = 0; i < 4; i++)
		{
			var x = -r * 0.95f + i * r * 0.45f;
			var plate = new Rect2(x, -r * 0.8f, r * 0.42f, r * 1.6f);
			DrawRect(plate, Def.Shell.Darkened(i * 0.06f));
			DrawRect(new Rect2(plate.Position, new Vector2(plate.Size.X, r * 0.18f)), new Color(1, 1, 1, 0.25f));
			DrawRect(plate, Def.Accent, false, 1.5f);
		}
		Ellipse(new Vector2(r * 0.85f, 0), new Vector2(r * 0.35f, r * 0.4f), Def.Leg);
	}

	void Boss(float r)
	{
		Legs(4, r * 0.5f, r * 1.35f, 4, 0.3f);
		var bite = Mathf.Sin(Gait * 1.5f) * 0.25f;
		for (var s = -1; s <= 1; s += 2)
		{
			var b = new Vector2(r * 0.85f, s * r * 0.25f);
			var tip = b + new Vector2(r * 0.55f, -s * r * (0.05f + bite));
			DrawPolyline(new[] { b, b + new Vector2(r * 0.35f, s * r * 0.15f), tip }, Def.Leg, 5, true);
		}
		Ellipse(new Vector2(-r * 0.2f, 0), new Vector2(r * 1.05f, r * 0.9f), Def.Accent);
		Ellipse(new Vector2(-r * 0.25f, 0), new Vector2(r * 0.95f, r * 0.8f), Def.Shell);
		for (var i = 0; i < 5; i++)
		{
			var x = -r * 0.9f + i * r * 0.38f;
			for (var s = -1; s <= 1; s += 2)
			{
				var p = new Vector2(x, s * r * 0.62f);
				DrawColoredPolygon(new[] { p + new Vector2(-5, 0), p + new Vector2(5, 0), p + new Vector2(0, s * 12) }, Def.Leg);
			}
		}
		DrawLine(new Vector2(r * 0.4f, 0), new Vector2(-r * 1.15f, 0), Def.Accent, 3);
		Ellipse(new Vector2(-r * 0.5f, -r * 0.35f), new Vector2(r * 0.4f, r * 0.15f), new Color(1, 1, 1, 0.25f));
		Ellipse(new Vector2(r * 0.7f, 0), new Vector2(r * 0.45f, r * 0.5f), Def.Leg.Lightened(0.1f));
	}

	/// <summary>Snapping jaws while chewing on a component.</summary>
	void Mandibles(float r)
	{
		var open = 0.25f + 0.25f * Mathf.Sin(_bite);
		var front = r * (Def.Style == "spider" ? 0.75f : 1f);
		for (var s = -1; s <= 1; s += 2)
		{
			var b = new Vector2(front, s * r * 0.15f);
			var tip = b + new Vector2(r * 0.45f, s * r * (0.2f + open) * 0.6f);
			var tip2 = tip + new Vector2(r * 0.15f, -s * r * 0.25f);
			DrawPolyline(new[] { b, tip, tip2 }, Def.Leg.Lightened(0.25f), Mathf.Max(2, r * 0.12f), true);
		}
	}

	void DrawEyes()
	{
		var r = Def.Radius;
		var front = Def.Style switch { "spider" => r * 0.7f, "slim" => r * 0.85f, _ => r * 0.9f };
		var spread = r * 0.22f;
		var size = Mathf.Max(r * 0.12f, 1.8f);
		for (var s = -1; s <= 1; s += 2)
		{
			var p = new Vector2(front, s * spread);
			FxUtil.DrawGlow(_eyes, p, size * 6, Def.Eye);
			_eyes.DrawCircle(p, size * 0.6f, Def.Eye.Lerp(Colors.White, 0.6f));
		}
		if (Def.Style == "boss")
			FxUtil.DrawGlow(_eyes, new Vector2(-r * 0.25f, 0), r * 1.2f, new Color(Def.Eye, 0.6f + 0.4f * Mathf.Sin(Gait * 0.8f)));
	}

	/// <summary>Paired legs from hip through a knee to the foot, animated with a tripod gait.</summary>
	void Legs(int pairs, float bodyHalf, float reach, float width, float swing, float rake = 0)
	{
		for (var i = 0; i < pairs; i++)
		{
			var t = (float)i / Mathf.Max(pairs - 1, 1);
			var hipX = Mathf.Lerp(bodyHalf, -bodyHalf, t) * 0.8f;
			for (var side = -1; side <= 1; side += 2)
			{
				var phase = Gait + i * Mathf.Pi + (side > 0 ? Mathf.Pi : 0);
				var step = Mathf.Sin(phase) * swing;
				var lift = Mathf.Max(Mathf.Cos(phase), 0);
				var angle = Mathf.Lerp(-0.9f, 0.9f, t) + rake + step;
				var hip = new Vector2(hipX, side * Def.Radius * 0.45f);
				var dir = new Vector2(Mathf.Sin(angle), side * Mathf.Cos(angle));
				var knee = hip + dir * reach * 0.55f + new Vector2(0, side * reach * 0.1f * lift);
				var foot = knee + new Vector2(Mathf.Sin(angle) * 1.4f, side * Mathf.Cos(angle) * 0.6f).Normalized() * reach * 0.6f;
				DrawPolyline(new[] { hip, knee, foot }, Def.Leg, width, true);
			}
		}
	}

	void Antennae(float r)
	{
		var wiggle = Mathf.Sin(Gait * 0.7f) * 0.15f;
		for (var s = -1; s <= 1; s += 2)
		{
			var b = new Vector2(Def.Radius * 0.95f, s * Def.Radius * 0.2f);
			var mid = b + new Vector2(r * 0.45f, s * r * (0.35f + wiggle));
			var tip = mid + new Vector2(r * 0.45f, s * r * (0.15f - wiggle));
			DrawPolyline(new[] { b, mid, tip }, Def.Leg, 1.3f, true);
		}
	}
}
