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
		var frame = BugBody.FrameSize(def);
		// Same pixel size as the board when it fits, otherwise the largest whole scale that does.
		var scale = Mathf.Clamp(Mathf.FloorToInt(size / Mathf.Max(frame.X, frame.Y)), 1, PixelWorld.Scale);
		_body = new BugBody(def) { Scale = Vector2.One * scale, Facing = -Mathf.Pi / 2, Gait = GD.Randf() * 40 };
		AddChild(_body);
		Size = CustomMinimumSize;
		_body.Position = (Size / 2).Floor();
		Resized += () => _body.Position = (Size / 2).Floor();
	}

	public override void _Process(double delta) => _body.Animate((float)delta * 18, (float)delta);
}
