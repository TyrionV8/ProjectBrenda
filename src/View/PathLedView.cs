using System.Collections.Generic;
using Godot;

namespace HardwareDefence.View;

/// <summary>
/// One of the LEDs along the route: a tiny SMD LED whose light really lights the board.
/// Bugs chew them as they pass; damaged ones flicker and dead ones go dark, and with them
/// their patch of the board.
/// </summary>
public partial class PathLedView : Node2D
{
	const int Px = PixelWorld.Scale;
	const float Energy = 0.5f;
	static readonly Dictionary<string, Texture2D> Sprites = new();

	readonly Color _color;
	Sprite2D _sprite;
	Node2D _lens;
	Sprite2D _glow;
	PointLight2D _light;
	float _ratio = 1;
	bool _dead;
	float _t = GD.Randf() * 10;

	public PathLedView(Vector2 pos, Color color)
	{
		Position = pos;
		_color = color;
	}

	public bool Dead => _dead;
	public Color Color => _color;

	/// <summary>The package (6x6 art pixels with its shadow); the lit lens is drawn on top.</summary>
	static Texture2D Package(bool dead)
	{
		var key = dead ? "dead" : "ok";
		if (Sprites.TryGetValue(key, out var tex))
			return tex;
		var c = new PixelCanvas(6, 6);
		c.Rect(1, 1, 4, 4, new Color("d8d2c4"));
		c.Set(4, 4, new Color("8e887c"));
		c.Rect(2, 2, 2, 2, dead ? new Color("2a2522") : new Color("4a4440"));
		if (dead)
			c.Set(2, 2, new Color("4a3f38"));
		c.DropShadow(1, 1, new Color(0, 0, 0, 0.45f));
		tex = c.ToTexture();
		Sprites[key] = tex;
		return tex;
	}

	public override void _Ready()
	{
		_sprite = new Sprite2D { Texture = Package(false), Scale = Vector2.One * Px };
		AddChild(_sprite);
		// The 2x2 lens glows by itself, whatever the light around it.
		_lens = new Node2D { Material = FxUtil.Unshaded };
		_lens.Draw += () =>
		{
			_lens.DrawRect(new Rect2(-Px, -Px, Px * 2, Px * 2), _color);
			_lens.DrawRect(new Rect2(-Px, -Px, Px, Px), _color.Lightened(0.7f));
		};
		AddChild(_lens);
		_glow = FxUtil.GlowSprite(_color, 0.3f);
		_glow.Modulate = new Color(_color, 0.7f);
		AddChild(_glow);
		// A wide, gentle pool of light (about 5 cells of radius). Neighbours overlap, so each one
		// is dim on its own; losing a few leaves a dark patch on the board.
		_light = FxUtil.Light(_color, Energy, 2.3f, FxUtil.WideLightTexture);
		AddChild(_light);
	}

	public void SetHp(float hp, float maxHp)
	{
		var dead = hp < 0;
		_ratio = maxHp <= 0 ? 1 : Mathf.Clamp(hp / maxHp, 0, 1);
		if (dead == _dead)
			return;
		_dead = dead;
		_sprite.Texture = Package(dead);
		_lens.Visible = _glow.Visible = _light.Visible = !dead;
		SetProcess(!dead);
	}

	public override void _Process(double delta)
	{
		_t += (float)delta;
		// Steady while healthy; a chewed LED stutters.
		var on = 1f;
		if (_ratio < 0.999f)
			on = Mathf.PosMod(_t * (6 + 10 * (1 - _ratio)), 1) < 0.3f + 0.6f * _ratio ? 1 : 0.25f;
		_light.Energy = Energy * on;
		_lens.Modulate = new Color(1, 1, 1, 0.4f + 0.6f * on);
		_glow.Modulate = new Color(_color, 0.7f * on);
	}
}
