using System.Collections.Generic;
using HardwareDefence.Data;
using HardwareDefence.Sim;
using Godot;

namespace HardwareDefence.View;

/// <summary>Presentation only: one arrival per match-local boss id, never driven by ability events.</summary>
internal sealed class BossIntroSequence
{
	readonly HashSet<int> _seen = new();
	readonly Dictionary<int, BossIntro> _live = new();
	readonly HashSet<int> _present = new();
	readonly List<int> _removed = new();

	internal readonly List<BossIntro> Arrivals = new();
	internal IEnumerable<BossIntro> Live => _live.Values;
	internal BossIntro Featured { get; private set; }
	internal int ArrivalCount { get; private set; }

	internal void Sync(Snapshot snapshot, float delta)
	{
		Arrivals.Clear();
		Featured = null;
		_present.Clear();
		if (snapshot.MatchState != MatchState.Playing)
		{
			_live.Clear();
			return;
		}
		foreach (var bug in snapshot.Bugs)
		{
			var def = Catalog.Enemy(bug.Type);
			if (def is not { Boss: true } || bug.Hp <= 0 || !_present.Add(bug.Id))
				continue;
			if (!_live.TryGetValue(bug.Id, out var intro))
			{
				var first = _seen.Add(bug.Id);
				intro = new BossIntro(bug.Id, def, TierFor(def.Id), first);
				_live.Add(bug.Id, intro);
				if (first)
				{
					ArrivalCount++;
					Arrivals.Add(intro);
				}
			}
			else
				intro.Age += Mathf.Max(delta, 0);
			intro.Position = PixelWorld.Snap(new Vector2(bug.X, bug.Y));
			// Several bosses in versus share a single panel. Prefer the greatest threat,
			// then the newest arrival; every live boss keeps its own unobtrusive presence.
			if (intro.Active && (Featured == null || intro.Tier > Featured.Tier
				|| intro.Tier == Featured.Tier && intro.Age < Featured.Age))
				Featured = intro;
		}
		_removed.Clear();
		foreach (var id in _live.Keys)
			if (!_present.Contains(id)) _removed.Add(id);
		foreach (var id in _removed) _live.Remove(id);
	}

	internal static int TierFor(string type)
	{
		for (var i = 0; i < Catalog.Levels.Count; i++)
			foreach (var wave in Catalog.Levels[i].Waves)
				foreach (var spawn in wave)
					if (spawn.Enemy == type) return Mathf.Clamp(i + 1, 1, 8);
		// A custom/versus-only boss still has a sensible presentation if it is absent
		// from the built-in waves. Built-in level order always takes precedence.
		return Catalog.Enemy(type)?.Ability switch
		{
			"keylogger" => 2, "mitm" => 3, "ddos" => 4, "rootkit" => 5,
			"ransomware" => 6, "worm" => 7, "panic" => 8, _ => 1,
		};
	}
}

internal sealed class BossIntro
{
	internal readonly int Id;
	internal readonly EnemyDef Def;
	internal readonly int Tier;
	internal float Age;
	internal Vector2 Position;
	internal bool AccentPlayed;

	internal float Duration => 1.6f + Tier * 0.33f;
	internal Vector2I ArtSize => new(108 + Tier * 12, 40 + Tier * 3);
	internal int Particles => 4 + Tier * 4;
	internal float Shake => 0.5f + Tier * 0.7f;
	internal float Glitch => Tier * 0.035f;
	internal bool Active => Age < Duration;
	internal float Alpha => Mathf.Clamp(Age / 0.12f, 0, 1) * Mathf.Clamp((Duration - Age) / 0.4f, 0, 1);
	internal float GlitchStrength => Active && Age > 0.18f && (int)(Age * 12) % 5 < 2 ? Glitch * Alpha : 0;

	internal BossIntro(int id, EnemyDef def, int tier, bool first)
	{
		Id = id;
		Def = def;
		Tier = Mathf.Clamp(tier, 1, 8);
		Age = first ? 0 : Duration;
		AccentPlayed = !first;
	}

	/// <summary>Lower board band: below the old warning, above build/cancel controls.
	/// All bounds are whole art pixels; no main viewport control or input interception.</summary>
	internal Rect2 Frame(Vector2 boardSize)
	{
		var artBoard = (boardSize / PixelWorld.Scale).Floor();
		var width = Mathf.Min(ArtSize.X, artBoard.X - 16);
		var height = Mathf.Min(ArtSize.Y, artBoard.Y - 32);
		return new Rect2(new Vector2(Mathf.Floor((artBoard.X - width) / 2),
			Mathf.Min(156, artBoard.Y - height - 28)) * PixelWorld.Scale,
			new Vector2(width, height) * PixelWorld.Scale);
	}
}
