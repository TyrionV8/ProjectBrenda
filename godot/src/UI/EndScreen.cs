using Brenda.Autoload;
using Brenda.Game;
using Godot;

namespace Brenda.UI;

/// <summary>Shown after the last level (with the original prototype's message).</summary>
public partial class EndScreen : Control
{
	public override void _Ready()
	{
		SetAnchorsPreset(LayoutPreset.FullRect);
		AddChild(new MenuBackground());
		AddChild(UI.Dim(0.72f));
		Audio.PlayMusic("ending");
		var box = UI.VBox(18);
		box.Alignment = BoxContainer.AlignmentMode.Center;
		box.AddChild(new GlitchTitle { Text = "SECURED", Color = UiTheme.Green, FontSize = 96, SizeFlagsHorizontal = SizeFlags.ShrinkCenter });
		box.AddChild(UI.Label("All circuits are safe. For now.", "HeaderLabel", HorizontalAlignment.Center));
		var msg = UI.Label("Você é Brabo!!\nNão fizemos mais fases, este é só um protótipo.\nSe quer mais, faz o pix :)", "", HorizontalAlignment.Center);
		msg.AddThemeColorOverride("font_color", UiTheme.Gold);
		box.AddChild(msg);
		box.AddChild(UI.Spacer(10));
		box.AddChild(UI.Label("Music: Juhani Junkala - Retro Game Music Pack (CC0)\nFonts: Silkscreen, Share Tech Mono (SIL OFL)", "DimLabel", HorizontalAlignment.Center));
		box.AddChild(UI.Spacer(10));
		var back = UI.Button("Main Menu", () => Transition.Go(GameSession.MenuScene), "BigButton");
		back.SizeFlagsHorizontal = SizeFlags.ShrinkCenter;
		box.AddChild(back);
		AddChild(UI.Centered(box));
		UI.Reveal(box, 0.2f);
	}
}
