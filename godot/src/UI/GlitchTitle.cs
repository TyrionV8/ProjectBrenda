using Godot;

namespace Brenda.UI;

/// <summary>Big title with a neon glow, RGB split and the occasional glitch burst.</summary>
public partial class GlitchTitle : Control
{
	public string Text = "BRENDA";
	public int FontSize = 120;
	public Color Color = UiTheme.Cyan;
	float _t;
	float _burst;

	public override void _Ready()
	{
		CustomMinimumSize = UiTheme.PixelBold.GetStringSize(Text, HorizontalAlignment.Left, -1, FontSize) + new Vector2(20, 10);
		MouseFilter = MouseFilterEnum.Ignore;
	}

	public override void _Process(double delta)
	{
		_t += (float)delta;
		_burst = Mathf.Max(_burst - (float)delta * 4, 0);
		if (GD.Randf() < delta * 0.35)
			_burst = 1;
		QueueRedraw();
	}

	public override void _Draw()
	{
		var font = UiTheme.PixelBold;
		var b = new Vector2(10, FontSize * 0.85f);
		var jitter = new Vector2((float)GD.RandRange(-6, 6) * _burst, 0);
		for (var i = 0; i < 6; i++)
			DrawString(font, b + Vector2.FromAngle(i * Mathf.Tau / 6) * 4, Text, HorizontalAlignment.Left, -1, FontSize, new Color(Color, 0.08f));
		var split = 2 + 10 * _burst;
		DrawString(font, b + jitter + new Vector2(-split, 0), Text, HorizontalAlignment.Left, -1, FontSize, new Color(1, 0.2f, 0.4f, 0.55f));
		DrawString(font, b + jitter + new Vector2(split, 0), Text, HorizontalAlignment.Left, -1, FontSize, new Color(0.2f, 0.6f, 1, 0.55f));
		DrawString(font, b + jitter, Text, HorizontalAlignment.Left, -1, FontSize, Color.Lerp(Colors.White, 0.35f + 0.1f * Mathf.Sin(_t * 3)));
		if (_burst <= 0.3f)
			return;
		var rng = new RandomNumberGenerator { Seed = (ulong)(_t * 30) };
		for (var i = 0; i < 4; i++)
			DrawRect(new Rect2(rng.RandfRange(0, Size.X * 0.6f), rng.RandfRange(0, Size.Y), rng.RandfRange(40, 200), rng.RandfRange(2, 6)), new Color(Color, 0.35f * _burst));
	}
}
