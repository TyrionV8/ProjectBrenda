using System.Collections.Generic;
using System.Linq;
using HardwareDefence.Data;
using HardwareDefence.Sim;
using HardwareDefence.View;
using Godot;

namespace HardwareDefence.Tests;

/// <summary>Asset/view contract, including art prepared before counter-turret gameplay.</summary>
public sealed class TurretArtTests : TestSuite
{
	static IEnumerable<TurretDef> Definitions() => Catalog.Turrets.Concat(new[]
	{
		new TurretDef { Id = "antivirus", Behavior = "antivirus", ColorHex = "3dff6a" },
		new TurretDef { Id = "firewall", Behavior = "firewall", ColorHex = "ff8a3d" },
		new TurretDef { Id = "backup", Behavior = "backup", ColorHex = "4da3ff" },
		new TurretDef { Id = "technician", Behavior = "technician", ColorHex = "ffd9a0" },
	}).DistinctBy(d => d.Id);

	[Test]
	public void NineTurretsHaveLocalAlignedEmissionAndCellSizedIcons()
	{
		foreach (var def in Definitions())
		{
			using var sheet = TurretView.HeadSheet(def).GetImage();
			CheckEq(sheet.GetSize(), new Vector2I(32, 16), $"{def.Id}: two 16x16 frames");
			var coloured = 0;
			var emissive = 0;
			for (var y = 0; y < 16; y++)
				for (var x = 0; x < 16; x++)
				{
					var body = sheet.GetPixel(x, y).A > 0;
					var glow = sheet.GetPixel(x + 16, y).A > 0;
					if (body) coloured++;
					if (glow) emissive++;
					Check(!glow || body, $"{def.Id}: emission stays on the head at {x},{y}");
				}
			Check(emissive > 0 && emissive < coloured / 2, $"{def.Id}: small readable emissive details");
			var icon = GD.Load<Texture2D>($"res://assets/sprites/turrets/{def.Id}_icon.png");
			CheckEq(icon.GetSize(), new Vector2(16, 16), $"{def.Id}: construction/menu icon keeps cell size");
		}
		foreach (var id in new[] { "scan", "shield", "restore", "repair" })
		{
			using var icon = GD.Load<Texture2D>($"res://assets/sprites/icons/{id}.png").GetImage();
			CheckEq(icon.GetSize(), new Vector2I(10, 10), $"{id}: upgrade icon size");
			var occupied = 0;
			for (var y = 0; y < 10; y++)
				for (var x = 0; x < 10; x++)
				{
					var pixel = icon.GetPixel(x, y);
					if (pixel.A <= 0) continue;
					occupied++;
					Check(pixel.R == 1 && pixel.G == 1 && pixel.B == 1, $"{id}: white pixels accept UI tint");
				}
			Check(occupied > 0 && occupied < 100, $"{id}: nonempty transparent icon");
		}
	}

	static IEnumerable<Node> Descendants(Node parent)
	{
		foreach (var child in parent.GetChildren())
		{
			yield return child;
			foreach (var descendant in Descendants(child)) yield return descendant;
		}
	}

	[Test]
	public void TurretFlashesAndBossLocksNeverCreateLightsOrHalos()
	{
		foreach (var def in Definitions())
		{
			var view = new TurretView(def, 1, Vector2I.One, .8f, def.Stats);
			SceneTests.Tree.Root.AddChild(view);
			try
			{
				var glow = view.GetChildren().OfType<Sprite2D>().Single(s => s.Hframes == 2 && s.Frame == 1);
				var body = view.GetChildren().OfType<Sprite2D>().Single(s => s.Hframes == 2 && s.Frame == 0);
				var additive = (CanvasItemMaterial)glow.Material;
				CheckEq(additive.BlendMode, CanvasItemMaterial.BlendModeEnum.Add, $"{def.Id}: additive emission");
				CheckEq(additive.LightMode, CanvasItemMaterial.LightModeEnum.Unshaded, $"{def.Id}: visible without ambient light");
				CheckEq(glow.Scale, Vector2.One * 3, $"{def.Id}: three pixels per art pixel");
				CheckNear(glow.Rotation, body.Rotation, $"{def.Id}: aligned emission rotation");
				if (def.Behavior == "laser") CheckNear(body.Rotation, Mathf.Pi / 2, "laser quarter turn");
				var constructed = glow.Modulate.R;
				view._Process(1);
				var idle = glow.Modulate.R;
				Check(constructed > idle, $"{def.Id}: construction flash settles");
				view.Fired();
				Check(glow.Modulate.R > idle, $"{def.Id}: firing flashes local emission");
				foreach (var kind in new[] { "encrypt", "panic" })
				{
					view.LockKind = kind;
					view.Apply(new TurretSnap { Locked = true });
					view._Process(.1);
					Check(glow.Modulate.R < idle && glow.Modulate.R > 0, $"{def.Id}/{kind}: locked emission dims but stays visible");
					view.Fired();
					view._Process(.5);
					Check(glow.Modulate.R < idle && body.Modulate.R < 1, $"{def.Id}/{kind}: no flash overrides lock");
					if (def.Behavior == "laser")
						Check(view.GetChildren().OfType<Node2D>().Any(n => n is not Sprite2D && !n.Visible), "locked laser beam hidden");
					view.Apply(new TurretSnap { Locked = false });
					view._Process(1);
					CheckNear(glow.Modulate.R, idle, $"{def.Id}/{kind}: unlocking restores emission");
					CheckNear(body.Modulate.R, 1, $"{def.Id}/{kind}: unlocking restores head");
				}
				Check(!Descendants(view).Any(n => n is Light2D), $"{def.Id}: no dynamic light in turret subtree");
				Check(!Descendants(view).OfType<Sprite2D>().Any(s => s.Texture == FxUtil.GlowTexture), $"{def.Id}: no soft halo sprites");
			}
			finally { view.Free(); }
		}
	}
}
