using Brenda.Autoload;
using Brenda.Data;
using Godot;

namespace Brenda.UI;

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
		CustomMinimumSize = new Vector2(186, 74);
		TooltipText = $"{def.Name}\nHP {def.MaxHp}  speed {def.Speed}  chews {def.ChewDps}/s";
		var icon = new BugIcon(def, 54) { Position = new Vector2(8, 10) };
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
		DrawString(UiTheme.PixelFont, new Vector2(70, 26), Def.Name.ToUpperInvariant(), HorizontalAlignment.Left, Size.X - 76, 11, ready ? UiTheme.TextColor : UiTheme.Dim);
		DrawString(UiTheme.PixelBold, new Vector2(70, 56), Def.VersusCost.ToString(), HorizontalAlignment.Left, -1, 22, ready ? UiTheme.Magenta : UiTheme.Dim);
		if (locked)
			DrawString(UiTheme.PixelFont, new Vector2(118, 56), _blocked.Replace("Unlocks in ", ""), HorizontalAlignment.Left, -1, 13, UiTheme.Gold);
		DrawString(UiTheme.PixelFont, new Vector2(Size.X - 20, Size.Y - 10), _hotkey.ToString(), HorizontalAlignment.Left, -1, 12, UiTheme.Dim);
		if (locked)
			DrawRect(new Rect2(Vector2.Zero, Size), new Color(0, 0, 0, 0.35f));
	}
}
