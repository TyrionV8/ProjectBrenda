using HardwareDefence.Data;
using Godot;

namespace HardwareDefence.Autoload;

/// <summary>Full-screen post-processing (vignette, scanlines, aberration) and the red damage pulse.</summary>
public partial class PostFx : CanvasLayer
{
	public static PostFx Instance { get; private set; }

	ColorRect _rect;
	ShaderMaterial _material;
	float _damage;
	float _bossGlitch;
	float _bossGlitchFresh;
	internal float BossGlitchStrength => _bossGlitch;

	public override void _EnterTree() => Instance = this;

	public override void _Ready()
	{
		Layer = 90;
		ProcessMode = ProcessModeEnum.Always;
		_material = new ShaderMaterial { Shader = GD.Load<Shader>("res://assets/shaders/post_fx_boss.gdshader") };
		_rect = new ColorRect { MouseFilter = Control.MouseFilterEnum.Ignore, Material = _material };
		_rect.SetAnchorsPreset(Control.LayoutPreset.FullRect);
		AddChild(_rect);
		var logicalSize = new Vector2((float)ProjectSettings.GetSetting("display/window/size/viewport_width"),
			(float)ProjectSettings.GetSetting("display/window/size/viewport_height"));
		// The game uses a fixed canvas layout. Keep displaced samples safely inside
		// the board, clear of the side panels and the versus boss bar at its top.
		var start = (Grid.Origin + new Vector2(12, 105)) / logicalSize;
		var end = (Grid.Origin + Grid.Size - new Vector2(12, 84)) / logicalSize;
		_material.SetShaderParameter("board_rect", new Vector4(start.X, start.Y, end.X, end.Y));
		_material.SetShaderParameter("logical_size", logicalSize);
		_material.SetShaderParameter("warning_gap", new Vector2(240 / logicalSize.Y, 435 / logicalSize.Y));
		Settings.Changed += ApplySettings;
		ApplySettings();
	}

	public override void _ExitTree()
	{
		Settings.Changed -= ApplySettings;
		if (Instance == this) Instance = null;
	}

	/// <summary>A brief board-only intrusion. Refreshed by a live introduction; no queued tween
	/// can outlive a boss or game screen. Disabled by the screen-effects preference.</summary>
	public static void SetBossGlitch(float amount)
	{
		if (Instance?._material == null) return;
		Instance._bossGlitch = (Settings.Instance?.ScreenFx ?? true) ? Mathf.Clamp(amount, 0, 0.3f) : 0;
		Instance._bossGlitchFresh = Instance._bossGlitch > 0 ? 0.1f : 0;
		Instance._material.SetShaderParameter("intrusion", Instance._bossGlitch);
		Instance.ApplySettings();
	}

	public static void PulseDamage(float amount = 0.8f)
	{
		if (Instance == null)
			return;
		Instance._damage = Mathf.Max(Instance._damage, amount);
		Instance._rect.Visible = true;
	}

	public override void _Process(double delta)
	{
		if (_bossGlitch > 0)
		{
			_bossGlitchFresh -= (float)delta;
			if (_bossGlitchFresh <= 0 || !(Settings.Instance?.ScreenFx ?? true)) SetBossGlitch(0);
		}
		if (_damage <= 0)
			return;
		_damage = Mathf.Max(_damage - (float)delta * 1.6f, 0);
		_material.SetShaderParameter("damage", _damage);
		if (_damage == 0)
			ApplySettings();
	}

	void ApplySettings()
	{
		var fx = Settings.Instance?.ScreenFx ?? true;
		if (!fx)
		{
			_bossGlitch = 0;
			_bossGlitchFresh = 0;
			_material.SetShaderParameter("intrusion", 0f);
		}
		_rect.Visible = fx || _damage > 0 || _bossGlitch > 0;
		var on = fx ? 1f : 0f;
		_material.SetShaderParameter("vignette", 0.45f * on);
		_material.SetShaderParameter("scanlines", 0.05f * on);
		_material.SetShaderParameter("aberration", 1.0f * on);
	}
}
