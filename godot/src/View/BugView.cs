using Brenda.Data;
using Brenda.Sim;
using Godot;

namespace Brenda.View;

/// <summary>A bug on screen, updated from snapshots. The root draws the health bar; the body rotates.</summary>
public partial class BugView : Node2D
{
	public readonly EnemyDef Def;
	public readonly int Id;
	BugBody _body;
	float _hp;
	float _flash;
	bool _barVisible;
	Vector2 _lastPos;
	bool _placed;

	public BugView(EnemyDef def, int id)
	{
		Def = def;
		Id = id;
		_hp = def.MaxHp;
	}

	public override void _Ready()
	{
		Material = FxUtil.Unshaded;
		_body = new BugBody(Def);
		AddChild(_body);
		Scale = Vector2.One * 0.2f;
		CreateTween().TweenProperty(this, "scale", Vector2.One, 0.25f).SetTrans(Tween.TransitionType.Back).SetEase(Tween.EaseType.Out);
	}

	public void Apply(BugSnap s, float delta)
	{
		var pos = new Vector2(s.X, s.Y);
		if (!_placed)
		{
			_placed = true;
			_lastPos = pos;
			_body.Rotation = s.Angle;
		}
		Position = pos;
		_body.Rotation = Mathf.LerpAngle(_body.Rotation, s.Angle, 1 - Mathf.Exp(-14 * delta));
		var moved = pos.DistanceTo(_lastPos);
		_lastPos = pos;
		_body.Chewing = s.Chewing;
		_body.Animate(moved * 0.45f / Mathf.Max(Def.Radius / 12f, 0.6f) + (s.Chewing ? delta * 2 : 0), delta);
		_body.SetFrozen(s.Slowed ? 1 : 0);
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

	/// <summary>Segmented health bar like the original prototype: one square per HP for weak bugs.</summary>
	public override void _Draw()
	{
		if (!_barVisible)
			return;
		var width = Mathf.Max(Def.Radius * 2.2f, 26);
		var y = -Def.Radius - 12;
		var segments = Def.MaxHp <= 12 ? (int)Def.MaxHp : 10;
		const float gap = 2;
		var segW = (width - gap * (segments - 1)) / segments;
		var ratio = Mathf.Clamp(_hp / Def.MaxHp, 0, 1);
		var filled = ratio * segments;
		DrawRect(new Rect2(-width / 2 - 2, y - 2, width + 4, 8), new Color(0, 0, 0, 0.6f));
		var color = new Color("ff4d5e").Lerp(new Color("7dff6a"), ratio);
		for (var i = 0; i < segments; i++)
		{
			var x = -width / 2 + i * (segW + gap);
			var fill = Mathf.Clamp(filled - i, 0, 1);
			DrawRect(new Rect2(x, y, segW, 4), new Color(1, 1, 1, 0.12f));
			if (fill > 0)
				DrawRect(new Rect2(x, y, segW * fill, 4), color);
		}
	}
}
