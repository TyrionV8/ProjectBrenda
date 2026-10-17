using System.Collections.Generic;
using Godot;

namespace HardwareDefence.View;

/// <summary>Tiny glowing "electrons" running along the board's decorative traces.</summary>
public partial class DecorPulses : Node2D
{
	const int Count = 14;
	const float Speed = 70;

	public List<Vector2[]> Lines = new();

	sealed class Pulse
	{
		public Vector2[] Line;
		public float Length;
		public float D;
		public float Speed;
	}

	readonly List<Pulse> _pulses = new();

	public override void _Ready()
	{
		Material = FxUtil.Additive;
		for (var i = 0; i < Count; i++)
			_pulses.Add(NewPulse(GD.Randf()));
	}

	Pulse NewPulse(float startRatio = 0)
	{
		var line = Lines.Count > 0 ? Lines[GD.RandRange(0, Lines.Count - 1)] : new Vector2[0];
		var length = 0f;
		for (var i = 1; i < line.Length; i++)
			length += line[i - 1].DistanceTo(line[i]);
		return new Pulse { Line = line, Length = length, D = length * startRatio, Speed = Speed * (float)GD.RandRange(0.6, 1.4) };
	}

	public override void _Process(double delta)
	{
		for (var i = 0; i < _pulses.Count; i++)
		{
			_pulses[i].D += _pulses[i].Speed * (float)delta;
			if (_pulses[i].D >= _pulses[i].Length)
				_pulses[i] = NewPulse();
		}
		QueueRedraw();
	}

	public override void _Draw()
	{
		foreach (var p in _pulses)
		{
			if (p.Line.Length < 2)
				continue;
			var pos = PointAt(p.Line, p.D);
			var fade = Mathf.Clamp(Mathf.Min(p.D, p.Length - p.D) / 30f, 0, 1);
			FxUtil.DrawGlow(this, pos, 18, new Color(0.3f, 0.8f, 1f, 0.7f * fade));
			DrawCircle(pos, 1.6f, new Color(0.85f, 0.97f, 1f, fade));
		}
	}

	static Vector2 PointAt(Vector2[] line, float d)
	{
		for (var i = 1; i < line.Length; i++)
		{
			var seg = line[i - 1].DistanceTo(line[i]);
			if (d <= seg)
				return line[i - 1].Lerp(line[i], d / Mathf.Max(seg, 0.001f));
			d -= seg;
		}
		return line[^1];
	}
}
