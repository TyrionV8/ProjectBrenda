using Godot;

namespace HardwareDefence.UI;

/// <summary>Big title with a neon glow, RGB split and the occasional glitch burst. "\n" starts a new line.</summary>
public partial class GlitchTitle : Control
{
	public string Text = "HARDWARE\nDEFENCE";
	public int FontSize = 96;
	public Color Color = UiTheme.Cyan;
	float _t;
	float _burst;

	string[] _lines;

	float LineHeight => FontSize * 1.05f;

	public override void _Ready()
	{
		_lines = Text.Split('\n');
		var width = 0f;
		foreach (var line in _lines)
			width = Mathf.Max(width, UiTheme.PixelBold.GetStringSize(line, HorizontalAlignment.Left, -1, FontSize).X);
		CustomMinimumSize = new Vector2(width + 20, LineHeight * (_lines.Length - 1) + FontSize + 10);
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
		var split = 2 + 10 * _burst;
		for (var l = 0; l < _lines.Length; l++)
		{
			var text = _lines[l];
			var b = new Vector2(10, FontSize * 0.85f + l * LineHeight);
			var jitter = new Vector2((float)GD.RandRange(-6, 6) * _burst, 0);
			for (var i = 0; i < 6; i++)
				DrawString(font, b + Vector2.FromAngle(i * Mathf.Tau / 6) * 4, text, HorizontalAlignment.Left, -1, FontSize, new Color(Color, 0.08f));
			DrawString(font, b + jitter + new Vector2(-split, 0), text, HorizontalAlignment.Left, -1, FontSize, new Color(1, 0.2f, 0.4f, 0.55f));
			DrawString(font, b + jitter + new Vector2(split, 0), text, HorizontalAlignment.Left, -1, FontSize, new Color(0.2f, 0.6f, 1, 0.55f));
			DrawString(font, b + jitter, text, HorizontalAlignment.Left, -1, FontSize, Color.Lerp(Colors.White, 0.35f + 0.1f * Mathf.Sin(_t * 3)));
		}
		if (_burst <= 0.3f)
			return;
		var rng = new RandomNumberGenerator { Seed = (ulong)(_t * 30) };
		for (var i = 0; i < 4; i++)
			DrawRect(new Rect2(rng.RandfRange(0, Size.X * 0.6f), rng.RandfRange(0, Size.Y), rng.RandfRange(40, 200), rng.RandfRange(2, 6)), new Color(Color, 0.35f * _burst));
	}
}
