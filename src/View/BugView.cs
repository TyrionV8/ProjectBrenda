using HardwareDefence.Data;
using HardwareDefence.Sim;
using Godot;

namespace HardwareDefence.View;

/// <summary>A bug on screen, updated from snapshots. The root draws the health bar; the body turns.</summary>
public partial class BugView : Node2D
{
	const int Px = PixelWorld.Scale;

	public readonly EnemyDef Def;
	public readonly int Id;
	BugBody _body;
	float _hp;
	float _flash;
	bool _barVisible;
	Vector2 _lastPos;
	bool _placed;
	float _angle;
	float _cloak;

	public BugView(EnemyDef def, int id)
	{
		Def = def;
		Id = id;
		_hp = def.MaxHp;
	}

	public override void _Ready()
	{
		Material = FxUtil.Unshaded;
		_body = new BugBody(Def) { Scale = Vector2.One * Px };
		AddChild(_body);
		// Spawn with a white flash rather than a scale pop, which would smear the pixels.
		_flash = 1;
	}

	public void Apply(BugSnap s, float delta)
	{
		var pos = new Vector2(s.X, s.Y);
		if (!_placed)
		{
			_placed = true;
			_lastPos = pos;
			_angle = s.Angle;
		}
		Position = pos;
		_angle = Mathf.LerpAngle(_angle, s.Angle, 1 - Mathf.Exp(-14 * delta));
		_body.Facing = _angle;
		var moved = pos.DistanceTo(_lastPos);
		_lastPos = pos;
		_body.Chewing = s.Chewing;
		_body.Animate(moved, delta);
		_body.SetFrozen(s.Slowed ? 1 : 0);
		if (Def.Ability == "trojan")
			_body.SetDisguised(!s.Revealed);
		if (Def.Ability == "rootkit")
		{
			_cloak = Mathf.MoveToward(_cloak, s.Cloaked ? 0.9f : 0, delta * 4);
			_body.SetCloak(_cloak);
		}
		if (s.Hp < _hp - 0.001f)
		{
			_flash = 0.8f;
			_barVisible = true;
			QueueRedraw();
		}
		if (!Mathf.IsEqualApprox(s.Hp, _hp))
		{
			_hp = s.Hp;
			QueueRedraw();
		}
		if (_flash > 0)
		{
			_flash = Mathf.Max(_flash - delta * 8, 0);
			_body.SetFlash(_flash);
		}
	}

	/// <summary>Segmented health bar in art pixels: one segment per HP for weak bugs.</summary>
	public override void _Draw()
	{
		if (!_barVisible)
			return;
		var frame = _body.CurrentFrameSize;
		var segments = Def.MaxHp <= 12 ? (int)Def.MaxHp : 10;
		var wanted = Mathf.Max(frame.X - 2, 9);
		segments = Mathf.Clamp(segments, 1, (wanted + 1) / 2);
		var segW = Mathf.Max((wanted - (segments - 1)) / segments, 1);
		var width = segments * segW + segments - 1;
		var x0 = -width / 2;
		var y0 = -frame.Y / 2 - 3;
		var ratio = Mathf.Clamp(_hp / Def.MaxHp, 0, 1);
		var filled = ratio * segments;
		var color = new Color("ff4d5e").Lerp(new Color("7dff6a"), ratio);
		DrawRect(new Rect2(new Vector2(x0 - 1, y0 - 1) * Px, new Vector2(width + 2, 4) * Px), new Color(0, 0, 0, 0.7f));
		for (var i = 0; i < segments; i++)
		{
			var x = x0 + i * (segW + 1);
			DrawRect(new Rect2(new Vector2(x, y0) * Px, new Vector2(segW, 2) * Px), new Color(1, 1, 1, 0.15f));
			var fill = Mathf.RoundToInt(Mathf.Clamp(filled - i, 0, 1) * segW);
			if (fill > 0)
				DrawRect(new Rect2(new Vector2(x, y0) * Px, new Vector2(fill, 2) * Px), color);
		}
	}
}
