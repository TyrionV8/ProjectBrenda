using System.Collections.Generic;
using System.Linq;
using Godot;

namespace HardwareDefence.Data;

/// <summary>All read-only game data: turret and bug types and the built-in levels.</summary>
public static class Catalog
{
	public const string LevelPath = "res://data/levels/level_{0}.json";
	public const string CustomLevelDir = "user://levels";

	static List<TurretDef> _turrets;
	static Dictionary<string, EnemyDef> _enemies;
	static List<EnemyDef> _enemyList;
	static List<LevelData> _levels;

	public static IReadOnlyList<TurretDef> Turrets => _turrets ??= JsonIO.Load<List<TurretDef>>("res://data/turrets.json");

	public static IReadOnlyList<EnemyDef> EnemyList => _enemyList ??= JsonIO.Load<List<EnemyDef>>("res://data/enemies.json");

	public static IReadOnlyDictionary<string, EnemyDef> Enemies => _enemies ??= EnemyList.ToDictionary(e => e.Id);

	/// <summary>Bugs the attacker can send in versus (hotkeys 1-9 follow this order).</summary>
	public static IReadOnlyList<EnemyDef> VersusEnemies => _versusEnemies ??= EnemyList.Where(e => e.InVersus).ToList();

	/// <summary>Bugs a level's waves can use (everything but the minions bosses release).</summary>
	public static IReadOnlyList<EnemyDef> WaveEnemies => _waveEnemies ??= EnemyList.Where(e => !e.Minion).ToList();

	static List<EnemyDef> _versusEnemies;
	static List<EnemyDef> _waveEnemies;

	public static IReadOnlyList<LevelData> Levels
	{
		get
		{
			if (_levels != null)
				return _levels;
			_levels = new List<LevelData>();
			for (var i = 1; FileAccess.FileExists(string.Format(LevelPath, i)); i++)
			{
				var level = JsonIO.Load<LevelData>(string.Format(LevelPath, i));
				level.SortWaves();
				_levels.Add(level);
			}
			return _levels;
		}
	}

	static Sim.VersusRules _versus;

	/// <summary>Versus rules from data/versus.json (falls back to the defaults in code).</summary>
	public static Sim.VersusRules Versus => _versus ??= FileAccess.FileExists("res://data/versus.json") ? JsonIO.Load<Sim.VersusRules>("res://data/versus.json") : new Sim.VersusRules();

	public static TurretDef Turret(string id) => Turrets.FirstOrDefault(t => t.Id == id);

	public static EnemyDef Enemy(string id) => Enemies.TryGetValue(id, out var e) ? e : null;

	/// <summary>Forget cached levels (after the editor saves one).</summary>
	public static void ReloadLevels() => _levels = null;

	/// <summary>Player-made levels saved by the editor in user://levels.</summary>
	public static List<(string Path, LevelData Level)> CustomLevels()
	{
		var list = new List<(string, LevelData)>();
		if (!DirAccess.DirExistsAbsolute(CustomLevelDir))
			return list;
		foreach (var file in DirAccess.GetFilesAt(CustomLevelDir).Where(f => f.EndsWith(".json")).OrderBy(f => f))
		{
			var path = $"{CustomLevelDir}/{file}";
			var level = JsonIO.Load<LevelData>(path);
			if (level == null)
				continue;
			level.SortWaves();
			list.Add((path, level));
		}
		return list;
	}
}
