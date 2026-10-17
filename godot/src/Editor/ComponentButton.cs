using Brenda.Data;
using Brenda.UI;
using Brenda.View;
using Godot;

namespace Brenda.Editor;

/// <summary>Palette entry that shows a miniature of the component.</summary>
public partial class ComponentButton : Button
{
	public readonly string Type;
	readonly ComponentData _sample;
	bool _active;

	public ComponentButton(string type)
	{
		Type = type;
		var kind = ComponentTypes.Get(type);
		_sample = new ComponentData { Type = type, Size = new[] { kind.DefaultSize.X, kind.DefaultSize.Y }, Label = type == "label" ? "TEXT" : null };
		Flat = true;
		FocusMode = FocusModeEnum.None;
		CustomMinimumSize = new Vector2(88, 70);
		TooltipText = kind.Name;
	}

	public bool Active
	{
		set
		{
			_active = value;
			QueueRedraw();
		}
	}

	public override void _Draw()
	{
		var r = new Rect2(Vector2.Zero, Size);
		DrawStyleBox(UiTheme.Box(_active ? new Color(0.07f, 0.22f, 0.33f, 1) : new Color(0.04f, 0.1f, 0.16f, 0.9f), _active ? UiTheme.Cyan : new Color(UiTheme.Cyan, IsHovered() ? 0.6f : 0.2f), 1, 6, 0, 0), r);
		var size = new Vector2(_sample.Size[0], _sample.Size[1]) * Grid.Cell;
		var area = new Vector2(Size.X - 10, Size.Y - 22);
		var k = Mathf.Min(area.X / size.X, area.Y / size.Y);
		var offset = new Vector2(5, 4) + (area - size * k) / 2;
		DrawSetTransform(offset, 0, Vector2.One * k);
		ComponentArt.Draw(this, _sample, size, BoardTheme.Get("green"), 3);
		DrawSetTransformMatrix(Transform2D.Identity);
		var name = ComponentTypes.Get(Type).Name.ToUpperInvariant();
		DrawString(UiTheme.PixelFont, new Vector2(4, Size.Y - 5), name, HorizontalAlignment.Center, Size.X - 8, 8, UiTheme.TextColor);
	}
}
