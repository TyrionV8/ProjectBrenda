using HardwareDefence.Sim;
using Godot;

namespace HardwareDefence.View;

/// <summary>Bullets, bombs and frost orbs, drawn from snapshots.</summary>
public partial class ProjectileView : Node2D
{
	public readonly int Id;
	readonly string _kind;
	readonly Color _color;
	float _angle;
	float _height;
	float _t;
	float _trail;
	Node2D _glow;

	public ProjectileView(int id, string kind, Color color)
	{
		Id = id;
		_kind = kind;
		_color = color;
	}

	public override void _Ready()
	{
		if (_kind == "bomb")
		{
			_glow = new Node2D { Material = FxUtil.Additive };
			_glow.Draw += DrawBombGlow;
			AddChild(_glow);
		}
		else
		{
			Material = FxUtil.Additive;
		}
	}

	public void Apply(ProjectileSnap s, float delta)
	{
		var pos = new Vector2(s.X, s.Y);
		_trail = Mathf.Min(_trail + pos.DistanceTo(Position), 16);
		Position = pos;
		_angle = s.A;
		_height = s.H;
		_t += delta;
		QueueRedraw();
		_glow?.QueueRedraw();
	}

	public override void _Draw()
	{
		switch (_kind)
		{
			case "bullet":
				var back = -Vector2.FromAngle(_angle) * _trail;
				DrawLine(back, Vector2.Zero, new Color(_color, 0.35f), 6);
				FxUtil.DrawGlow(this, Vector2.Zero, 30, new Color(_color, 0.9f));
				DrawRect(new Rect2(-3, -3, 6, 6), new Color(1, 1, 1, 0.95f));
				break;
			case "bomb":
				var lift = new Vector2(0, -_height * 26);
				var k = 1 + _height * 0.8f;
				DrawCircle(new Vector2(0, 3), 6 * (1 - _height * 0.4f), new Color(0, 0, 0, 0.35f * (1 - _height * 0.5f)));
				DrawCircle(lift, 7 * k, new Color("e9eef3"));
				DrawCircle(lift, 5 * k, _color);
				DrawCircle(lift + new Vector2(-1.5f, -1.5f) * k, 2 * k, Colors.White);
				break;
			default:
				FxUtil.DrawGlow(this, Vector2.Zero, 32, new Color(_color, 0.85f));
				DrawCircle(Vector2.Zero, 3.5f, new Color(0.85f, 0.97f, 1f));
				for (var i = 0; i < 3; i++)
				{
					var a = _t * 2.5f + Mathf.Tau * i / 3;
					DrawRect(new Rect2(PixelWorld.Snap(Vector2.FromAngle(a) * 9) - new Vector2(1.5f, 1.5f), new Vector2(3, 3)), new Color(0.8f, 0.95f, 1f, 0.9f));
				}
				break;
		}
	}

	void DrawBombGlow()
	{
		var lift = new Vector2(0, -_height * 26);
		var blink = 0.6f + 0.4f * Mathf.Sin(_t * 40);
		FxUtil.DrawGlow(_glow, lift, 28, new Color(_color, 0.8f * blink));
	}
}
