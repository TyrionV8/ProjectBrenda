using HardwareDefence.Autoload;
using HardwareDefence.Data;
using Godot;

namespace HardwareDefence.UI;

/// <summary>Attacker palette entry: bug, name, energy cost and unlock countdown.</summary>
public partial class BugSlot : Button
{
	public readonly EnemyDef Def;
	readonly int _hotkey;
	string _blocked;
	float _flash;

	public BugSlot(EnemyDef def, int hotkey)
	{
		Def = def;
		_hotkey = hotkey;
		Flat = true;
		FocusMode = FocusModeEnum.None;
		CustomMinimumSize = new Vector2(186, 56);
		Loc.Tooltip(this, $"{def.Name}\nHP {def.MaxHp}  speed {def.Speed}  chews {def.ChewDps}/s");
		var icon = new BugIcon(def, 50) { Position = new Vector2(4, 3) };
		AddChild(icon);
		MouseEntered += () =>
		{
			Audio.Ui("hover");
			QueueRedraw();
		};
		MouseExited += QueueRedraw;
	}

	/// <summary>Why it can't be sent right now (null = ready).</summary>
	public string Blocked
	{
		set
		{
			if (value == _blocked)
				return;
			_blocked = value;
			QueueRedraw();
		}
	}

	public void Sent()
	{
		_flash = 1;
		SetProcess(true);
	}

	public override void _Process(double delta)
	{
		_flash = Mathf.Max(_flash - (float)delta * 4, 0);
		QueueRedraw();
		if (_flash == 0)
			SetProcess(false);
	}

	public override void _Draw()
	{
		var ready = _blocked == null;
		var locked = _blocked != null && _blocked.StartsWith("Unlocks");
		var hovered = IsHovered();
		var bg = new Color(0.05f, 0.11f, 0.18f, 0.95f).Lerp(new Color(UiTheme.Magenta, 0.5f), _flash);
		if (hovered && ready)
			bg = bg.Lightened(0.08f);
		var border = ready ? new Color(UiTheme.Magenta, hovered ? 0.8f : 0.35f) : new Color(1, 1, 1, 0.1f);
		DrawStyleBox(UiTheme.Box(bg, border, 2, 8, 0, 0, new Color(0, 0, 0, 0.4f), 6), new Rect2(Vector2.Zero, Size));
		var name = Loc.S(Def.Name).ToUpperInvariant();
		var lines = UiTheme.PixelFont.GetStringSize(name, fontSize: UiTheme.PixelSmall).X <= Size.X - 60
			? new[] { name } : name.Split(' ', 2);
		for (var i = 0; i < lines.Length; i++)
			DrawString(UiTheme.PixelFont, new Vector2(54, 16 + i * 17), lines[i], HorizontalAlignment.Left, Size.X - 58, UiTheme.PixelSmall, ready ? UiTheme.TextColor : UiTheme.Dim);
		DrawString(UiTheme.PixelBold, new Vector2(54, 53), Def.VersusCost.ToString(), HorizontalAlignment.Left, -1, UiTheme.PixelSmall, ready ? UiTheme.Magenta : UiTheme.Dim);
		if (locked)
			DrawString(UiTheme.PixelFont, new Vector2(96, 53), _blocked.Replace("Unlocks in ", ""), HorizontalAlignment.Left, -1, UiTheme.PixelSmall, UiTheme.Gold);
		DrawString(UiTheme.PixelFont, new Vector2(Size.X - 18, Size.Y - 8), _hotkey.ToString(), HorizontalAlignment.Left, -1, UiTheme.PixelSmall, UiTheme.Dim);
		if (locked)
			DrawRect(new Rect2(Vector2.Zero, Size), new Color(0, 0, 0, 0.35f));
	}
}
