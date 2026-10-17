using Brenda.Data;
using Brenda.View;
using Godot;

namespace Brenda.UI;

/// <summary>A turret drawn large with its head slowly scanning; used on the upgrade screen.</summary>
public partial class TurretPreview : Control
{
	static readonly Texture2D Socket = GD.Load<Texture2D>("res://assets/art/turrets/socket.svg");
	readonly TurretDef _def;
	readonly Texture2D _icon;
	float _t = GD.Randf() * Mathf.Tau;

	public TurretPreview(TurretDef def)
	{
		_def = def;
		_icon = GD.Load<Texture2D>(def.Icon);
		CustomMinimumSize = new Vector2(110, 110);
		MouseFilter = MouseFilterEnum.Ignore;
	}

	public override void _Process(double delta)
	{
		_t += (float)delta;
		QueueRedraw();
	}

	public override void _Draw()
	{
		var c = Size / 2;
		FxUtil.DrawGlow(this, c, 140, new Color(_def.Color, 0.25f + 0.08f * Mathf.Sin(_t * 2)));
		DrawTextureRect(Socket, new Rect2(c - new Vector2(44, 44), new Vector2(88, 88)), false);
		DrawSetTransform(c, _def.NeedsDirection ? 0 : Mathf.Sin(_t * 0.8f) * 0.6f, Vector2.One);
		DrawTextureRect(_icon, new Rect2(-new Vector2(44, 44), new Vector2(88, 88)), false);
		DrawSetTransformMatrix(Transform2D.Identity);
	}
}
