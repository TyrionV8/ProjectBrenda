using System;
using System.Collections.Generic;

namespace HardwareDefence.Tests;

/// <summary>Marks a test method (sync or async Task). Run with `-- --test`.</summary>
[AttributeUsage(AttributeTargets.Method)]
public sealed class TestAttribute : Attribute
{
}

/// <summary>Minimal assertions: failures are collected, not thrown.</summary>
public abstract class TestSuite
{
	public readonly List<string> Errors = new();
	/// <summary>Optional note printed next to PASS (e.g. simulation results).</summary>
	public string Note;

	protected void Check(bool condition, string message)
	{
		if (!condition)
			Errors.Add(message);
	}

	protected void CheckEq<T>(T actual, T expected, string message = "")
	{
		if (!EqualityComparer<T>.Default.Equals(actual, expected))
			Errors.Add($"{message} expected {expected}, got {actual}");
	}

	protected void CheckNear(float actual, float expected, string message = "", float tolerance = 0.001f)
	{
		if (Math.Abs(actual - expected) > tolerance)
			Errors.Add($"{message} expected {expected:0.####}, got {actual:0.####}");
	}
}
