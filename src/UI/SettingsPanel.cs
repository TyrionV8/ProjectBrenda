using System;
using HardwareDefence.Autoload;
using Godot;

namespace HardwareDefence.UI;

/// <summary>Volume sliders and display options. Raises <see cref="Closed"/> when the player goes back.</summary>
public partial class SettingsPanel : PanelContainer
{
	public event Action Closed;

	public void EmitClosed() => Closed?.Invoke();

	public override void _Ready()
	{
		CustomMinimumSize = new Vector2(520, 0);
		var box = Ui.VBox(14);
		AddChild(box);
		box.AddChild(Ui.Label("SETTINGS", "HeaderLabel", HorizontalAlignment.Center));
		box.AddChild(Ui.Spacer(6));
		var languageRow = Ui.HBox();
		languageRow.AddChild(Ui.Label("Language", "BodyLabel"));
		var language = new OptionButton { Name = "LanguageOption", SizeFlagsHorizontal = SizeFlags.ExpandFill };
		language.AddItem("English"); language.AddItem("Português brasileiro");
		language.Selected = Settings.Instance.Language == "pt_BR" ? 1 : 0;
		language.ItemSelected += index => Settings.Instance.SetLanguage(index == 1 ? "pt_BR" : "en");
		languageRow.AddChild(language);
		box.AddChild(languageRow);
		foreach (var (bus, text) in new[] { ("Master", "Master volume"), ("Music", "Music"), ("SFX", "Effects"), ("UI", "Interface sounds") })
			box.AddChild(Slider(bus, text));
		box.AddChild(Ui.Spacer(4));
		if (!OS.HasFeature("mobile") && !OS.HasFeature("web"))
		{
			var full = new CheckBox { Text = "Full screen", ButtonPressed = Settings.Instance.Fullscreen };
			full.Toggled += on =>
			{
				Audio.Ui("click");
				Settings.Instance.SetFullscreen(on);
			};
			box.AddChild(full);
		}
		var fx = new CheckBox { Text = "Screen effects (scanlines, vignette)", ButtonPressed = Settings.Instance.ScreenFx };
		fx.Toggled += on =>
		{
			Audio.Ui("click");
			Settings.Instance.SetScreenFx(on);
		};
		box.AddChild(fx);
		var tips = new CheckBox { Name = "TipsOption", Text = "Tips", ButtonPressed = Settings.Instance.Tips };
		tips.Toggled += on => Settings.Instance.SetTips(on);
		box.AddChild(tips);
		box.AddChild(Ui.Spacer(8));
		var back = Ui.Button("Back", () => Closed?.Invoke());
		back.SizeFlagsHorizontal = SizeFlags.ShrinkCenter;
		back.CustomMinimumSize = new Vector2(200, 0);
		box.AddChild(back);
	}

	static Control Slider(string bus, string text)
	{
		var row = Ui.VBox(4);
		var top = Ui.HBox();
		var name = Ui.Label(text.ToUpperInvariant(), "SmallHeader");
		name.SizeFlagsHorizontal = SizeFlags.ExpandFill;
		top.AddChild(name);
		var value = Ui.Label("", "SmallHeader");
		top.AddChild(value);
		row.AddChild(top);
		var slider = new HSlider { MinValue = 0, MaxValue = 1, Step = 0.01, Value = Settings.Instance.Volumes[bus], CustomMinimumSize = new Vector2(0, 26) };
		Loc.Set(value, $"{Mathf.RoundToInt(slider.Value * 100)}%");
		slider.ValueChanged += v =>
		{
			Loc.Set(value, $"{Mathf.RoundToInt(v * 100)}%");
			Settings.Instance.SetVolume(bus, (float)v);
		};
		slider.DragEnded += _ => Audio.Play("place", bus == "Master" ? "SFX" : bus);
		row.AddChild(slider);
		return row;
	}
}
