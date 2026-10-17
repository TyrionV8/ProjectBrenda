using System.Linq;
using System.Collections.Generic;
using Godot;

namespace HardwareDefence.Sim;

/// <summary>Support runs on the same fixed steps as weapons; locked support is inactive.</summary>
public sealed partial class Match
{
	readonly List<Turret> _firewalls = new();
	void RefreshScans()
	{
		var scanners = Turrets.Where(t => t.Def.Behavior == "antivirus" && t.Locked <= 0).ToArray();
		foreach (var b in Bugs)
		{
			if (b.Def.Ability is not ("rootkit" or "trojan")) continue;
			var scanned = !b.Gone && scanners.Any(t => t.Def.Behavior == "antivirus" && t.Locked <= 0 && t.Pos.DistanceTo(b.Pos) <= t.Range);
			b.Exposed = scanned && b.Def.Ability == "rootkit";
			if (!scanned || b.Def.Ability != "trojan" || b.Revealed)
				continue;
			b.Revealed = true;
			Events.Add(new SimEvent(EventKind.BossAbility, b.Id, b.Pos, Type: "reveal"));
		}
	}

	void RecoverTurrets(float dt)
	{
		// Freeze eligibility before changing timers, so turret construction order cannot
		// make a just-unlocked backup accelerate the rest of this same step.
		var backups = Turrets.Where(t => t.Def.Behavior == "backup" && t.Locked <= 0).ToArray();
		foreach (var t in Turrets)
		{
			t.LockImmunity = Mathf.Max(0, t.LockImmunity - dt);
			if (t.Locked <= 0) continue;
			var backup = backups.Where(b => b.Pos.DistanceTo(t.Pos) <= b.Range)
				.OrderByDescending(b => b.Stat("recovery_rate")).FirstOrDefault();
			// Backup hardware boots its own recovery image first, even after a global panic.
			// It remains locked and cannot help neighbours until this intrinsic reboot finishes.
			if (t.Def.Behavior == "backup" && (backup == null || t.Stat("recovery_rate") > backup.Stat("recovery_rate")))
				backup = t;
			t.Locked = Mathf.Max(0, t.Locked - dt * (backup?.Stat("recovery_rate") ?? 1));
			if (t.Locked <= 0 && backup != null)
			{
				t.LockImmunity = backup.Stat("immunity");
				Events.Add(new SimEvent(EventKind.TurretAbility, backup.Id, t.Pos, backup.Range, "restore"));
			}
		}
	}

	void ProtectShot(Projectile p, Vector2 from, Vector2 to)
	{
		if (p.Protected) return;
		foreach (var t in _firewalls)
		{
			if (t.Locked > 0) continue;
			var nearest = Geometry2D.GetClosestPointToSegment(t.Pos, from, to);
			if (nearest.DistanceTo(t.Pos) > t.Range) continue;
			p.Protected = true;
			Events.Add(new SimEvent(EventKind.TurretAbility, t.Id, nearest, t.Range, "block"));
			return;
		}
	}

	void StepCounter(Turret t, float dt)
	{
		IdleScan(t, dt);
		t.Cooldown -= dt;
		if (t.Def.Behavior == "technician")
		{
			var c = Components.Where(c => c.MaxHp > 0 && (c.Destroyed || c.Hp < c.MaxHp) && c.DistanceTo(t.Pos) <= t.Range)
				.OrderBy(c => c.Destroyed ? 1 : 0).ThenBy(c => c.Ratio).ThenBy(c => c.Index).FirstOrDefault();
			if (c == null) return;
			var repair = t.Stat("repair_rate") * dt;
			if (c.Destroyed)
			{
				c.RepairProgress = Mathf.Min(c.MaxHp, c.RepairProgress + repair);
				if (c.RepairProgress >= c.MaxHp)
				{
					c.Hp = c.MaxHp;
					c.Destroyed = false;
					c.RepairProgress = 0;
				}
			}
			else c.Hp = Mathf.Min(c.MaxHp, c.Hp + repair);
			if (t.Cooldown <= 0)
			{
				t.Cooldown = t.Stat("cooldown");
				Aim(t, c.Rect.GetCenter(), dt, 14);
				Events.Add(new SimEvent(EventKind.TurretAbility, t.Id, c.Rect.GetCenter(), c.Index, "repair"));
			}
			return;
		}
		if (t.Cooldown > 0) return;
		if (t.Def.Behavior == "antivirus")
		{
			t.Cooldown = t.Stat("cooldown");
			Events.Add(new SimEvent(EventKind.TurretAbility, t.Id, t.Pos, t.Range, "scan"));
		}
		else if (t.Def.Behavior == "firewall")
		{
			var packet = BugsInRange(t.Pos, t.Range).FirstOrDefault(b => b.Def.Id == "packet");
			if (packet != null)
			{
				t.Cooldown = t.Stat("cooldown");
				DamageBug(packet, packet.Hp);
				Events.Add(new SimEvent(EventKind.TurretAbility, t.Id, packet.Pos, t.Range, "block"));
			}
		}
		else if (t.Def.Behavior == "backup")
		{
			var locked = Turrets.FirstOrDefault(b => b.Locked > 0 && b.Pos.DistanceTo(t.Pos) <= t.Range);
			if (locked != null)
			{
				t.Cooldown = t.Stat("cooldown");
				Events.Add(new SimEvent(EventKind.TurretAbility, t.Id, locked.Pos, t.Range, "restore"));
			}
		}
	}
}
