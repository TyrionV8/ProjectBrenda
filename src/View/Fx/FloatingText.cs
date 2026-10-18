using Godot;

namespace HardwareDefence.View;

/// <summary>"+3"-style text in the board's pixel font that rises and fades.</summary>
public partial class FloatingText : Node2D
{
	public string Text = "";
	public Color TextColor = Colors.White;
	float _t;
	Vector2 _start;

	public override void _Ready()
	{
		ZIndex = 20;
		Material = FxUtil.Unshaded;
		_start = Position;
	}

	public override void _Process(double delta)
	{
		_t += (float)delta;
		if (_t >= 1.05f)
		{
			QueueFree();
			return;
		}
		var rise = Mathf.Ease(Mathf.Min(_t / 0.8f, 1), 0.4f) * 34;
		Position = PixelWorld.Snap(_start - new Vector2(0, rise));
		Modulate = new Color(1, 1, 1, _t < 0.8f ? 1 : 1 - (_t - 0.8f) / 0.25f);
	}

	public override void _Draw() => MicroFont.DrawOutlined(this, Vector2.Zero, Text, TextColor, new Color(0, 0, 0, 0.85f));
}
