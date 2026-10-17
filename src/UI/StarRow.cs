using Godot;

namespace Brenda.UI;

/// <summary>Up to three stars (level rating).</summary>
public partial class StarRow : Control
{
	readonly int _stars;
	readonly float _size;

	public StarRow(int stars, float size)
	{
		_stars = stars;
		_size = size;
		CustomMinimumSize = new Vector2(size * 3.4f, size);
		MouseFilter = MouseFilterEnum.Ignore;
	}

	public override void _Draw()
	{
		for (var i = 0; i < 3; i++)
		{
			var c = new Vector2(_size * (0.6f + i * 1.1f), _size / 2);
			var points = new Vector2[10];
			for (var k = 0; k < 10; k++)
			{
				var r = k % 2 == 0 ? _size * 0.48f : _size * 0.2f;
				points[k] = c + Vector2.FromAngle(-Mathf.Pi / 2 + k * Mathf.Pi / 5) * r;
			}
			var lit = i < _stars;
			DrawColoredPolygon(points, lit ? UiTheme.Gold : new Color(1, 1, 1, 0.1f));
			var outline = new Vector2[11];
			points.CopyTo(outline, 0);
			outline[10] = points[0];
			DrawPolyline(outline, lit ? UiTheme.Gold.Lightened(0.4f) : new Color(1, 1, 1, 0.2f), 2, true);
		}
	}
}
