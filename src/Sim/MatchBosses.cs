using Godot;

namespace HardwareDefence.Sim;

/// <summary>
/// Boss abilities: each boss plays out a real kind of attack. They report what they do as
/// <see cref="EventKind.BossAbility"/> events (Type says what), which the view turns into effects.
/// </summary>
public sealed partial class Match
{
	void StepAbility(Bug b, float dt)
	{
		var def = b.Def;
		switch (def.Ability)
		{
			case "keylogger":
				// Every logged keystroke leaks a little money.
				if (!Tick(b, dt, def.AbilityEvery))
					return;
				if (Money > 0)
				{
					Money--;
					Events.Add(new SimEvent(EventKind.BossAbility, b.Id, b.Pos, 1, "steal"));
				}
				break;
			case "ddos":
				// Floods the route with junk packets right where it is.
				if (!Tick(b, dt, def.AbilityEvery))
					return;
				for (var i = 0; i < def.SpawnCount; i++)
					_spawnQueue.Add((def.Spawn, b.Progress + 6 - i * 10));
				Events.Add(new SimEvent(EventKind.BossAbility, b.Id, b.Pos, def.SpawnCount, "flood"));
				break;
			case "rootkit":
				// Alternates between running in plain sight and hiding from the turrets.
				b.AbilityTimer -= dt;
				if (b.AbilityTimer > 0)
					return;
				b.Cloaked = !b.Cloaked;
				b.AbilityTimer = b.Cloaked ? def.AbilityTime : def.AbilityEvery;
				Events.Add(new SimEvent(EventKind.BossAbility, b.Id, b.Pos, 0, b.Cloaked ? "cloak" : "uncloak"));
				break;
			case "ransomware":
				// Encrypts every turret around it for a while.
				if (!Tick(b, dt, def.AbilityEvery))
					return;
				foreach (var t in Turrets)
					if (t.Pos.DistanceTo(b.Pos) <= def.AbilityRadius)
						t.Locked = Mathf.Max(t.Locked, def.AbilityTime);
				Events.Add(new SimEvent(EventKind.BossAbility, b.Id, b.Pos, def.AbilityRadius, "encrypt"));
				break;
			case "panic":
				// Crashes the system: every turret on the board stops for a moment.
				if (!Tick(b, dt, def.AbilityEvery))
					return;
				foreach (var t in Turrets)
					t.Locked = Mathf.Max(t.Locked, def.AbilityTime);
				Events.Add(new SimEvent(EventKind.BossAbility, b.Id, b.Pos, def.AbilityTime, "panic"));
				break;
		}
	}

	/// <summary>Counts down the ability timer; true (and restarts it) when it's time to act.</summary>
	static bool Tick(Bug b, float dt, float every)
	{
		b.AbilityTimer -= dt;
		if (b.AbilityTimer > 0)
			return false;
		b.AbilityTimer += Mathf.Max(every, 0.1f);
		return true;
	}

	/// <summary>Bosses that react to damage: the trojan's disguise falls off, the worm forks.</summary>
	void OnBossHurt(Bug b)
	{
		switch (b.Def.Ability)
		{
			case "trojan" when !b.Revealed && b.Hp <= b.Def.MaxHp * 0.5f && b.Hp > 0:
				b.Revealed = true;
				Events.Add(new SimEvent(EventKind.BossAbility, b.Id, b.Pos, 0, "reveal"));
				break;
			case "worm":
				// A copy at two thirds and at one third of its health.
				while (b.Forks < 2 && b.Hp > 0 && b.Hp <= b.Def.MaxHp * (1 - (b.Forks + 1) / 3f))
				{
					b.Forks++;
					for (var i = 0; i < b.Def.SpawnCount; i++)
						_spawnQueue.Add((b.Def.Spawn, b.Progress - 18 - i * 12));
					Events.Add(new SimEvent(EventKind.BossAbility, b.Id, b.Pos, b.Forks, "fork"));
				}
				break;
		}
	}

	/// <summary>
	/// Man-in-the-middle: the first time a shot comes within reach of one, it has a 50% chance
	/// of being intercepted (decided from the shot's id, so it's the same on every replay).
	/// </summary>
	bool Intercepted(Projectile p, Vector2 at)
	{
		if (p.Screened)
			return false;
		foreach (var b in Bugs)
		{
			if (b.Gone || b.Def.Ability != "mitm" || b.Pos.DistanceTo(at) > b.Def.AbilityRadius)
				continue;
			p.Screened = true;
			if ((uint)p.Id * 2654435761u % 100 >= 50)
				return false;
			Events.Add(new SimEvent(EventKind.BossAbility, b.Id, at, p.Id, "intercept"));
			return true;
		}
		return false;
	}
}
