using Godot;

namespace HardwareDefence.View;

/// <summary>
/// Renders the game world at 1/<see cref="Scale"/> of the screen resolution and scales it up,
/// so every sprite, line, particle and light lands on the same pixel grid (one art pixel is
/// <see cref="Scale"/> screen pixels; a board cell is 16 art pixels). World coordinates stay
/// in screen units: the camera zooms out by the same factor the container zooms in.
/// Input is not forwarded; screens read the mouse from the main viewport (same coordinates).
/// </summary>
public partial class PixelWorld : SubViewportContainer
{
	/// <summary>Screen pixels per art pixel.</summary>
	public const int Scale = 3;

	static Shader _upscale;

	public SubViewport Target { get; private set; }
	public Camera2D Camera { get; private set; }
	/// <summary>Add world nodes here.</summary>
	public Node2D Root { get; private set; }

	public PixelWorld(Vector2 size = default)
	{
		if (size == default)
			size = new Vector2(1368, 768);
		Stretch = true;
		StretchShrink = Scale;
		MouseFilter = MouseFilterEnum.Ignore;
		// Linear sampling plus the shader gives crisp pixels that stay even at any window size.
		TextureFilter = TextureFilterEnum.Linear;
		Material = new ShaderMaterial { Shader = _upscale ??= GD.Load<Shader>("res://assets/shaders/pixel_upscale.gdshader") };
		Size = size;
		Target = new SubViewport
		{
			Disable3D = true,
			HandleInputLocally = false,
			CanvasItemDefaultTextureFilter = Godot.Viewport.DefaultCanvasItemTextureFilter.Nearest,
			Snap2DTransformsToPixel = true,
			Snap2DVerticesToPixel = true,
			Msaa2D = Godot.Viewport.Msaa.Disabled,
			RenderTargetUpdateMode = SubViewport.UpdateMode.Always,
		};
		AddChild(Target);
		Camera = new Camera2D { AnchorMode = Camera2D.AnchorModeEnum.FixedTopLeft, Zoom = Vector2.One / Scale };
		Target.AddChild(Camera);
		Root = new Node2D();
		Target.AddChild(Root);
	}

	public override void _Ready() => Camera.MakeCurrent();

	/// <summary>Rounds a world position to the art pixel grid.</summary>
	public static Vector2 Snap(Vector2 p) => (p / Scale).Round() * Scale;

	/// <summary>A sprite whose texels are art pixels.</summary>
	public static Sprite2D Sprite(Texture2D texture, bool centered = true)
	{
		var sprite = new Sprite2D { Texture = texture, Centered = centered, Scale = Vector2.One * Scale };
		if (centered)
			sprite.Offset = OddOffset(texture.GetSize());
		return sprite;
	}

	/// <summary>
	/// Keeps an odd-sized centred sprite on whole pixels: without it the texture would start
	/// half a texel off the grid and nearest sampling would shimmer.
	/// </summary>
	public static Vector2 OddOffset(Vector2 frameSize) =>
		new(Mathf.PosMod(frameSize.X, 2) == 1 ? 0.5f : 0, Mathf.PosMod(frameSize.Y, 2) == 1 ? 0.5f : 0);
}
