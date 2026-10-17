using HardwareDefence.Data;
using Godot;

namespace HardwareDefence.Sim;

/// <summary>Turret behaviours and projectiles (converted from the prototype: 30 steps = 1 s).</summary>
public sealed partial class Match
{
	void StepTurret(Turret t, float dt)
	{
		switch (t.Def.Behavior)
		{
			case "blaster":
				StepBlaster(t, dt);
				break;
			case "mortar":
				StepMortar(t, dt);
				break;
			case "burst":
				StepBurst(t, dt);
				break;
			case "laser":
				StepLaser(t, dt);
				break;
			case "freezer":
				StepFreezer(t, dt);
				break;
		}
	}

	static void Aim(Turret t, Vector2 target, float dt, float speed)
	{
		var wanted = (target - t.Pos).Angle();
		t.HeadAngle = Mathf.LerpAngle(t.HeadAngle, wanted, 1 - Mathf.Exp(-speed * dt));
	}

	/// <summary>Slow sweep while there is nothing to shoot.</summary>
	void IdleScan(Turret t, float dt)
	{
		var wanted = Mathf.Sin(Time * 0.7f + t.Cell.X) * 1.2f + t.Cell.Y;
		t.HeadAngle = Mathf.LerpAngle(t.HeadAngle, wanted, 1 - Mathf.Exp(-2 * dt));
	}

	void StepBlaster(Turret t, float dt)
	{
		t.Cooldown -= dt;
		var target = FurthestInRange(t.Pos, t.Range);
		if (target == null)
		{
			IdleScan(t, dt);
			return;
		}
		Aim(t, target.Pos, dt, 14);
		if (t.Cooldown > 0)
			return;
		t.Cooldown = t.Stat("cooldown");
		t.HeadAngle = (target.Pos - t.Pos).Angle();
		var dir = Vector2.FromAngle(t.HeadAngle);
		AddProjectile(new Projectile
		{
			Kind = ProjectileKind.Bullet, Source = t.Def.Id, Pos = t.Muzzle(30), Vel = dir * t.Stat("projectile_speed"),
			Damage = t.Stat("damage"), Size = 4, MaxDistance = t.Range * 2.5f,
		});
		Events.Add(new SimEvent(EventKind.TurretFired, t.Id, t.Muzzle(30), Type: t.Def.Id));
	}

	void StepMortar(Turret t, float dt)
	{
		t.Cooldown -= dt;
		var target = Nearest(t.Pos);
		if (target == null)
		{
			IdleScan(t, dt);
			return;
		}
		Aim(t, target.Pos, dt, 6);
		if (t.Cooldown > 0 || FurthestInRange(t.Pos, t.Range) == null)
			return;
		t.Cooldown = t.Stat("cooldown");
		var start = t.Muzzle(28);
		AddProjectile(new Projectile
		{
			Kind = ProjectileKind.Bomb, Source = t.Def.Id, Pos = start, Start = start, Target = target.Pos,
			Duration = Mathf.Max(start.DistanceTo(target.Pos) / Mathf.Max(t.Stat("projectile_speed"), 1), 0.15f),
			Damage = t.Stat("damage"), Radius = t.Stat("blast_radius"),
		});
		Events.Add(new SimEvent(EventKind.TurretFired, t.Id, start, Type: t.Def.Id));
	}

	void StepBurst(Turret t, float dt)
	{
		t.Cooldown -= dt;
		t.Spin = Mathf.Lerp(t.Spin, 0.6f, dt * 2);
		t.HeadAngle = Mathf.Wrap(t.HeadAngle + t.Spin * dt, -Mathf.Pi, Mathf.Pi);
		if (t.Cooldown > 0 || FurthestInRange(t.Pos, t.Range) == null)
			return;
		t.Cooldown = t.Stat("cooldown");
		var shots = Mathf.Max((int)t.Stat("shots"), 1);
		var offset = t.Alternate ? Mathf.Pi / shots : 0;
		t.Alternate = !t.Alternate;
		for (var i = 0; i < shots; i++)
		{
			var dir = Vector2.FromAngle(Mathf.Tau * i / shots + offset);
			AddProjectile(new Projectile
			{
				Kind = ProjectileKind.Bullet, Source = t.Def.Id, Pos = t.Pos + dir * 20, Vel = dir * t.Stat("projectile_speed"),
				Damage = t.Stat("damage"), Size = 3, MaxDistance = t.Range,
			});
		}
		t.Spin = 9;
		Events.Add(new SimEvent(EventKind.TurretFired, t.Id, t.Pos, Type: t.Def.Id));
	}

	/// <summary>Start and end of a laser beam, in board pixels.</summary>
	public static (Vector2 From, Vector2 To) Beam(Turret t)
	{
		var dir = Vector2.FromAngle(t.Direction);
		return (t.Pos + dir * 24, t.Pos + dir * (24 + t.Stat("beam_length")));
	}

	public const float BeamWidth = 6;

	void StepLaser(Turret t, float dt)
	{
		var (from, to) = Beam(t);
		var dps = t.Stat("dps");
		foreach (var b in Bugs)
		{
			if (b.Gone)
				continue;
			var closest = Geometry2D.GetClosestPointToSegment(b.Pos, from, to);
			if (closest.DistanceTo(b.Pos) <= b.Radius + BeamWidth * 0.5f)
				DamageBug(b, dps * dt);
		}
	}

	void StepFreezer(Turret t, float dt)
	{
		t.HeadAngle = Mathf.Wrap(t.HeadAngle + 7 * dt, -Mathf.Pi, Mathf.Pi);
		t.Cooldown -= dt;
		if (t.Cooldown > 0)
			return;
		for (var i = 0; i < Turret.OrbSlots.Length; i++)
		{
			t.NextSlot = (t.NextSlot + 1) % Turret.OrbSlots.Length;
			var orb = FindProjectile(t.Orbs[t.NextSlot]);
			if (orb != null && !orb.Launched)
				continue;
			var pos = t.Pos + Turret.OrbSlots[t.NextSlot];
			var p = AddProjectile(new Projectile
			{
				Kind = ProjectileKind.Orb, Source = t.Def.Id, Pos = pos, Anchor = pos, OwnerId = t.Id,
				SlowFactor = t.Stat("slow_factor"), SlowDuration = t.Stat("slow_duration"), Speed = t.Stat("projectile_speed"),
				SearchRange = t.Range, Phase = t.NextSlot,
			});
			t.Orbs[t.NextSlot] = p.Id;
			t.Cooldown = t.Stat("cooldown");
			Events.Add(new SimEvent(EventKind.TurretFired, t.Id, pos, Type: t.Def.Id));
			return;
		}
		// Every slot is busy: check again next step, like the original.
		t.Cooldown = 0;
	}

	void StepProjectile(Projectile p, float dt)
	{
		switch (p.Kind)
		{
			case ProjectileKind.Bullet:
				StepBullet(p, dt);
				break;
			case ProjectileKind.Bomb:
				StepBomb(p, dt);
				break;
			case ProjectileKind.Orb:
				StepOrb(p, dt);
				break;
		}
	}

	void StepBullet(Projectile p, float dt)
	{
		var from = p.Pos;
		var step = p.Vel * dt;
		p.Pos += step;
		p.Travelled += step.Length();
		// Test the whole segment travelled this step so fast shots can't skip over a bug.
		foreach (var b in Bugs)
		{
			if (b.Gone)
				continue;
			var hit = Geometry2D.GetClosestPointToSegment(b.Pos, from, p.Pos);
			if (hit.DistanceTo(b.Pos) <= b.Radius + p.Size)
			{
				DamageBug(b, p.Damage);
				Events.Add(new SimEvent(EventKind.BulletHit, p.Id, hit, Type: p.Source));
				p.Done = true;
				return;
			}
		}
		var bounds = new Rect2(Vector2.One * -64, Grid.Size + Vector2.One * 128);
		if (p.Travelled >= p.MaxDistance || !bounds.HasPoint(p.Pos))
			p.Done = true;
	}

	void StepBomb(Projectile p, float dt)
	{
		p.T += dt / p.Duration;
		if (p.T >= 1)
		{
			foreach (var b in Bugs)
				if (!b.Gone && p.Target.DistanceTo(b.Pos) <= p.Radius + b.Radius * 0.5f)
					DamageBug(b, p.Damage);
			Events.Add(new SimEvent(EventKind.Explosion, p.Id, p.Target, p.Radius, p.Source));
			p.Done = true;
			return;
		}
		p.Pos = p.Start.Lerp(p.Target, p.T);
		p.Height = Mathf.Sin(p.T * Mathf.Pi);
	}

	void StepOrb(Projectile p, float dt)
	{
		p.Phase += dt;
		var target = p.TargetId != 0 ? FindBug(p.TargetId) : null;
		if (target == null)
		{
			target = Nearest(p.Pos, p.SearchRange);
			p.TargetId = target?.Id ?? 0;
			if (target != null)
				p.Launched = true;
		}
		if (target == null)
		{
			if (!p.Launched)
				p.Pos = p.Anchor + new Vector2(0, Mathf.Sin(p.Phase * 3) * 2);
			return;
		}
		var toTarget = target.Pos - p.Pos;
		var dist = toTarget.Length();
		if (dist <= target.Radius + 5)
		{
			target.ApplySlow(p.SlowFactor, p.SlowDuration);
			Events.Add(new SimEvent(EventKind.OrbPopped, p.Id, p.Pos, Type: p.Source));
			p.Done = true;
			return;
		}
		p.Pos += toTarget / dist * Mathf.Min(p.Speed * dt, dist);
	}
}
