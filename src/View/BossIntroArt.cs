using Godot;

namespace HardwareDefence.View;

/// <summary>Small, transient attack vignettes, painted on the board's existing 3x pixel grid.</summary>
internal static class BossIntroArt
{
	static readonly Color Paper = new("d9eee4");
	static readonly Color InkDark = new("0a111b");
	static readonly Color Muted = new("617789");
	static readonly Color Green = new("80e8ae");
	static readonly Color Red = new("ff6775");
	static readonly Color Gold = new("ffd18a");
	static readonly Color Blue = new("71c5ed");

	internal static void Draw(CanvasItem canvas, BossIntroSequence sequence, Vector2 boardSize)
	{
		foreach (var live in sequence.Live)
			if (live.Tier >= 5) Presence(canvas, live, boardSize);
		var intro = sequence.Featured;
		if (intro == null || intro.Alpha <= 0) return;
		var frame = intro.Frame(boardSize);
		var origin = frame.Position / PixelWorld.Scale;
		var width = (int)frame.Size.X / PixelWorld.Scale;
		var height = (int)frame.Size.Y / PixelWorld.Scale;
		var accent = Accent(intro);
		var world = new Ink(canvas, Vector2.Zero, intro.Alpha);
		var p = new Ink(canvas, origin, intro.Alpha);

		Decorations(world, p, intro, boardSize, width, height, accent);
		p.Rect(2, 2, width, height, new Color(0, 0, 0, 0.35f));
		p.Rect(0, 0, width, height, new Color(InkDark, 0.91f));
		p.Frame(0, 0, width, height, accent);
		p.Rect(1, 1, width - 2, 8, new Color(accent, 0.17f));
		p.Rect(4, 4, 2, 2, accent);
		p.Text(9, 3, Title(intro.Def.Ability), Paper);
		p.Text(width - 24, 3, $"{intro.Tier:00}/08", accent);
		p.Rect(1, 9, width - 2, 1, new Color(accent, 0.45f));
		switch (intro.Def.Ability)
		{
			case "trojan": Download(p, intro, width); break;
			case "keylogger": Keystrokes(p, intro, width); break;
			case "mitm": Network(p, intro, width, height); break;
			case "ddos": Traffic(p, intro, width, height); break;
			case "rootkit": Processes(p, intro, width, height); break;
			case "ransomware": Ransom(p, intro, width, height); break;
			case "worm": Forks(p, intro, width, height); break;
			case "panic": Panic(p, intro, width, height); break;
		}
		// The closing line drains instead of moving/scaling the pixel panel.
		p.Rect(1, height - 2, Mathf.Max(1, (int)((width - 2) * (1 - intro.Age / intro.Duration))), 1, accent);
	}

	static Color Accent(BossIntro intro) => intro.Def.Ability switch
	{
		"trojan" or "mitm" or "rootkit" or "worm" => Green,
		"ransomware" => Gold, "panic" => Blue, _ => Red,
	};

	static string Title(string ability) => ability switch
	{
		"trojan" => "DOWNLOAD MANAGER", "keylogger" => "INPUT HOOK",
		"mitm" => "NETWORK TAP", "ddos" => "TRAFFIC MONITOR",
		"rootkit" => "PROCESS LIST", "ransomware" => "READ_ME.TXT",
		"worm" => "FORK()", "panic" => "FATAL EXCEPTION", _ => "UNKNOWN PROCESS",
	};

	static void Download(Ink p, BossIntro intro, int width)
	{
		p.Text(9, 15, "FREE_RAM.EXE", Paper);
		p.Text(9, 23, "MEMORY BOOSTER", Muted);
		var percent = Mathf.Min(99, (int)(intro.Age / intro.Duration * 100));
		p.Frame(9, 31, width - 35, 6, Green);
		p.Rect(11, 33, (int)((width - 39) * percent / 100f), 2, Green);
		p.Rect(width - 22, 30, 15, 8, new Color(Green, 0.2f));
		p.Text(width - 20, 32, "RUN", Green);
		// A friendly download icon, with a suspicious little red status pixel.
		p.Frame(width - 24, 14, 14, 13, Muted);
		p.Rect(width - 18, 16, 2, 6, Paper);
		p.Rect(width - 20, 20, 6, 1, Paper);
		p.Rect(width - 19, 21, 4, 1, Paper);
		p.Rect(width - 18, 22, 2, 1, Paper);
		p.Rect(width - 13, 15, 2, 2, Red);
	}

	static void Keystrokes(Ink p, BossIntro intro, int width)
	{
		const string login = "> LOGIN: ADMIN";
		const string password = "> PASS: ********";
		var keys = (int)(intro.Age * 23);
		var first = login[..Mathf.Min(login.Length, keys)];
		var second = password[..Mathf.Clamp(keys - login.Length, 0, password.Length)];
		p.Text(9, 15, first, Green);
		p.Text(9, 24, second, Paper);
		if ((int)(intro.Age * 8) % 2 == 0)
			p.Rect(10 + MicroFont.Width(keys < login.Length ? first : second), keys < login.Length ? 15 : 24, 3, 5, Paper);
		p.Rect(9, 35, 2, 2, Red);
		p.Text(14, 34, "REC", Red);
		p.Text(width - 70, 34, $"{Mathf.Min(keys, 99):00} KEYS CAPTURED", Muted);
	}

	static void Network(Ink p, BossIntro intro, int width, int height)
	{
		var middle = width / 2;
		p.Elbow(16, 24, middle, 28, Muted);
		p.Elbow(middle, 28, width - 20, 20, Muted);
		p.Elbow(middle, 28, width - 20, 34, Muted);
		p.Frame(12, 21, 8, 7, Paper);
		p.Frame(middle - 4, 25, 9, 8, Red);
		p.Frame(width - 24, 17, 8, 7, Green);
		p.Frame(width - 24, 31, 8, 7, Green);
		p.Text(9, 13, "SRC", Muted);
		p.Text(middle - 11, 16, "PROXY", Red);
		p.Text(width - 40, 11, "CPU", Muted);
		var packet = (int)(intro.Age * 26) % (width - 38);
		p.Rect(19 + packet, 27, 3, 2, packet > middle - 19 ? Red : Green);
		p.Rect(middle - 8, 23, 2, 12, Red);
		p.Text(9, height - 9, "ROUTE HIJACKED", Green);
	}

	static void Traffic(Ink p, BossIntro intro, int width, int height)
	{
		var bottom = height - 14;
		p.Rect(10, 14, 1, bottom - 13, Muted);
		p.Rect(10, bottom, width - 20, 1, Muted);
		for (var x = 14; x < width - 12; x += 8)
		{
			var index = (x - 14) / 8;
			var rise = 3 + (int)(intro.Age * 12) + index % 5 * 3;
			var bar = Mathf.Min(bottom - 13, rise);
			p.Rect(x, bottom - bar, 5, bar, bar > 13 ? Red : Gold);
			if (rise > bottom - 13) p.Rect(x + 1, 11 + index % 2, 2, 2, Paper);
		}
		for (var x = 12; x < width - 12; x += 6) p.Rect(x, 24, 3, 1, Gold);
		p.Text(11, height - 9, "9999 PKT/S", Paper);
		p.Text(width - 43, height - 9, "OVERFLOW", Red);
	}

	static void Processes(Ink p, BossIntro intro, int width, int height)
	{
		var step = (int)(intro.Age * 9);
		var names = new[] { "0042 KERNEL.SYS", "0841 INPUT.EXE", "0999 ROOTKIT.KO", "1204 WATCHDOG" };
		for (var i = 0; i < names.Length; i++)
		{
			var y = 14 + i * 8;
			if ((step + i * 3) % 5 == 0 || i == 2 && step % 3 != 0)
			{
				p.Rect(9, y + 2, width - 25 - i * 9, 1, new Color(Green, 0.15f));
				continue;
			}
			p.Text(9 + (i == 2 ? step % 3 * 2 : 0), y, names[i], i == 2 ? Green : Muted);
			p.Rect(width - 13, y + 1, 2, 2, i == 2 ? Red : Green);
		}
		p.Text(9, height - 9, "PID NOT FOUND", Green);
	}

	static void Ransom(Ink p, BossIntro intro, int width, int height)
	{
		Lock(p, 13, 19, Gold);
		p.Text(35, 15, "YOUR FILES ARE LOCKED", Paper);
		p.Text(35, 24, "PAY 0.5 BTC", Gold);
		p.Text(35, 34, "KEY EXPIRES IN", Muted);
		var countdown = Mathf.Max(1, 7 - (int)intro.Age);
		p.Text(width - 34, 33, $"00:0{countdown}", Gold);
		for (var x = 9; x < width - 9; x += 7)
			p.Rect(x, height - 12, 4, 2, (x / 7) % 2 == 0 ? Red : Gold);
		p.Text(9, height - 8, "DECRYPTION KEY REQUIRED", Red);
	}

	static void Forks(Ink p, BossIntro intro, int width, int height)
	{
		var copies = Mathf.Min(9, 1 + (int)(intro.Age * 3));
		p.Text(9, 14, $"> FORK()  CHILDREN: {copies:00}", Paper);
		p.Elbow(width / 2, 22, 23, 30, Muted);
		p.Elbow(width / 2, 22, width - 30, 30, Muted);
		for (var i = 0; i < 3; i++)
		{
			var x = 12 + i * (width - 24) / 3;
			p.Frame(x, 28, 43, 18, new Color(Green, 0.65f));
			Worm(p, x + 4, 32, Green);
			p.Text(x + 18, 32, $"{4001 + i}", Muted);
			p.Rect(x + 18, 40, 16 + (i + copies) % 7, 1, Green);
		}
		p.Text(9, height - 9, "COPIES SPREADING", Green);
	}

	static void Panic(Ink p, BossIntro intro, int width, int height)
	{
		var count = Mathf.Max(1, 3 - (int)intro.Age);
		p.Rect(8, 14, 28, 32, new Color(Blue, 0.16f));
		p.Text(16, 17, count.ToString(), Paper, 4);
		p.Text(44, 16, "STOP 0X0000007E", Blue);
		p.Text(44, 25, "KERNEL STATE: UNSTABLE", Red);
		p.Text(44, 34, "MEMORY DUMP ARMED", Paper);
		p.Text(10, height - 13, ":(  SYSTEM FAILURE IMMINENT", Blue);
		p.Rect(1, 11 + (int)(intro.Age * 20) % (height - 14), width - 2, 1, new Color(Blue, 0.35f));
	}

	static void Decorations(Ink world, Ink p, BossIntro intro, Vector2 size, int width, int height, Color accent)
	{
		// More fragments per tier, never a solid full-screen mask. This lower band is
		// clear of the warning text, the versus bar and the build/cancel controls.
		var art = (size / PixelWorld.Scale).Floor();
		var frame = intro.Frame(size);
		for (var i = 0; i < intro.Tier * 2; i++)
		{
			var x = 12 + (i * 59 + (int)(intro.Age * (7 + intro.Tier))) % ((int)art.X - 28);
			var y = 146 + (i * 19 + (int)(intro.Age * 11)) % 80;
			if (frame.HasPoint(new Vector2(x, y) * PixelWorld.Scale)) continue;
			world.Rect(x, y, 1 + i % 3, 1, new Color(accent, 0.35f));
		}
		if (intro.Def.Ability == "worm")
		{
			var count = Mathf.Min(6, 2 + (int)(intro.Age * 3));
			for (var i = count; i > 0; i--)
			{
				var dx = (i % 2 == 0 ? -1 : 1) * (5 + i * 3);
				var dy = -3 - i * 2;
				p.Frame(dx, dy, width, height, new Color(Green, 0.12f + (6 - i) * 0.035f));
				p.Text(dx + 7, dy + 3, $"FORK() PID {4000 + i}", new Color(Green, 0.3f));
			}
			for (var i = 0; i < count; i++)
			{
				var x = 20 + i * 47;
				var y = 51 + i % 2 * 14;
				Worm(world, x, y, new Color(Green, 0.35f));
				world.Text(x + 13, y, $"PID {4001 + i}", new Color(Green, 0.35f));
			}
		}
		if (intro.Def.Ability != "panic") return;
		var tick = (int)(intro.Age * 14);
		for (var i = 0; i < 6; i++)
		{
			var y = i < 2 ? 43 + i * 15 : 147 + i * 13;
			var x = 9 + (tick * 23 + i * 67) % 110;
			world.Rect(x, y, (int)art.X - x - 12, 1 + (i + tick) % 3, new Color(Blue, 0.17f));
			world.Rect(x + 4, y + 2, 13 + i * 4, 1, new Color(Red, 0.65f));
		}
		// A short preview of the beloved blue screen, distinct from the real ability:
		// two 0.12-second flashes, restricted to a narrow strip of board.
		if (intro.Age is > 0.9f and < 1.02f or > 2.0f and < 2.12f)
		{
			world.Rect(12, 47, (int)art.X - 24, 25, new Color("1f3fbf", 0.45f));
			world.Text(22, 52, ":(  SYSTEM_THREAD_EXCEPTION_NOT_HANDLED", Paper);
			world.Text(22, 62, "PREPARING MEMORY DUMP...", Blue);
		}
	}

	static void Presence(CanvasItem canvas, BossIntro intro, Vector2 boardSize)
	{
		var art = (boardSize / PixelWorld.Scale).Floor();
		var position = (intro.Position / PixelWorld.Scale).Round();
		position.X = Mathf.Clamp(position.X, 30, art.X - 30);
		position.Y = Mathf.Clamp(position.Y, 38, art.Y - 24);
		var p = new Ink(canvas, position, 0.45f);
		var accent = Accent(intro);
		var tick = (int)(intro.Age * 5);
		var radius = 13 + intro.Tier;
		for (var side = -1; side <= 1; side += 2)
		{
			p.Rect(side * radius, -8, 1, 4, accent);
			p.Rect(side * radius - (side > 0 ? 3 : 0), -8, 4, 1, accent);
			p.Rect(side * radius, 5, 1, 4, accent);
			p.Rect(side * radius - (side > 0 ? 3 : 0), 9, 4, 1, accent);
		}
		if (intro.Def.Ability == "rootkit" && tick % 4 < 2)
			p.Text(-18 + tick % 3, 14, "PID --", Green);
		if (intro.Def.Ability == "ransomware")
		{
			p.Text(-25, 15, $"{(tick * 17) % 256:X2} LOCK {intro.Id % 256:X2}", Gold);
			p.Rect(-radius - 3, tick % 12 - 6, 2, 2, Red);
		}
		if (intro.Def.Ability == "worm")
		{
			Worm(p, -26 - tick % 3, 12, Green);
			Worm(p, 17 + tick % 3, 12, Green);
		}
		if (intro.Def.Ability == "panic")
		{
			p.Rect(-27, -4 + tick % 9, 54, 1, Blue);
			p.Text(-25, 15, "STACK CORRUPT", Blue);
		}
	}

	static void Lock(Ink p, int x, int y, Color color)
	{
		p.Frame(x + 3, y, 9, 9, color);
		p.Frame(x + 4, y + 1, 7, 8, color);
		p.Rect(x, y + 7, 15, 12, color);
		p.Rect(x + 6, y + 10, 3, 4, InkDark);
		p.Rect(x + 7, y + 14, 1, 2, InkDark);
	}

	static void Worm(Ink p, int x, int y, Color color)
	{
		for (var i = 0; i < 4; i++)
		{
			p.Rect(x + i * 3, y + i % 2, 3, 4, color);
			p.Rect(x + i * 3 + 1, y + 4 + i % 2, 1, 1, color);
		}
		p.Rect(x + 10, y + 1, 1, 1, Red);
	}

	readonly struct Ink
	{
		readonly CanvasItem _canvas;
		readonly Vector2 _origin;
		readonly float _alpha;
		internal Ink(CanvasItem canvas, Vector2 artOrigin, float alpha)
		{
			_canvas = canvas;
			_origin = artOrigin;
			_alpha = alpha;
		}
		internal void Rect(int x, int y, int width, int height, Color color)
		{
			if (width <= 0 || height <= 0) return;
			_canvas.DrawRect(new Rect2((_origin + new Vector2(x, y)) * PixelWorld.Scale,
				new Vector2(width, height) * PixelWorld.Scale), new Color(color, color.A * _alpha));
		}
		internal void Frame(int x, int y, int width, int height, Color color)
		{
			Rect(x, y, width, 1, color);
			Rect(x, y + height - 1, width, 1, color);
			Rect(x, y + 1, 1, height - 2, color);
			Rect(x + width - 1, y + 1, 1, height - 2, color);
		}
		internal void Text(int x, int y, string text, Color color, int scale = 1) =>
			MicroFont.Draw(_canvas, (_origin + new Vector2(x, y)) * PixelWorld.Scale,
				text, new Color(color, color.A * _alpha), PixelWorld.Scale * scale);
		internal void Elbow(int x, int y, int endX, int endY, Color color)
		{
			Rect(Mathf.Min(x, endX), y, Mathf.Abs(endX - x) + 1, 1, color);
			Rect(endX, Mathf.Min(y, endY), 1, Mathf.Abs(endY - y) + 1, color);
		}
	}
}
