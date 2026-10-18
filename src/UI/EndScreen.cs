using HardwareDefence.Autoload;
using HardwareDefence.Game;
using Godot;

namespace HardwareDefence.UI;

/// <summary>Shown after the last level (with the original prototype's message).</summary>
public partial class EndScreen : Control
{
	public override void _Ready()
	{
		SetAnchorsPreset(LayoutPreset.FullRect);
		AddChild(new MenuBackground());
		AddChild(Ui.Dim(0.72f));
		Audio.PlayMusic("ending");
		var box = Ui.VBox(18);
		box.Alignment = BoxContainer.AlignmentMode.Center;
		box.AddChild(new GlitchTitle { Text = "SECURED", Color = UiTheme.Green, FontSize = 96, SizeFlagsHorizontal = SizeFlags.ShrinkCenter });
		box.AddChild(Ui.Label("All circuits are safe. For now.", "BodyLabel", HorizontalAlignment.Center));
		var msg = Ui.Label("ENDING_MESSAGE", "BodyLabel", HorizontalAlignment.Center);
		msg.AddThemeColorOverride("font_color", UiTheme.Gold);
		box.AddChild(msg);
		box.AddChild(Ui.Spacer(10));
		box.AddChild(Ui.Label("Music: Juhani Junkala - Retro Game Music Pack (CC0)\nFonts: Silkscreen, VT323 (SIL Open Font License)", "DimLabel", HorizontalAlignment.Center));
		box.AddChild(Ui.Spacer(10));
		var back = Ui.Button("Main Menu", () => Transition.Go(GameSession.MenuScene), "BigButton");
		back.SizeFlagsHorizontal = SizeFlags.ShrinkCenter;
		box.AddChild(back);
		AddChild(Ui.Centered(box));
		Ui.Reveal(box, 0.2f);
	}
}
