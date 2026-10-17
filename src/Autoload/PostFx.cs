using Godot;

namespace HardwareDefence.Autoload;

/// <summary>Full-screen post-processing (vignette, scanlines, aberration) and the red damage pulse.</summary>
public partial class PostFx : CanvasLayer
{
	public static PostFx Instance { get; private set; }

	ColorRect _rect;
	ShaderMaterial _material;
	float _damage;

	public override void _EnterTree() => Instance = this;

	public override void _Ready()
	{
		Layer = 90;
		ProcessMode = ProcessModeEnum.Always;
		_material = new ShaderMaterial { Shader = GD.Load<Shader>("res://assets/shaders/post_fx.gdshader") };
		_rect = new ColorRect { MouseFilter = Control.MouseFilterEnum.Ignore, Material = _material };
		_rect.SetAnchorsPreset(Control.LayoutPreset.FullRect);
		AddChild(_rect);
		Settings.Changed += ApplySettings;
		ApplySettings();
	}

	public override void _ExitTree() => Settings.Changed -= ApplySettings;

	public static void PulseDamage(float amount = 0.8f)
	{
		if (Instance == null)
			return;
		Instance._damage = Mathf.Max(Instance._damage, amount);
		Instance._rect.Visible = true;
	}

	public override void _Process(double delta)
	{
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
		_rect.Visible = fx || _damage > 0;
		var on = fx ? 1f : 0f;
		_material.SetShaderParameter("vignette", 0.45f * on);
		_material.SetShaderParameter("scanlines", 0.05f * on);
		_material.SetShaderParameter("aberration", 1.0f * on);
	}
}
