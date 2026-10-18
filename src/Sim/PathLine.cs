using System.Collections.Generic;
using Godot;

namespace HardwareDefence.Sim;

/// <summary>A polyline that can be sampled by distance travelled (the bug route).</summary>
public sealed class PathLine
{
	readonly Vector2[] _points;
	readonly float[] _distance;

	public float Length { get; }
	public IReadOnlyList<Vector2> Points => _points;

	public PathLine(Vector2[] points)
	{
		_points = points;
		_distance = new float[points.Length];
		for (var i = 1; i < points.Length; i++)
			_distance[i] = _distance[i - 1] + points[i - 1].DistanceTo(points[i]);
		Length = points.Length > 0 ? _distance[^1] : 0;
	}

	public Vector2 Sample(float d)
	{
		if (_points.Length == 0)
			return Vector2.Zero;
		if (d <= 0)
			return _points[0];
		for (var i = 1; i < _points.Length; i++)
		{
			if (d <= _distance[i])
			{
				var seg = _distance[i] - _distance[i - 1];
				return _points[i - 1].Lerp(_points[i], seg <= 0 ? 0 : (d - _distance[i - 1]) / seg);
			}
		}
		return _points[^1];
	}

	public float AngleAt(float d)
	{
		var a = Sample(d);
		var b = Sample(Mathf.Min(d + 6, Length));
		return a == b ? (Sample(Length) - Sample(Mathf.Max(Length - 6, 0))).Angle() : (b - a).Angle();
	}

	/// <summary>Points every <paramref name="step"/> pixels along the route.</summary>
	public IEnumerable<Vector2> Samples(float step)
	{
		for (var d = 0f; d <= Length; d += step)
			yield return Sample(d);
	}
}
