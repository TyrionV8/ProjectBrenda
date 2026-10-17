using System;
using HardwareDefence.Autoload;
using Godot;

namespace HardwareDefence.UI;

/// <summary>Small helpers to build menus in code with consistent feedback (hover sound + pop).</summary>
public static class Ui
{
	public static Button Button(string text, Action onPressed, string variation = "")
	{
		var b = new Button { Text = text };
		if (variation != "")
			b.ThemeTypeVariation = variation;
		b.Pressed += () =>
		{
			Audio.Ui("click");
			onPressed?.Invoke();
		};
		b.MouseEntered += () =>
		{
			if (b.Disabled)
				return;
			Audio.Ui("hover");
			Pop(b, 1.04f);
		};
		b.MouseExited += () => Pop(b, 1);
		b.Resized += () => b.PivotOffset = b.Size / 2;
		return b;
	}

	public static Label Label(string text, string variation = "", HorizontalAlignment align = HorizontalAlignment.Left)
	{
		var l = new Label { Text = text, HorizontalAlignment = align };
		if (variation != "")
			l.ThemeTypeVariation = variation;
		return l;
	}

	public static VBoxContainer VBox(int separation = 12)
	{
		var b = new VBoxContainer();
		b.AddThemeConstantOverride("separation", separation);
		return b;
	}

	public static HBoxContainer HBox(int separation = 12)
	{
		var b = new HBoxContainer();
		b.AddThemeConstantOverride("separation", separation);
		return b;
	}

	public static Control Spacer(int height = 0, bool expand = false)
	{
		var c = new Control { CustomMinimumSize = new Vector2(0, height), MouseFilter = Control.MouseFilterEnum.Ignore };
		if (expand)
			c.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
		return c;
	}

	public static CenterContainer Centered(Control child)
	{
		var c = new CenterContainer();
		c.SetAnchorsPreset(Control.LayoutPreset.FullRect);
		c.AddChild(child);
		return c;
	}

	public static ColorRect Dim(float alpha = 0.7f)
	{
		var dim = new ColorRect { Color = new Color(0, 0.02f, 0.05f, alpha) };
		dim.SetAnchorsPreset(Control.LayoutPreset.FullRect);
		return dim;
	}

	public static Control Divider()
	{
		var wrap = VBox(0);
		wrap.AddChild(Spacer(6));
		wrap.AddChild(new ColorRect { Color = new Color(UiTheme.Cyan, 0.18f), CustomMinimumSize = new Vector2(0, 2) });
		wrap.AddChild(Spacer(6));
		return wrap;
	}

	/// <summary>Slide/fade a control in when a screen opens.</summary>
	public static async void Reveal(CanvasItem node, float delay = 0, Vector2 offset = default)
	{
		if (offset == default)
			offset = new Vector2(0, 24);
		node.Modulate = new Color(node.Modulate, 0);
		if (!node.IsInsideTree())
			await node.ToSignal(node, Node.SignalName.TreeEntered);
		await node.ToSignal(node.GetTree(), SceneTree.SignalName.ProcessFrame);
		if (!GodotObject.IsInstanceValid(node))
			return;
		var tween = node.CreateTween().SetParallel();
		tween.TweenProperty(node, "modulate:a", 1f, 0.35f).SetDelay(delay);
		if (node is Control control)
		{
			var target = control.Position;
			control.Position = target + offset;
			tween.TweenProperty(control, "position", target, 0.45f).SetDelay(delay).SetTrans(Tween.TransitionType.Cubic).SetEase(Tween.EaseType.Out);
		}
	}

	static void Pop(Control control, float to) =>
		control.CreateTween().TweenProperty(control, "scale", Vector2.One * to, 0.12f).SetTrans(Tween.TransitionType.Cubic).SetEase(Tween.EaseType.Out);
}
