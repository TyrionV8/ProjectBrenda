using HardwareDefence.Sim;
using Godot;

namespace HardwareDefence.View;

/// <summary>The processor the player defends. Shows its integrity as a ring of segments.</summary>
public partial class CpuView : Node2D
{
	public static readonly Color Healthy = new(0.25f, 0.95f, 1f);
	public static readonly Color Critical = new(1f, 0.25f, 0.3f);
	static readonly Font Bold = GD.Load<Font>("res://assets/fonts/Silkscreen-Bold.ttf");

	public string Label = "CPU";
	int _hp = Match.CpuMaxHp;
	Sprite2D _sprite;
	Sprite2D _core;
	Sprite2D _halo;
	PointLight2D _light;
	Node2D _ring;
	float _t;
	float _hit;

	public override void _Ready()
	{
		_halo = FxUtil.GlowSprite(Healthy, 2.2f);
		AddChild(_halo);
		_sprite = new Sprite2D { Texture = GD.Load<Texture2D>("res://assets/art/board/cpu.svg"), Scale = Vector2.One * 0.5f };
		AddChild(_sprite);
		_core = FxUtil.GlowSprite(Healthy, 0.55f);
		AddChild(_core);
		_light = FxUtil.Light(Healthy, 1.1f, 1.3f);
		AddChild(_light);
		_ring = new Node2D { Material = FxUtil.Unshaded };
		_ring.Draw += DrawRing;
		AddChild(_ring);
	}

	public int Hp
	{
		get => _hp;
		set
		{
			if (value < _hp)
				_hit = 1;
			_hp = value;
			if (_hp <= 0)
				_sprite.Modulate = new Color(0.35f, 0.3f, 0.3f);
		}
	}

	public override void _Process(double delta)
	{
		_t += (float)delta;
		_hit = Mathf.Max(_hit - (float)delta * 2.5f, 0);
		var health = (float)_hp / Match.CpuMaxHp;
		var color = Critical.Lerp(Healthy, health);
		var flicker = health <= 0.3f && _hp > 0 && (_t % 0.9f) < 0.12f ? 0.3f : 1;
		var beat = 0.8f + 0.2f * Mathf.Sin(_t * (3 + (1 - health) * 6));
		if (_hp <= 0)
		{
			color = new Color(0.2f, 0.05f, 0.05f);
			beat = 0.2f;
		}
		var hitColor = color.Lerp(Critical, _hit);
		_core.Modulate = new Color(hitColor, beat * flicker);
		_halo.Modulate = new Color(hitColor, 0.22f * beat * flicker + _hit * 0.5f);
		_light.Color = hitColor;
		_light.Energy = (0.9f + _hit * 1.5f) * beat * flicker;
		_sprite.Position = new Vector2((float)GD.RandRange(-2, 2), (float)GD.RandRange(-2, 2)) * _hit;
		_ring.QueueRedraw();
	}

	void DrawRing()
	{
		var health = (float)_hp / Match.CpuMaxHp;
		var color = Critical.Lerp(Healthy, health);
		const float radius = 62;
		const float gap = 0.06f;
		var seg = Mathf.Tau / Match.CpuMaxHp;
		for (var i = 0; i < Match.CpuMaxHp; i++)
		{
			var start = -Mathf.Pi / 2 + i * seg + gap;
			var c = i < _hp ? new Color(color, 0.95f) : new Color(0.3f, 0.35f, 0.4f, 0.35f);
			_ring.DrawArc(Vector2.Zero, radius, start, start + seg - gap * 2, 8, c, 5, true);
		}
		var text = _hp.ToString();
		var size = Bold.GetStringSize(text, HorizontalAlignment.Left, -1, 22);
		var pos = new Vector2(-size.X / 2, 8);
		_ring.DrawStringOutline(Bold, pos, text, HorizontalAlignment.Left, -1, 22, 5, new Color(0, 0, 0, 0.85f));
		_ring.DrawString(Bold, pos, text, HorizontalAlignment.Left, -1, 22, color.Lerp(Colors.White, 0.4f));
		var lsize = Bold.GetStringSize(Label, HorizontalAlignment.Left, -1, 10);
		_ring.DrawString(Bold, new Vector2(-lsize.X / 2, -16), Label, HorizontalAlignment.Left, -1, 10, new Color(0.15f, 0.2f, 0.25f, 0.9f));
	}
}
