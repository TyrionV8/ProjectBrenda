using System;
using System.Collections.Generic;
using System.Linq;
using HardwareDefence.Data;
using Godot;

namespace HardwareDefence.Autoload;

/// <summary>Campaign progress: unlocked levels, stars, upgrades and points. Saved in user://save.json.</summary>
public static class GameState
{
	public const string SavePath = "user://save.json";
	public const int StartPoints = 5;
	/// <summary>Points earned the first time each level is cleared.</summary>
	public const int PointsPerLevel = 3;

	sealed class SaveData
	{
		public int Version { get; set; } = 2;
		public List<bool> Unlocked { get; set; }
		public List<int> Stars { get; set; }
		public Dictionary<string, Dictionary<string, int>> Upgrades { get; set; }
		public int Points { get; set; }
	}

	public static event Action Changed;

	public static int CurrentLevel;
	public static List<bool> Unlocked = new();
	public static List<int> Stars = new();
	public static Dictionary<string, Dictionary<string, int>> Upgrades = new();
	public static int Points = StartPoints;

	static GameState() => Reset();

	public static void Reset()
	{
		var count = Catalog.Levels.Count;
		CurrentLevel = 0;
		Unlocked = Enumerable.Repeat(false, count).ToList();
		if (count > 0)
			Unlocked[0] = true;
		Stars = Enumerable.Repeat(0, count).ToList();
		Upgrades = new Dictionary<string, Dictionary<string, int>>();
		Points = StartPoints;
		Changed?.Invoke();
	}

	public static bool HasSave() => FileAccess.FileExists(SavePath);

	public static void Save() => JsonIO.Save(SavePath, new SaveData { Unlocked = Unlocked, Stars = Stars, Upgrades = Upgrades, Points = Points });

	public static bool Load()
	{
		if (!HasSave())
			return false;
		SaveData data;
		try
		{
			data = JsonIO.Load<SaveData>(SavePath);
		}
		catch (Exception e)
		{
			GD.PushError($"Broken save file: {e.Message}");
			return false;
		}
		if (data == null)
			return false;
		Reset();
		for (var i = 0; i < Math.Min(data.Unlocked?.Count ?? 0, Unlocked.Count); i++)
			Unlocked[i] = data.Unlocked[i];
		for (var i = 0; i < Math.Min(data.Stars?.Count ?? 0, Stars.Count); i++)
			Stars[i] = data.Stars[i];
		Upgrades = data.Upgrades ?? new();
		Points = data.Points;
		CurrentLevel = LastUnlocked();
		Changed?.Invoke();
		return true;
	}

	public static int LastUnlocked() => Math.Max(Unlocked.LastIndexOf(true), 0);

	public static bool IsUnlocked(int index) => index >= 0 && index < Unlocked.Count && Unlocked[index];

	public static IReadOnlyDictionary<string, int> LevelsOf(TurretDef def) =>
		Upgrades.TryGetValue(def.Id, out var levels) ? levels : null;

	public static int UpgradeLevel(string turret, string upgrade) =>
		Upgrades.TryGetValue(turret, out var levels) && levels.TryGetValue(upgrade, out var l) ? l : 0;

	public static bool CanBuy(TurretDef turret, UpgradeDef up) => Points > 0 && UpgradeLevel(turret.Id, up.Id) < up.MaxLevel;

	public static bool Buy(TurretDef turret, UpgradeDef up)
	{
		if (!CanBuy(turret, up))
			return false;
		if (!Upgrades.TryGetValue(turret.Id, out var levels))
			Upgrades[turret.Id] = levels = new Dictionary<string, int>();
		levels[up.Id] = levels.GetValueOrDefault(up.Id) + 1;
		Points--;
		Changed?.Invoke();
		return true;
	}

	public static Dictionary<string, float> TurretStats(TurretDef def) => def.ComputeStats(LevelsOf(def));

	/// <summary>Marks the current level cleared with the given stars. Returns true after the last level.</summary>
	public static bool CompleteCurrentLevel(int stars)
	{
		if (CurrentLevel < Stars.Count)
			Stars[CurrentLevel] = Math.Max(Stars[CurrentLevel], stars);
		var next = CurrentLevel + 1;
		if (next >= Unlocked.Count)
		{
			Save();
			return true;
		}
		if (!Unlocked[next])
		{
			Unlocked[next] = true;
			Points += PointsPerLevel;
		}
		CurrentLevel = next;
		Save();
		Changed?.Invoke();
		return false;
	}
}
