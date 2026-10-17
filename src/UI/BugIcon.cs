using HardwareDefence.Data;
using HardwareDefence.View;
using Godot;

namespace HardwareDefence.UI;

/// <summary>A small walking bug for lists (spawn queue, bug palette), at a whole-number pixel scale.</summary>
public partial class BugIcon : Control
{
	readonly BugBody _body;

	public BugIcon(EnemyDef def, float size = 30)
	{
		CustomMinimumSize = new Vector2(size, size);
		MouseFilter = MouseFilterEnum.Ignore;
		TextureFilter = TextureFilterEnum.Nearest;
		ClipContents = true;
		// Offset by a whole stride so a row of icons doesn't march in lockstep.
		var stride = (5 + def.Radius * 0.35f) * 4;
		_body = new BugBody(def) { Scale = Vector2.One * PixelWorld.Scale, Facing = -Mathf.Pi / 2, Gait = GD.Randf() * stride };
		AddChild(_body);
		Size = CustomMinimumSize;
		LayoutPortrait();
		Resized += LayoutPortrait;
	}

	void LayoutPortrait()
	{
		// Bugs face upwards here: frame width becomes portrait height. Keep the head
		// at the top when the whole animal cannot fit; crop the rear instead of
		// shrinking its pixels. A larger bestiary panel can show the complete body.
		var height = BugBody.FrameSize(_body.Def).X * PixelWorld.Scale;
		_body.Position = new Vector2(Size.X / 2, Mathf.Max(Size.Y, height) / 2).Floor();
	}

	public override void _Process(double delta) => _body.Animate((float)delta * 18, (float)delta);
}
