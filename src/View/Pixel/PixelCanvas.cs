using System;
using System.Collections.Generic;
using Godot;

namespace HardwareDefence.View;

/// <summary>
/// A small RGBA image drawn pixel by pixel in art pixels (managed memory, turned into a
/// texture at the end). Used for the procedural art: board, components, CPU. Shapes are
/// tested against pixel centres, so a disc of radius 3.5 at (8, 8) is a clean 7 px circle.
/// </summary>
public sealed class PixelCanvas
{
	public readonly int Width;
	public readonly int Height;
	readonly Color[] _px;

	public PixelCanvas(int width, int height)
	{
		Width = Math.Max(width, 1);
		Height = Math.Max(height, 1);
		_px = new Color[Width * Height];
	}

	public bool Inside(int x, int y) => x >= 0 && y >= 0 && x < Width && y < Height;

	public Color Get(int x, int y) => Inside(x, y) ? _px[y * Width + x] : new Color(0, 0, 0, 0);

	public bool Opaque(int x, int y) => Get(x, y).A > 0.01f;

	/// <summary>Replaces a pixel (use a transparent colour to erase).</summary>
	public void Set(int x, int y, Color c)
	{
		if (Inside(x, y))
			_px[y * Width + x] = c;
	}

	/// <summary>Draws a pixel over what is there (alpha blending).</summary>
	public void Plot(int x, int y, Color c)
	{
		if (!Inside(x, y) || c.A <= 0)
			return;
		var i = y * Width + x;
		if (c.A >= 1)
		{
			_px[i] = c;
			return;
		}
		var d = _px[i];
		var a = c.A + d.A * (1 - c.A);
		_px[i] = a <= 0 ? new Color(0, 0, 0, 0) : new Color(
			(c.R * c.A + d.R * d.A * (1 - c.A)) / a,
			(c.G * c.A + d.G * d.A * (1 - c.A)) / a,
			(c.B * c.A + d.B * d.A * (1 - c.A)) / a, a);
	}

	/// <summary>Multiplies the colour of opaque pixels (shading, scorching).</summary>
	public void Tint(int x, int y, Color mul)
	{
		if (!Inside(x, y))
			return;
		var i = y * Width + x;
		var d = _px[i];
		_px[i] = new Color(d.R * mul.R, d.G * mul.G, d.B * mul.B, d.A);
	}

	public void Fill(Color c) => Array.Fill(_px, c);

	public void Rect(int x, int y, int w, int h, Color c)
	{
		for (var yy = y; yy < y + h; yy++)
			for (var xx = x; xx < x + w; xx++)
				Plot(xx, yy, c);
	}

	/// <summary>1 px rectangle outline.</summary>
	public void Frame(int x, int y, int w, int h, Color c)
	{
		HLine(x, x + w - 1, y, c);
		HLine(x, x + w - 1, y + h - 1, c);
		VLine(x, y + 1, y + h - 2, c);
		VLine(x + w - 1, y + 1, y + h - 2, c);
	}

	public void HLine(int x0, int x1, int y, Color c)
	{
		for (var x = Math.Min(x0, x1); x <= Math.Max(x0, x1); x++)
			Plot(x, y, c);
	}

	public void VLine(int x, int y0, int y1, Color c)
	{
		for (var y = Math.Min(y0, y1); y <= Math.Max(y0, y1); y++)
			Plot(x, y, c);
	}

	/// <summary>Bresenham line, both ends included; <paramref name="brush"/> squares make it thicker.</summary>
	public void Line(int x0, int y0, int x1, int y1, Color c, int brush = 1)
	{
		int dx = Math.Abs(x1 - x0), dy = -Math.Abs(y1 - y0);
		int sx = x0 < x1 ? 1 : -1, sy = y0 < y1 ? 1 : -1;
		var err = dx + dy;
		var seen = brush > 1 ? new HashSet<(int, int)>() : null;
		while (true)
		{
			if (brush <= 1)
				Plot(x0, y0, c);
			else
				for (var by = 0; by < brush; by++)
					for (var bx = 0; bx < brush; bx++)
						if (seen.Add((x0 + bx, y0 + by)))
							Plot(x0 + bx, y0 + by, c);
			if (x0 == x1 && y0 == y1)
				break;
			var e2 = 2 * err;
			if (e2 >= dy)
			{
				err += dy;
				x0 += sx;
			}
			if (e2 <= dx)
			{
				err += dx;
				y0 += sy;
			}
		}
	}

	public void Polyline(IReadOnlyList<Vector2I> points, Color c, int brush = 1)
	{
		for (var i = 1; i < points.Count; i++)
			Line(points[i - 1].X, points[i - 1].Y, points[i].X, points[i].Y, c, brush);
	}

	public void Ellipse(float cx, float cy, float rx, float ry, Color c)
	{
		for (var y = (int)MathF.Floor(cy - ry); y <= (int)MathF.Ceiling(cy + ry); y++)
			for (var x = (int)MathF.Floor(cx - rx); x <= (int)MathF.Ceiling(cx + rx); x++)
			{
				var nx = (x + 0.5f - cx) / rx;
				var ny = (y + 0.5f - cy) / ry;
				if (nx * nx + ny * ny <= 1)
					Plot(x, y, c);
			}
	}

	public void Disc(float cx, float cy, float r, Color c) => Ellipse(cx, cy, r, r, c);

	/// <summary>Circle outline <paramref name="thickness"/> pixels wide, inside radius r.</summary>
	public void Ring(float cx, float cy, float r, float thickness, Color c)
	{
		var inner = r - thickness;
		for (var y = (int)MathF.Floor(cy - r); y <= (int)MathF.Ceiling(cy + r); y++)
			for (var x = (int)MathF.Floor(cx - r); x <= (int)MathF.Ceiling(cx + r); x++)
			{
				var d = new Vector2(x + 0.5f - cx, y + 0.5f - cy).Length();
				if (d <= r && d > inner)
					Plot(x, y, c);
			}
	}

	/// <summary>Ellipse shaded like a dome lit from the top-left; ramp goes dark to light.</summary>
	public void Dome(float cx, float cy, float rx, float ry, Color[] ramp)
	{
		var light = new Vector3(-0.55f, -0.6f, 0.58f).Normalized();
		for (var y = (int)MathF.Floor(cy - ry); y <= (int)MathF.Ceiling(cy + ry); y++)
			for (var x = (int)MathF.Floor(cx - rx); x <= (int)MathF.Ceiling(cx + rx); x++)
			{
				var nx = (x + 0.5f - cx) / rx;
				var ny = (y + 0.5f - cy) / ry;
				var d2 = nx * nx + ny * ny;
				if (d2 > 1)
					continue;
				var i = nx * light.X + ny * light.Y + MathF.Sqrt(1 - d2) * light.Z;
				var tone = i > 0.93f ? 3 : i > 0.72f ? 2 : i > 0.4f ? 1 : 0;
				Plot(x, y, ramp[Math.Min(tone, ramp.Length - 1)]);
			}
	}

	/// <summary>Adds a 1 px border around opaque pixels.</summary>
	public void Outline(Color c, bool diagonal = false)
	{
		var add = new List<int>();
		for (var y = 0; y < Height; y++)
			for (var x = 0; x < Width; x++)
			{
				if (Opaque(x, y))
					continue;
				if (Opaque(x + 1, y) || Opaque(x - 1, y) || Opaque(x, y + 1) || Opaque(x, y - 1)
					|| (diagonal && (Opaque(x + 1, y + 1) || Opaque(x - 1, y - 1) || Opaque(x + 1, y - 1) || Opaque(x - 1, y + 1))))
					add.Add(y * Width + x);
			}
		foreach (var i in add)
			_px[i] = c;
	}

	/// <summary>A copy of the opaque pixels, offset and flattened to one colour, drawn underneath.</summary>
	public void DropShadow(int dx, int dy, Color c)
	{
		var copy = (Color[])_px.Clone();
		for (var y = 0; y < Height; y++)
			for (var x = 0; x < Width; x++)
			{
				var sx = x - dx;
				var sy = y - dy;
				if (Opaque(x, y) || sx < 0 || sy < 0 || sx >= Width || sy >= Height || copy[sy * Width + sx].A <= 0.01f)
					continue;
				_px[y * Width + x] = new Color(c, c.A * copy[sy * Width + sx].A);
			}
	}

	public void Draw(PixelCanvas other, int ox, int oy)
	{
		for (var y = 0; y < other.Height; y++)
			for (var x = 0; x < other.Width; x++)
				Plot(x + ox, y + oy, other._px[y * other.Width + x]);
	}

	public void Text(int x, int y, string text, Color c) => MicroFont.Draw(this, x, y, text, c);

	/// <summary>Text centred on a point (rounded down to whole pixels).</summary>
	public void TextCentered(float cx, float cy, string text, Color c) =>
		MicroFont.Draw(this, (int)MathF.Floor(cx - MicroFont.Width(text) / 2f), (int)MathF.Floor(cy - MicroFont.Height / 2f), text, c);

	/// <summary>A copy of part of the canvas.</summary>
	public PixelCanvas Region(int x, int y, int w, int h)
	{
		var c = new PixelCanvas(w, h);
		for (var yy = 0; yy < h; yy++)
			for (var xx = 0; xx < w; xx++)
				c._px[yy * w + xx] = Get(x + xx, y + yy);
		return c;
	}

	/// <summary>
	/// The canvas as a grid of sprites (art pixel scale) under one node. 2D lights affect at most
	/// 16 lights per canvas item, so big lit surfaces are split up for every light to reach them.
	/// Fully transparent tiles are skipped.
	/// </summary>
	public Node2D ToTiles(int tile = 32)
	{
		var root = new Node2D();
		for (var ty = 0; ty < Height; ty += tile)
			for (var tx = 0; tx < Width; tx += tile)
			{
				var part = Region(tx, ty, Math.Min(tile, Width - tx), Math.Min(tile, Height - ty));
				if (Array.TrueForAll(part._px, p => p.A <= 0.01f))
					continue;
				root.AddChild(new Sprite2D
				{
					Texture = part.ToTexture(), Centered = false, Scale = Vector2.One * PixelWorld.Scale,
					Position = new Vector2(tx, ty) * PixelWorld.Scale,
				});
			}
		return root;
	}

	public Image ToImage()
	{
		var bytes = new byte[Width * Height * 4];
		for (var i = 0; i < _px.Length; i++)
		{
			var c = _px[i].Clamp();
			bytes[i * 4] = (byte)Math.Round(c.R * 255);
			bytes[i * 4 + 1] = (byte)Math.Round(c.G * 255);
			bytes[i * 4 + 2] = (byte)Math.Round(c.B * 255);
			bytes[i * 4 + 3] = (byte)Math.Round(c.A * 255);
		}
		return Image.CreateFromData(Width, Height, false, Image.Format.Rgba8, bytes);
	}

	public ImageTexture ToTexture() => ImageTexture.CreateFromImage(ToImage());

	/// <summary>A 4x4 ordered-dither threshold (0..1) for mixing two tones in a pattern.</summary>
	public static float Bayer(int x, int y)
	{
		ReadOnlySpan<int> m = stackalloc int[] { 0, 8, 2, 10, 12, 4, 14, 6, 3, 11, 1, 9, 15, 7, 13, 5 };
		return (m[(y & 3) * 4 + (x & 3)] + 0.5f) / 16f;
	}
}
