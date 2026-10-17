using System;
using System.Collections.Generic;
using Godot;

namespace HardwareDefence.Autoload;

/// <summary>Small procedural sound effects (the original project only had a few samples).</summary>
public static class SfxSynth
{
	const int Rate = 22050;
	static readonly Random Rng = new(7);

	public static Dictionary<string, AudioStreamWav> BuildAll() => new()
	{
		["death"] = Death(),
		["explode"] = Explode(),
		["cpu_hit"] = CpuHit(),
		["place"] = Arpeggio(new[] { 660f, 990f }, 0.055f, Wave.Square, 0.35f),
		["error"] = Arpeggio(new[] { 220f, 0f, 220f }, 0.07f, Wave.Square, 0.3f),
		["wave"] = Arpeggio(new[] { 440f, 554.4f, 659.3f, 880f }, 0.075f, Wave.Triangle, 0.5f),
		["upgrade"] = Arpeggio(new[] { 523.3f, 659.3f, 784f, 1046.5f }, 0.06f, Wave.Square, 0.3f),
		["win"] = Arpeggio(new[] { 523.3f, 659.3f, 784f, 1046.5f, 784f, 1046.5f, 1318.5f }, 0.09f, Wave.Triangle, 0.5f),
		["lose"] = Arpeggio(new[] { 392f, 311.1f, 261.6f, 196f, 130.8f }, 0.14f, Wave.Square, 0.35f),
		["pew"] = Sweep(1500, 500, 0.08f, Wave.Square, 0.22f),
		["freeze"] = Freeze(),
		["crunch"] = Crunch(),
		["send"] = Sweep(300, 900, 0.09f, Wave.Triangle, 0.35f),
	};

	enum Wave
	{
		Square,
		Triangle,
		Sine,
	}

	static float Noise() => (float)(Rng.NextDouble() * 2 - 1);

	static float Osc(Wave wave, double phase)
	{
		var p = (float)(phase % 1.0);
		return wave switch
		{
			Wave.Square => p < 0.5f ? 1 : -1,
			Wave.Triangle => 4 * Math.Abs(p - 0.5f) - 1,
			_ => Mathf.Sin(p * Mathf.Tau),
		};
	}

	static AudioStreamWav Death()
	{
		var n = (int)(Rate * 0.16f);
		var data = new float[n];
		double phase = 0;
		for (var i = 0; i < n; i++)
		{
			var t = (float)i / n;
			phase += Mathf.Lerp(900f, 160f, t) / Rate;
			data[i] = (Osc(Wave.Square, phase) * 0.55f + Noise() * 0.45f * (1 - t)) * Mathf.Pow(1 - t, 1.5f) * 0.45f;
		}
		return ToWav(data);
	}

	static AudioStreamWav Explode()
	{
		var n = (int)(Rate * 0.55f);
		var data = new float[n];
		float low = 0;
		double phase = 0;
		for (var i = 0; i < n; i++)
		{
			var t = (float)i / n;
			low = Mathf.Lerp(low, Noise(), Mathf.Lerp(0.5f, 0.05f, t));
			phase += Mathf.Lerp(90f, 35f, t) / Rate;
			data[i] = (low * 0.8f + Osc(Wave.Sine, phase) * 0.6f * (1 - t)) * Mathf.Pow(1 - t, 2) * 0.8f;
		}
		return ToWav(data);
	}

	static AudioStreamWav CpuHit()
	{
		var n = (int)(Rate * 0.35f);
		var data = new float[n];
		double phase = 0;
		for (var i = 0; i < n; i++)
		{
			var t = (float)i / n;
			phase += (110 + Mathf.Sin(t * 60) * 12) / Rate;
			var saw = (float)(phase % 1.0) * 2 - 1;
			data[i] = (saw * 0.6f + Noise() * 0.4f * Mathf.Pow(1 - t, 4)) * (1 - t) * 0.6f;
		}
		return ToWav(data);
	}

	static AudioStreamWav Freeze()
	{
		var n = (int)(Rate * 0.3f);
		var data = new float[n];
		var freqs = new[] { 1760f, 2217f, 2637f, 3136f };
		for (var i = 0; i < n; i++)
		{
			var t = (float)i / n;
			float s = 0;
			for (var k = 0; k < freqs.Length; k++)
				s += Mathf.Sin(Mathf.Tau * freqs[k] * i / Rate + k) * (0.5f + 0.5f * Mathf.Sin(t * 40 + k * 1.7f));
			data[i] = s / freqs.Length * Mathf.Pow(1 - t, 2) * 0.4f;
		}
		return ToWav(data);
	}

	/// <summary>Bugs chewing on a component: short crackly bursts.</summary>
	static AudioStreamWav Crunch()
	{
		var n = (int)(Rate * 0.12f);
		var data = new float[n];
		float lp = 0;
		for (var i = 0; i < n; i++)
		{
			var t = (float)i / n;
			lp = Mathf.Lerp(lp, Noise(), 0.35f);
			var gate = (i / 300) % 2 == 0 ? 1f : 0.3f;
			data[i] = lp * gate * Mathf.Pow(1 - t, 1.2f) * 0.5f;
		}
		return ToWav(data);
	}

	static AudioStreamWav Sweep(float from, float to, float length, Wave wave, float volume)
	{
		var n = (int)(Rate * length);
		var data = new float[n];
		double phase = 0;
		for (var i = 0; i < n; i++)
		{
			var t = (float)i / n;
			phase += Mathf.Lerp(from, to, t) / Rate;
			data[i] = Osc(wave, phase) * (1 - t) * volume;
		}
		return ToWav(data);
	}

	static AudioStreamWav Arpeggio(float[] notes, float noteLength, Wave wave, float volume)
	{
		var perNote = (int)(Rate * noteLength);
		var data = new float[perNote * notes.Length];
		for (var k = 0; k < notes.Length; k++)
		{
			double phase = 0;
			for (var i = 0; i < perNote; i++)
			{
				var t = (float)i / perNote;
				phase += notes[k] / Rate;
				var env = Mathf.Min(t * 20, 1) * (1 - t * 0.7f);
				data[k * perNote + i] = notes[k] <= 0 ? 0 : Osc(wave, phase) * env * volume;
			}
		}
		return ToWav(data);
	}

	static AudioStreamWav ToWav(float[] data)
	{
		var bytes = new byte[data.Length * 2];
		for (var i = 0; i < data.Length; i++)
		{
			var v = (short)(Mathf.Clamp(data[i], -1, 1) * 32767);
			bytes[i * 2] = (byte)(v & 0xff);
			bytes[i * 2 + 1] = (byte)((v >> 8) & 0xff);
		}
		return new AudioStreamWav { Format = AudioStreamWav.FormatEnum.Format16Bits, MixRate = Rate, Stereo = false, Data = bytes };
	}
}
