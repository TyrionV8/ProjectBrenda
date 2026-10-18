using System;
using System.Collections.Generic;
using HardwareDefence.Autoload;
using HardwareDefence.Data;
using Godot;

namespace HardwareDefence.Game;

/// <summary>
/// Placing turrets: a ghost follows the pointer, snapped to the grid. Click/tap to build; the
/// laser needs a second click to choose its direction. Right click, Esc or CANCEL stop placing.
/// </summary>
public partial class BuildController : Node2D
{
	public GameScreen Screen;
	public TurretDef Selected { get; private set; }
	public bool Aiming { get; private set; }
	public event Action<TurretDef> SelectionChanged;
	public event Action Denied;

	Vector2I _cell = new(-100, -100);
	Vector2I _aimCell;
	float _aimDir;
	View.TurretView _hover;
	bool _inside;
	float _deny;

	public override void _Ready()
	{
		Material = View.FxUtil.Unshaded;
		ZIndex = 10;
	}

	public void Select(TurretDef def)
	{
		if (!Screen.Playing || !GameState.IsTurretAvailable(def))
			return;
		Selected = def;
		Aiming = false;
		Audio.Ui("click");
		SelectionChanged?.Invoke(def);
		UpdatePointer(Screen.BoardMouse());
		QueueRedraw();
	}

	public void Cancel()
	{
		if (Selected == null)
			return;
		Selected = null;
		Aiming = false;
		SelectionChanged?.Invoke(null);
		QueueRedraw();
	}

	/// <summary>True while placing; Esc then cancels instead of pausing.</summary>
	public bool Busy => Selected != null;

	/// <summary>Mouse/touch input forwarded by the game screen. Returns true when it was used.</summary>
	public bool HandlePointer(InputEvent e)
	{
		if (!Screen.Playing)
			return false;
		if (e is InputEventMouseMotion)
			UpdatePointer(Screen.BoardMouse());
		else if (e is InputEventMouseButton mb && mb.Pressed)
		{
			UpdatePointer(Screen.BoardMouse());
			if (mb.ButtonIndex == MouseButton.Left)
				Primary();
			else if (mb.ButtonIndex == MouseButton.Right)
				Secondary();
			return true;
		}
		return false;
	}

	/// <summary>Esc while placing: stop aiming, or stop placing.</summary>
	public void Back() => Secondary();

	public override void _Process(double delta)
	{
		if (_deny <= 0)
			return;
		_deny = Mathf.Max(_deny - (float)delta * 3, 0);
		QueueRedraw();
	}

	bool CanAfford => Screen.Feed.Current != null && Selected != null && Screen.Feed.Current.Money >= Selected.Cost;

	void Primary()
	{
		if (Selected == null)
			return;
		if (Aiming)
		{
			if (Screen.CanBuild(_aimCell) && CanAfford)
			{
				Screen.Feed.Build(Selected, _aimCell, _aimDir);
				Aiming = false;
			}
			else
				Deny();
			QueueRedraw();
			return;
		}
		if (!_inside)
			return;
		if (!Screen.CanBuild(_cell) || !CanAfford)
		{
			Deny();
			return;
		}
		if (Selected.NeedsDirection)
		{
			Aiming = true;
			_aimCell = _cell;
			_aimDir = 0;
			Audio.Ui("click");
			QueueRedraw();
		}
		else
			Screen.Feed.Build(Selected, _cell, 0);
	}

	void Secondary()
	{
		if (Aiming)
		{
			Aiming = false;
			QueueRedraw();
		}
		else
			Cancel();
	}

	void Deny()
	{
		_deny = 1;
		Audio.Play("error", "UI");
		Denied?.Invoke();
	}

	void UpdatePointer(Vector2 local)
	{
		_inside = new Rect2(Vector2.Zero, Grid.Size).HasPoint(local);
		if (Aiming)
		{
			var dir = local - Grid.CellCenter(_aimCell);
			if (dir.Length() > 6)
				_aimDir = Mathf.Snapped(dir.Angle(), Mathf.Pi / 2);
			QueueRedraw();
			return;
		}
		var cell = Grid.ToCell(local);
		if (cell != _cell)
		{
			_cell = cell;
			QueueRedraw();
		}
		var hovered = _inside && Selected == null ? Screen.TurretViewAt(cell) : null;
		if (hovered == _hover)
			return;
		if (IsInstanceValid(_hover))
			_hover.ShowRange = false;
		_hover = hovered;
		if (_hover != null)
			_hover.ShowRange = true;
	}

	public override void _Draw()
	{
		if (Selected == null || (!Aiming && !_inside))
			return;
		var cell = Aiming ? _aimCell : _cell;
		var center = Grid.CellCenter(cell);
		var ok = Aiming || (Screen.CanBuild(cell) && CanAfford);
		var color = (ok ? new Color(0.3f, 1f, 0.85f) : new Color(1f, 0.3f, 0.35f)).Lerp(new Color(1, 0.2f, 0.2f), _deny);
		var rect = Grid.CellRect(cell, Vector2I.One).Grow(-2);
		DrawRect(rect, new Color(color, 0.18f));
		DrawRect(rect, new Color(color, 0.9f), false, View.PixelWorld.Scale);
		var stats = Screen.Feed.StatsFor(Selected);
		var range = stats.GetValueOrDefault("range");
		if (range > 0 && !Selected.NeedsDirection)
		{
			DrawCircle(center, range, new Color(color, 0.06f));
			DrawArc(center, range, 0, Mathf.Tau, 64, new Color(color, 0.5f), View.PixelWorld.Scale);
		}
		var icon = GD.Load<Texture2D>(Selected.Icon);
		DrawSetTransform(center, Selected.NeedsDirection ? _aimDir : 0, Vector2.One * View.PixelWorld.Scale);
		DrawTexture(icon, -icon.GetSize() / 2, new Color(1, 1, 1, 0.7f));
		DrawSetTransformMatrix(Transform2D.Identity);
		if (!Selected.NeedsDirection)
			return;
		var length = 24 + stats.GetValueOrDefault("beam_length", 48);
		for (var d = 0; d < 4; d++)
		{
			var a = d * Mathf.Pi / 2;
			var active = Aiming && Mathf.IsZeroApprox(Mathf.Wrap(a - _aimDir, -Mathf.Pi, Mathf.Pi));
			var tip = center + Vector2.FromAngle(a) * (active ? length : 30);
			DrawLine(center + Vector2.FromAngle(a) * 24, tip, new Color(Selected.Color, active ? 0.9f : 0.3f), active ? 6 : 3);
		}
		var label = Aiming ? "click to confirm" : "click to aim";
		View.MicroFont.DrawOutlined(this, center + new Vector2(0, -36), label, Colors.White, new Color(0, 0, 0, 0.85f));
	}
}
