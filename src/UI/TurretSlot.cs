using HardwareDefence.Autoload;
using HardwareDefence.Data;
using Godot;

namespace HardwareDefence.UI;

/// <summary>Inventory slot for one turret type: preview, name, cost and hotkey.</summary>
public partial class TurretSlot : Button
{
	// Nine native-pixel inventory entries leave space for cash, the boss bar and footer.
	public const int SlotHeight = 52;
	public const int TextLeft = 62;
	public const int NameSize = UiTheme.PixelSmall;
	public readonly TurretDef Def;
	readonly int _hotkey;
	readonly Texture2D _icon;
	bool _selected;
	bool _affordable = true;
	float _t;

	public TurretSlot(TurretDef def, int hotkey)
	{
		Def = def;
		_hotkey = hotkey;
		_icon = GD.Load<Texture2D>(def.Icon);
		Flat = true;
		FocusMode = FocusModeEnum.None;
		CustomMinimumSize = new Vector2(186, SlotHeight);
		TextureFilter = TextureFilterEnum.Nearest;
		TooltipText = $"{def.Name}\n{def.Description}";
		MouseEntered += () =>
		{
			Audio.Ui("hover");
			QueueRedraw();
		};
		MouseExited += QueueRedraw;
	}

	public bool Selected
	{
		set
		{
			_selected = value;
			QueueRedraw();
		}
	}

	public bool Affordable
	{
		set
		{
			if (value == _affordable)
				return;
			_affordable = value;
			QueueRedraw();
		}
	}

	public override void _Process(double delta)
	{
		if (!_selected)
			return;
		_t += (float)delta;
		QueueRedraw();
	}

	public static string[] NameLines(string name)
	{
		// Long first words (ANTIVIRUS) use the full text column; never reduce pixel font size.
		return name.ToUpperInvariant().Split(' ', 2);
	}

	public override void _Draw()
	{
		var r = new Rect2(Vector2.Zero, Size);
		var hovered = IsHovered();
		var bg = hovered ? new Color(0.08f, 0.17f, 0.26f, 0.98f) : new Color(0.05f, 0.11f, 0.18f, 0.95f);
		var border = new Color(UiTheme.Cyan, 0.25f);
		if (_selected)
		{
			var pulse = 0.6f + 0.4f * Mathf.Sin(_t * 6);
			border = new Color(Def.Color, pulse);
			DrawStyleBox(UiTheme.Box(new Color(0, 0, 0, 0), new Color(0, 0, 0, 0), 0, 8, 0, 0, new Color(Def.Color, 0.35f * pulse), 14), r);
		}
		else if (hovered)
			border = new Color(Def.Color, 0.7f);
		DrawStyleBox(UiTheme.Box(bg, border, 2, 8, 0, 0, new Color(0, 0, 0, 0.4f), 6), r);
		var well = new Rect2(7, 2, 48, 48);
		DrawStyleBox(UiTheme.Box(new Color(0.01f, 0.03f, 0.05f, 1), new Color(1, 1, 1, 0.06f), 1, 8, 0, 0), well);
		var mod = _affordable ? Colors.White : new Color(0.5f, 0.5f, 0.55f, 0.7f);
		var size = _icon.GetSize() * View.PixelWorld.Scale;
		DrawTextureRect(_icon, new Rect2((well.GetCenter() - size / 2).Floor(), size), false, mod);
		var words = NameLines(Def.Name);
		for (var i = 0; i < words.Length; i++)
			DrawString(UiTheme.PixelFont, new Vector2(TextLeft, 15 + i * 16), words[i], HorizontalAlignment.Left, Size.X - TextLeft - 4, NameSize, _affordable ? UiTheme.TextColor : UiTheme.Dim);
		DrawString(UiTheme.PixelBold, new Vector2(TextLeft, 47), $"${Def.Cost}", HorizontalAlignment.Left, -1, UiTheme.PixelSmall, _affordable ? UiTheme.Gold : UiTheme.Red);
		DrawRect(new Rect2(TextLeft + 55, 43, 18, 3), new Color(Def.Color, _affordable ? 0.9f : 0.3f));
		DrawRect(new Rect2(Size.X - 26, Size.Y - 26, 18, 18), new Color(1, 1, 1, 0.08f));
		DrawString(UiTheme.PixelFont, new Vector2(Size.X - 22, Size.Y - 12), _hotkey.ToString(), HorizontalAlignment.Left, -1, UiTheme.PixelSmall, UiTheme.Dim);
	}
}
