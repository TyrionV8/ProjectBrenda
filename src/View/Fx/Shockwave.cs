using Godot;

namespace HardwareDefence.View;

/// <summary>Expanding ring that fades out.</summary>
public partial class Shockwave : Node2D
{
	public float Radius = 60;
	public Color RingColor = Colors.White;
	public float Duration = 0.4f;
	float _t;

	public override void _Ready() => Material = FxUtil.Additive;

	public override void _Process(double delta)
	{
		_t += (float)delta / Duration;
		if (_t >= 1)
		{
			QueueFree();
			return;
		}
		QueueRedraw();
	}

	public override void _Draw()
	{
		var k = Mathf.Ease(_t, 0.4f);
		var r = Mathf.Lerp(Radius * 0.15f, Radius, k);
		var a = 1 - _t;
		DrawArc(Vector2.Zero, r, 0, Mathf.Tau, 48, new Color(RingColor, a * 0.9f), Mathf.Lerp(10, 1.5f, _t), true);
		DrawArc(Vector2.Zero, r * 0.85f, 0, Mathf.Tau, 48, new Color(RingColor, a * 0.25f), Mathf.Lerp(18, 3, _t), true);
	}
}
