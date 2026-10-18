using System.Collections.Generic;
using HardwareDefence.Sim;
using Godot;

namespace HardwareDefence.View;

/// <summary>Hard-edged counter effects on the shared art-pixel grid, without lights or halos.</summary>
public partial class CounterFx : Node2D
{
	sealed class Pulse
	{
		public string Type;
		public Vector2 From, To;
		public float Range, Age;
	}
	readonly List<Pulse> _pulses = new();
	const int Px = PixelWorld.Scale;
	public override void _Ready()
	{
		ZIndex = 4;
		Material = FxUtil.Unshaded;
	}
	public void Emit(SimEvent e, Vector2 source)
	{
		_pulses.Add(new Pulse { Type = e.Type, From = source, To = e.Pos, Range = e.Value });
		// Many burst pellets may cross a shield together; keep effects bounded.
		if (_pulses.Count > 100) _pulses.RemoveAt(0);
		QueueRedraw();
	}
	public override void _Process(double delta)
	{
		foreach (var p in _pulses) p.Age += (float)delta;
		_pulses.RemoveAll(p => p.Age > 0.65f);
		QueueRedraw();
	}
	void Pixel(Vector2 p, Color color, int size = 1) => DrawRect(new Rect2(PixelWorld.Snap(p), Vector2.One * Px * size), color);
	public override void _Draw()
	{
		foreach (var p in _pulses)
		{
			var alpha = 1 - p.Age / 0.65f;
			if (p.Type is "block" or "restore")
			{
				var link = new Color(new Color(p.Type == "block" ? "ff8a3d" : "4da3ff"), alpha);
				var length = p.From.DistanceTo(p.To);
				for (var d = 15f; d < length; d += 6) Pixel(p.From.Lerp(p.To, d / length), link);
			}
			if (p.Type == "scan")
			{
				var color = new Color(new Color("3dff6a"), alpha);
				var reach = p.Range * (0.2f + p.Age / 0.65f * 0.8f);
				for (var degrees = 0; degrees < 360; degrees += 6)
					Pixel(p.From + Vector2.FromAngle(Mathf.DegToRad(degrees)) * reach, color);
				var angle = p.Age * 9;
				for (var d = 12f; d < reach; d += 6) Pixel(p.From + Vector2.FromAngle(angle) * d, color);
			}
			else if (p.Type == "block")
			{
				var color = new Color(new Color("ff8a3d"), alpha);
				// A stepped shield outline at the protected projectile/packet.
				var offsets = new[] { new Vector2(-3,-4),new Vector2(-2,-5),new Vector2(0,-5),new Vector2(2,-5),new Vector2(3,-4),new Vector2(-3,-2),new Vector2(3,-2),new Vector2(-2,0),new Vector2(2,0),new Vector2(-1,1),new Vector2(0,2),new Vector2(1,1) };
				foreach (var v in offsets) Pixel(p.To + new Vector2(0,18) + v * Px, color);
			}
			else if (p.Type == "restore")
			{
				var color = new Color(new Color("4da3ff"), alpha);
				for (var i = 0; i < 4; i++)
				{
					var center = p.To + Vector2.FromAngle(i * Mathf.Pi / 2) * (15 + p.Age * 35);
					Pixel(center, color, 2);
				}
				Pixel(p.To + new Vector2(-1,-3) * Px, color, 2);
				Pixel(p.To + new Vector2(-2,-2) * Px, color, 2);
			}
			else if (p.Type == "repair")
			{
				var color = new Color(new Color("ffd9a0"), alpha);
				var distance = p.From.DistanceTo(p.To);
				for (var d = 18f; d < distance; d += 6)
					Pixel(p.From.Lerp(p.To, d / distance), color);
				for (var i = 0; i < 5; i++)
					Pixel(p.To + Vector2.FromAngle(i * 1.25f + p.Age * 8) * (6 + p.Age * 15), color);
			}
		}
	}
}
