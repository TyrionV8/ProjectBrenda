using System.Collections.Generic;
using HardwareDefence.Data;
using HardwareDefence.Sim;
using Godot;

namespace HardwareDefence.View;

/// <summary>A turret on screen: socket, rotating head, coloured light, status LED and (laser) beam.</summary>
public partial class TurretView : Node2D
{
	static readonly Texture2D Socket = GD.Load<Texture2D>("res://assets/art/turrets/socket.svg");

	public readonly TurretDef Def;
	public readonly int Id;
	public Dictionary<string, float> Stats;
	public float Direction;
	Node2D _head;
	Sprite2D _headSprite;
	PointLight2D _light;
	Sprite2D _status;
	Sprite2D _core;
	Node2D _beam;
	PointLight2D _beamLight;
	float _t = GD.Randf() * 10;
	bool _showRange;
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
		Stats = stats;
		Position = Grid.CellCenter(cell);
	}

	public float Range => Def.Behavior == "laser" ? 0 : Stats.GetValueOrDefault("range");

	public override void _Ready()
	{
		AddChild(new Sprite2D { Texture = Socket, Scale = Vector2.One * 0.5f });
		_core = FxUtil.GlowSprite(Def.Color, 0.55f);
		_core.Modulate = new Color(Def.Color, 0.5f);
		AddChild(_core);
		_head = new Node2D { Rotation = Direction };
		AddChild(_head);
		_headSprite = new Sprite2D { Texture = GD.Load<Texture2D>(Def.Icon), Scale = Vector2.One * 0.5f };
		_head.AddChild(_headSprite);
		_light = FxUtil.Light(Def.Color, 1, 0.85f);
		AddChild(_light);
		_status = FxUtil.GlowSprite(Def.Color, 0.12f);
		_status.Position = new Vector2(15, 15);
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
		Scale = Vector2.One * 0.3f;
		CreateTween().TweenProperty(this, "scale", Vector2.One, 0.35f).SetTrans(Tween.TransitionType.Back).SetEase(Tween.EaseType.Out);
		_light.Energy = 4;
		CreateTween().TweenProperty(_light, "energy", 1f, 0.6f);
	}

	public void Apply(TurretSnap s) => _head.Rotation = s.Head;

	public void Fired()
	{
		_headSprite.Position = new Vector2(Def.Behavior == "burst" || Def.Behavior == "freezer" ? 0 : -5, 0);
		CreateTween().TweenProperty(_headSprite, "position", Vector2.Zero, 0.18f).SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Cubic);
		_light.Energy = 2;
		CreateTween().TweenProperty(_light, "energy", 1f, 0.25f);
		_core.Modulate = new Color(Def.Color, 1);
		CreateTween().TweenProperty(_core, "modulate:a", 0.5f, 0.3f);
	}

	public override void _Process(double delta)
	{
		_t += (float)delta;
		_status.Modulate = new Color(Def.Color, 0.35f + 0.65f * Mathf.Abs(Mathf.Sin(_t * 2.2f)));
		_beam?.QueueRedraw();
	}

	public override void _Draw()
	{
		if (!_showRange)
			return;
		var r = Range;
		if (r <= 0)
			return;
		DrawCircle(Vector2.Zero, r, new Color(Def.Color, 0.07f));
		DrawArc(Vector2.Zero, r, 0, Mathf.Tau, 64, new Color(Def.Color, 0.55f), 2, true);
	}

	void DrawBeam()
	{
		var length = Stats.GetValueOrDefault("beam_length");
		var dir = Vector2.FromAngle(Direction);
		var from = dir * 24;
		var to = dir * (24 + length);
		var hitting = BeamHits.Count > 0;
		var flicker = 0.85f + 0.15f * Mathf.Sin(_t * 40) + (hitting ? 0.1f : 0);
		_beam.DrawLine(from, to, new Color(Def.Color, 0.25f * flicker), Match.BeamWidth * 3, true);
		_beam.DrawLine(from, to, new Color(Def.Color, 0.7f * flicker), Match.BeamWidth, true);
		_beam.DrawLine(from, to, new Color(1, 0.9f, 0.95f, 0.9f), 2, true);
		for (var k = (_t * 220) % 40; k < length; k += 40)
			_beam.DrawCircle(from + dir * k, 2.5f, new Color(1, 1, 1, 0.8f));
		FxUtil.DrawGlow(_beam, to, 28, new Color(Def.Color, 0.8f));
		FxUtil.DrawGlow(_beam, from, 32, new Color(Def.Color, flicker));
	}
}
