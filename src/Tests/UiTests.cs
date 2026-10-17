using System.Linq;
using HardwareDefence.Data;
using HardwareDefence.Editor;
using HardwareDefence.UI;
using Godot;

namespace HardwareDefence.Tests;

public sealed class UiTests : TestSuite
{
	[Test]
	public void PixelFontResourcesKeepSharpIntegerPixels()
	{
		foreach (var font in new[] { UiTheme.PixelFont, UiTheme.PixelBold, UiTheme.BodyFont }.Cast<FontFile>())
		{
			CheckEq((int)font.Antialiasing, 0, $"{font.ResourcePath}: antialiasing disabled");
			CheckEq((int)font.Hinting, 0, $"{font.ResourcePath}: hinting disabled");
			CheckEq((int)font.SubpixelPositioning, 0, $"{font.ResourcePath}: subpixel positioning disabled");
		}
		CheckNear(UiTheme.BodySize * 80f / 1000, 2, "VT323 structural pixel is exactly two screen pixels");
		foreach (var ch in "Você é Brabo! Não há circuitos perdidos: ação, proteção.")
			Check(UiTheme.BodyFont.HasChar(ch), $"VT323 has Portuguese character '{ch}'");
	}

	[Test]
	public void FiveTurretLabelsFitTheInventoryAt1366By768()
	{
		const float contentWidth = 186;
		foreach (var turret in Catalog.Turrets)
		{
			foreach (var word in turret.Name.ToUpperInvariant().Split(' ', 2))
				Check(UiTheme.PixelFont.GetStringSize(word, fontSize: TurretSlot.NameSize).X <= contentWidth - TurretSlot.TextLeft - 4,
					$"{turret.Name}: full name fits beside the icon");
			Check(UiTheme.PixelBold.GetStringSize($"${turret.Cost}", fontSize: UiTheme.PixelNormal).X <= contentWidth - TurretSlot.TextLeft - 26,
				$"{turret.Name}: price fits before the hotkey");
		}
		// Includes side-panel padding, cash heading/value, dividers, five gaps and bottom buttons.
		Check(Catalog.Turrets.Count * TurretSlot.SlotHeight + 220 <= 768, "five inventory entries and HUD controls fit vertically");
	}
	[Test]
	public void TimelineCaptionsFitIncludingBossNames()
	{
		foreach (var enemy in Catalog.WaveEnemies)
			Check(UiTheme.PixelFont.GetStringSize(enemy.Name.ToUpperInvariant(), fontSize: UiTheme.PixelSmall).X <= WaveTimeline.LabelWidth - 58,
				$"{enemy.Name}: full timeline caption fits at native pixel size");
	}

}
