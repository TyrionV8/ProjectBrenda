using System;
using Brenda.Autoload;
using Godot;

namespace Brenda.UI;

/// <summary>Volume sliders and display options. Raises <see cref="Closed"/> when the player goes back.</summary>
public partial class SettingsPanel : PanelContainer
{
	public event Action Closed;

	public void EmitClosed() => Closed?.Invoke();

	public override void _Ready()
	{
		CustomMinimumSize = new Vector2(520, 0);
		var box = UI.VBox(14);
		AddChild(box);
		box.AddChild(UI.Label("SETTINGS", "HeaderLabel", HorizontalAlignment.Center));
		box.AddChild(UI.Spacer(6));
		foreach (var (bus, text) in new[] { ("Master", "Master volume"), ("Music", "Music"), ("SFX", "Effects"), ("UI", "Interface sounds") })
			box.AddChild(Slider(bus, text));
		box.AddChild(UI.Spacer(4));
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
		box.AddChild(UI.Spacer(8));
		var back = UI.Button("Back", () => Closed?.Invoke());
		back.SizeFlagsHorizontal = SizeFlags.ShrinkCenter;
		back.CustomMinimumSize = new Vector2(200, 0);
		box.AddChild(back);
	}

	static Control Slider(string bus, string text)
	{
		var row = UI.VBox(4);
		var top = UI.HBox();
		var name = UI.Label(text.ToUpperInvariant(), "SmallHeader");
		name.SizeFlagsHorizontal = SizeFlags.ExpandFill;
		top.AddChild(name);
		var value = UI.Label("", "SmallHeader");
		top.AddChild(value);
		row.AddChild(top);
		var slider = new HSlider { MinValue = 0, MaxValue = 1, Step = 0.01, Value = Settings.Instance.Volumes[bus], CustomMinimumSize = new Vector2(0, 26) };
		value.Text = $"{Mathf.RoundToInt(slider.Value * 100)}%";
		slider.ValueChanged += v =>
		{
			value.Text = $"{Mathf.RoundToInt(v * 100)}%";
			Settings.Instance.SetVolume(bus, (float)v);
		};
		slider.DragEnded += _ => Audio.Play("place", bus == "Master" ? "SFX" : bus);
		row.AddChild(slider);
		return row;
	}
}
