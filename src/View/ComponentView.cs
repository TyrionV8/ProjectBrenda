using HardwareDefence.Data;
using Godot;

namespace HardwareDefence.View;

/// <summary>
/// One board component: its pixel art, repainted when its health changes; smokes when badly
/// damaged; LEDs blink and light up the board around them.
/// </summary>
public partial class ComponentView : Node2D
{
	const int Px = PixelWorld.Scale;

	public readonly ComponentData Data;
	public readonly int Index;
	readonly BoardTheme _theme;
	float _ratio = 1;
	bool _destroyed;
	Sprite2D _sprite;
	CpuParticles2D _smoke;
	Sprite2D _ledGlow;
	PointLight2D _ledLight;
	float _phase = GD.Randf() * Mathf.Tau;
	readonly float _blinkSpeed = (float)GD.RandRange(0.6, 2.2);

	public Vector2 Size => new Vector2(Data.Size[0], Data.Size[1]) * Grid.Cell;
	public Rect2 Rect => new(Position, Size);

	public ComponentView(ComponentData data, BoardTheme theme, int index)
	{
		Data = data;
		_theme = theme;
		Index = index;
		Position = new Vector2(data.Cell[0], data.Cell[1]) * Grid.Cell;
	}

	public override void _Ready()
	{
		_sprite = new Sprite2D
		{
			Texture = ComponentArt.Texture(Data, _theme, Index + 1), Centered = false, Scale = Vector2.One * Px,
			Position = -Vector2.One * ComponentArt.Pad * Px,
		};
		AddChild(_sprite);
		if (Data.Type != "led")
		{
			SetProcess(false);
			return;
		}
		var color = Data.Color != null ? new Color(Data.Color) : new Color("ff3344");
		_ledGlow = FxUtil.GlowSprite(color, 0.9f);
		_ledGlow.Position = Size / 2;
		AddChild(_ledGlow);
		_ledLight = FxUtil.Light(color, 0.9f, 0.45f);
		_ledLight.Position = Size / 2;
		AddChild(_ledLight);
	}

	/// <summary>Remaining HP from the snapshot (-1 = destroyed).</summary>
	public void SetHp(float hp, float maxHp)
	{
		var destroyed = hp < 0;
		var ratio = maxHp <= 0 ? 1 : Mathf.Clamp(hp / maxHp, 0, 1);
		// Repaint in 5% steps so chewing doesn't repaint every frame.
		if (destroyed == _destroyed && Mathf.Abs(ratio - _ratio) < 0.05f)
			return;
		_destroyed = destroyed;
		_ratio = ratio;
		Repaint();
		var smoking = _destroyed || _ratio < 0.5f;
		if (smoking && _smoke == null)
			AddSmoke();
		if (_smoke != null)
			_smoke.Amount = _destroyed ? 6 : 4;
		if (_destroyed && _ledLight != null)
		{
			_ledLight.Energy = 0;
			_ledGlow.Visible = false;
			SetProcess(false);
		}
	}

	public void Restore()
	{
		_destroyed = false;
		_ratio = 1;
		_smoke?.QueueFree();
		_smoke = null;
		Repaint();
	}

	void Repaint()
	{
		if (_sprite == null)
			return;
		if (!_destroyed && _ratio >= 0.999f)
		{
			_sprite.Texture = ComponentArt.Texture(Data, _theme, Index + 1);
			return;
		}
		var canvas = ComponentArt.Paint(Data, _theme, Index + 1);
		ComponentArt.Damage(canvas, Data, _ratio, _destroyed, Index + 1);
		_sprite.Texture = canvas.ToTexture();
	}

	void AddSmoke()
	{
		_smoke = new CpuParticles2D
		{
			Position = Size / 2, Amount = 4, Lifetime = 2, Texture = FxUtil.PixelTexture,
			EmissionShape = CpuParticles2D.EmissionShapeEnum.Rectangle, EmissionRectExtents = Size * 0.3f,
			Direction = Vector2.Up, Spread = 20, Gravity = Vector2.Zero, InitialVelocityMin = 8, InitialVelocityMax = 20,
			ScaleAmountMin = 0.75f, ScaleAmountMax = 1.5f, ZIndex = 5,
			ColorRamp = new Gradient { Colors = new[] { new Color(0.25f, 0.25f, 0.27f, 0.6f), new Color(0.1f, 0.1f, 0.1f, 0) } },
		};
		AddChild(_smoke);
	}

	public override void _Process(double delta)
	{
		_phase += (float)delta * _blinkSpeed;
		var on = (_phase % 7f) < 6.6f ? 0.55f + 0.45f * Mathf.Sin(_phase) : 0.15f;
		on *= Mathf.Lerp(0.3f, 1, _ratio);
		_ledGlow.Modulate = new Color(_ledGlow.Modulate, on);
		_ledLight.Energy = on * 0.9f;
	}
}
