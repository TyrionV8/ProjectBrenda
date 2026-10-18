using System.Linq;
using HardwareDefence.Data;
using HardwareDefence.Editor;
using Godot;

namespace HardwareDefence.Tests;

public sealed class EditorTests : TestSuite
{
	[Test]
	public void UndoRedoRestoresLevel()
	{
		EditorState.Load(EditorState.NewLevel(), null);
		var before = EditorState.Level.Components.Count;
		EditorState.Modify(l => l.Components.Add(new ComponentData { Type = "chip", Cell = new[] { 2, 2 }, Size = new[] { 2, 2 } }));
		CheckEq(EditorState.Level.Components.Count, before + 1, "added");
		Check(EditorState.Dirty, "dirty after change");
		EditorState.UndoStep();
		CheckEq(EditorState.Level.Components.Count, before, "undone");
		EditorState.RedoStep();
		CheckEq(EditorState.Level.Components.Count, before + 1, "redone");
	}

	[Test]
	public void CanPlaceRespectsRouteAndOverlap()
	{
		EditorState.Load(EditorState.NewLevel(), null);
		var onRoute = new ComponentData { Type = "chip", Cell = new[] { 3, 7 }, Size = new[] { 1, 1 } };
		Check(!MapCanvas.CanPlace(onRoute, -1), "can't place on the route");
		var free = new ComponentData { Type = "chip", Cell = new[] { 2, 2 }, Size = new[] { 2, 2 } };
		Check(MapCanvas.CanPlace(free, -1), "free spot");
		EditorState.Modify(l => l.Components.Add(free));
		var overlap = new ComponentData { Type = "smd", Cell = new[] { 3, 3 } };
		Check(!MapCanvas.CanPlace(overlap, -1), "overlap blocked");
		var label = new ComponentData { Type = "label", Cell = new[] { 3, 3 }, Size = new[] { 3, 1 }, Label = "HI" };
		Check(MapCanvas.CanPlace(label, -1), "labels can overlap");
		var outside = new ComponentData { Type = "chip", Cell = new[] { 20, 15 }, Size = new[] { 2, 2 } };
		Check(!MapCanvas.CanPlace(outside, -1), "must fit on the board");
	}

	[Test]
	public void SaveCustomLevelAndList()
	{
		EditorState.Load(EditorState.NewLevel(), null);
		EditorState.Modify(l => l.Title = "Test Board 123");
		var path = EditorState.Save();
		Check(path != null && path.StartsWith("user://levels/"), $"saved to {path}");
		Check(!EditorState.Dirty, "clean after save");
		var listed = Catalog.CustomLevels().Any(x => x.Path == path && x.Level.Title == "Test Board 123");
		Check(listed, "listed as custom level");
		CheckEq(EditorState.Level.Validate(Catalog.Enemies.Keys.ToList()).Count, 0, "new level template is valid");
		DirAccess.RemoveAbsolute(path);
		EditorState.Load(EditorState.NewLevel(), null);
	}
}
