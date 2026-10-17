using System.Threading.Tasks;
using Godot;

namespace Brenda.Autoload;

/// <summary>Scene changes with a short "signal lost" fade.</summary>
public partial class Transition : CanvasLayer
{
	public static Transition Instance { get; private set; }

	ColorRect _rect;
	ShaderMaterial _material;
	bool _busy;

	public override void _EnterTree() => Instance = this;

	public override void _Ready()
	{
		Layer = 100;
		ProcessMode = ProcessModeEnum.Always;
		_material = new ShaderMaterial { Shader = GD.Load<Shader>("res://assets/shaders/transition.gdshader") };
		_rect = new ColorRect { Color = new Color(0.02f, 0.04f, 0.07f, 0), MouseFilter = Control.MouseFilterEnum.Ignore, Material = _material };
		_rect.SetAnchorsPreset(Control.LayoutPreset.FullRect);
		AddChild(_rect);
	}

	public static void Go(string path) => _ = Instance.ChangeScene(path);

	public async Task ChangeScene(string path)
	{
		if (_busy)
			return;
		_busy = true;
		_rect.MouseFilter = Control.MouseFilterEnum.Stop;
		var tween = CreateTween();
		tween.TweenMethod(Callable.From<float>(SetProgress), 0f, 1f, 0.28f);
		await ToSignal(tween, Tween.SignalName.Finished);
		GetTree().Paused = false;
		Engine.TimeScale = 1;
		GetTree().ChangeSceneToFile(path);
		await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
		await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
		tween = CreateTween();
		tween.TweenMethod(Callable.From<float>(SetProgress), 1f, 0f, 0.35f);
		await ToSignal(tween, Tween.SignalName.Finished);
		_rect.MouseFilter = Control.MouseFilterEnum.Ignore;
		_busy = false;
	}

	void SetProgress(float value)
	{
		_rect.Color = new Color(_rect.Color, value);
		_material.SetShaderParameter("progress", value);
	}
}
