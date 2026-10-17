using System.Collections.Generic;
using HardwareDefence.Data;
using Godot;

namespace HardwareDefence.View;

/// <summary>
/// A bug's pixel-art sprite (assets/sprites/bugs/ID.png, made by tools/sprites): 4 walking
/// and 2 chewing frames, an additive layer for glowing eyes and a drop shadow. It turns in
/// 90 degree steps so its pixels stay on the grid.
/// </summary>
public partial class BugBody : Node2D
{
	public const int Frames = 6;
	static readonly Shader FlashShader = GD.Load<Shader>("res://assets/shaders/flash.gdshader");
	static readonly Dictionary<string, Texture2D> Sheets = new();

	public readonly EnemyDef Def;
	/// <summary>Distance walked (world units); drives the walk cycle.</summary>
	public float Gait;
	public bool Chewing;
	Sprite2D _shadow;
	Sprite2D _sprite;
	Sprite2D _glow;
	float _bite;
	float _facing;

	public BugBody(EnemyDef def) => Def = def;

	public static Texture2D Sheet(EnemyDef def)
	{
		if (Sheets.TryGetValue(def.Id, out var tex))
			return tex;
		var path = $"res://assets/sprites/bugs/{def.Id}.png";
		tex = GD.Load<Texture2D>(ResourceLoader.Exists(path) ? path : "res://assets/sprites/bugs/bug.png");
		Sheets[def.Id] = tex;
		return tex;
	}

	/// <summary>Size of one frame in art pixels.</summary>
	public static Vector2I FrameSize(EnemyDef def)
	{
		var size = Sheet(def).GetSize();
		return new Vector2I((int)size.X / Frames, (int)size.Y / 2);
	}

	/// <summary>Direction the bug faces (radians); drawn snapped to 90 degrees.</summary>
	public float Facing
	{
		get => _facing;
		set
		{
			_facing = value;
			if (_sprite == null)
				return;
			var snapped = Mathf.Snapped(value, Mathf.Pi / 2);
			_shadow.Rotation = _sprite.Rotation = _glow.Rotation = snapped;
		}
	}

	public override void _Ready()
	{
		var sheet = Sheet(Def);
		var offset = PixelWorld.OddOffset(FrameSize(Def));
		Sprite2D Layer(int frame, Material material) => new()
		{
			Texture = sheet, Hframes = Frames, Vframes = 2, Frame = frame, Offset = offset, Material = material,
		};
		_shadow = Layer(0, null);
		_shadow.Modulate = new Color(0, 0, 0, 0.4f);
		_shadow.Position = new Vector2(1, 1);
		AddChild(_shadow);
		_sprite = Layer(0, new ShaderMaterial { Shader = FlashShader });
		AddChild(_sprite);
		_glow = Layer(Frames, FxUtil.Additive);
		AddChild(_glow);
		Facing = _facing;
	}

	public void SetFlash(float value) => ((ShaderMaterial)_sprite.Material).SetShaderParameter("flash", value);

	public void SetFrozen(float value) => ((ShaderMaterial)_sprite.Material).SetShaderParameter("frozen", value);

	public void Animate(float distance, float delta)
	{
		Gait += distance;
		_bite = Chewing ? _bite + delta * 7 : 0;
		var step = 5 + Def.Radius * 0.35f;
		var frame = Chewing ? 4 + (int)_bite % 2 : (int)(Gait / step) % 4;
		_shadow.Frame = _sprite.Frame = frame;
		_glow.Frame = Frames + frame;
	}
}
