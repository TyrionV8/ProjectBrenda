using System.Collections.Generic;
using HardwareDefence.Sim;
using Godot;

namespace HardwareDefence.View;

/// <summary>The processor the player defends. Shows its integrity as a ring of pixel segments.</summary>
public partial class CpuView : Node2D
{
	const int Px = PixelWorld.Scale;
	/// <summary>Chip size in art pixels (fits the 3x3 cell CPU footprint).</summary>
	const int ChipPx = 36;
	const int Pad = 2;
	public static readonly Color Healthy = new(0.25f, 0.95f, 1f);
	public static readonly Color Critical = new(1f, 0.25f, 0.3f);

	string _label = "CPU";
	int _hp = Match.CpuMaxHp;
	Sprite2D _sprite;
	Sprite2D _core;
	Sprite2D _halo;
	PointLight2D _light;
	Node2D _ring;
	float _t;
	float _hit;
	/// <summary>Ring pixels (art pixels from the centre) and the HP segment each belongs to.</summary>
	static List<(Vector2I Pixel, int Segment)> _ringPixels;

	public string Label
	{
		get => _label;
		set
		{
			_label = value;
			if (_sprite != null)
				_sprite.Texture = Paint(_label).ToTexture();
		}
	}

	public override void _Ready()
	{
		_halo = FxUtil.GlowSprite(Healthy, 2.2f);
		AddChild(_halo);
		_sprite = new Sprite2D { Texture = Paint(_label).ToTexture(), Scale = Vector2.One * Px };
		AddChild(_sprite);
		_core = FxUtil.GlowSprite(Healthy, 0.4f);
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

	/// <summary>The chip: gold pins, dark package, a silver heat spreader with the label and a die window.</summary>
	static PixelCanvas Paint(string label)
	{
		var c = new PixelCanvas(ChipPx + Pad * 2, ChipPx + Pad * 2);
		const int o = Pad;
		var gold = new Color("d9a441");
		for (var p = 6; p <= ChipPx - 8; p += 4)
		{
			c.Rect(o + p, o, 2, 3, gold);
			c.Rect(o + p, o + ChipPx - 3, 2, 3, gold);
			c.Rect(o, o + p, 3, 2, gold);
			c.Rect(o + ChipPx - 3, o + p, 3, 2, gold);
			c.Set(o + p, o, new Color("ffe7a3"));
			c.Set(o, o + p, new Color("ffe7a3"));
		}
		var package = new Color("1a2026");
		c.Rect(o + 3, o + 3, ChipPx - 6, ChipPx - 6, package);
		c.Frame(o + 3, o + 3, ChipPx - 6, ChipPx - 6, new Color("2f3a44"));
		c.HLine(o + 4, o + ChipPx - 5, o + 4, new Color("3c4955"));
		c.Rect(o + 5, o + 5, 3, 1, gold);
		c.Rect(o + 5, o + 6, 1, 2, gold);
		// Heat spreader.
		const int s0 = 8;
		const int s = ChipPx - s0 * 2;
		c.Rect(o + s0, o + s0, s, s, new Color("b9c2cc"));
		c.HLine(o + s0, o + s0 + s - 1, o + s0, new Color("e3e8ee"));
		c.VLine(o + s0, o + s0, o + s0 + s - 1, new Color("d3dae2"));
		c.HLine(o + s0, o + s0 + s - 1, o + s0 + s - 1, new Color("6d7682"));
		c.VLine(o + s0 + s - 1, o + s0 + 1, o + s0 + s - 1, new Color("8a939e"));
		var text = label ?? "CPU";
		while (text.Length > 0 && MicroFont.Width(text) > s - 4)
			text = text[..^1];
		c.TextCentered(o + ChipPx / 2f, o + s0 + 4.5f, text, new Color("2a3038"));
		// Die window (the glowing core sits on top of it).
		c.Rect(o + 12, o + 16, 12, 10, new Color("0b1a22"));
		c.Frame(o + 12, o + 16, 12, 10, new Color("2a3640"));
		c.DropShadow(2, 2, new Color(0, 0, 0, 0.45f));
		return c;
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
		_core.Position = new Vector2(0, 3) * Px;
		_halo.Modulate = new Color(hitColor, 0.22f * beat * flicker + _hit * 0.5f);
		_light.Color = hitColor;
		_light.Energy = (0.9f + _hit * 1.5f) * beat * flicker;
		_sprite.Position = new Vector2(GD.RandRange(-1, 1), GD.RandRange(-1, 1)) * Px * Mathf.Round(_hit);
		_ring.QueueRedraw();
	}

	static List<(Vector2I, int)> RingPixels()
	{
		if (_ringPixels != null)
			return _ringPixels;
		_ringPixels = new List<(Vector2I, int)>();
		const float outer = 23;
		const float inner = 21;
		var seg = Mathf.Tau / Match.CpuMaxHp;
		for (var y = -24; y < 24; y++)
			for (var x = -24; x < 24; x++)
			{
				var p = new Vector2(x + 0.5f, y + 0.5f);
				var d = p.Length();
				if (d > outer || d <= inner)
					continue;
				// Clockwise from the top, with a one-pixel-ish gap between segments.
				var a = Mathf.PosMod(p.Angle() + Mathf.Pi / 2, Mathf.Tau);
				var within = Mathf.PosMod(a, seg);
				if (within * d < 1.2f)
					continue;
				_ringPixels.Add((new Vector2I(x, y), (int)(a / seg)));
			}
		return _ringPixels;
	}

	void DrawRing()
	{
		var health = (float)_hp / Match.CpuMaxHp;
		var color = Critical.Lerp(Healthy, health);
		var off = new Color(0.3f, 0.35f, 0.4f, 0.35f);
		foreach (var (pixel, segment) in RingPixels())
			_ring.DrawRect(new Rect2(new Vector2(pixel.X, pixel.Y) * Px, Vector2.One * Px), segment < _hp ? new Color(color, 0.95f) : off);
		var text = _hp.ToString();
		MicroFont.DrawOutlined(_ring, new Vector2(0, 3 * Px), text, color.Lerp(Colors.White, 0.55f), new Color(0, 0, 0, 0.85f));
	}
}
