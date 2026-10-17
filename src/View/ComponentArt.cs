using System;
using HardwareDefence.Data;
using Godot;

namespace HardwareDefence.View;

/// <summary>
/// Procedural drawings of board components, plus their damage states. Everything is drawn
/// in a rect starting at (0, 0) of the calling canvas item.
/// </summary>
public static class ComponentArt
{
	public static readonly Font SilkFont = GD.Load<Font>("res://assets/fonts/Silkscreen-Regular.ttf");
	public static readonly Font MarkFont = GD.Load<Font>("res://assets/fonts/ShareTechMono-Regular.ttf");

	static readonly Color Gold = new("d9a441");
	static readonly Color GoldDark = new("7a5212");
	static readonly Color Silver = new("aeb7c2");
	static readonly Color Shadow = new(0, 0, 0, 0.45f);

	/// <summary>Main colour of a component (used for debris when it's chewed).</summary>
	public static Color MainColor(ComponentData c) => c.Type switch
	{
		"capacitor" => c.Color != null ? new Color(c.Color) : new Color("1e3f7a"),
		"connector" => new Color("d9d2c3"),
		"resistor" => new Color("d8c39a"),
		"led" => c.Color != null ? new Color(c.Color) : new Color("ff3344"),
		"crystal" or "shield" or "battery" or "port" => Silver,
		"heatsink" => c.Color != null ? new Color(c.Color) : new Color("3d4c5c"),
		"fingers" => Gold,
		"bga" => new Color("2e4d33"),
		_ => new Color("1d2026"),
	};

	public static void Draw(CanvasItem ci, ComponentData c, Vector2 size, BoardTheme theme, int seed)
	{
		var r = new Rect2(Vector2.Zero, size);
		switch (c.Type)
		{
			case "chip": Chip(ci, r, c.Label ?? $"U{seed % 90 + 1}", c.Color); break;
			case "bga": Bga(ci, r, c.Label); break;
			case "header": Header(ci, r); break;
			case "connector": Connector(ci, r, c.Color); break;
			case "capacitor": Capacitor(ci, r, c.Color); break;
			case "smd": Smd(ci, r, seed); break;
			case "resistor": Resistor(ci, r, seed); break;
			case "led": LedPad(ci, r, c.Color); break;
			case "crystal": Crystal(ci, r, c.Label); break;
			case "inductor": Inductor(ci, r, c.Label); break;
			case "mosfet": Mosfet(ci, r); break;
			case "ram_slot": Slot(ci, r, c.Color ?? "16191e", latches: true, notchAt: 0.42f); break;
			case "pcie_slot": Slot(ci, r, c.Color ?? "121417", latches: false, notchAt: 0.18f); break;
			case "sata": Sata(ci, r, c.Color); break;
			case "port": Port(ci, r, c.Label); break;
			case "battery": Battery(ci, r); break;
			case "heatsink": Heatsink(ci, r, c.Color); break;
			case "shield": ShieldCan(ci, r, c.Label); break;
			case "switch": KeySwitch(ci, r, c.Color); break;
			case "jumper": Jumper(ci, r, c.Color); break;
			case "fingers": Fingers(ci, r); break;
			case "hole": Hole(ci, r); break;
			case "label": Label(ci, r, c.Label ?? "", theme.Silk); break;
			default: Chip(ci, r, c.Type, null); break;
		}
	}

	/// <summary>Bite marks, cracks and scorching depending on how damaged it is.</summary>
	public static void DrawDamage(CanvasItem ci, Vector2 size, float ratio, bool destroyed, int seed)
	{
		if (!destroyed && ratio >= 0.999f)
			return;
		var rng = new RandomNumberGenerator { Seed = (ulong)(seed * 7919 + 13) };
		var r = new Rect2(Vector2.Zero, size).Grow(-3);
		var soot = new Color(0.06f, 0.035f, 0.02f, 0.9f);
		if (destroyed)
		{
			var center = r.GetCenter();
			var radius = Mathf.Max(size.X, size.Y) * 0.65f;
			for (var i = 0; i < 6; i++)
				ci.DrawCircle(center, radius * (1 - i * 0.14f), new Color(0.05f, 0.03f, 0.02f, 0.18f));
			ci.DrawRect(r, new Color(0.04f, 0.03f, 0.03f, 0.72f));
		}
		var damage = destroyed ? 1f : 1f - ratio;
		var bites = Mathf.CeilToInt(damage * 9);
		for (var i = 0; i < bites; i++)
		{
			var edge = rng.RandiRange(0, 3);
			var t = rng.RandfRange(0.1f, 0.9f);
			var p = edge switch
			{
				0 => new Vector2(r.Position.X + r.Size.X * t, r.Position.Y),
				1 => new Vector2(r.End.X, r.Position.Y + r.Size.Y * t),
				2 => new Vector2(r.Position.X + r.Size.X * t, r.End.Y),
				_ => new Vector2(r.Position.X, r.Position.Y + r.Size.Y * t),
			};
			var radius = rng.RandfRange(4, 9) * (0.6f + damage * 0.6f);
			var pts = new Vector2[9];
			for (var k = 0; k < pts.Length; k++)
			{
				var a = Mathf.Tau * k / pts.Length;
				pts[k] = p + Vector2.FromAngle(a) * radius * rng.RandfRange(0.6f, 1.15f);
			}
			ci.DrawColoredPolygon(pts, soot);
		}
		if (damage > 0.4f)
		{
			var cracks = Mathf.CeilToInt((damage - 0.4f) * 8);
			for (var i = 0; i < cracks; i++)
			{
				var p = new Vector2(rng.RandfRange(r.Position.X, r.End.X), rng.RandfRange(r.Position.Y, r.End.Y));
				var line = new Vector2[4];
				line[0] = p;
				for (var k = 1; k < line.Length; k++)
					line[k] = line[k - 1] + Vector2.FromAngle(rng.RandfRange(0, Mathf.Tau)) * rng.RandfRange(5, 12);
				ci.DrawPolyline(line, new Color(0, 0, 0, 0.75f), 1.6f, true);
			}
		}
	}

	static void Box(CanvasItem ci, Rect2 r, Color body, float radius = 3)
	{
		ci.DrawRect(new Rect2(r.Position + new Vector2(3, 4), r.Size), Shadow);
		ci.DrawRect(r, body);
		ci.DrawRect(new Rect2(r.Position, new Vector2(r.Size.X, Mathf.Min(5, r.Size.Y * 0.2f))), new Color(1, 1, 1, 0.08f));
	}

	static void Text(CanvasItem ci, Font font, Vector2 center, string text, int size, Color color, float maxWidth = -1)
	{
		if (string.IsNullOrEmpty(text))
			return;
		var w = font.GetStringSize(text, HorizontalAlignment.Left, -1, size).X;
		if (maxWidth > 0 && w > maxWidth)
		{
			size = Math.Max(6, (int)(size * maxWidth / w));
			w = font.GetStringSize(text, HorizontalAlignment.Left, -1, size).X;
		}
		ci.DrawString(font, center + new Vector2(-w / 2, size * 0.35f), text, HorizontalAlignment.Left, -1, size, color);
	}

	static void Chip(CanvasItem ci, Rect2 r, string label, string color)
	{
		var body = r.Grow(-7);
		var wide = r.Size.X >= r.Size.Y * 1.5f;
		var tall = r.Size.Y >= r.Size.X * 1.5f;
		var pin = new Color("b9c2cc");
		if (!tall)
			for (var x = body.Position.X + 5; x < body.End.X - 3; x += 8)
			{
				ci.DrawRect(new Rect2(x - 2.5f, body.Position.Y - 5, 5, 6), pin);
				ci.DrawRect(new Rect2(x - 2.5f, body.End.Y - 1, 5, 6), pin);
			}
		if (!wide)
			for (var y = body.Position.Y + 5; y < body.End.Y - 3; y += 8)
			{
				ci.DrawRect(new Rect2(body.Position.X - 5, y - 2.5f, 6, 5), pin);
				ci.DrawRect(new Rect2(body.End.X - 1, y - 2.5f, 6, 5), pin);
			}
		Box(ci, body, color != null ? new Color(color) : new Color("16191e"));
		ci.DrawRect(body, new Color("2c323b"), false, 1.5f);
		ci.DrawCircle(body.Position + new Vector2(7, 7), 2.5f, new Color("2c323b"));
		Text(ci, MarkFont, body.GetCenter(), label, 12, new Color(1, 1, 1, 0.42f), body.Size.X - 8);
	}

	static void Bga(CanvasItem ci, Rect2 r, string label)
	{
		var sub = r.Grow(-4);
		Box(ci, sub, new Color("2e4d33"));
		ci.DrawRect(sub, new Color("4d7a52"), false, 1.5f);
		// Tiny decoupling caps around the die.
		for (var x = sub.Position.X + 8; x < sub.End.X - 6; x += 10)
		{
			ci.DrawRect(new Rect2(x, sub.Position.Y + 5, 5, 3), new Color("c9a26b"));
			ci.DrawRect(new Rect2(x, sub.End.Y - 8, 5, 3), new Color("c9a26b"));
		}
		var die = sub.Grow(-sub.Size.X * 0.2f);
		ci.DrawRect(new Rect2(die.Position + new Vector2(2, 3), die.Size), Shadow);
		ci.DrawRect(die, new Color("9aa4b1"));
		ci.DrawRect(new Rect2(die.Position, new Vector2(die.Size.X, die.Size.Y * 0.45f)), new Color("c3cbd4"));
		ci.DrawRect(die, new Color("5d6773"), false, 1.5f);
		Text(ci, MarkFont, die.GetCenter(), label ?? "SoC", 14, new Color(0.15f, 0.17f, 0.2f, 0.75f), die.Size.X - 6);
	}

	static void Header(CanvasItem ci, Rect2 r)
	{
		var body = r.Grow(-6);
		Box(ci, body, new Color("141518"));
		var cols = Math.Max(1, Mathf.RoundToInt(body.Size.X / 16));
		var rows = Math.Max(1, Mathf.RoundToInt(body.Size.Y / 16));
		var cell = new Vector2(body.Size.X / cols, body.Size.Y / rows);
		for (var x = 0; x < cols; x++)
			for (var y = 0; y < rows; y++)
			{
				var c = body.Position + cell * new Vector2(x + 0.5f, y + 0.5f);
				ci.DrawRect(new Rect2(c - Vector2.One * 4.5f, Vector2.One * 9), new Color("1f2125"));
				ci.DrawRect(new Rect2(c - Vector2.One * 3, Vector2.One * 6), Gold);
				ci.DrawRect(new Rect2(c - new Vector2(3, 3), new Vector2(6, 2)), new Color("ffe7a3"));
			}
	}

	static void Connector(CanvasItem ci, Rect2 r, string color)
	{
		var body = r.Grow(-5);
		var col = color != null ? new Color(color) : new Color("d9d2c3");
		Box(ci, body, col);
		ci.DrawRect(body, col.Darkened(0.35f), false, 1.5f);
		var cols = Math.Max(1, Mathf.RoundToInt(body.Size.X / 16));
		var rows = Math.Max(1, Mathf.RoundToInt(body.Size.Y / 16));
		var cell = new Vector2(body.Size.X / cols, body.Size.Y / rows);
		for (var x = 0; x < cols; x++)
			for (var y = 0; y < rows; y++)
			{
				var c = body.Position + cell * new Vector2(x + 0.5f, y + 0.5f);
				ci.DrawRect(new Rect2(c - Vector2.One * 5, Vector2.One * 10), col.Darkened(0.25f));
				ci.DrawRect(new Rect2(c - Vector2.One * 3.5f, Vector2.One * 7), new Color("2a2622"));
			}
	}

	static void Capacitor(CanvasItem ci, Rect2 r, string color)
	{
		var center = r.GetCenter();
		var radius = Mathf.Min(r.Size.X, r.Size.Y) * 0.42f;
		var col = color != null ? new Color(color) : new Color("1e3f7a");
		ci.DrawCircle(center + new Vector2(3, 4), radius, Shadow);
		ci.DrawCircle(center, radius, col.Darkened(0.35f));
		ci.DrawCircle(center, radius - 1.5f, col);
		ci.DrawArc(center, radius - radius * 0.15f, Mathf.Pi * 0.65f, Mathf.Pi * 1.35f, 20, col.Lightened(0.55f), radius * 0.22f);
		ci.DrawCircle(center, radius * 0.7f, new Color("a9b4c2"));
		ci.DrawCircle(center + new Vector2(-1.5f, -2) * (radius / 18), radius * 0.48f, new Color("d5dde7"));
		foreach (var a in new[] { 0f, Mathf.Pi / 2, Mathf.Pi / 4 })
		{
			var d = Vector2.FromAngle(a) * radius * 0.58f;
			ci.DrawLine(center - d, center + d, new Color("6b7684"), Mathf.Max(1.5f, radius * 0.1f));
		}
	}

	static void Smd(CanvasItem ci, Rect2 r, int seed)
	{
		var rng = new RandomNumberGenerator { Seed = (ulong)(seed * 31 + 5) };
		var n = rng.RandiRange(3, 5);
		for (var i = 0; i < n; i++)
		{
			var vertical = rng.Randf() < 0.5f;
			var size = vertical ? new Vector2(7, 13) : new Vector2(13, 7);
			var pos = new Vector2(rng.RandfRange(4, r.Size.X - size.X - 4), rng.RandfRange(4, r.Size.Y - size.Y - 4));
			var body = rng.Randf() < 0.55f ? new Color("c9a26b") : new Color("1a1c20");
			ci.DrawRect(new Rect2(pos + new Vector2(1, 2), size), Shadow);
			ci.DrawRect(new Rect2(pos, size), body);
			if (vertical)
			{
				ci.DrawRect(new Rect2(pos, new Vector2(size.X, 3)), Silver);
				ci.DrawRect(new Rect2(pos + new Vector2(0, size.Y - 3), new Vector2(size.X, 3)), Silver);
			}
			else
			{
				ci.DrawRect(new Rect2(pos, new Vector2(3, size.Y)), Silver);
				ci.DrawRect(new Rect2(pos + new Vector2(size.X - 3, 0), new Vector2(3, size.Y)), Silver);
			}
		}
	}

	static void Resistor(CanvasItem ci, Rect2 r, int seed)
	{
		var horizontal = r.Size.X >= r.Size.Y;
		var a = horizontal ? new Vector2(r.Position.X + 7, r.GetCenter().Y) : new Vector2(r.GetCenter().X, r.Position.Y + 7);
		var b = horizontal ? new Vector2(r.End.X - 7, r.GetCenter().Y) : new Vector2(r.GetCenter().X, r.End.Y - 7);
		foreach (var p in new[] { a, b })
		{
			ci.DrawCircle(p, 7, GoldDark);
			ci.DrawCircle(p, 5, Gold);
		}
		ci.DrawLine(a, b, Silver, 3);
		var len = (b - a).Length() - 26;
		var body = horizontal ? new Rect2(a.X + 13, a.Y - 9, len, 18) : new Rect2(a.X - 9, a.Y + 13, 18, len);
		Box(ci, body, new Color("d8c39a"));
		var bands = new[] { new Color("8a4b13"), new Color("111111"), new Color("d12f2f"), Gold };
		for (var i = 0; i < bands.Length; i++)
		{
			var off = 6 + i * 8 + (i == 3 ? 6 : 0);
			ci.DrawRect(horizontal ? new Rect2(body.Position.X + off, body.Position.Y, 4, body.Size.Y) : new Rect2(body.Position.X, body.Position.Y + off, body.Size.X, 4), bands[(i + seed) % 3 == 0 && i < 3 ? (i + 1) % 3 : i]);
		}
	}

	static void LedPad(CanvasItem ci, Rect2 r, string color)
	{
		var c = r.GetCenter();
		var col = color != null ? new Color(color) : new Color("ff3344");
		ci.DrawRect(new Rect2(c - new Vector2(14, 6), new Vector2(8, 12)), Gold);
		ci.DrawRect(new Rect2(c + new Vector2(6, -6), new Vector2(8, 12)), Gold);
		ci.DrawCircle(c + new Vector2(1, 2), 9, Shadow);
		ci.DrawCircle(c, 9, col.Darkened(0.55f));
		ci.DrawCircle(c, 7, col);
		ci.DrawCircle(c + new Vector2(-2.5f, -2.5f), 3, col.Lightened(0.7f));
	}

	static void Crystal(CanvasItem ci, Rect2 r, string label)
	{
		var body = r.Grow(-8);
		var radius = Mathf.Min(body.Size.X, body.Size.Y) / 2;
		var sb = new StyleBoxFlat { BgColor = new Color("c5ccd5"), BorderColor = new Color("7d8794"), CornerDetail = 8, AntiAliasing = true };
		sb.SetCornerRadiusAll((int)radius);
		sb.SetBorderWidthAll(2);
		sb.ShadowColor = Shadow;
		sb.ShadowSize = 3;
		sb.ShadowOffset = new Vector2(2, 3);
		ci.DrawStyleBox(sb, body);
		Text(ci, MarkFont, body.GetCenter(), label ?? "16.000", 11, new Color(0.2f, 0.22f, 0.26f, 0.8f), body.Size.X - radius);
	}

	static void Inductor(CanvasItem ci, Rect2 r, string label)
	{
		var body = r.Grow(-6);
		ci.DrawRect(new Rect2(body.Position.X - 2, body.GetCenter().Y - 6, body.Size.X + 4, 12), Silver);
		var sb = new StyleBoxFlat { BgColor = new Color("2d3138"), BorderColor = new Color("484e57"), AntiAliasing = true, ShadowColor = Shadow, ShadowSize = 3, ShadowOffset = new Vector2(2, 3) };
		sb.SetCornerRadiusAll(5);
		sb.SetBorderWidthAll(1);
		ci.DrawStyleBox(sb, body);
		ci.DrawRect(new Rect2(body.Position + new Vector2(3, 3), new Vector2(body.Size.X - 6, 4)), new Color(1, 1, 1, 0.08f));
		Text(ci, MarkFont, body.GetCenter(), label ?? "R47", 11, new Color(1, 1, 1, 0.5f), body.Size.X - 6);
	}

	static void Mosfet(CanvasItem ci, Rect2 r)
	{
		var tab = new Rect2(r.Position + new Vector2(10, 5), new Vector2(r.Size.X - 20, 12));
		ci.DrawRect(tab, Silver);
		var body = new Rect2(r.Position + new Vector2(8, 15), new Vector2(r.Size.X - 16, r.Size.Y - 28));
		Box(ci, body, new Color("17191d"));
		for (var i = 0; i < 3; i++)
			ci.DrawRect(new Rect2(body.Position.X + 4 + i * (body.Size.X - 12) / 2, body.End.Y, 4, 7), Silver);
	}

	static void Slot(CanvasItem ci, Rect2 r, string color, bool latches, float notchAt)
	{
		var horizontal = r.Size.X >= r.Size.Y;
		var body = horizontal ? r.Grow(-3).GrowSide(Side.Top, -8).GrowSide(Side.Bottom, -8) : r.Grow(-3).GrowSide(Side.Left, -8).GrowSide(Side.Right, -8);
		var col = new Color(color);
		Box(ci, body, col);
		ci.DrawRect(body, col.Lightened(0.15f), false, 1.5f);
		var groove = horizontal ? new Rect2(body.Position.X + 10, body.GetCenter().Y - 2.5f, body.Size.X - 20, 5) : new Rect2(body.GetCenter().X - 2.5f, body.Position.Y + 10, 5, body.Size.Y - 20);
		ci.DrawRect(groove, new Color(0, 0, 0, 0.8f));
		// Contacts along the groove.
		var length = horizontal ? groove.Size.X : groove.Size.Y;
		for (var d = 3f; d < length - 3; d += 5)
		{
			var p = horizontal ? new Vector2(groove.Position.X + d, groove.GetCenter().Y) : new Vector2(groove.GetCenter().X, groove.Position.Y + d);
			ci.DrawRect(new Rect2(p - Vector2.One, Vector2.One * 2), new Color(Gold, 0.7f));
		}
		var notch = horizontal ? new Rect2(groove.Position.X + groove.Size.X * notchAt, groove.Position.Y - 1, 5, groove.Size.Y + 2) : new Rect2(groove.Position.X - 1, groove.Position.Y + groove.Size.Y * notchAt, groove.Size.X + 2, 5);
		ci.DrawRect(notch, col);
		if (!latches)
			return;
		var latch = new Color("e2e6ea");
		if (horizontal)
		{
			ci.DrawRect(new Rect2(body.Position.X - 2, body.Position.Y - 3, 8, body.Size.Y + 6), latch);
			ci.DrawRect(new Rect2(body.End.X - 6, body.Position.Y - 3, 8, body.Size.Y + 6), latch);
		}
		else
		{
			ci.DrawRect(new Rect2(body.Position.X - 3, body.Position.Y - 2, body.Size.X + 6, 8), latch);
			ci.DrawRect(new Rect2(body.Position.X - 3, body.End.Y - 6, body.Size.X + 6, 8), latch);
		}
	}

	static void Sata(CanvasItem ci, Rect2 r, string color)
	{
		var body = r.Grow(-7);
		var col = color != null ? new Color(color) : new Color("b3242b");
		Box(ci, body, col);
		var inner = body.Grow(-5);
		ci.DrawRect(inner, new Color(0, 0, 0, 0.75f));
		ci.DrawRect(new Rect2(inner.Position.X, inner.Position.Y, inner.Size.X * 0.3f, inner.Size.Y * 0.45f), col);
	}

	static void Port(CanvasItem ci, Rect2 r, string kind)
	{
		var body = r.Grow(-3);
		Box(ci, body, new Color("b8c0ca"));
		ci.DrawRect(body, new Color("6d7682"), false, 1.5f);
		ci.DrawRect(new Rect2(body.Position + new Vector2(3, 3), new Vector2(body.Size.X - 6, 3)), new Color(1, 1, 1, 0.35f));
		switch (kind)
		{
			case "rj45":
				var opening = new Rect2(body.Position + body.Size * 0.18f, body.Size * 0.64f);
				ci.DrawRect(opening, new Color("15171a"));
				ci.DrawRect(new Rect2(opening.GetCenter().X - opening.Size.X * 0.2f, opening.End.Y - 6, opening.Size.X * 0.4f, 6), new Color("6d7682"));
				ci.DrawCircle(body.Position + new Vector2(7, 7), 3, new Color("3dff6a"));
				ci.DrawCircle(new Vector2(body.End.X - 7, body.Position.Y + 7), 3, new Color("ffb43d"));
				break;
			case "hdmi":
				var h = new Rect2(body.Position + new Vector2(6, body.Size.Y * 0.3f), new Vector2(body.Size.X - 12, body.Size.Y * 0.4f));
				ci.DrawRect(h, new Color("15171a"));
				ci.DrawRect(h.Grow(-4), new Color("1e2a40"));
				break;
			case "audio":
				foreach (var (c, i) in new[] { (new Color("3dff6a"), 0), (new Color("ff5fd2"), 1), (new Color("4da3ff"), 2) })
				{
					var p = body.Position + new Vector2(body.Size.X / 2, body.Size.Y * (0.2f + 0.3f * i));
					ci.DrawCircle(p, Mathf.Min(body.Size.X, body.Size.Y) * 0.14f, c);
					ci.DrawCircle(p, Mathf.Min(body.Size.X, body.Size.Y) * 0.07f, new Color("101010"));
				}
				break;
			default:
				for (var i = 0; i < 2; i++)
				{
					var slot = new Rect2(body.Position.X + 6, body.Position.Y + 6 + i * body.Size.Y / 2, body.Size.X - 12, body.Size.Y / 2 - 12);
					ci.DrawRect(slot, new Color("15171a"));
					ci.DrawRect(new Rect2(slot.Position + new Vector2(3, slot.Size.Y * 0.35f), new Vector2(slot.Size.X - 6, slot.Size.Y * 0.3f)), new Color("2b62c9"));
				}
				break;
		}
	}

	static void Battery(CanvasItem ci, Rect2 r)
	{
		var center = r.GetCenter();
		var radius = Mathf.Min(r.Size.X, r.Size.Y) * 0.44f;
		ci.DrawCircle(center + new Vector2(3, 4), radius + 3, Shadow);
		ci.DrawCircle(center, radius + 3, new Color("1b1d21"));
		ci.DrawCircle(center, radius, new Color("aeb6c0"));
		ci.DrawCircle(center + new Vector2(-3, -4), radius * 0.82f, new Color("d3dae2"));
		ci.DrawArc(center, radius * 0.9f, 0, Mathf.Tau, 40, new Color("8b939d"), 2, true);
		Text(ci, MarkFont, center + new Vector2(0, -radius * 0.15f), "+", (int)(radius * 0.6f), new Color(0.3f, 0.33f, 0.38f));
		Text(ci, MarkFont, center + new Vector2(0, radius * 0.35f), "CR2032", 11, new Color(0.3f, 0.33f, 0.38f));
	}

	static void Heatsink(CanvasItem ci, Rect2 r, string color)
	{
		var body = r.Grow(-4);
		var col = color != null ? new Color(color) : new Color("3d4c5c");
		Box(ci, body, col.Darkened(0.3f));
		var horizontalFins = body.Size.X < body.Size.Y;
		for (var d = 4f; d < (horizontalFins ? body.Size.Y : body.Size.X) - 2; d += 7)
		{
			var fin = horizontalFins ? new Rect2(body.Position.X + 2, body.Position.Y + d, body.Size.X - 4, 4) : new Rect2(body.Position.X + d, body.Position.Y + 2, 4, body.Size.Y - 4);
			ci.DrawRect(fin, col.Lightened(0.15f));
			ci.DrawRect(horizontalFins ? new Rect2(fin.Position, new Vector2(fin.Size.X, 1.5f)) : new Rect2(fin.Position, new Vector2(1.5f, fin.Size.Y)), col.Lightened(0.45f));
		}
		foreach (var p in new[] { body.Position + new Vector2(5, 5), new Vector2(body.End.X - 5, body.Position.Y + 5), new Vector2(body.Position.X + 5, body.End.Y - 5), body.End - new Vector2(5, 5) })
			ci.DrawCircle(p, 3.5f, new Color("dcdfe3"));
	}

	static void ShieldCan(CanvasItem ci, Rect2 r, string label)
	{
		var body = r.Grow(-4);
		Box(ci, body, new Color("b5bdc7"));
		ci.DrawRect(body, new Color("7f8894"), false, 1.5f);
		for (var x = body.Position.X + 8; x < body.End.X - 4; x += 10)
			for (var y = body.Position.Y + 8; y < body.End.Y - 4; y += 10)
				ci.DrawCircle(new Vector2(x, y), 1.6f, new Color("8e97a2"));
		Text(ci, MarkFont, body.GetCenter(), label ?? "", 12, new Color(0.25f, 0.28f, 0.32f, 0.8f), body.Size.X - 8);
	}

	static void KeySwitch(CanvasItem ci, Rect2 r, string color)
	{
		var body = r.Grow(-5);
		var sb = new StyleBoxFlat { BgColor = new Color("1d2026"), BorderColor = new Color("3a4049"), AntiAliasing = true, ShadowColor = Shadow, ShadowSize = 3, ShadowOffset = new Vector2(2, 3) };
		sb.SetCornerRadiusAll(4);
		sb.SetBorderWidthAll(1);
		ci.DrawStyleBox(sb, body);
		var inner = body.Grow(-6);
		ci.DrawRect(inner, new Color("2a2e36"));
		var stem = color != null ? new Color(color) : new Color("d93b3b");
		var c = body.GetCenter();
		ci.DrawRect(new Rect2(c - new Vector2(6, 1.8f), new Vector2(12, 3.6f)), stem);
		ci.DrawRect(new Rect2(c - new Vector2(1.8f, 6), new Vector2(3.6f, 12)), stem);
	}

	static void Jumper(CanvasItem ci, Rect2 r, string color)
	{
		var body = r.Grow(-10);
		Box(ci, body, new Color("141518"));
		var cap = color != null ? new Color(color) : new Color("2f6fd6");
		var capRect = new Rect2(body.Position + new Vector2(2, 2), new Vector2(body.Size.X - 4, body.Size.Y * 0.6f));
		ci.DrawRect(capRect, cap);
		ci.DrawRect(new Rect2(capRect.Position, new Vector2(capRect.Size.X, 3)), cap.Lightened(0.4f));
	}

	static void Fingers(CanvasItem ci, Rect2 r)
	{
		var horizontal = r.Size.X >= r.Size.Y;
		var length = horizontal ? r.Size.X : r.Size.Y;
		for (var d = 6f; d < length - 6; d += 9)
		{
			var f = horizontal ? new Rect2(r.Position.X + d, r.Position.Y + 6, 6, r.Size.Y - 12) : new Rect2(r.Position.X + 6, r.Position.Y + d, r.Size.X - 12, 6);
			ci.DrawRect(f, Gold);
			ci.DrawRect(horizontal ? new Rect2(f.Position, new Vector2(2, f.Size.Y)) : new Rect2(f.Position, new Vector2(f.Size.X, 2)), new Color("ffe7a3"));
		}
	}

	static void Hole(CanvasItem ci, Rect2 r)
	{
		var c = r.GetCenter();
		ci.DrawCircle(c, 16, GoldDark);
		ci.DrawCircle(c, 13, Gold);
		ci.DrawCircle(c, 8, new Color("030608"));
	}

	static void Label(CanvasItem ci, Rect2 r, string text, Color silk)
	{
		Text(ci, SilkFont, r.GetCenter(), text, 14, silk, r.Size.X - 4);
	}
}
