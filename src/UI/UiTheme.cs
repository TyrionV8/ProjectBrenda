using Godot;

namespace Brenda.UI;

/// <summary>The game's UI theme. Built in code and saved to assets/ui/theme.tres (`-- --build-theme`).</summary>
public static class UiTheme
{
	public static readonly Color Cyan = new("3ef0ff");
	public static readonly Color Gold = new("ffd34d");
	public static readonly Color Red = new("ff4d5e");
	public static readonly Color Green = new("7dff6a");
	public static readonly Color Magenta = new("ff5fd2");
	public static readonly Color TextColor = new("d8f6ff");
	public static readonly Color Dim = new("7f9bb0");
	public static readonly Color Panel = new(0.03f, 0.07f, 0.12f, 0.92f);

	public static readonly Font BodyFont = GD.Load<Font>("res://assets/fonts/ShareTechMono-Regular.ttf");
	public static readonly Font PixelFont = GD.Load<Font>("res://assets/fonts/Silkscreen-Regular.ttf");
	public static readonly Font PixelBold = GD.Load<Font>("res://assets/fonts/Silkscreen-Bold.ttf");

	public static Theme Build()
	{
		var t = new Theme { DefaultFont = BodyFont, DefaultFontSize = 20 };
		t.SetColor("font_color", "Label", TextColor);

		LabelVariation(t, "TitleLabel", PixelBold, 64, Cyan);
		LabelVariation(t, "HeaderLabel", PixelBold, 28, TextColor);
		LabelVariation(t, "SmallHeader", PixelFont, 14, Dim);
		LabelVariation(t, "ValueLabel", PixelBold, 34, Cyan);
		LabelVariation(t, "MoneyLabel", PixelBold, 34, Gold);
		LabelVariation(t, "DimLabel", BodyFont, 18, Dim);

		t.SetFont("font", "Button", PixelFont);
		t.SetFontSize("font_size", "Button", 20);
		t.SetColor("font_color", "Button", TextColor);
		t.SetColor("font_hover_color", "Button", Colors.White);
		t.SetColor("font_pressed_color", "Button", new Color("061018"));
		t.SetColor("font_focus_color", "Button", Colors.White);
		t.SetColor("font_disabled_color", "Button", new Color(0.5f, 0.58f, 0.65f, 0.6f));
		t.SetStylebox("normal", "Button", Box(new Color(0.04f, 0.12f, 0.2f, 0.9f), new Color(Cyan, 0.45f), 2, 6, 18, 10));
		t.SetStylebox("hover", "Button", Box(new Color(0.07f, 0.22f, 0.33f, 0.95f), Cyan, 2, 6, 18, 10, new Color(Cyan, 0.35f), 10));
		t.SetStylebox("pressed", "Button", Box(Cyan, Cyan, 2, 6, 18, 10, new Color(Cyan, 0.5f), 14));
		t.SetStylebox("disabled", "Button", Box(new Color(0.04f, 0.08f, 0.12f, 0.7f), new Color(0.4f, 0.5f, 0.6f, 0.25f), 2, 6, 18, 10));
		t.SetStylebox("focus", "Button", Box(new Color(0, 0, 0, 0), new Color(1, 1, 1, 0.7f), 2, 6, 18, 10));

		Variation(t, "BigButton", "Button");
		t.SetFontSize("font_size", "BigButton", 26);
		t.SetStylebox("normal", "BigButton", Box(new Color(0.04f, 0.12f, 0.2f, 0.9f), new Color(Cyan, 0.5f), 2, 8, 28, 14));
		t.SetStylebox("hover", "BigButton", Box(new Color(0.07f, 0.22f, 0.33f, 0.95f), Cyan, 2, 8, 28, 14, new Color(Cyan, 0.4f), 14));
		t.SetStylebox("pressed", "BigButton", Box(Cyan, Cyan, 2, 8, 28, 14, new Color(Cyan, 0.5f), 18));
		t.SetStylebox("disabled", "BigButton", Box(new Color(0.04f, 0.08f, 0.12f, 0.7f), new Color(0.4f, 0.5f, 0.6f, 0.25f), 2, 8, 28, 14));

		Variation(t, "DangerButton", "Button");
		t.SetStylebox("normal", "DangerButton", Box(new Color(0.2f, 0.04f, 0.07f, 0.9f), new Color(Red, 0.5f), 2, 6, 18, 10));
		t.SetStylebox("hover", "DangerButton", Box(new Color(0.33f, 0.07f, 0.1f, 0.95f), Red, 2, 6, 18, 10, new Color(Red, 0.35f), 10));
		t.SetStylebox("pressed", "DangerButton", Box(Red, Red, 2, 6, 18, 10));

		Variation(t, "ToolButton", "Button");
		t.SetFontSize("font_size", "ToolButton", 12);
		t.SetStylebox("normal", "ToolButton", Box(new Color(0.04f, 0.1f, 0.16f, 0.9f), new Color(Cyan, 0.25f), 1, 4, 5, 6));
		t.SetStylebox("hover", "ToolButton", Box(new Color(0.07f, 0.2f, 0.3f, 0.95f), Cyan, 1, 4, 5, 6));
		t.SetStylebox("pressed", "ToolButton", Box(new Color(Cyan, 0.85f), Cyan, 1, 4, 5, 6));
		t.SetStylebox("disabled", "ToolButton", Box(new Color(0.04f, 0.08f, 0.12f, 0.6f), new Color(0.4f, 0.5f, 0.6f, 0.2f), 1, 4, 5, 6));
		t.SetColor("font_pressed_color", "ToolButton", new Color("061018"));
		t.SetColor("font_hover_pressed_color", "ToolButton", new Color("061018"));

		var glass = Box(Panel, new Color(Cyan, 0.35f), 2, 10, 20, 18, new Color(0, 0, 0, 0.55f), 16);
		t.SetStylebox("panel", "PanelContainer", glass);
		t.SetStylebox("panel", "Panel", glass);
		Variation(t, "SidePanel", "PanelContainer");
		var side = Box(new Color(0.02f, 0.05f, 0.09f, 0.96f), new Color(Cyan, 0.25f), 0, 0, 14, 16);
		side.BorderWidthLeft = 2;
		side.BorderWidthRight = 2;
		t.SetStylebox("panel", "SidePanel", side);
		Variation(t, "CardPanel", "PanelContainer");
		t.SetStylebox("panel", "CardPanel", Box(new Color(0.04f, 0.1f, 0.17f, 0.92f), new Color(Cyan, 0.22f), 2, 10, 14, 14));

		t.SetStylebox("slider", "HSlider", Box(new Color(0.02f, 0.06f, 0.1f, 1), new Color(Cyan, 0.35f), 1, 4, 0, 4));
		t.SetStylebox("grabber_area", "HSlider", Box(new Color(Cyan, 0.75f), new Color(0, 0, 0, 0), 0, 4, 0, 4));
		t.SetStylebox("grabber_area_highlight", "HSlider", Box(Cyan, new Color(0, 0, 0, 0), 0, 4, 0, 4));
		t.SetIcon("grabber", "HSlider", Dot(22, Cyan));
		t.SetIcon("grabber_highlight", "HSlider", Dot(22, Colors.White));

		t.SetFont("font", "CheckBox", PixelFont);
		t.SetFontSize("font_size", "CheckBox", 18);
		t.SetColor("font_color", "CheckBox", TextColor);
		t.SetColor("font_hover_color", "CheckBox", Colors.White);
		t.SetColor("font_pressed_color", "CheckBox", Cyan);
		t.SetColor("font_hover_pressed_color", "CheckBox", Cyan);
		foreach (var state in new[] { "normal", "pressed", "hover", "hover_pressed", "focus", "disabled" })
			t.SetStylebox(state, "CheckBox", new StyleBoxEmpty { ContentMarginLeft = 2, ContentMarginRight = 2, ContentMarginTop = 2, ContentMarginBottom = 2 });
		t.SetIcon("checked", "CheckBox", Check(true));
		t.SetIcon("unchecked", "CheckBox", Check(false));

		var field = Box(new Color(0.02f, 0.05f, 0.09f, 1), new Color(Cyan, 0.35f), 1, 4, 10, 8);
		t.SetStylebox("normal", "LineEdit", field);
		t.SetStylebox("focus", "LineEdit", Box(new Color(0.02f, 0.05f, 0.09f, 1), Cyan, 1, 4, 10, 8));
		t.SetStylebox("normal", "SpinBox", field);
		t.SetStylebox("normal", "OptionButton", Box(new Color(0.04f, 0.12f, 0.2f, 0.9f), new Color(Cyan, 0.45f), 1, 4, 10, 6));
		t.SetStylebox("hover", "OptionButton", Box(new Color(0.07f, 0.22f, 0.33f, 0.95f), Cyan, 1, 4, 10, 6));
		t.SetStylebox("pressed", "OptionButton", Box(new Color(0.07f, 0.22f, 0.33f, 0.95f), Cyan, 1, 4, 10, 6));
		t.SetFont("font", "OptionButton", BodyFont);
		t.SetFontSize("font_size", "OptionButton", 18);
		t.SetStylebox("panel", "PopupMenu", Box(Panel, new Color(Cyan, 0.35f), 1, 4, 6, 6));
		t.SetStylebox("panel", "TooltipPanel", Box(new Color(0.02f, 0.05f, 0.09f, 0.97f), new Color(Cyan, 0.5f), 1, 4, 10, 8));
		t.SetColor("font_color", "TooltipLabel", TextColor);
		t.SetFontSize("font_size", "TooltipLabel", 16);
		t.SetStylebox("panel", "TabContainer", Box(new Color(0.03f, 0.07f, 0.12f, 0.9f), new Color(Cyan, 0.25f), 1, 4, 10, 10));
		t.SetStylebox("tab_selected", "TabContainer", Box(new Color(0.07f, 0.22f, 0.33f, 1), Cyan, 1, 4, 14, 6));
		t.SetStylebox("tab_unselected", "TabContainer", Box(new Color(0.03f, 0.07f, 0.12f, 1), new Color(Cyan, 0.25f), 1, 4, 14, 6));
		t.SetStylebox("tab_hovered", "TabContainer", Box(new Color(0.05f, 0.14f, 0.22f, 1), new Color(Cyan, 0.6f), 1, 4, 14, 6));
		t.SetFont("font", "TabContainer", PixelFont);
		t.SetFontSize("font_size", "TabContainer", 14);
		return t;
	}

	static void Variation(Theme t, string name, string baseType)
	{
		t.AddType(name);
		t.SetTypeVariation(name, baseType);
	}

	static void LabelVariation(Theme t, string name, Font font, int size, Color color)
	{
		Variation(t, name, "Label");
		t.SetFont("font", name, font);
		t.SetFontSize("font_size", name, size);
		t.SetColor("font_color", name, color);
	}

	public static StyleBoxFlat Box(Color bg, Color border, int borderWidth, int radius, int padX, int padY, Color shadow = default, int shadowSize = 0)
	{
		var s = new StyleBoxFlat
		{
			BgColor = bg, BorderColor = border, ContentMarginLeft = padX, ContentMarginRight = padX, ContentMarginTop = padY,
			ContentMarginBottom = padY, ShadowColor = shadow, ShadowSize = shadowSize, AntiAliasing = true,
		};
		s.SetBorderWidthAll(borderWidth);
		s.SetCornerRadiusAll(radius);
		return s;
	}

	static ImageTexture Dot(int size, Color color)
	{
		var img = Image.CreateEmpty(size, size, false, Image.Format.Rgba8);
		var c = (size - 1) / 2f;
		for (var y = 0; y < size; y++)
			for (var x = 0; x < size; x++)
			{
				var d = new Vector2(x - c, y - c).Length();
				var a = Mathf.Clamp(c - d + 0.5f, 0, 1);
				var inner = Mathf.Clamp(c - 3 - d + 0.5f, 0, 1);
				img.SetPixel(x, y, new Color(color.Lerp(new Color("061018"), inner * 0.7f), a));
			}
		return ImageTexture.CreateFromImage(img);
	}

	static ImageTexture Check(bool on)
	{
		const int size = 26;
		var img = Image.CreateEmpty(size, size, false, Image.Format.Rgba8);
		for (var y = 0; y < size; y++)
			for (var x = 0; x < size; x++)
			{
				var edge = x < 2 || y < 2 || x >= size - 2 || y >= size - 2;
				var inner = x >= 6 && y >= 6 && x < size - 6 && y < size - 6;
				var c = edge ? new Color(Cyan, 0.8f) : inner && on ? Cyan : new Color(0.02f, 0.06f, 0.1f, 0.9f);
				img.SetPixel(x, y, c);
			}
		return ImageTexture.CreateFromImage(img);
	}
}
