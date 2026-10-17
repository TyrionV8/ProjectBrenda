using HardwareDefence.Data;
using HardwareDefence.View;
using Godot;

namespace HardwareDefence.UI;

/// <summary>Full sprite at exactly 3x; unknown entries retain only an animated silhouette.</summary>
public partial class BestiaryPortrait : Control
{
	readonly EnemyDef _enemy;
	readonly TurretDef _turret;
	readonly bool _known;
	readonly Texture2D _sheet;
	float _time;
	public BestiaryPortrait(EnemyDef enemy, TurretDef turret, bool known)
	{
		_enemy = enemy;
		_turret = turret;
		_known = known;
		_sheet = enemy != null ? BugBody.Sheet(enemy) : GD.Load<Texture2D>(turret.Icon);
		CustomMinimumSize = new Vector2(0, 240);
		TextureFilter = TextureFilterEnum.Nearest;
		MouseFilter = MouseFilterEnum.Ignore;
	}
	public override void _Process(double delta) { _time += (float)delta; QueueRedraw(); }
	public override void _Draw()
	{
		DrawRect(new Rect2(Vector2.Zero, Size), new Color(_known ? "081725" : "314459"));
		var frame = _enemy != null ? BugBody.FrameSize(_enemy) : (Vector2I)_sheet.GetSize();
		var artSize = (Vector2)frame * PixelWorld.Scale;
		var target = new Rect2(((Size - artSize) / 2).Floor(), artSize);
		var source = new Rect2(_enemy != null ? new Vector2((int)(_time * 8) % BugBody.WalkFrames * frame.X, 0) : Vector2.Zero, frame);
		var tint = _known ? Colors.White : new Color(0, 0, 0, 1);
		DrawTextureRectRegion(_sheet, target, source, tint);
		if (_known && _turret != null)
		{
			// Existing status pixels blink, without scaling the turret or adding a halo.
			var dot = target.Position + new Vector2(12, 12);
			DrawRect(new Rect2(dot, new Vector2(3, 3)), new Color(_turret.Color, 0.6f + 0.4f * Mathf.Sin(_time * 3)));
		}
	}
}
