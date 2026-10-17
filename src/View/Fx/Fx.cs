using System;
using HardwareDefence.Data;
using Godot;

namespace HardwareDefence.View;

/// <summary>Spawns short-lived visual effects. Lives inside the board, so positions are board-local.</summary>
public partial class Fx : Node2D
{
	public event Action<float> ShakeRequested;

	public void Sparks(Vector2 pos, Color color, int amount = 14, float speed = 160, float lifetime = 0.45f, float size = 0.5f)
	{
		var p = Particles(pos, amount, lifetime);
		// Sparks are single art pixels (big ones two): the pixel texture is 8 world units wide.
		p.Texture = FxUtil.PixelTexture;
		p.Material = FxUtil.Additive;
		p.InitialVelocityMin = speed * 0.4f;
		p.InitialVelocityMax = speed;
		p.DampingMin = speed * 1.2f;
		p.DampingMax = speed * 2f;
		p.ScaleAmountMin = 0.375f;
		p.ScaleAmountMax = size >= 0.6f ? 0.75f : 0.375f;
		p.ColorRamp = Fade(color);
		p.Emitting = true;
	}

	public void Debris(Vector2 pos, Color color, int amount = 16, float speed = 120)
	{
		var p = Particles(pos, amount, 0.7f);
		p.Texture = FxUtil.PixelTexture;
		p.InitialVelocityMin = speed * 0.3f;
		p.InitialVelocityMax = speed;
		p.DampingMin = speed;
		p.DampingMax = speed * 1.6f;
		p.AngularVelocityMin = -360;
		p.AngularVelocityMax = 360;
		p.ScaleAmountMin = 0.35f;
		p.ScaleAmountMax = 0.8f;
		p.ColorRamp = Fade(color);
		p.Emitting = true;
	}

	public void Smoke(Vector2 pos, float radius, int amount = 10, float lifetime = 1.4f)
	{
		var p = Particles(pos, amount, lifetime);
		p.Explosiveness = 0.3f;
		p.Texture = FxUtil.PixelTexture;
		p.EmissionShape = CpuParticles2D.EmissionShapeEnum.Sphere;
		p.EmissionSphereRadius = radius;
		p.Direction = Vector2.Up;
		p.Spread = 25;
		p.InitialVelocityMin = 10;
		p.InitialVelocityMax = 30;
		p.ScaleAmountMin = 0.75f;
		p.ScaleAmountMax = 1.5f;
		var g = new Gradient { Colors = new[] { new Color(0.2f, 0.2f, 0.22f, 0.7f), new Color(0.1f, 0.1f, 0.1f, 0) } };
		p.ColorRamp = g;
		p.Emitting = true;
	}

	public void GlowFlash(Vector2 pos, Color color, float size = 1, float duration = 0.25f)
	{
		var sprite = FxUtil.GlowSprite(color, size * 0.4f);
		sprite.Position = pos;
		AddChild(sprite);
		var tween = sprite.CreateTween().SetParallel();
		tween.TweenProperty(sprite, "scale", Vector2.One * size, duration).SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Cubic);
		tween.TweenProperty(sprite, "modulate:a", 0f, duration);
		tween.Chain().TweenCallback(Callable.From(sprite.QueueFree));
	}

	public void LightFlash(Vector2 pos, Color color, float energy = 1.5f, float size = 1, float duration = 0.3f)
	{
		var light = FxUtil.Light(color, energy, size);
		light.Position = pos;
		AddChild(light);
		var tween = light.CreateTween();
		tween.TweenProperty(light, "energy", 0f, duration).SetEase(Tween.EaseType.Out);
		tween.TweenCallback(Callable.From(light.QueueFree));
	}

	public void Shockwave(Vector2 pos, float radius, Color color, float duration = 0.4f) =>
		AddChild(new Shockwave { Position = pos, Radius = radius, RingColor = color, Duration = duration });

	public void FloatingText(Vector2 pos, string text, Color color) =>
		AddChild(new FloatingText { Position = PixelWorld.Snap(pos), Text = text, TextColor = color });

	public void Explosion(Vector2 pos, float radius)
	{
		var orange = new Color(1f, 0.6f, 0.2f);
		GlowFlash(pos, new Color(1f, 0.75f, 0.4f), radius / 40f, 0.35f);
		GlowFlash(pos, new Color(1f, 1f, 0.9f), radius / 110f, 0.12f);
		Shockwave(pos, radius, orange, 0.45f);
		Sparks(pos, orange, 26, radius * 2.2f, 0.55f, 0.7f);
		Debris(pos, new Color(0.35f, 0.3f, 0.28f), 10, radius * 1.4f);
		LightFlash(pos, orange, 2.2f, radius / 70f, 0.45f);
		ShakeRequested?.Invoke(4);
	}

	public void BugDeath(Vector2 pos, EnemyDef def)
	{
		var big = def.Radius / 12f;
		Debris(pos, def.Shell, (int)(10 + 6 * big), 90 * big);
		Sparks(pos, def.Eye.Lerp(Colors.White, 0.3f), (int)(6 + 4 * big), 110 * big, 0.35f, 0.45f);
		GlowFlash(pos, def.Shell.Lerp(Colors.White, 0.4f), 0.6f * big, 0.2f);
		FloatingText(pos + new Vector2(0, -def.Radius), $"+{def.Reward}", new Color("ffd34d"));
		if (def.Style == "boss")
		{
			Shockwave(pos, 90, def.Eye, 0.6f);
			LightFlash(pos, def.Eye, 2, 1.4f, 0.6f);
			ShakeRequested?.Invoke(8);
		}
	}

	public void ComponentDestroyed(Rect2 rect, Color color)
	{
		var center = rect.GetCenter();
		var size = Mathf.Max(rect.Size.X, rect.Size.Y);
		Debris(center, color, (int)(10 + size / 6), 60 + size);
		Sparks(center, new Color(1f, 0.7f, 0.3f), 18, 140, 0.5f, 0.55f);
		GlowFlash(center, new Color(1f, 0.55f, 0.2f), size / 50f, 0.3f);
		LightFlash(center, new Color(1f, 0.5f, 0.2f), 1.8f, size / 90f, 0.5f);
		Smoke(center, size * 0.3f, 14, 1.8f);
		ShakeRequested?.Invoke(2.5f);
	}

	public void Shake(float amount) => ShakeRequested?.Invoke(amount);

	CpuParticles2D Particles(Vector2 pos, int amount, float lifetime)
	{
		var p = new CpuParticles2D
		{
			Position = pos, OneShot = true, Explosiveness = 0.95f, Amount = Math.Max(amount, 1), Lifetime = lifetime,
			Direction = Vector2.Right, Spread = 180, Gravity = Vector2.Zero,
		};
		p.Finished += p.QueueFree;
		AddChild(p);
		return p;
	}

	static Gradient Fade(Color color) => new() { Colors = new[] { color, new Color(color, 0) } };
}
