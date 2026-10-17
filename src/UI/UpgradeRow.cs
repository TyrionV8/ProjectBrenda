using System;
using HardwareDefence.Autoload;
using HardwareDefence.Data;
using Godot;

namespace HardwareDefence.UI;

/// <summary>One upgrade on the upgrade screen: icon, name, level blocks and a "+" to buy.</summary>
public partial class UpgradeRow : Button
{
	public readonly TurretDef Turret;
	public readonly UpgradeDef Upgrade;
	public event Action<UpgradeRow> Focused;
	readonly Texture2D _icon;
	float _flash;

	public UpgradeRow(TurretDef turret, UpgradeDef upgrade)
	{
		Turret = turret;
		Upgrade = upgrade;
		_icon = GD.Load<Texture2D>(upgrade.Icon);
		Flat = true;
		FocusMode = FocusModeEnum.None;
		CustomMinimumSize = new Vector2(0, 52);
		MouseEntered += () =>
		{
			Audio.Ui("hover");
			Focused?.Invoke(this);
			QueueRedraw();
		};
		MouseExited += QueueRedraw;
		GameState.Changed += QueueRedraw;
		SetProcess(false);
	}

	public override void _ExitTree() => GameState.Changed -= QueueRedraw;

	public void Bought()
	{
		_flash = 1;
		SetProcess(true);
	}

	public override void _Process(double delta)
	{
		_flash = Mathf.Max(_flash - (float)delta * 3, 0);
		QueueRedraw();
		if (_flash == 0)
			SetProcess(false);
	}

	public override void _Draw()
	{
		var level = GameState.UpgradeLevel(Turret.Id, Upgrade.Id);
		var maxed = level >= Upgrade.MaxLevel;
		var canBuy = GameState.CanBuy(Turret, Upgrade);
		var hovered = IsHovered();
		var bg = new Color(0.05f, 0.11f, 0.18f, 0.9f).Lerp(new Color(Turret.Color, 0.35f), _flash);
		if (hovered)
			bg = bg.Lightened(0.08f);
		DrawRect(new Rect2(Vector2.Zero, Size), bg);
		DrawRect(new Rect2(Vector2.Zero, Size), new Color(Turret.Color, hovered ? 0.55f : 0.18f), false, 1.5f);
		DrawTextureRect(_icon, new Rect2(8, 10, 32, 32), false, level > 0 ? Turret.Color : new Color(Turret.Color, 0.5f));
		DrawString(UiTheme.PixelFont, new Vector2(48, 22), Upgrade.Label.ToUpperInvariant(), HorizontalAlignment.Left, Size.X - 90, 12, UiTheme.TextColor);
		var bar = new Rect2(48, 31, Size.X - 96, 9);
		var blocks = Mathf.Min(Upgrade.MaxLevel, 25);
		var bw = bar.Size.X / blocks;
		var filled = (float)level / Upgrade.MaxLevel * blocks;
		for (var i = 0; i < blocks; i++)
			DrawRect(new Rect2(bar.Position.X + i * bw, bar.Position.Y, bw - 1.5f, bar.Size.Y), i < filled ? Turret.Color : new Color(1, 1, 1, 0.08f));
		DrawString(UiTheme.PixelFont, new Vector2(48, 50), maxed ? "MAX" : $"{level}/{Upgrade.MaxLevel}", HorizontalAlignment.Left, -1, 10, maxed ? UiTheme.Gold : UiTheme.Dim);
		var plus = new Rect2(Size.X - 40, 10, 32, 32);
		var plusColor = canBuy ? Turret.Color : new Color(0.4f, 0.45f, 0.5f, 0.4f);
		DrawRect(plus, new Color(plusColor, 0.15f));
		DrawRect(plus, plusColor, false, 2);
		if (maxed)
		{
			DrawString(UiTheme.PixelFont, plus.Position + new Vector2(5, 21), "OK", HorizontalAlignment.Left, -1, 10, plusColor);
			return;
		}
		var c = plus.GetCenter();
		DrawLine(c - new Vector2(8, 0), c + new Vector2(8, 0), plusColor, 3);
		DrawLine(c - new Vector2(0, 8), c + new Vector2(0, 8), plusColor, 3);
	}
}
