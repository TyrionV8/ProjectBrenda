using HardwareDefence.Data;
using HardwareDefence.View;
using Godot;

namespace HardwareDefence.UI;

/// <summary>A turret on the upgrade screen (same pixel size as on the board) with a pulsing glow.</summary>
public partial class TurretPreview : Control
{
	readonly TurretDef _def;
	readonly Texture2D _icon;
	float _t = GD.Randf() * Mathf.Tau;

	public TurretPreview(TurretDef def)
	{
		_def = def;
		_icon = GD.Load<Texture2D>(def.Icon);
		CustomMinimumSize = new Vector2(110, 110);
		MouseFilter = MouseFilterEnum.Ignore;
		TextureFilter = TextureFilterEnum.Nearest;
	}

	public override void _Process(double delta)
	{
		_t += (float)delta;
		QueueRedraw();
	}

	public override void _Draw()
	{
		var c = (Size / 2).Floor();
		FxUtil.DrawGlow(this, c, 140, new Color(_def.Color, 0.25f + 0.08f * Mathf.Sin(_t * 2)));
		var size = _icon.GetSize() * PixelWorld.Scale;
		DrawTextureRect(_icon, new Rect2(c - size / 2, size), false);
	}
}
