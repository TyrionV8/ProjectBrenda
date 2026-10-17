using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using HardwareDefence.Data;
using Godot;

namespace HardwareDefence.Autoload;

/// <summary>Campaign progress and money earned from each level's best remaining cash. Saved as v3.</summary>
public static class GameState
{
	public const string SavePath = "user://save.json";
	public const int LegacyPointValue = 20;
	static string _activeSavePath = SavePath;

	/// <summary>Captures use a disposable campaign file, never the player's normal save.</summary>
	public static IDisposable UseTemporarySave() => new TemporarySaveScope();

	sealed class TemporarySaveScope : IDisposable
	{
		readonly string _previousPath = _activeSavePath;
		readonly string _temporaryPath = $"user://capture-campaign-{Guid.NewGuid():N}.json";
		bool _disposed;

		public TemporarySaveScope() => _activeSavePath = _temporaryPath;

		public void Dispose()
		{
			if (_disposed) return;
			_disposed = true;
			_activeSavePath = _previousPath;
			DirAccess.RemoveAbsolute(_temporaryPath);
			DirAccess.RemoveAbsolute(_temporaryPath + ".tmp");
		}
	}

	sealed class SaveData
	{
		// Missing versions belong to the old points format. Save() always writes version 3.
		public int Version { get; set; } = 2;
		public List<bool> Unlocked { get; set; }
		public List<int> Stars { get; set; }
		public Dictionary<string, Dictionary<string, int>> Upgrades { get; set; }
		[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
		public int Points { get; set; }
		public int Balance { get; set; }
		public List<int> BestRemainingMoney { get; set; }
		public HashSet<string> UnlockedTurrets { get; set; }
	}

	public static event Action Changed;

	public static int CurrentLevel;
	public static List<bool> Unlocked = new();
	public static List<int> Stars = new();
	public static Dictionary<string, Dictionary<string, int>> Upgrades = new();
	public static int Balance;
	public static List<int> BestRemainingMoney = new();
	public static HashSet<string> UnlockedTurrets = new();

	static GameState() => Reset();

	public static void Reset()
	{
		var count = Catalog.Levels.Count;
		CurrentLevel = 0;
		Unlocked = Enumerable.Repeat(false, count).ToList();
		if (count > 0)
			Unlocked[0] = true;
		Stars = Enumerable.Repeat(0, count).ToList();
		Upgrades = new();
		Balance = 0;
		BestRemainingMoney = Enumerable.Repeat(0, count).ToList();
		UnlockedTurrets = new();
		Changed?.Invoke();
	}

	public static bool HasSave() => FileAccess.FileExists(_activeSavePath);

	/// <summary>Write a complete temporary file before replacing the previous save.</summary>
	public static bool Save(string path = null)
	{
		path ??= _activeSavePath;
		var temporary = path + ".tmp";
		var data = new SaveData
		{
			Version = 3, Unlocked = Unlocked, Stars = Stars, Upgrades = Upgrades, Balance = Balance,
			BestRemainingMoney = BestRemainingMoney, UnlockedTurrets = UnlockedTurrets,
		};
		Error writeError;
		using (var file = FileAccess.Open(temporary, FileAccess.ModeFlags.Write))
		{
			if (file == null)
			{
				GD.PushError($"Can't write campaign save: {FileAccess.GetOpenError()}");
				return false;
			}
			file.StoreString(JsonIO.Write(data, true));
			file.Flush();
			writeError = file.GetError();
		}
		if (writeError != Error.Ok)
		{
			GD.PushError($"Can't finish writing campaign save: {writeError}");
			DirAccess.RemoveAbsolute(temporary);
			return false;
		}
		var error = DirAccess.RenameAbsolute(temporary, path);
		if (error == Error.Ok)
			return true;
		GD.PushError($"Can't replace campaign save: {error}");
		DirAccess.RemoveAbsolute(temporary);
		return false;
	}

	public static bool Load(string path = null)
	{
		path ??= _activeSavePath;
		if (!FileAccess.FileExists(path))
			return false;
		SaveData data;
		try
		{
			data = JsonIO.Load<SaveData>(path);
		}
		catch (Exception e)
		{
			GD.PushError($"Broken save file: {e.Message}");
			return false;
		}
		if (data == null || data.Version < 1 || data.Version > 3)
			return false;
		// Build the whole state first: subscribers must never observe a half-loaded migration.
		var count = Catalog.Levels.Count;
		Unlocked = Enumerable.Range(0, count).Select(i => i == 0 || (i < (data.Unlocked?.Count ?? 0) && data.Unlocked[i])).ToList();
		Stars = Enumerable.Range(0, count).Select(i => i < (data.Stars?.Count ?? 0) ? Math.Clamp(data.Stars[i], 0, 3) : 0).ToList();
		Upgrades = new();
		foreach (var (turret, levels) in data.Upgrades ?? new())
		{
			if (levels == null)
				continue;
			Upgrades[turret] = levels.ToDictionary(pair => pair.Key, pair => Math.Max(0, pair.Value));
		}
		Balance = data.Version < 3 ? (int)Math.Clamp((long)data.Points * LegacyPointValue, 0, int.MaxValue) : Math.Max(0, data.Balance);
		BestRemainingMoney = Enumerable.Range(0, count).Select(i => data.Version == 3 && i < (data.BestRemainingMoney?.Count ?? 0) ? Math.Max(0, data.BestRemainingMoney[i]) : 0).ToList();
		UnlockedTurrets = data.Version == 3 && data.UnlockedTurrets != null ? new(data.UnlockedTurrets.Where(id => !string.IsNullOrWhiteSpace(id))) : new();
		CurrentLevel = LastUnlocked();
		Changed?.Invoke();
		return true;
	}

	public static int LastUnlocked() => Math.Max(Unlocked.LastIndexOf(true), 0);
	public static bool IsUnlocked(int index) => index >= 0 && index < Unlocked.Count && Unlocked[index];
	public static bool IsTurretAvailable(TurretDef def) => def != null && (def.UnlockPrice <= 0 || UnlockedTurrets.Contains(def.Id));
	public static bool CanUnlock(TurretDef def) => def != null && !string.IsNullOrWhiteSpace(def.Id) && !IsTurretAvailable(def) && Balance >= def.UnlockPrice;

	public static bool Unlock(TurretDef def)
	{
		if (!CanUnlock(def))
			return false;
		Balance -= def.UnlockPrice;
		UnlockedTurrets.Add(def.Id);
		if (!Save())
		{
			Balance += def.UnlockPrice;
			UnlockedTurrets.Remove(def.Id);
			return false;
		}
		Changed?.Invoke();
		return true;
	}

	public static IReadOnlyDictionary<string, int> LevelsOf(TurretDef def) =>
		Upgrades.TryGetValue(def.Id, out var levels) ? levels : null;

	public static int UpgradeLevel(string turret, string upgrade) =>
		Upgrades.TryGetValue(turret, out var levels) && levels.TryGetValue(upgrade, out var l) ? l : 0;

	/// <summary>Price of the next level, or zero when the upgrade has reached its maximum.</summary>
	public static int NextUpgradePrice(TurretDef turret, UpgradeDef up)
	{
		if (turret == null || up == null || !turret.Upgrades.Contains(up) || up.Price <= 0 || up.PriceStep < 0)
			return 0;
		var level = UpgradeLevel(turret.Id, up.Id);
		return level >= up.MaxLevel ? 0 : (int)Math.Min((long)up.Price + (long)up.PriceStep * level, int.MaxValue);
	}

	public static bool CanBuy(TurretDef turret, UpgradeDef up)
	{
		var price = NextUpgradePrice(turret, up);
		return IsTurretAvailable(turret) && price > 0 && Balance >= price;
	}

	public static bool Buy(TurretDef turret, UpgradeDef up)
	{
		if (!CanBuy(turret, up))
			return false;
		var price = NextUpgradePrice(turret, up);
		var hadTurret = Upgrades.TryGetValue(turret.Id, out var levels);
		if (!hadTurret)
			Upgrades[turret.Id] = levels = new();
		var hadUpgrade = levels.TryGetValue(up.Id, out var previous);
		levels[up.Id] = previous + 1;
		Balance -= price;
		if (!Save())
		{
			Balance += price;
			if (hadUpgrade) levels[up.Id] = previous;
			else levels.Remove(up.Id);
			if (!hadTurret) Upgrades.Remove(turret.Id);
			return false;
		}
		Changed?.Invoke();
		return true;
	}

	public static Dictionary<string, float> TurretStats(TurretDef def) => def.ComputeStats(LevelsOf(def));

	/// <summary>Credit only improvement over this campaign level's record, then advance. Match money is independent.</summary>
	public static bool CompleteCurrentLevel(int stars, int remainingMoney)
	{
		if (CurrentLevel < 0 || CurrentLevel >= Stars.Count)
			return false;
		var level = CurrentLevel;
		var oldStars = Stars[level];
		var oldBest = BestRemainingMoney[level];
		var oldBalance = Balance;
		var next = level + 1;
		var finished = next >= Unlocked.Count;
		var wasUnlocked = !finished && Unlocked[next];
		Stars[level] = Math.Max(oldStars, Math.Clamp(stars, 0, 3));
		BestRemainingMoney[level] = Math.Max(oldBest, Math.Max(0, remainingMoney));
		Balance = (int)Math.Min((long)Balance + BestRemainingMoney[level] - oldBest, int.MaxValue);
		if (!finished)
		{
			Unlocked[next] = true;
			CurrentLevel = next;
		}
		if (!Save())
		{
			Stars[level] = oldStars;
			BestRemainingMoney[level] = oldBest;
			Balance = oldBalance;
			CurrentLevel = level;
			if (!finished) Unlocked[next] = wasUnlocked;
			return false;
		}
		Changed?.Invoke();
		return finished;
	}
}
