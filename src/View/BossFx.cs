using System.Collections.Generic;
using HardwareDefence.Autoload;
using HardwareDefence.Data;
using HardwareDefence.Sim;
using Godot;

namespace HardwareDefence.View;

/// <summary>
/// What the bosses' attacks look like: process-name tags, a keylogger pulling keystrokes off
/// the keys, man-in-the-middle sniffing lines to the turrets, DDoS packet storms, a rootkit
/// glitching out of sight, ransomware encryption waves, worm forks and the kernel panic's blue
/// screen. Driven by snapshots (where the bosses are) and BossAbility events.
/// </summary>
public partial class BossFx : Node2D
{
	const int Px = PixelWorld.Scale;
	const string Keys = "QWERTYUIOPASDFGHJKLZXCVBNM1234567890";
	static readonly Color Red = new("ff4d5e");
	static readonly Color Green = new("3dff6a");
	static readonly Color Sniff = new("b8ffcf");
	static readonly Color Bsod = new("1f3fbf");

	public Fx Fx;
	public BoardView Board;
	/// <summary>The trojan dropped its disguise.</summary>
	public event System.Action Revealed;
	public float RouteLength = 1;

	readonly List<BugSnap> _bosses = new();
	readonly List<Vector2> _turrets = new();
	readonly List<Letter> _letters = new();
	readonly List<Ring> _rings = new();
	internal readonly BossIntroSequence Introductions = new();
	Node2D _introLayer;
	Node2D _screen;
	float _t;
	float _bsod;
	int _bsodBossId;
	internal float BlueScreenRemaining => _bsod;
	float _letterTimer;
	readonly RandomNumberGenerator _rng = new();

	sealed class Letter
	{
		public Vector2 From;
		public int Target;
		public char Char;
		public float T;
	}

	sealed class Ring
	{
		public Vector2 Pos;
		public float Radius;
		public float T;
	}

	public override void _Ready()
	{
		Material = FxUtil.Unshaded;
		ZIndex = 3;
		// A boss walking through its terminal window must remain visible. The board
		// is at 0, creatures/projectiles at 2, and the original process tags at 3.
		_introLayer = new Node2D { Name = "ArrivalVignettes", Material = FxUtil.Unshaded, ZIndex = -2 };
		_introLayer.Draw += () => BossIntroArt.Draw(_introLayer, Introductions, Board?.BoardSize ?? Grid.Size);
		AddChild(_introLayer);
		// The blue screen covers the whole board, above everything else.
		_screen = new Node2D { Material = FxUtil.Unshaded, ZIndex = 30 };
		_screen.Draw += DrawBlueScreen;
		AddChild(_screen);
	}

	public override void _ExitTree() => PostFx.SetBossGlitch(0);

	static EnemyDef Def(BugSnap b) => Catalog.Enemy(b.Type);

	public void Sync(Snapshot snap, float dt)
	{
		_t += dt;
		_bosses.Clear();
		foreach (var b in snap.Bugs)
			if (Def(b) is { Boss: true } && b.Hp > 0)
				_bosses.Add(b);
		Introductions.Sync(snap, dt);
		foreach (var arrival in Introductions.Arrivals)
		{
			Fx?.Sparks(arrival.Position, arrival.Def.Eye.Lerp(Colors.White, 0.25f), arrival.Particles,
				70 + arrival.Tier * 15, 0.3f + arrival.Tier * 0.04f);
			Fx?.Shake(arrival.Shake);
		}
		foreach (var intro in Introductions.Live)
			if (intro.Active && intro.Age >= 0.55f && !intro.AccentPlayed)
			{
				intro.AccentPlayed = true;
				Fx?.Shake(intro.Shake * 0.65f);
			}
		PostFx.SetBossGlitch(Introductions.Featured?.GlitchStrength ?? 0);
		_turrets.Clear();
		foreach (var t in snap.Turrets)
			_turrets.Add(Grid.CellCenter(new Vector2I(t.Cell[0], t.Cell[1])));

		// The keylogger pulls keystrokes off the keys (or the board) around it.
		_letterTimer -= dt;
		if (_letterTimer <= 0)
		{
			_letterTimer = 0.12f;
			foreach (var b in _bosses)
				if (Def(b).Ability == "keylogger")
					_letters.Add(new Letter { From = KeyNear(new Vector2(b.X, b.Y)), Target = b.Id, Char = Keys[_rng.RandiRange(0, Keys.Length - 1)] });
		}
		for (var i = _letters.Count - 1; i >= 0; i--)
		{
			_letters[i].T += dt / 0.55f;
			if (_letters[i].T >= 1 || Find(_letters[i].Target) == null)
				_letters.RemoveAt(i);
		}
		for (var i = _rings.Count - 1; i >= 0; i--)
		{
			_rings[i].T += dt / 0.7f;
			if (_rings[i].T >= 1)
				_rings.RemoveAt(i);
		}
		_bsod = Mathf.Max(_bsod - dt, 0);
		if (Find(_bsodBossId) == null) _bsod = 0;
		QueueRedraw();
		_introLayer?.QueueRedraw();
		_screen?.QueueRedraw();
	}

	BugSnap Find(int id) => _bosses.Find(b => b.Id == id);

	Vector2 KeyNear(Vector2 pos)
	{
		var best = pos + Vector2.FromAngle(_rng.Randf() * Mathf.Tau) * 90;
		var bestD = 170f;
		if (Board == null)
			return best;
		var start = _rng.RandiRange(0, Mathf.Max(Board.Components.Count - 1, 0));
		for (var k = 0; k < Board.Components.Count; k++)
		{
			var c = Board.Components[(start + k) % Board.Components.Count];
			if (c.Data.Type != "switch")
				continue;
			var d = c.Rect.GetCenter().DistanceTo(pos);
			if (d < bestD && d > 30)
				return c.Rect.GetCenter();
		}
		return best;
	}

	/// <summary>Effects for a BossAbility event. Returns the sound to play, if any.</summary>
	public string OnAbility(SimEvent e)
	{
		var at = e.Pos;
		switch (e.Type)
		{
			case "steal":
				Fx.FloatingText(at + new Vector2(0, -36), "-$1", Red);
				return "steal";
			case "intercept":
				Fx.Sparks(at, Red, 6, 90, 0.25f, 0.4f);
				if (_rng.Randf() < 0.3f)
					Fx.FloatingText(at, "INTERCEPTED", Red);
				return null;
			case "flood":
				Fx.Sparks(at, Red, 18, 160, 0.4f, 0.5f);
				Fx.FloatingText(at + new Vector2(0, -40), "SYN FLOOD", Red);
				return "pew";
			case "cloak":
				Fx.Sparks(at, Green, 14, 120, 0.35f, 0.4f);
				Fx.FloatingText(at + new Vector2(0, -36), "PID HIDDEN", Green);
				return "glitch";
			case "uncloak":
				Fx.GlowFlash(at, Green, 1.2f, 0.25f);
				Fx.FloatingText(at + new Vector2(0, -36), "ROOTKIT FOUND", Green);
				return null;
			case "encrypt":
				_rings.Add(new Ring { Pos = at, Radius = e.Value });
				Fx.FloatingText(at + new Vector2(0, -40), "FILES ENCRYPTED", new Color("f0c040"));
				return "lock";
			case "panic":
				// Snapshots are applied before events. A delayed event from a removed
				// boss must not bring its screen-covering attack back after death.
				if (Find(e.Id) == null) return null;
				_bsod = 1.1f;
				_bsodBossId = e.Id;
				Fx.Shake(9);
				return "crash";
			case "reveal":
				Revealed?.Invoke();
				Fx.Sparks(at, Red, 24, 180, 0.45f, 0.6f);
				Fx.GlowFlash(at, Red, 1.6f, 0.3f);
				Fx.FloatingText(at + new Vector2(0, -40), "PAYLOAD DETECTED", Red);
				Fx.Shake(4);
				return "glitch";
			case "payload":
				Fx.Explosion(at, 70);
				Fx.FloatingText(at + new Vector2(0, -40), $"PAYLOAD x{(int)e.Value}", Red);
				return "glitch";
			case "fork":
				Fx.GlowFlash(at, Green, 1.4f, 0.3f);
				Fx.Sparks(at, Green, 16, 140, 0.4f, 0.5f);
				Fx.FloatingText(at + new Vector2(0, -40), $"FORK() PID {4000 + (int)e.Id % 999}", Green);
				return "glitch";
		}
		return null;
	}

	public override void _Draw()
	{
		foreach (var b in _bosses)
		{
			var def = Def(b);
			var pos = new Vector2(b.X, b.Y);
			switch (def.Ability)
			{
				case "mitm":
					DrawSniffing(pos, def.AbilityRadius);
					break;
				case "ddos":
					DrawPacketStorm(pos, b.Id);
					break;
				case "rootkit" when b.Cloaked:
					DrawHidden(pos, b.Id);
					break;
			}
			DrawTag(b, def, pos);
		}
		foreach (var l in _letters)
		{
			var target = Find(l.Target);
			if (target == null)
				continue;
			var to = new Vector2(target.X, target.Y);
			var p = l.From.Lerp(to, Mathf.Ease(l.T, 2.2f)) + new Vector2(0, -Mathf.Sin(l.T * Mathf.Pi) * 18);
			MicroFont.DrawOutlined(this, PixelWorld.Snap(p), l.Char.ToString(), l.T < 0.8f ? Colors.White : Red, new Color(0, 0, 0, 0.8f));
		}
		foreach (var r in _rings)
			DrawEncryptionRing(r);
	}

	/// <summary>Process name above the boss, like a task manager entry.</summary>
	void DrawTag(BugSnap b, EnemyDef def, Vector2 pos)
	{
		var top = pos + new Vector2(0, -(BugBody.FrameSize(def).Y / 2 + 9) * Px);
		var name = def.Title ?? def.Name.ToUpperInvariant();
		var color = Red;
		switch (def.Ability)
		{
			case "trojan" when !b.Revealed:
				// Still pretending to be a harmless download.
				var percent = Mathf.Clamp((int)(b.Progress / Mathf.Max(RouteLength, 1) * 100), 0, 99);
				MicroFont.DrawOutlined(this, top, "FREE_RAM.EXE", Green, new Color(0, 0, 0, 0.85f));
				var bar = new Rect2(top + new Vector2(-16, 4) * Px, new Vector2(32, 3) * Px);
				DrawRect(bar, new Color(0, 0, 0, 0.8f));
				DrawRect(new Rect2(bar.Position + Vector2.One * Px, new Vector2(Mathf.Round(30 * percent / 100f), 1) * Px), Green);
				MicroFont.DrawOutlined(this, top + new Vector2(0, 10) * Px, $"INSTALLING {percent}%", Colors.White, new Color(0, 0, 0, 0.85f));
				return;
			case "rootkit" when b.Cloaked:
				name = "????????";
				color = Green;
				break;
			case "keylogger":
				if (Mathf.PosMod(_t, 1) < 0.6f)
					MicroFont.DrawOutlined(this, top + new Vector2(0, -7) * Px, "REC", Red, new Color(0, 0, 0, 0.85f));
				break;
			case "ransomware":
				if (Mathf.PosMod(_t * 0.7f, 1) < 0.65f)
					MicroFont.DrawOutlined(this, top + new Vector2(0, -7) * Px, "PAY 0.5 BTC", new Color("f0c040"), new Color(0, 0, 0, 0.85f));
				break;
		}
		MicroFont.DrawOutlined(this, top, name, color, new Color(0, 0, 0, 0.85f));
	}

	/// <summary>Dashed lines tapping every turret in reach, with stolen packets running along them.</summary>
	void DrawSniffing(Vector2 pos, float radius)
	{
		for (var i = 0; i < _turrets.Count; i++)
		{
			var tp = _turrets[i];
			var length = tp.DistanceTo(pos);
			if (length > radius * 2.4f || length < 1)
				continue;
			var dir = (pos - tp) / length;
			for (var d = 0f; d < length; d += 4 * Px)
				DrawRect(new Rect2(PixelWorld.Snap(tp + dir * d), Vector2.One * Px), new Color(Sniff, 0.8f));
			var k = Mathf.PosMod(_t * 0.9f + i * 0.37f, 1);
			DrawRect(new Rect2(PixelWorld.Snap(tp + dir * length * k) - Vector2.One * Px, Vector2.One * 2 * Px), Red);
		}
		// The interception field: a dotted ring that slowly turns.
		const int dots = 28;
		for (var i = 0; i < dots; i++)
		{
			if ((i + (int)(_t * 6)) % 3 == 0)
				continue;
			var a = Mathf.Tau * i / dots + _t * 0.4f;
			DrawRect(new Rect2(PixelWorld.Snap(pos + Vector2.FromAngle(a) * radius), Vector2.One * Px), new Color(Sniff, 0.9f));
		}
	}

	/// <summary>Junk packets swarming around the botnet.</summary>
	void DrawPacketStorm(Vector2 pos, int seed)
	{
		var rng = new RandomNumberGenerator { Seed = (ulong)(seed * 977 + Mathf.FloorToInt(_t * 8)) };
		for (var i = 0; i < 14; i++)
		{
			var a = rng.Randf() * Mathf.Tau;
			var r = rng.RandfRange(40, 90);
			var p = PixelWorld.Snap(pos + Vector2.FromAngle(a + _t * 2) * r);
			DrawRect(new Rect2(p, new Vector2(2, 1) * Px), new Color(Red, rng.RandfRange(0.4f, 0.9f)));
		}
		if (Mathf.PosMod(_t * 1.3f, 1) < 0.5f)
			MicroFont.DrawOutlined(this, PixelWorld.Snap(pos + new Vector2(30, 24)), "SYN", Red, new Color(0, 0, 0, 0.8f));
	}

	/// <summary>A hidden process: corner brackets and a few stray green glyph pixels.</summary>
	void DrawHidden(Vector2 pos, int seed)
	{
		var c = new Color(Green, 0.5f + 0.3f * Mathf.Sin(_t * 20));
		const int s = 13;
		foreach (var (sx, sy) in new[] { (-1, -1), (1, -1), (-1, 1), (1, 1) })
		{
			var corner = PixelWorld.Snap(pos + new Vector2(sx, sy) * s * Px);
			DrawRect(new Rect2(corner - new Vector2(sx > 0 ? 3 : 0, 0) * Px, new Vector2(3, 1) * Px), c);
			DrawRect(new Rect2(corner - new Vector2(0, sy > 0 ? 3 : 0) * Px, new Vector2(1, 3) * Px), c);
		}
		var rng = new RandomNumberGenerator { Seed = (ulong)(seed * 131 + Mathf.FloorToInt(_t * 15)) };
		for (var i = 0; i < 6; i++)
			DrawRect(new Rect2(PixelWorld.Snap(pos + new Vector2(rng.RandiRange(-12, 12), rng.RandiRange(-12, 12)) * Px), Vector2.One * Px), c);
	}

	/// <summary>An expanding ring of scrambled hex: the ransomware's encryption wave.</summary>
	void DrawEncryptionRing(Ring r)
	{
		var radius = r.Radius * Mathf.Ease(r.T, 0.5f);
		var alpha = 1 - r.T;
		var count = Mathf.Max((int)(radius / 22), 6);
		var rng = new RandomNumberGenerator { Seed = (ulong)Mathf.FloorToInt(_t * 10) };
		for (var i = 0; i < count; i++)
		{
			var p = PixelWorld.Snap(r.Pos + Vector2.FromAngle(Mathf.Tau * i / count) * radius);
			MicroFont.DrawOutlined(this, p, $"{rng.RandiRange(0, 255):X2}", new Color(1, 0.3f, 0.35f, alpha), new Color(0, 0, 0, 0.7f * alpha));
		}
		DrawArc(r.Pos, radius, 0, Mathf.Tau, 48, new Color(Red, 0.6f * alpha), Px);
	}

	/// <summary>The kernel panic's blue screen of death, briefly over the whole board.</summary>
	void DrawBlueScreen()
	{
		if (_bsod <= 0 || Board == null)
			return;
		var size = Board.BoardSize;
		var flicker = _bsod > 0.9f ? 1 : Mathf.PosMod(_bsod * 9, 1) < 0.8f ? 1 : 0.4f;
		var alpha = Mathf.Min(_bsod / 0.4f, 1) * 0.72f * flicker;
		_screen.DrawRect(new Rect2(Vector2.Zero, size), new Color(Bsod, alpha));
		var rng = new RandomNumberGenerator { Seed = (ulong)Mathf.FloorToInt(_bsod * 20) };
		for (var i = 0; i < 7; i++)
		{
			var y = rng.RandiRange(0, (int)(size.Y / Px)) * Px;
			_screen.DrawRect(new Rect2(0, y, size.X, rng.RandiRange(1, 3) * Px), new Color(1, 1, 1, 0.12f * alpha));
		}
		var white = new Color(1, 1, 1, Mathf.Min(alpha * 1.4f, 1));
		var left = new Vector2(16, 40) * Px;
		MicroFont.Draw(_screen, left, ":(", white, Px * 5);
		MicroFont.Draw(_screen, left + new Vector2(0, 34) * Px, "YOUR BOARD RAN INTO A PROBLEM AND NEEDS TO RESTART.", white, Px);
		MicroFont.Draw(_screen, left + new Vector2(0, 44) * Px, "*** STOP: 0X0000007E (0XC0000005, 0XFFFFF880)", white, Px);
		MicroFont.Draw(_screen, left + new Vector2(0, 52) * Px, "SYSTEM_THREAD_EXCEPTION_NOT_HANDLED", white, Px);
		MicroFont.Draw(_screen, left + new Vector2(0, 66) * Px, $"DUMPING MEMORY... {Mathf.Clamp((int)((1.1f - _bsod) * 90), 0, 99)}%", white, Px);
	}
}
