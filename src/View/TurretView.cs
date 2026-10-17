using System.Collections.Generic;
using HardwareDefence.Data;
using HardwareDefence.Sim;
using Godot;

namespace HardwareDefence.View;

/// <summary>
/// A turret on screen: pixel-art socket and head (assets/sprites/turrets), barrels drawn as
/// pixel lines pointing where it aims, a coloured light, a status LED and (laser) the beam.
/// Heads never rotate as sprites, except the laser in 90 degree steps.
/// </summary>
public partial class TurretView : Node2D
{
	const int Px = PixelWorld.Scale;
	static readonly Texture2D Socket = GD.Load<Texture2D>("res://assets/sprites/turrets/socket.png");
	static readonly Color Steel = new("b8c0ca");

	public readonly TurretDef Def;
	public readonly int Id;
	public Dictionary<string, float> Stats;
	public float Direction;
	float _head;
	float _kick;
	Node2D _barrels;
	Sprite2D _headSprite;
	Sprite2D _headGlow;
	PointLight2D _light;
	Sprite2D _status;
	Sprite2D _core;
	Node2D _beam;
	PointLight2D _beamLight;
	float _t = GD.Randf() * 10;
	bool _showRange;
	bool _locked;
	Node2D _lock;
	/// <summary>What locked it: "encrypt" (ransomware) or "panic" (kernel panic).</summary>
	public string LockKind = "encrypt";
	/// <summary>Bugs currently burning in the laser (for sparks), board-local.</summary>
	public readonly List<Vector2> BeamHits = new();

	public bool ShowRange
	{
		get => _showRange;
		set
		{
			_showRange = value;
			QueueRedraw();
		}
	}

	public TurretView(TurretDef def, int id, Vector2I cell, float direction, Dictionary<string, float> stats)
	{
		Def = def;
		Id = id;
		Direction = direction;
		_head = direction;
		Stats = stats;
		Position = Grid.CellCenter(cell);
	}

	public float Range => Def.Behavior == "laser" ? 0 : Stats.GetValueOrDefault("range");

	/// <summary>Head sheet: frame 0 the head, frame 1 its emissive pixels.</summary>
	public static Texture2D HeadSheet(TurretDef def) => GD.Load<Texture2D>($"res://assets/sprites/turrets/{def.Id}.png");

	public override void _Ready()
	{
		AddChild(PixelWorld.Sprite(Socket));
		_core = FxUtil.GlowSprite(Def.Color, 0.45f);
		_core.Modulate = new Color(Def.Color, 0.4f);
		AddChild(_core);
		_barrels = new Node2D();
		_barrels.Draw += DrawBarrels;
		AddChild(_barrels);
		var sheet = HeadSheet(Def);
		_headSprite = new Sprite2D { Texture = sheet, Hframes = 2, Frame = 0, Scale = Vector2.One * Px };
		AddChild(_headSprite);
		_headGlow = new Sprite2D { Texture = sheet, Hframes = 2, Frame = 1, Scale = Vector2.One * Px, Material = FxUtil.Additive };
		AddChild(_headGlow);
		if (Def.Behavior == "laser")
			_headSprite.Rotation = _headGlow.Rotation = Mathf.Snapped(Direction, Mathf.Pi / 2);
		_light = FxUtil.Light(Def.Color, 1, 0.6f);
		AddChild(_light);
		_status = FxUtil.GlowSprite(Def.Color, 0.12f);
		_status.Position = new Vector2(5, 5) * Px;
		AddChild(_status);
		if (Def.Behavior == "laser")
		{
			_beam = new Node2D { Material = FxUtil.Additive };
			_beam.Draw += DrawBeam;
			AddChild(_beam);
			var length = Stats.GetValueOrDefault("beam_length");
			_beamLight = FxUtil.Light(Def.Color, 0.7f, 0.35f);
			_beamLight.Position = Vector2.FromAngle(Direction) * (24 + length * 0.5f);
			_beamLight.Rotation = Direction;
			_beamLight.Scale = new Vector2(Mathf.Max(length / 90f, 0.5f), 0.6f);
			AddChild(_beamLight);
		}
		// Built: a bright flash instead of a scale pop (which would blur the pixels).
		_light.Energy = 4;
		CreateTween().TweenProperty(_light, "energy", 1f, 0.6f);
		_headSprite.Modulate = new Color(2.5f, 2.5f, 2.5f);
		CreateTween().TweenProperty(_headSprite, "modulate", Colors.White, 0.35f);
	}

	public void Apply(TurretSnap s)
	{
		SetLocked(s.Locked);
		if (Mathf.IsEqualApprox(s.Head, _head))
			return;
		_head = s.Head;
		_barrels.QueueRedraw();
	}

	/// <summary>Encrypted or crashed by a boss: dimmed, beam off, a padlock (or a crash sign) on top.</summary>
	void SetLocked(bool locked)
	{
		if (locked == _locked)
			return;
		_locked = locked;
		if (_lock == null)
		{
			_lock = new Node2D { Material = FxUtil.Unshaded, ZIndex = 4 };
			_lock.Draw += DrawLock;
			AddChild(_lock);
		}
		_lock.Visible = locked;
		var dim = locked ? new Color(0.45f, 0.45f, 0.5f) : Colors.White;
		_headSprite.Modulate = _headGlow.Modulate = dim;
		_barrels.Modulate = dim;
		if (_beam != null)
			_beam.Visible = _beamLight.Visible = !locked;
		_light.Energy = locked ? 0.3f : 1;
	}

	static readonly string[] PadlockArt =
	{
		"..###..",
		".#...#.",
		".#...#.",
		"#######",
		"###.###",
		"###.###",
		"#######",
	};

	void DrawLock()
	{
		var blink = Mathf.PosMod(_t * 3, 1) < 0.7f;
		if (LockKind == "panic")
		{
			// Crashed: a blinking warning sign and a few dead pixels.
			if (blink)
				MicroFont.DrawOutlined(_lock, new Vector2(0, -2 * Px), "!", new Color("ff4d5e"), new Color(0, 0, 0, 0.9f));
			var rng = new RandomNumberGenerator { Seed = (ulong)(Mathf.FloorToInt(_t * 10) + Id * 31) };
			for (var i = 0; i < 4; i++)
				_lock.DrawRect(new Rect2(new Vector2(rng.RandiRange(-7, 6), rng.RandiRange(-7, 6)) * Px, Vector2.One * Px), new Color("3f6fff"));
			return;
		}
		// Encrypted: a gold padlock and scrambled hex underneath.
		var gold = new Color("f0c040");
		for (var y = 0; y < PadlockArt.Length; y++)
			for (var x = 0; x < PadlockArt[y].Length; x++)
				if (PadlockArt[y][x] == '#')
					_lock.DrawRect(new Rect2(new Vector2(x - 3.5f, y - 9) * Px, Vector2.One * Px), y >= 3 ? gold : new Color("c9d0d8"));
		var rnd = new RandomNumberGenerator { Seed = (ulong)(Mathf.FloorToInt(_t * 8) + Id * 17) };
		var hex = $"{rnd.RandiRange(0, 255):X2}{rnd.RandiRange(0, 255):X2}";
		MicroFont.DrawOutlined(_lock, new Vector2(0, 5 * Px), hex, new Color("ff4d5e"), new Color(0, 0, 0, 0.9f));
	}

	public void Fired()
	{
		_kick = 1;
		_light.Energy = 2;
		CreateTween().TweenProperty(_light, "energy", 1f, 0.25f);
		_core.Modulate = new Color(Def.Color, 1);
		CreateTween().TweenProperty(_core, "modulate:a", 0.4f, 0.3f);
	}

	public override void _Process(double delta)
	{
		_t += (float)delta;
		_status.Modulate = new Color(Def.Color, 0.35f + 0.65f * Mathf.Abs(Mathf.Sin(_t * 2.2f)));
		if (_kick > 0)
		{
			_kick = Mathf.Max(_kick - (float)delta * 6, 0);
			_barrels.QueueRedraw();
		}
		_beam?.QueueRedraw();
		if (_locked)
			_lock.QueueRedraw();
	}

	/// <summary>A straight barrel from the centre, in art pixels: thickness, length, colours.</summary>
	void Barrel(float angle, float from, float length, int thick, Color body, Color tip)
	{
		var dir = Vector2.FromAngle(angle);
		var start = dir * from * Px;
		var end = dir * length * Px;
		_barrels.DrawLine(start, end, body, thick * Px);
		_barrels.DrawLine(end - dir * Px, end, tip, thick * Px);
	}

	void DrawBarrels()
	{
		var kick = Mathf.Round(_kick * 2);
		switch (Def.Behavior)
		{
			case "blaster":
				Barrel(_head, 0, 9 - kick, 2, Steel, Def.Color.Lightened(0.4f));
				break;
			case "mortar":
				Barrel(_head, 0, 7 - kick, 4, Def.Color.Darkened(0.55f), Def.Color.Darkened(0.75f));
				break;
			case "burst":
				for (var k = 0; k < 4; k++)
					Barrel(_head + Mathf.Pi / 4 + k * Mathf.Pi / 2, 2, 7.5f - kick, 2, Steel, Def.Color.Lightened(0.3f));
				break;
		}
	}

	public override void _Draw()
	{
		if (!_showRange)
			return;
		var r = Range;
		if (r <= 0)
			return;
		DrawCircle(Vector2.Zero, r, new Color(Def.Color, 0.07f));
		DrawArc(Vector2.Zero, r, 0, Mathf.Tau, 64, new Color(Def.Color, 0.55f), Px);
	}

	void DrawBeam()
	{
		var length = Stats.GetValueOrDefault("beam_length");
		var dir = Vector2.FromAngle(Direction);
		var from = dir * 18;
		var to = dir * (24 + length);
		var hitting = BeamHits.Count > 0;
		var flicker = 0.85f + 0.15f * Mathf.Sin(_t * 40) + (hitting ? 0.1f : 0);
		_beam.DrawLine(from, to, new Color(Def.Color, 0.25f * flicker), Match.BeamWidth * 3);
		_beam.DrawLine(from, to, new Color(Def.Color, 0.7f * flicker), Match.BeamWidth);
		_beam.DrawLine(from, to, new Color(1, 0.9f, 0.95f, 0.9f), Px);
		for (var k = (_t * 220) % 40; k < length; k += 40)
			_beam.DrawRect(new Rect2(PixelWorld.Snap(from + dir * k) - Vector2.One * Px, Vector2.One * Px * 2), new Color(1, 1, 1, 0.8f));
		FxUtil.DrawGlow(_beam, to, 28, new Color(Def.Color, 0.8f));
		FxUtil.DrawGlow(_beam, from, 32, new Color(Def.Color, flicker));
	}
}
