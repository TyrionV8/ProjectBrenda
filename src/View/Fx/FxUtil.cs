using Godot;

namespace HardwareDefence.View;

/// <summary>Shared textures and materials for glows and lights.</summary>
public static class FxUtil
{
	public static readonly Texture2D LightTexture = GD.Load<Texture2D>("res://assets/fx/light_soft.png");
	public static readonly Texture2D GlowTexture = GD.Load<Texture2D>("res://assets/fx/glow.png");
	public static readonly Texture2D PixelTexture = GD.Load<Texture2D>("res://assets/fx/pixel.png");

	static CanvasItemMaterial _additive;
	static CanvasItemMaterial _unshaded;

	/// <summary>Additive and unaffected by scene lighting: makes things look emissive.</summary>
	public static CanvasItemMaterial Additive => _additive ??= new CanvasItemMaterial
	{
		BlendMode = CanvasItemMaterial.BlendModeEnum.Add,
		LightMode = CanvasItemMaterial.LightModeEnum.Unshaded,
	};

	/// <summary>Normal blending but ignores lights/CanvasModulate (for UI-like world text).</summary>
	public static CanvasItemMaterial Unshaded => _unshaded ??= new CanvasItemMaterial { LightMode = CanvasItemMaterial.LightModeEnum.Unshaded };

	public static Sprite2D GlowSprite(Color color, float scale = 1)
	{
		return new Sprite2D { Texture = GlowTexture, Material = Additive, Modulate = color, Scale = Vector2.One * scale };
	}

	public static PointLight2D Light(Color color, float energy = 1, float size = 1)
	{
		return new PointLight2D { Texture = LightTexture, Color = color, Energy = energy, TextureScale = size };
	}

	public static void DrawGlow(CanvasItem ci, Vector2 center, float size, Color color) =>
		ci.DrawTextureRect(GlowTexture, new Rect2(center - Vector2.One * size / 2, Vector2.One * size), false, color);

	public static Vector2[] Ellipse(Vector2 center, Vector2 radii, int points = 20)
	{
		var result = new Vector2[points];
		for (var i = 0; i < points; i++)
		{
			var a = Mathf.Tau * i / points;
			result[i] = center + new Vector2(Mathf.Cos(a) * radii.X, Mathf.Sin(a) * radii.Y);
		}
		return result;
	}
}
