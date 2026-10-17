using System.Collections.Generic;
using System.Linq;
using HardwareDefence.Data;
using HardwareDefence.View;
using HardwareDefence.UI;
using Godot;

namespace HardwareDefence.Tests;

/// <summary>Checks the generator/rendering contract and the special boss layers.</summary>
public sealed class SpriteTests : TestSuite
{
	[Test]
	public void MiniaturesKeepThreePixelScaleAndCropBehindTheHead()
	{
		foreach (var id in new[] { "bug", "spider", "brute", "boss", "worm" })
			foreach (var size in new[] { 30, 36, 50 })
			{
				var icon = new BugIcon(Catalog.Enemy(id), size);
				SceneTests.Tree.Root.AddChild(icon);
				try
				{
					var body = icon.GetChild<BugBody>(0);
					CheckEq(body.Scale, Vector2.One * 3, $"{id}/{size}: three screen pixels per art pixel");
					Check(icon.ClipContents, $"{id}/{size}: portrait clips inside its slot");
					CheckEq(icon.Size, new Vector2(size, size), $"{id}/{size}: parent slot size preserved");
					var sprite = body.GetChild<Sprite2D>(1);
					var rect = sprite.GetRect();
					// The front edge points up. It stays inside the top three-pixel margin,
					// even for the 31-pixel Worm in a 30-pixel queue slot.
					var front = sprite.Transform * new Vector2(rect.End.X, rect.GetCenter().Y);
					var top = body.Transform * front;
					Check(top.Y >= -3 && top.Y < size / 2f, $"{id}/{size}: head remains in view");
					Check(body.Position.X == Mathf.Floor(size / 2f), $"{id}/{size}: portrait centred horizontally");
				}
				finally { icon.Free(); }
			}
	}

	[Test]
	public void EveryEnemyHasAlignedColourAndEmissionFrames()
	{
		var ids = Catalog.EnemyList.Select(e => e.Id).Append("trojan_disguise");
		foreach (var id in ids)
		{
			var path = $"res://assets/sprites/bugs/{id}.png";
			Check(ResourceLoader.Exists(path), $"{id}: real sheet exists (no fallback)");
			using var image = GD.Load<Texture2D>(path).GetImage();
			CheckEq(image.GetWidth() % BugBody.Frames, 0, $"{id}: all columns present");
			CheckEq(image.GetHeight() % 2, 0, $"{id}: colour and emission rows");
			var width = image.GetWidth() / BugBody.Frames;
			var height = image.GetHeight() / 2;
			Check(width % 2 == 1 && height % 2 == 1, $"{id}: odd frame pivots");
			var walk = new HashSet<string>();
			var chew = new HashSet<string>();
			for (var frame = 0; frame < BugBody.Frames; frame++)
			{
				using var drawing = image.GetRegion(new Rect2I(frame * width, 0, width, height));
				var pixels = System.Convert.ToBase64String(drawing.GetData());
				if (frame < BugBody.WalkFrames)
					walk.Add(pixels);
				else if (frame < BugBody.IdleFrame)
					chew.Add(pixels);
				var colour = 0;
				var emission = 0;
				var connected = new HashSet<Vector2I>();
				for (var y = 0; y < height; y++)
					for (var x = frame * width; x < (frame + 1) * width; x++)
					{
						if (image.GetPixel(x, y).A > 0)
						{
							colour++;
							connected.Add(new Vector2I(x, y));
						}
						if (image.GetPixel(x, height + y).A > 0)
							emission++;
					}
				Check(colour > 0 && emission > 0, $"{id}: frame {frame} has colour and light");
				// Every toe, antenna and jaw must connect to the body, including diagonal
				// pixel steps. This catches fragments hidden by a dark review background.
				if (connected.Count > 0)
				{
					var start = connected.First();
					connected.Remove(start);
					var pending = new Stack<Vector2I>();
					pending.Push(start);
					while (pending.TryPop(out var point))
						for (var dx = -1; dx <= 1; dx++)
							for (var dy = -1; dy <= 1; dy++)
							{
								var neighbor = point + new Vector2I(dx, dy);
								if (connected.Remove(neighbor))
									pending.Push(neighbor);
							}
					CheckEq(connected.Count, 0, $"{id}: frame {frame} has no detached limbs");
				}
			}
			CheckEq(walk.Count, BugBody.WalkFrames, $"{id}: eight distinct walking poses");
			CheckEq(chew.Count, BugBody.ChewFrames, $"{id}: six distinct chewing poses");
		}
	}

	[Test]
	public void WalkChewAndRestSelectTheirOwnRows()
	{
		var body = new BugBody(Catalog.Enemy("bug"));
		SceneTests.Tree.Root.AddChild(body);
		try
		{
			var sprite = body.GetChild<Sprite2D>(1);
			var glow = body.GetChild<Sprite2D>(2);
			var walk = new HashSet<int>();
			for (var i = 0; i < 300; i++)
			{
				body.Animate(0.5f, 0.01f);
				walk.Add(sprite.Frame);
				CheckEq(glow.Frame - sprite.Frame, BugBody.Frames, "walking light stays aligned");
			}
			CheckEq(walk.Count, BugBody.WalkFrames, "all eight walking drawings reached");
			body.Animate(0, 0.2f);
			CheckEq(sprite.Frame, BugBody.IdleFrame, "stopped bug rests");
			body.Chewing = true;
			var chew = new HashSet<int>();
			for (var i = 0; i < 100; i++)
			{
				body.Animate(0, 0.02f);
				chew.Add(sprite.Frame);
				Check(sprite.Frame >= BugBody.WalkFrames && sprite.Frame < BugBody.IdleFrame, "stationary chewing overrides rest");
				CheckEq(glow.Frame - sprite.Frame, BugBody.Frames, "chewing light stays aligned");
			}
			CheckEq(chew.Count, BugBody.ChewFrames, "all six bite drawings reached");
			body.Chewing = false;
			body.Animate(1, 0.01f);
			Check(sprite.Frame < BugBody.WalkFrames, "walking resumes after bite");
		}
		finally { body.Free(); }
	}

	[Test]
	public void BossDisguiseAndShaderEffectsSurviveAnimation()
	{
		var body = new BugBody(Catalog.Enemy("trojan"));
		SceneTests.Tree.Root.AddChild(body);
		try
		{
			var realSize = body.CurrentFrameSize;
			body.SetDisguised(true);
			Check(body.CurrentFrameSize.X < realSize.X, "gift is smaller than the horse");
			body.Chewing = true;
			body.Animate(0, 0.25f);
			body.Facing = 0.8f;
			body.SetFlash(0.6f);
			body.SetFrozen(1);
			body.SetCloak(0.9f);
			body.SetDisguised(false);
			CheckEq(body.CurrentFrameSize, realSize, "revealed size restored");
			var shadow = body.GetChild<Sprite2D>(0);
			var sprite = body.GetChild<Sprite2D>(1);
			var glow = body.GetChild<Sprite2D>(2);
			CheckEq(shadow.Texture, sprite.Texture, "shadow swaps with disguise");
			CheckEq(glow.Texture, sprite.Texture, "emission swaps with disguise");
			CheckEq(glow.Frame - sprite.Frame, BugBody.Frames, "disguise preserves selected bite");
			CheckNear(sprite.Rotation, Mathf.Pi / 2, "quarter-turn rotation");
			var material = (ShaderMaterial)sprite.Material;
			CheckNear(material.GetShaderParameter("flash").AsSingle(), 0.6f, "damage flash preserved");
			CheckNear(material.GetShaderParameter("frozen").AsSingle(), 1, "freeze preserved");
			CheckNear(material.GetShaderParameter("dissolve").AsSingle(), 0.9f, "cloak preserved");
			Check(!shadow.Visible && glow.Modulate.A < 0.4f, "cloak dims shadow and glow");
			body.SetCloak(0);
			Check(shadow.Visible && glow.Modulate.A == 1, "uncloaking restores both layers");
		}
		finally { body.Free(); }
	}
}
