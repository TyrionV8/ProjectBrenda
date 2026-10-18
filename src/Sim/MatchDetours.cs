using System.Linq;
using Godot;

namespace HardwareDefence.Sim;

/// <summary>Route → component edge → chew → the exact saved route point.</summary>
public sealed partial class Match
{
	bool TryStartDetour(Bug b)
	{
		var def = b.Def;
		if (def.Boss || def.Id == "packet" || def.DetourChance <= 0 || def.DetourRange <= 0
			|| def.DetourChewTime <= 0 || def.ChewDps <= 0 || b.DetourCooldown > 0)
			return false;

		// Stable nearest-first ordering also keeps seeded replays independent of hash iteration.
		var range = def.DetourRange * Data.Grid.Cell;
		foreach (var target in Components.Where(c => c.Chewable && !b.DetourConsidered.Contains(c.Index)
			&& c.DistanceTo(b.Pos) <= range).OrderBy(c => c.DistanceTo(b.Pos)).ThenBy(c => c.Index))
		{
			b.DetourConsidered.Add(target.Index);
			if (_detourRandom.NextDouble() >= def.DetourChance)
				continue;

			b.Detour = DetourState.Going;
			b.DetourTarget = target.Index;
			b.DetourExit = b.Pos;
			var edge = target.ClosestPoint(b.Pos);
			// Stop with the mouth at the edge instead of burying the body in the component.
			var distance = edge.DistanceTo(b.Pos);
			b.DetourDestination = distance <= b.Radius ? b.Pos : edge + (b.Pos - edge).Normalized() * b.Radius;
			b.DetourChewLeft = def.DetourChewTime;
			b.ChewFxTimer = 0;
			return true;
		}
		return false;
	}

	void StepDetour(Bug b, float dt)
	{
		var target = b.DetourTarget >= 0 && b.DetourTarget < Components.Count ? Components[b.DetourTarget] : null;
		if (b.Detour != DetourState.Returning && (target == null || !target.Chewable))
			ReturnToRoute(b);

		switch (b.Detour)
		{
			case DetourState.Going:
				if (MoveDetouringBug(b, b.DetourDestination, dt))
				{
					b.Detour = DetourState.Chewing;
					b.ChewTarget = target.Index;
					b.Angle = (target.Rect.GetCenter() - b.Pos).Angle();
				}
				break;
			case DetourState.Chewing:
				// Clamp the last bite to the remaining time: never exceed the configured budget.
				var biteTime = Mathf.Min(dt, b.DetourChewLeft);
				ChewDetourTarget(b, target, biteTime);
				b.DetourChewLeft = Mathf.Max(b.DetourChewLeft - dt, 0);
				if (target.Destroyed || b.DetourChewLeft <= 0)
					ReturnToRoute(b);
				break;
			case DetourState.Returning:
				if (MoveDetouringBug(b, b.DetourExit, dt))
				{
					b.Detour = DetourState.OnRoute;
					b.DetourTarget = -1;
					b.DetourCooldown = Mathf.Max(b.Def.DetourCooldown, 0);
				}
				break;
		}
	}

	static void ReturnToRoute(Bug b)
	{
		b.Detour = DetourState.Returning;
		b.ChewTarget = -1;
	}

	static bool MoveDetouringBug(Bug b, Vector2 destination, float dt)
	{
		var delta = destination - b.Pos;
		var distance = delta.Length();
		if (distance > 0)
			b.Angle = delta.Angle();
		var step = Mathf.Max(b.Def.Speed * b.SlowFactor * dt, 0);
		if (distance <= step)
		{
			b.Pos = destination;
			return true;
		}
		b.Pos += delta / distance * step;
		return false;
	}

	void ChewDetourTarget(Bug b, BoardComponent target, float dt)
	{
		target.Hp -= b.Def.ChewDps * b.SlowFactor * dt;
		b.ChewFxTimer -= dt;
		if (b.ChewFxTimer <= 0)
		{
			b.ChewFxTimer = 0.35f;
			Events.Add(new SimEvent(EventKind.Chew, target.Index, target.ClosestPoint(b.Pos), Type: b.Def.Id));
		}
		if (target.Hp > 0)
			return;
		target.Hp = 0;
		target.Destroyed = true;
		if (Mode == MatchMode.Versus)
			Energy = Mathf.Min(Energy + target.MaxHp * Rules.ComponentEnergy, Rules.EnergyCap);
		Events.Add(new SimEvent(EventKind.ComponentDestroyed, target.Index, target.Rect.GetCenter(), target.MaxHp, target.Data.Type));
	}
}
