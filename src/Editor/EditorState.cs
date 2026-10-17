using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Brenda.Data;
using Godot;

namespace Brenda.Editor;

/// <summary>
/// The level being edited, with undo/redo and where it is saved. Static so it survives a
/// test play (the editor scene is reloaded afterwards).
/// </summary>
public static class EditorState
{
	public static LevelData Level = NewLevel();
	/// <summary>File it was loaded from / will be saved to (null = not saved yet).</summary>
	public static string Path;
	public static bool Dirty;
	public static event Action Changed;

	static readonly Stack<string> Undo = new();
	static readonly Stack<string> Redo = new();

	public static LevelData NewLevel() => new()
	{
		Title = "New level", Theme = "green", StartMoney = 30, Music = "music_level_1",
		Path = new List<int[]> { new[] { -1, 7 }, new[] { 10, 7 }, new[] { 10, 12 } },
		Waves = new List<List<SpawnData>>
		{
			Enumerable.Range(0, 6).Select(i => new SpawnData { Enemy = "bug", Time = i * 2f }).ToList(),
		},
	};

	public static void Load(LevelData level, string path)
	{
		Level = level;
		Path = path;
		Dirty = false;
		Undo.Clear();
		Redo.Clear();
		Changed?.Invoke();
	}

	/// <summary>Call before changing <see cref="Level"/>, so the change can be undone.</summary>
	public static void Checkpoint()
	{
		Undo.Push(JsonIO.Write(Level));
		if (Undo.Count > 80)
		{
			var keep = Undo.Take(60).Reverse().ToList();
			Undo.Clear();
			foreach (var s in keep)
				Undo.Push(s);
		}
		Redo.Clear();
		Dirty = true;
	}

	/// <summary>Call after a change so views refresh.</summary>
	public static void Touch() => Changed?.Invoke();

	public static void Modify(Action<LevelData> change)
	{
		Checkpoint();
		change(Level);
		Changed?.Invoke();
	}

	public static bool CanUndo => Undo.Count > 0;
	public static bool CanRedo => Redo.Count > 0;

	public static void UndoStep()
	{
		if (Undo.Count == 0)
			return;
		Redo.Push(JsonIO.Write(Level));
		Level = JsonIO.Read<LevelData>(Undo.Pop());
		Dirty = true;
		Changed?.Invoke();
	}

	public static void RedoStep()
	{
		if (Redo.Count == 0)
			return;
		Undo.Push(JsonIO.Write(Level));
		Level = JsonIO.Read<LevelData>(Redo.Pop());
		Dirty = true;
		Changed?.Invoke();
	}

	public static bool IsBuiltIn => Path != null && Path.StartsWith("res://");

	/// <summary>
	/// Saves to the current path. Built-in levels can only be overwritten when running from the
	/// Godot editor (so the change can be committed); otherwise they are saved as a custom copy.
	/// </summary>
	public static string Save(bool asCopy = false)
	{
		Level.SortWaves();
		if (asCopy || Path == null || (IsBuiltIn && !OS.HasFeature("editor")))
			Path = FreeCustomPath(Level.Title);
		if (!Path.StartsWith("res://"))
			DirAccess.MakeDirRecursiveAbsolute(Catalog.CustomLevelDir);
		if (!JsonIO.Save(Path, Level))
			return null;
		if (IsBuiltIn)
			Catalog.ReloadLevels();
		Dirty = false;
		Changed?.Invoke();
		return Path;
	}

	static string FreeCustomPath(string title)
	{
		var slug = Regex.Replace(title.ToLowerInvariant(), "[^a-z0-9]+", "_").Trim('_');
		if (slug == "")
			slug = "level";
		var path = $"{Catalog.CustomLevelDir}/{slug}.json";
		for (var i = 2; FileAccess.FileExists(path); i++)
			path = $"{Catalog.CustomLevelDir}/{slug}_{i}.json";
		return path;
	}

	public static string DisplayName => (Path == null ? "unsaved" : Path.GetFile()) + (Dirty ? " *" : "");
}
