using Brenda.Data;
using Brenda.View;
using Godot;

namespace Brenda.UI;

/// <summary>A small animated bug for lists (spawn queue, bug palette).</summary>
public partial class BugIcon : Control
{
	readonly BugBody _body;

	public BugIcon(EnemyDef def, float size = 30)
	{
		CustomMinimumSize = new Vector2(size, size);
		MouseFilter = MouseFilterEnum.Ignore;
		_body = new BugBody(def)
		{
			Scale = Vector2.One * Mathf.Clamp(size * 0.36f / def.Radius, 0.2f, 1.4f), Rotation = -Mathf.Pi / 2, Gait = GD.Randf() * Mathf.Tau,
		};
		AddChild(_body);
		Size = CustomMinimumSize;
		_body.Position = Size / 2;
		Resized += () => _body.Position = Size / 2;
	}

	public override void _Process(double delta) => _body.Animate((float)delta * 6, (float)delta);
}
