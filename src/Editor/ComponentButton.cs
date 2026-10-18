using HardwareDefence.Data;
using HardwareDefence.UI;
using HardwareDefence.View;
using Godot;

namespace HardwareDefence.Editor;

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
		CustomMinimumSize = new Vector2(80, 70);
		Loc.Tooltip(this, kind.Name);
		TextureFilter = TextureFilterEnum.Nearest;
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
		var tex = ComponentArt.Texture(_sample, BoardTheme.Get("green"), 3);
		var size = tex.GetSize();
		var area = new Vector2(Size.X - 10, Size.Y - 22);
		var k = Mathf.Min(area.X / size.X, area.Y / size.Y);
		if (k >= 1)
			k = Mathf.Floor(k);
		DrawTextureRect(tex, new Rect2((new Vector2(5, 4) + (area - size * k) / 2).Floor(), size * k), false);
		// Compact captions stay readable at the native 1 px font grid; tooltip keeps the full name.
		var name = Type switch
		{
			"capacitor" => "CAPACITOR",
			"fingers" => "EDGE CONN.",
			"label" => "TEXT",
			"hole" => "MOUNT HOLE",
			_ => ComponentTypes.Get(Type).Name.ToUpperInvariant(),
		};
		var captions = Ui.WrapText(UiTheme.PixelFont, Loc.S(name), UiTheme.PixelTiny, Size.X - 8);
		for (var i = 0; i < captions.Length; i++)
			DrawString(UiTheme.PixelFont, new Vector2(4, Size.Y - 5 - (captions.Length - 1 - i) * 9), captions[i], HorizontalAlignment.Center, Size.X - 8, UiTheme.PixelTiny, UiTheme.TextColor);
	}
}
