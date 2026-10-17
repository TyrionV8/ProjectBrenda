using System;
using System.Collections.Generic;
using HardwareDefence.Data;
using Godot;

namespace HardwareDefence.View;

/// <summary>
/// Pixel-art board components, painted into a <see cref="PixelCanvas"/> in art pixels (a cell
/// is 16 px), plus their damage states. The canvas has a <see cref="Pad"/> pixel margin for
/// the drop shadow, so the art of a component at (0, 0) starts at (Pad, Pad).
/// </summary>
public static class ComponentArt
{
	public const int Pad = 3;
	const int C = BoardView.CellPx;

	static readonly Color Gold = new("d9a441");
	static readonly Color GoldLight = new("ffe7a3");
	static readonly Color GoldDark = new("7a5212");
	static readonly Color Silver = new("aeb7c2");
	static readonly Color SilverLight = new("dfe5ec");
	static readonly Color SilverDark = new("6d7682");
	static readonly Color Black = new("16191e");
	static readonly Color BlackLight = new("2c323b");
	static readonly Color Shadow = new(0, 0, 0, 0.42f);

	static readonly Dictionary<string, ImageTexture> Cache = new();

	/// <summary>Main colour of a component (used for debris when it's chewed).</summary>
	public static Color MainColor(ComponentData c) => c.Type switch
	{
		"capacitor" => c.Color != null ? new Color(c.Color) : new Color("1e3f7a"),
		"connector" => new Color("d9d2c3"),
		"resistor" => new Color("d8c39a"),
		"led" or "led_5mm" => c.Color != null ? new Color(c.Color) : new Color("ff3344"),
		"crystal" or "shield" or "battery" or "port" => Silver,
		"heatsink" => c.Color != null ? new Color(c.Color) : new Color("3d4c5c"),
		"fingers" => Gold,
		"bga" => new Color("2e4d33"),
		_ => new Color("1d2026"),
	};

	/// <summary>Taller parts cast longer shadows.</summary>
	static int ShadowLength(string type) => type switch
	{
		"capacitor" or "heatsink" or "port" or "connector" or "battery" or "inductor" or "switch" or "ram_slot" or "pcie_slot" or "sata" or "header" => 2,
		"label" or "fingers" or "hole" => 0,
		_ => 1,
	};

	/// <summary>The undamaged art (cached), as a texture with the shadow margin.</summary>
	public static ImageTexture Texture(ComponentData c, BoardTheme theme, int seed)
	{
		var key = $"{c.Type}|{c.Size[0]}x{c.Size[1]}|{c.Color}|{c.Label}|{theme.Silk.ToHtml()}|{seed}";
		if (!Cache.TryGetValue(key, out var tex))
		{
			tex = Paint(c, theme, seed).ToTexture();
			if (Cache.Count > 400)
				Cache.Clear();
			Cache[key] = tex;
		}
		return tex;
	}

	public static PixelCanvas Paint(ComponentData c, BoardTheme theme, int seed)
	{
		var w = c.Size[0] * C;
		var h = c.Size[1] * C;
		var canvas = new PixelCanvas(w + Pad * 2, h + Pad * 2);
		var art = new PixelCanvas(w, h);
		var rng = new RandomNumberGenerator { Seed = (ulong)(seed * 31 + 5) };
		switch (c.Type)
		{
			case "chip": Chip(art, c.Label ?? $"U{seed % 90 + 1}", c.Color); break;
			case "bga": Bga(art, c.Label); break;
			case "header": Header(art); break;
			case "connector": Connector(art, c.Color); break;
			case "capacitor": Capacitor(art, c.Color); break;
			case "smd": Smd(art, rng); break;
			case "resistor": Resistor(art, seed); break;
			case "led": Led(art, c.Color); break;
			case "led_5mm": Led5mm(art, c.Color); break;
			case "crystal": Crystal(art, c.Label); break;
			case "inductor": Inductor(art, c.Label); break;
			case "mosfet": Mosfet(art); break;
			case "ram_slot": Slot(art, c.Color ?? "16191e", true, 0.42f); break;
			case "pcie_slot": Slot(art, c.Color ?? "121417", false, 0.18f); break;
			case "sata": Sata(art, c.Color); break;
			case "port": Port(art, c.Label); break;
			case "battery": Battery(art); break;
			case "heatsink": Heatsink(art, c.Color); break;
			case "shield": ShieldCan(art, c.Label); break;
			case "switch": KeySwitch(art, c.Color); break;
			case "jumper": Jumper(art, c.Color); break;
			case "fingers": Fingers(art); break;
			case "hole": Hole(art); break;
			case "label": art.TextCentered(w / 2f, h / 2f, Fit(c.Label ?? "", w - 2), theme.Silk); break;
			default: Chip(art, c.Type, null); break;
		}
		canvas.Draw(art, Pad, Pad);
		var shadow = ShadowLength(c.Type);
		if (shadow > 0)
			canvas.DropShadow(shadow, shadow, Shadow);
		return canvas;
	}

	/// <summary>Bites (pixels chewed away with a scorched rim), cracks and, once destroyed, charring.</summary>
	public static void Damage(PixelCanvas canvas, ComponentData c, float ratio, bool destroyed, int seed)
	{
		if (!destroyed && ratio >= 0.999f)
			return;
		var rng = new RandomNumberGenerator { Seed = (ulong)(seed * 7919 + 13) };
		var w = c.Size[0] * C;
		var h = c.Size[1] * C;
		var damage = destroyed ? 1f : 1f - ratio;
		var soot = new Color(0.07f, 0.04f, 0.02f, 0.95f);
		if (destroyed)
		{
			for (var y = 0; y < canvas.Height; y++)
				for (var x = 0; x < canvas.Width; x++)
				{
					var p = canvas.Get(x, y);
					if (p.A <= 0.01f)
						continue;
					var grey = (p.R + p.G + p.B) / 3;
					canvas.Set(x, y, new Color(grey * 0.35f + 0.02f, grey * 0.3f + 0.015f, grey * 0.28f + 0.01f, p.A));
				}
		}
		var bites = Mathf.CeilToInt(damage * (6 + (w + h) / 16));
		for (var i = 0; i < bites; i++)
		{
			var edge = rng.RandiRange(0, 3);
			var t = rng.RandfRange(0.1f, 0.9f);
			var p = edge switch
			{
				0 => new Vector2(w * t, 0),
				1 => new Vector2(w, h * t),
				2 => new Vector2(w * t, h),
				_ => new Vector2(0, h * t),
			} + Vector2.One * Pad;
			var radius = rng.RandfRange(1.6f, 3.4f) * (0.7f + damage * 0.6f);
			Bite(canvas, p, radius, soot, rng);
		}
		if (damage > 0.4f)
		{
			var cracks = Mathf.CeilToInt((damage - 0.4f) * 6);
			for (var i = 0; i < cracks; i++)
			{
				var x = rng.RandiRange(Pad + 2, Pad + w - 3);
				var y = rng.RandiRange(Pad + 2, Pad + h - 3);
				for (var k = 0; k < 6; k++)
				{
					if (canvas.Opaque(x, y))
						canvas.Set(x, y, soot);
					x += rng.RandiRange(-1, 1);
					y += rng.RandiRange(-1, 1);
				}
			}
		}
	}

	static void Bite(PixelCanvas canvas, Vector2 centre, float radius, Color soot, RandomNumberGenerator rng)
	{
		var r = (int)MathF.Ceiling(radius) + 1;
		for (var y = -r; y <= r; y++)
			for (var x = -r; x <= r; x++)
			{
				var px = (int)centre.X + x;
				var py = (int)centre.Y + y;
				var d = new Vector2(x, y).Length() + rng.RandfRange(-0.4f, 0.4f);
				if (d <= radius)
					canvas.Set(px, py, new Color(0, 0, 0, 0));
				else if (d <= radius + 1.1f && canvas.Opaque(px, py))
					canvas.Set(px, py, soot);
			}
	}

	static string Fit(string text, int width)
	{
		while (text.Length > 0 && MicroFont.Width(text) > width)
			text = text[..^1];
		return text;
	}

	static void Label(PixelCanvas a, float cx, float cy, string text, Color color, int maxWidth)
	{
		if (string.IsNullOrEmpty(text) || maxWidth < 3)
			return;
		a.TextCentered(cx, cy, Fit(text.ToUpperInvariant(), maxWidth), color);
	}

	/// <summary>Flat body with a lit top edge and a darker bottom edge.</summary>
	static void Body(PixelCanvas a, int x, int y, int w, int h, Color col)
	{
		a.Rect(x, y, w, h, col);
		a.HLine(x, x + w - 1, y, col.Lightened(0.14f));
		a.HLine(x, x + w - 1, y + h - 1, col.Darkened(0.3f));
	}

	// ------------------------------------------------------------------ the parts

	static void Chip(PixelCanvas a, string label, string color)
	{
		var w = a.Width;
		var h = a.Height;
		var wide = w >= h * 1.5f;
		var tall = h >= w * 1.5f;
		const int inset = 3;
		var bx = inset;
		var by = inset;
		var bw = w - inset * 2;
		var bh = h - inset * 2;
		if (!tall)
			for (var x = bx + 2; x < bx + bw - 2; x += 3)
			{
				a.Rect(x, by - 2, 2, 2, Silver);
				a.Rect(x, by + bh, 2, 2, Silver);
				a.Set(x, by - 2, SilverLight);
				a.Set(x + 1, by + bh + 1, SilverDark);
			}
		if (!wide)
			for (var y = by + 2; y < by + bh - 2; y += 3)
			{
				a.Rect(bx - 2, y, 2, 2, Silver);
				a.Rect(bx + bw, y, 2, 2, Silver);
				a.Set(bx - 2, y, SilverLight);
				a.Set(bx + bw + 1, y + 1, SilverDark);
			}
		var body = color != null ? new Color(color) : Black;
		Body(a, bx, by, bw, bh, body);
		a.Frame(bx, by, bw, bh, body.Lightened(0.08f));
		a.Disc(bx + 3, by + 3, 1.2f, body.Lightened(0.2f));
		Label(a, w / 2f, h / 2f + 0.5f, label, new Color(1, 1, 1, 0.5f), bw - 2);
	}

	static void Bga(PixelCanvas a, string label)
	{
		var w = a.Width;
		var h = a.Height;
		var sub = new Color("2e4d33");
		Body(a, 1, 1, w - 2, h - 2, sub);
		a.Frame(1, 1, w - 2, h - 2, new Color("4d7a52"));
		for (var x = 5; x < w - 6; x += 4)
		{
			a.Rect(x, 3, 2, 1, new Color("c9a26b"));
			a.Rect(x, h - 4, 2, 1, new Color("c9a26b"));
		}
		var dx = w / 5;
		var dy = h / 5;
		a.Rect(dx + 1, dy + 1, w - dx * 2, h - dy * 2, new Color(0, 0, 0, 0.35f));
		a.Rect(dx, dy, w - dx * 2, h - dy * 2, new Color("9aa4b1"));
		a.Rect(dx, dy, w - dx * 2, (h - dy * 2) / 2, new Color("b4bdc8"));
		a.Frame(dx, dy, w - dx * 2, h - dy * 2, new Color("5d6773"));
		a.HLine(dx + 1, w - dx - 2, dy + 1, new Color("d5dce4"));
		Label(a, w / 2f, h / 2f + 0.5f, label ?? "SOC", new Color(0.12f, 0.14f, 0.17f, 0.85f), w - dx * 2 - 2);
	}

	static void Header(PixelCanvas a)
	{
		var w = a.Width;
		var h = a.Height;
		Body(a, 2, 2, w - 4, h - 4, new Color("141518"));
		for (var cx = 0; cx < w / C; cx++)
			for (var cy = 0; cy < h / C; cy++)
			{
				var x = cx * C + C / 2 - 2;
				var y = cy * C + C / 2 - 2;
				a.Rect(x - 1, y - 1, 6, 6, new Color("24262b"));
				a.Rect(x, y, 4, 4, Gold);
				a.HLine(x, x + 3, y, GoldLight);
				a.VLine(x + 3, y + 1, y + 3, GoldDark);
			}
	}

	static void Connector(PixelCanvas a, string color)
	{
		var w = a.Width;
		var h = a.Height;
		var col = color != null ? new Color(color) : new Color("d9d2c3");
		Body(a, 2, 2, w - 4, h - 4, col);
		a.Frame(2, 2, w - 4, h - 4, col.Darkened(0.35f));
		for (var cx = 0; cx < w / C; cx++)
			for (var cy = 0; cy < h / C; cy++)
			{
				var x = cx * C + C / 2 - 3;
				var y = cy * C + C / 2 - 3;
				a.Rect(x, y, 6, 6, col.Darkened(0.25f));
				a.Rect(x + 1, y + 1, 4, 4, new Color("2a2622"));
				a.HLine(x + 1, x + 4, y + 4, new Color("4a443e"));
			}
	}

	static void Capacitor(PixelCanvas a, string color)
	{
		var cx = a.Width / 2f;
		var cy = a.Height / 2f;
		var r = MathF.Min(a.Width, a.Height) * 0.44f;
		var col = color != null ? new Color(color) : new Color("1e3f7a");
		a.Disc(cx, cy, r, col.Darkened(0.45f));
		a.Disc(cx, cy, r - 1, col);
		// Lit side of the sleeve.
		a.Ring(cx, cy, r - 1, 1.2f, col.Lightened(0.15f));
		for (var y = 0; y < a.Height; y++)
			for (var x = 0; x < a.Width; x++)
				if (x + y < cx + cy - r * 0.6f && a.Opaque(x, y))
					a.Tint(x, y, new Color(1.35f, 1.35f, 1.35f));
		// Aluminium top with the vent cross.
		var top = r * 0.66f;
		a.Disc(cx, cy, top, new Color("a9b4c2"));
		a.Disc(cx - 0.6f, cy - 0.6f, top - 1, new Color("cfd7e1"));
		var k = (int)MathF.Round(top * 0.7f);
		var ix = (int)MathF.Floor(cx);
		var iy = (int)MathF.Floor(cy);
		a.HLine(ix - k, ix + k - 1, iy, new Color("6b7684"));
		a.VLine(ix, iy - k, iy + k - 1, new Color("6b7684"));
		a.HLine(ix - k, ix + k - 1, iy - 1, new Color("6b7684"));
		a.VLine(ix - 1, iy - k, iy + k - 1, new Color("6b7684"));
	}

	static void Smd(PixelCanvas a, RandomNumberGenerator rng)
	{
		var n = rng.RandiRange(3, 5);
		for (var i = 0; i < n; i++)
		{
			var vertical = rng.Randf() < 0.5f;
			var size = vertical ? new Vector2I(3, 5) : new Vector2I(5, 3);
			var x = rng.RandiRange(1, a.Width - size.X - 1);
			var y = rng.RandiRange(1, a.Height - size.Y - 1);
			var body = rng.Randf() < 0.55f ? new Color("c9a26b") : new Color("1a1c20");
			a.Rect(x, y, size.X, size.Y, body);
			if (vertical)
			{
				a.HLine(x, x + 2, y, Silver);
				a.HLine(x, x + 2, y + 4, SilverDark);
			}
			else
			{
				a.VLine(x, y, y + 2, Silver);
				a.VLine(x + 4, y, y + 2, SilverDark);
			}
		}
	}

	static void Resistor(PixelCanvas a, int seed)
	{
		var horizontal = a.Width >= a.Height;
		var len = horizontal ? a.Width : a.Height;
		var mid = (horizontal ? a.Height : a.Width) / 2;
		void At(int along, int across, int lw, int lh, Color col)
		{
			if (horizontal)
				a.Rect(along, mid + across, lw, lh, col);
			else
				a.Rect(mid + across, along, lh, lw, col);
		}
		// Pads, lead, body, bands.
		foreach (var p in new[] { 2, len - 6 })
		{
			At(p, -2, 4, 4, GoldDark);
			At(p, -2, 3, 3, Gold);
		}
		At(4, -1, len - 8, 1, SilverLight);
		At(4, 0, len - 8, 1, SilverDark);
		var bodyLen = len - 14;
		At(7, -3, bodyLen, 6, new Color("d8c39a"));
		At(7, -3, bodyLen, 1, new Color("efe0bd"));
		At(7, 2, bodyLen, 1, new Color("a8946c"));
		var bands = new[] { new Color("8a4b13"), new Color("111111"), new Color("d12f2f"), Gold };
		for (var i = 0; i < 4; i++)
		{
			var colour = bands[(i + seed) % 3 == 0 && i < 3 ? (i + 1) % 3 : i];
			At(9 + i * 3 + (i == 3 ? 2 : 0), -3, 2, 6, colour);
		}
	}

	// SMD (surface-mount device): flat ceramic package, two solder pads and a square lens.
	static void Led(PixelCanvas a, string color)
	{
		var col = color != null ? new Color(color) : new Color("ff3344");
		var cx = a.Width / 2;
		var cy = a.Height / 2;
		a.Rect(cx - 7, cy - 3, 3, 6, SilverDark);
		a.Rect(cx + 4, cy - 3, 3, 6, SilverDark);
		a.HLine(cx - 7, cx - 5, cy - 3, SilverLight);
		a.HLine(cx + 4, cx + 6, cy - 3, SilverLight);
		a.Rect(cx - 5, cy - 4, 10, 8, new Color("d8d2c4"));
		a.HLine(cx - 5, cx + 4, cy + 3, new Color("8e887c"));
		a.Rect(cx - 3, cy - 3, 6, 6, col.Darkened(0.65f));
		a.Rect(cx - 2, cy - 2, 4, 4, col);
		a.Rect(cx - 2, cy - 2, 2, 1, col.Lightened(0.75f));
		// Cathode marking on the ceramic, separate from the coloured lens.
		a.VLine(cx + 3, cy - 2, cy + 1, new Color("4e6558"));
	}

	// Through-hole 5 mm LED, seen from above: leads/pads, circular rim, dome and reflection.
	static void Led5mm(PixelCanvas a, string color)
	{
		var col = color != null ? new Color(color) : new Color("ff3344");
		var cx = a.Width / 2;
		var cy = a.Height / 2;
		a.Rect(cx - 1, cy - 7, 3, 3, Silver);
		a.Rect(cx - 1, cy + 4, 3, 3, SilverDark);
		a.Set(cx, cy - 7, SilverLight);
		a.Set(cx, cy + 6, SilverLight);
		a.Disc(cx, cy, 5.5f, col.Darkened(0.75f));
		a.Ring(cx, cy, 4.8f, 1, col.Darkened(0.25f));
		a.Disc(cx, cy - 1, 3.6f, col.Darkened(0.1f));
		a.Rect(cx - 2, cy - 3, 2, 2, col.Lightened(0.75f));
		a.Set(cx - 3, cy - 1, col.Lightened(0.35f));
		// Flat cathode edge gives the dome a recognisable package silhouette.
		a.VLine(cx + 5, cy - 2, cy + 2, col.Darkened(0.6f));
	}

	/// <summary>Only the lens emits through the darkness; pads and package still receive light.</summary>
	public static ImageTexture LedLens(ComponentData data)
	{
		var key = $"lens|{data.Type}|{data.Color}";
		if (Cache.TryGetValue(key, out var texture))
			return texture;
		var a = new PixelCanvas(C, C);
		var col = MainColor(data);
		var cx = C / 2;
		var cy = C / 2;
		if (data.Type == "led_5mm")
		{
			a.Disc(cx, cy - 1, 2.6f, col);
			a.Rect(cx - 2, cy - 3, 2, 2, col.Lightened(0.75f));
		}
		else
		{
			a.Rect(cx - 2, cy - 2, 4, 4, col);
			a.Rect(cx - 2, cy - 2, 2, 1, col.Lightened(0.75f));
		}
		texture = a.ToTexture();
		Cache[key] = texture;
		return texture;
	}

	static void Crystal(PixelCanvas a, string label)
	{
		var x = 3;
		var y = 3;
		var w = a.Width - 6;
		var h = a.Height - 6;
		var body = new Color("c5ccd5");
		a.Rect(x + 1, y, w - 2, h, body);
		a.Rect(x, y + 1, w, h - 2, body);
		a.HLine(x + 1, x + w - 2, y, new Color("e6ebf0"));
		a.HLine(x + 1, x + w - 2, y + h - 1, new Color("7d8794"));
		a.VLine(x, y + 1, y + h - 2, new Color("7d8794"));
		a.VLine(x + w - 1, y + 1, y + h - 2, new Color("7d8794"));
		Label(a, a.Width / 2f, a.Height / 2f + 0.5f, label ?? "16.000", new Color(0.18f, 0.2f, 0.24f, 0.85f), w - 4);
	}

	static void Inductor(PixelCanvas a, string label)
	{
		var w = a.Width;
		var h = a.Height;
		a.Rect(1, h / 2 - 2, w - 2, 4, Silver);
		a.HLine(1, w - 2, h / 2 - 2, SilverLight);
		var col = new Color("2d3138");
		a.Rect(3, 2, w - 6, h - 4, col);
		a.Rect(2, 3, w - 4, h - 6, col);
		a.HLine(3, w - 4, 2, new Color("484e57"));
		a.HLine(4, w - 5, 4, new Color(1, 1, 1, 0.08f));
		a.HLine(3, w - 4, h - 3, new Color("1c1f24"));
		Label(a, w / 2f, h / 2f + 0.5f, label ?? "R47", new Color(1, 1, 1, 0.5f), w - 4);
	}

	static void Mosfet(PixelCanvas a)
	{
		var w = a.Width;
		a.Rect(4, 1, w - 8, 4, Silver);
		a.HLine(4, w - 5, 1, SilverLight);
		a.Disc(w / 2f, 3, 1, SilverDark);
		Body(a, 3, 5, w - 6, 6, new Color("17191d"));
		for (var i = 0; i < 3; i++)
			a.Rect(4 + i * (w - 10) / 2, 11, 2, 3, Silver);
	}

	static void Slot(PixelCanvas a, string color, bool latches, float notchAt)
	{
		var horizontal = a.Width >= a.Height;
		var col = new Color(color);
		var len = horizontal ? a.Width : a.Height;
		var thick = horizontal ? a.Height : a.Width;
		void At(int along, int across, int lw, int lh, Color c)
		{
			if (horizontal)
				a.Rect(along, across, lw, lh, c);
			else
				a.Rect(across, along, lh, lw, c);
		}
		var y0 = thick / 2 - 4;
		At(1, y0, len - 2, 8, col);
		At(1, y0, len - 2, 1, col.Lightened(0.2f));
		At(1, y0 + 7, len - 2, 1, col.Darkened(0.4f));
		At(4, y0 + 3, len - 8, 2, new Color(0, 0, 0, 0.85f));
		for (var d = 5; d < len - 5; d += 2)
			At(d, y0 + 3, 1, 1, new Color(Gold, 0.8f));
		At((int)(4 + (len - 8) * notchAt), y0 + 2, 2, 4, col);
		if (!latches)
			return;
		var latch = new Color("e2e6ea");
		At(0, y0 - 1, 3, 10, latch);
		At(len - 3, y0 - 1, 3, 10, latch);
		At(0, y0 + 8, 3, 1, new Color("9aa2ab"));
		At(len - 3, y0 + 8, 3, 1, new Color("9aa2ab"));
	}

	static void Sata(PixelCanvas a, string color)
	{
		var col = color != null ? new Color(color) : new Color("b3242b");
		Body(a, 3, 3, a.Width - 6, a.Height - 6, col);
		a.Rect(5, 5, a.Width - 10, a.Height - 10, new Color(0, 0, 0, 0.8f));
		a.Rect(5, 5, (a.Width - 10) / 3, (a.Height - 10) / 2, col);
		for (var x = 7; x < a.Width - 6; x += 2)
			a.Set(x, a.Height - 7, new Color(Gold, 0.8f));
	}

	static void Port(PixelCanvas a, string kind)
	{
		var w = a.Width;
		var h = a.Height;
		Body(a, 1, 1, w - 2, h - 2, new Color("b8c0ca"));
		a.Frame(1, 1, w - 2, h - 2, new Color("6d7682"));
		a.HLine(2, w - 3, 2, new Color("e4e9ef"));
		switch (kind)
		{
			case "rj45":
				a.Rect(w / 5, h / 5, w - w * 2 / 5, h - h * 2 / 5, new Color("15171a"));
				a.Rect(w / 2 - 3, h - h / 5 - 3, 6, 3, new Color("6d7682"));
				for (var x = w / 5 + 2; x < w - w / 5 - 2; x += 2)
					a.Set(x, h / 5 + 1, new Color(Gold, 0.9f));
				a.Rect(3, 3, 2, 2, new Color("3dff6a"));
				a.Rect(w - 5, 3, 2, 2, new Color("ffb43d"));
				break;
			case "hdmi":
				var top = h * 3 / 10;
				a.Rect(4, top, w - 8, h * 2 / 5, new Color("15171a"));
				a.Rect(6, top + 2, w - 12, h * 2 / 5 - 4, new Color("1e2a40"));
				a.Rect(4, top + h * 2 / 5 - 2, 2, 2, new Color("b8c0ca"));
				a.Rect(w - 6, top + h * 2 / 5 - 2, 2, 2, new Color("b8c0ca"));
				break;
			case "audio":
				var colours = new[] { new Color("3dff6a"), new Color("ff5fd2"), new Color("4da3ff") };
				for (var i = 0; i < 3; i++)
				{
					var cy = h * (0.2f + 0.3f * i);
					var r = MathF.Min(w, h) * 0.15f;
					a.Disc(w / 2f, cy, r, colours[i]);
					a.Disc(w / 2f, cy, r * 0.5f, new Color("101010"));
				}
				break;
			default:
				for (var i = 0; i < 2; i++)
				{
					var y = 4 + i * h / 2;
					a.Rect(4, y, w - 8, h / 2 - 7, new Color("15171a"));
					a.Rect(6, y + (h / 2 - 7) / 3, w - 12, Math.Max((h / 2 - 7) / 3, 1), new Color("2b62c9"));
				}
				break;
		}
	}

	static void Battery(PixelCanvas a)
	{
		var cx = a.Width / 2f;
		var cy = a.Height / 2f;
		var r = MathF.Min(a.Width, a.Height) * 0.44f;
		a.Disc(cx, cy, r + 1, new Color("1b1d21"));
		a.Disc(cx, cy, r, new Color("aeb6c0"));
		a.Disc(cx - 1, cy - 1, r * 0.8f, new Color("d3dae2"));
		a.Ring(cx, cy, r * 0.92f, 1, new Color("8b939d"));
		a.TextCentered(cx, cy - 3, "+", new Color(0.3f, 0.33f, 0.38f));
		Label(a, cx, cy + 4, "CR2032", new Color(0.3f, 0.33f, 0.38f), (int)(r * 1.7f));
	}

	static void Heatsink(PixelCanvas a, string color)
	{
		var w = a.Width;
		var h = a.Height;
		var col = color != null ? new Color(color) : new Color("3d4c5c");
		Body(a, 2, 2, w - 4, h - 4, col.Darkened(0.35f));
		var horizontalFins = w < h;
		var span = horizontalFins ? h : w;
		for (var d = 4; d < span - 4; d += 3)
		{
			if (horizontalFins)
			{
				a.Rect(3, d, w - 6, 2, col.Lightened(0.12f));
				a.HLine(3, w - 4, d, col.Lightened(0.4f));
			}
			else
			{
				a.Rect(d, 3, 2, h - 6, col.Lightened(0.12f));
				a.VLine(d, 3, h - 4, col.Lightened(0.4f));
			}
		}
		foreach (var (x, y) in new[] { (4, 4), (w - 5, 4), (4, h - 5), (w - 5, h - 5) })
		{
			a.Disc(x + 0.5f, y + 0.5f, 1.6f, new Color("dcdfe3"));
			a.Set(x, y, new Color("8a9099"));
		}
	}

	static void ShieldCan(PixelCanvas a, string label)
	{
		var w = a.Width;
		var h = a.Height;
		Body(a, 2, 2, w - 4, h - 4, new Color("b5bdc7"));
		a.Frame(2, 2, w - 4, h - 4, new Color("7f8894"));
		a.HLine(3, w - 4, 3, new Color("dde3ea"));
		for (var x = 6; x < w - 4; x += 4)
			for (var y = 6; y < h - 4; y += 4)
				a.Set(x, y, new Color("8e97a2"));
		if (!string.IsNullOrEmpty(label))
		{
			var lw = Math.Min(MicroFont.Width(label.ToUpperInvariant()) + 4, w - 8);
			a.Rect((w - lw) / 2, h / 2 - 4, lw, 8, new Color("b5bdc7"));
			Label(a, w / 2f, h / 2f, label, new Color(0.22f, 0.25f, 0.29f, 0.9f), w - 10);
		}
	}

	static void KeySwitch(PixelCanvas a, string color)
	{
		var w = a.Width;
		var h = a.Height;
		var housing = new Color("1d2026");
		a.Rect(2, 1, w - 4, h - 2, housing);
		a.Rect(1, 2, w - 2, h - 4, housing);
		a.HLine(2, w - 3, 1, new Color("3a4049"));
		a.Rect(4, 4, w - 8, h - 8, new Color("2a2e36"));
		var stem = color != null ? new Color(color) : new Color("d93b3b");
		var cx = w / 2;
		var cy = h / 2;
		a.Rect(cx - 3, cy - 1, 6, 2, stem);
		a.Rect(cx - 1, cy - 3, 2, 6, stem);
		a.Set(cx - 1, cy - 3, stem.Lightened(0.4f));
	}

	static void Jumper(PixelCanvas a, string color)
	{
		var w = a.Width;
		var h = a.Height;
		Body(a, 4, 4, w - 8, h - 8, new Color("141518"));
		var cap = color != null ? new Color(color) : new Color("2f6fd6");
		a.Rect(5, 5, w - 10, (h - 8) * 3 / 5, cap);
		a.HLine(5, w - 6, 5, cap.Lightened(0.4f));
		a.Rect(6, h - 7, 2, 2, Gold);
		a.Rect(w - 8, h - 7, 2, 2, Gold);
	}

	static void Fingers(PixelCanvas a)
	{
		var horizontal = a.Width >= a.Height;
		var len = horizontal ? a.Width : a.Height;
		var thick = horizontal ? a.Height : a.Width;
		for (var d = 2; d < len - 3; d += 4)
		{
			if (horizontal)
			{
				a.Rect(d, 2, 3, thick - 4, Gold);
				a.VLine(d, 2, thick - 3, GoldLight);
				a.HLine(d, d + 2, thick - 3, GoldDark);
			}
			else
			{
				a.Rect(2, d, thick - 4, 3, Gold);
				a.HLine(2, thick - 3, d, GoldLight);
				a.VLine(thick - 3, d, d + 2, GoldDark);
			}
		}
	}

	static void Hole(PixelCanvas a)
	{
		var cx = a.Width / 2f;
		var cy = a.Height / 2f;
		a.Disc(cx, cy, 6, GoldDark);
		a.Disc(cx - 0.5f, cy - 0.5f, 5, Gold);
		a.Disc(cx, cy, 3.4f, new Color("030608"));
	}
}
