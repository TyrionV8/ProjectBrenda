using System;
using System.Collections.Generic;
using System.Linq;
using Brenda.Data;

namespace Brenda.Sim;

public enum WaveState
{
	Countdown,
	Spawning,
	Waiting,
	Done,
}

/// <summary>
/// Campaign waves: a short countdown, then each bug spawns at its time. A wave ends when
/// everything it spawned is dead or escaped.
/// </summary>
public sealed class WaveRunner
{
	/// <summary>Pause before each wave (the prototype used a 3 second timeline).</summary>
	public const float CountdownTime = 3f;

	readonly List<List<SpawnData>> _waves;
	float _time;
	int _next;

	public WaveState State { get; private set; } = WaveState.Countdown;
	public int Index { get; private set; }
	public float Countdown { get; private set; } = CountdownTime;
	public int Count => _waves.Count;

	public WaveRunner(List<List<SpawnData>> waves)
	{
		_waves = waves;
		if (_waves.Count == 0)
			State = WaveState.Done;
	}

	/// <summary>Bugs of the current wave that haven't spawned yet.</summary>
	public IEnumerable<SpawnData> Upcoming()
	{
		if (Index >= _waves.Count || State == WaveState.Done)
			return Enumerable.Empty<SpawnData>();
		return State == WaveState.Countdown ? _waves[Index] : _waves[Index].Skip(_next);
	}

	public void Step(float dt, int alive, Action<string> spawn, Action<EventKind, int> notify)
	{
		switch (State)
		{
			case WaveState.Countdown:
				Countdown -= dt;
				if (Countdown <= 0)
				{
					State = WaveState.Spawning;
					_time = 0;
					_next = 0;
					notify(EventKind.WaveStarted, Index);
				}
				break;
			case WaveState.Spawning:
				_time += dt;
				var wave = _waves[Index];
				while (_next < wave.Count && wave[_next].Time <= _time)
					spawn(wave[_next++].Enemy);
				if (_next >= wave.Count)
					State = WaveState.Waiting;
				break;
			case WaveState.Waiting:
				if (alive > 0)
					break;
				if (Index >= _waves.Count - 1)
				{
					State = WaveState.Done;
				}
				else
				{
					Index++;
					State = WaveState.Countdown;
					Countdown = CountdownTime;
					notify(EventKind.WaveCountdown, Index);
				}
				break;
		}
	}
}
