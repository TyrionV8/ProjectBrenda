using Godot;

namespace HardwareDefence.View;

/// <summary>"+3"-style text that pops, rises and fades.</summary>
public partial class FloatingText : Node2D
{
	public string Text = "";
	public Color TextColor = Colors.White;
	public Font Font;
	public int FontSize = 16;

	public override void _Ready()
	{
		ZIndex = 20;
		Material = FxUtil.Unshaded;
		Scale = Vector2.One * 0.4f;
		var tween = CreateTween();
		tween.TweenProperty(this, "scale", Vector2.One, 0.18f).SetTrans(Tween.TransitionType.Back).SetEase(Tween.EaseType.Out);
		tween.Parallel().TweenProperty(this, "position:y", Position.Y - 34, 0.8f).SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Cubic);
		tween.TweenProperty(this, "modulate:a", 0f, 0.25f);
		tween.TweenCallback(Callable.From(QueueFree));
	}

	public override void _Draw()
	{
		var width = Font.GetStringSize(Text, HorizontalAlignment.Left, -1, FontSize).X;
		var pos = new Vector2(-width / 2, FontSize * 0.35f);
		DrawStringOutline(Font, pos, Text, HorizontalAlignment.Left, -1, FontSize, 4, new Color(0, 0, 0, 0.8f));
		DrawString(Font, pos, Text, HorizontalAlignment.Left, -1, FontSize, TextColor);
	}
}
