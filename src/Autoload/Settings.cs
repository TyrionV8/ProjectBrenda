using System;
using System.Collections.Generic;
using Godot;
using HardwareDefence.UI;

namespace HardwareDefence.Autoload;

/// <summary>Player settings (volumes, fullscreen, screen effects, player name), saved in user://settings.cfg.</summary>
public partial class Settings : Node
{
	const string Path = "user://settings.cfg";
	public static readonly string[] Buses = { "Master", "Music", "SFX", "UI" };

	public static Settings Instance { get; private set; }
	public static event Action Changed;

	public readonly Dictionary<string, float> Volumes = new() { ["Master"] = 0.8f, ["Music"] = 0.7f, ["SFX"] = 0.8f, ["UI"] = 0.8f };
	public bool Fullscreen;
	public bool ScreenFx = true;
	public bool Tips = true;
	public string Language { get; private set; } = DefaultLanguage(OS.GetLocaleLanguage());
	public static string DefaultLanguage(string systemLanguage) => systemLanguage == "pt" ? "pt_BR" : "en";
	public string PlayerName = "Player";
	public string LastAddress = "127.0.0.1";

	public override void _EnterTree() => Instance = this;

	public override void _Ready()
	{
		LoadFile();
		SetLanguage(Language, persist: false);
		foreach (var bus in Buses)
			ApplyVolume(bus);
		if (DisplayServer.GetName() == "headless")
			return;
		if (Fullscreen)
			DisplayServer.WindowSetMode(DisplayServer.WindowMode.Fullscreen);
		else
			FitWindow();
	}

	public void SetVolume(string bus, float value)
	{
		Volumes[bus] = Mathf.Clamp(value, 0, 1);
		ApplyVolume(bus);
		SaveFile();
		Changed?.Invoke();
	}

	public void SetFullscreen(bool on)
	{
		Fullscreen = on;
		if (on)
			DisplayServer.WindowSetMode(DisplayServer.WindowMode.Fullscreen);
		else
		{
			DisplayServer.WindowSetMode(DisplayServer.WindowMode.Windowed);
			FitWindow();
		}
		SaveFile();
		Changed?.Invoke();
	}

	public void SetScreenFx(bool on)
	{
		ScreenFx = on;
		SaveFile();
		Changed?.Invoke();
	}

	public void SetTips(bool on)
	{
		Tips = on;
		SaveFile();
		Changed?.Invoke();
	}

 public void SetLanguage(string language, bool persist = true)
 {
  Language = language == "pt_BR" ? "pt_BR" : "en";
  TranslationServer.SetLocale(Language);
  if (IsInsideTree()) Loc.Refresh(GetTree().Root);
  if (persist) SaveFile();
  Changed?.Invoke();
 }

	public void SetNetwork(string name, string address)
	{
		PlayerName = name;
		LastAddress = address;
		SaveFile();
	}

	void ApplyVolume(string bus)
	{
		var index = AudioServer.GetBusIndex(bus);
		if (index < 0)
			return;
		var value = Volumes[bus];
		AudioServer.SetBusMute(index, value <= 0.001f);
		AudioServer.SetBusVolumeDb(index, Mathf.LinearToDb(Mathf.Max(value, 0.001f)));
	}

	/// <summary>Opens the window at 4/5 of the screen width keeping 16:9, centred (as the original did).</summary>
	static void FitWindow()
	{
		if (OS.HasFeature("mobile"))
			return;
		var screen = DisplayServer.ScreenGetUsableRect();
		var width = (int)(screen.Size.X * 0.8f);
		var size = new Vector2I(width, width * 9 / 16);
		DisplayServer.WindowSetSize(size);
		DisplayServer.WindowSetPosition(screen.Position + (screen.Size - size) / 2);
	}

	void LoadFile()
	{
		var cfg = new ConfigFile();
		if (cfg.Load(Path) != Error.Ok)
			return;
		Language = (string)cfg.GetValue("interface", "language", Language);
		foreach (var bus in Buses)
			Volumes[bus] = (float)cfg.GetValue("audio", bus, Volumes[bus]);
		Fullscreen = (bool)cfg.GetValue("display", "fullscreen", Fullscreen);
		Tips = (bool)cfg.GetValue("gameplay", "tips", Tips);
		ScreenFx = (bool)cfg.GetValue("display", "screen_fx", ScreenFx);
		PlayerName = (string)cfg.GetValue("network", "name", PlayerName);
		LastAddress = (string)cfg.GetValue("network", "address", LastAddress);
	}

	void SaveFile()
	{
		var cfg = new ConfigFile();
		foreach (var bus in Buses)
			cfg.SetValue("audio", bus, Volumes[bus]);
		cfg.SetValue("interface", "language", Language);
		cfg.SetValue("gameplay", "tips", Tips);
		cfg.SetValue("display", "fullscreen", Fullscreen);
		cfg.SetValue("display", "screen_fx", ScreenFx);
		cfg.SetValue("network", "name", PlayerName);
		cfg.SetValue("network", "address", LastAddress);
		cfg.Save(Path);
	}
}
