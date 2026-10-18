using System.Collections.Generic;
using HardwareDefence.Data;
using Godot;

namespace HardwareDefence.View;

/// <summary>
/// A bug's pixel-art sprite (assets/sprites/bugs/ID.png, made by tools/sprites): 8 walking,
/// 6 chewing and one resting frame, additive shell markings and a drop shadow. It turns in
/// 90 degree steps so its pixels stay on the grid.
/// </summary>
public partial class BugBody : Node2D
{
	public const int WalkFrames = 8;
	public const int ChewFrames = 6;
	public const int IdleFrame = WalkFrames + ChewFrames;
	public const int Frames = IdleFrame + 1;
	static readonly Shader FlashShader = GD.Load<Shader>("res://assets/shaders/flash.gdshader");
	static readonly Dictionary<string, Texture2D> Sheets = new();

	public readonly EnemyDef Def;
	Texture2D _sheet;
	/// <summary>Distance walked (world units); drives the walk cycle.</summary>
	public float Gait;
	public bool Chewing;
	Sprite2D _shadow;
	Sprite2D _sprite;
	Sprite2D _glow;
	float _bite;
	float _rest;
	float _facing;

	public BugBody(EnemyDef def) => Def = def;

	public static Texture2D Sheet(EnemyDef def) => Sheet(def.Id);

	static Texture2D Sheet(string id)
	{
		if (Sheets.TryGetValue(id, out var tex))
			return tex;
		var path = $"res://assets/sprites/bugs/{id}.png";
		tex = GD.Load<Texture2D>(ResourceLoader.Exists(path) ? path : "res://assets/sprites/bugs/bug.png");
		Sheets[id] = tex;
		return tex;
	}

	/// <summary>Size of one frame in art pixels.</summary>
	public static Vector2I FrameSize(EnemyDef def) => FrameSize(Sheet(def));

	static Vector2I FrameSize(Texture2D sheet)
	{
		var size = sheet.GetSize();
		return new Vector2I((int)size.X / Frames, (int)size.Y / 2);
	}

	/// <summary>Size of the frame being shown (the trojan's disguise is smaller).</summary>
	public Vector2I CurrentFrameSize => FrameSize(_sheet ?? Sheet(Def));

	/// <summary>The trojan arrives disguised; this swaps between the disguise and its real look.</summary>
	public void SetDisguised(bool disguised)
	{
		var sheet = disguised ? Sheet(Def.Id + "_disguise") : Sheet(Def);
		if (sheet == _sheet || _sprite == null)
			return;
		_sheet = sheet;
		var offset = PixelWorld.OddOffset(FrameSize(sheet));
		foreach (var layer in new[] { _shadow, _sprite, _glow })
		{
			layer.Texture = sheet;
			layer.Offset = offset;
		}
	}

	/// <summary>Rootkit hiding: the sprite breaks up into flickering pixels (0 = solid).</summary>
	public void SetCloak(float amount)
	{
		((ShaderMaterial)_sprite.Material).SetShaderParameter("dissolve", amount);
		_shadow.Visible = amount < 0.5f;
		_glow.Modulate = new Color(1, 1, 1, 1 - amount * 0.8f);
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
		var sheet = _sheet = Sheet(Def);
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
		Gait += Mathf.Max(distance, 0);
		_bite = Chewing ? _bite + delta * 12 : 0;
		_rest = distance > 0.001f ? 0 : _rest + delta;
		// Preserve the original stride length: twice as many drawings, half the
		// distance per drawing. A short grace period avoids idle flicker on snapshots.
		var step = (5 + Def.Radius * 0.35f) / 2;
		var frame = Chewing ? WalkFrames + (int)_bite % ChewFrames
			: _rest >= 0.12f ? IdleFrame : (int)(Gait / step) % WalkFrames;
		_shadow.Frame = _sprite.Frame = frame;
		_glow.Frame = Frames + frame;
	}
}
