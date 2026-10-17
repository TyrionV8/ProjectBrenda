using System.Collections.Generic;
using Godot;

namespace HardwareDefence.Autoload;

/// <summary>Music with cross-fades and a small pool of one-shot sound players.</summary>
public partial class Audio : Node
{
	static readonly Dictionary<string, string> Music = new()
	{
		["title"] = "res://assets/audio/music_title.ogg",
		["music_level_1"] = "res://assets/audio/music_level_1.ogg",
		["music_level_2"] = "res://assets/audio/music_level_2.ogg",
		["music_level_3"] = "res://assets/audio/music_level_3.ogg",
		["ending"] = "res://assets/audio/music_ending.ogg",
	};

	static readonly Dictionary<string, string> Samples = new()
	{
		["shoot"] = "res://assets/audio/sfx_shoot.ogg",
		["bomb"] = "res://assets/audio/sfx_bomb.ogg",
		["hover"] = "res://assets/audio/ui_hover.ogg",
		["click"] = "res://assets/audio/ui_click.ogg",
	};

	const int PoolSize = 14;
	/// <summary>The same sound won't restart faster than this (keeps ten turrets from stacking up).</summary>
	const double MinRepeat = 0.05;

	public static Audio Instance { get; private set; }

	readonly Dictionary<string, AudioStream> _streams = new();
	readonly List<AudioStreamPlayer> _players = new();
	readonly Dictionary<string, double> _lastPlayed = new();
	readonly AudioStreamPlayer[] _music = new AudioStreamPlayer[2];
	int _next;
	int _musicIndex;
	string _musicId = "";

	public override void _EnterTree() => Instance = this;

	public override void _Ready()
	{
		ProcessMode = ProcessModeEnum.Always;
		foreach (var (id, path) in Samples)
			_streams[id] = GD.Load<AudioStream>(path);
		foreach (var (id, wav) in SfxSynth.BuildAll())
			_streams[id] = wav;
		for (var i = 0; i < PoolSize; i++)
		{
			var player = new AudioStreamPlayer();
			AddChild(player);
			_players.Add(player);
		}
		for (var i = 0; i < 2; i++)
		{
			_music[i] = new AudioStreamPlayer { Bus = "Music" };
			AddChild(_music[i]);
		}
	}

	public static void Play(string id, string bus = "SFX", float volumeDb = 0, float pitchJitter = 0.06f) =>
		Instance?.PlayInternal(id, bus, volumeDb, pitchJitter);

	public static void Ui(string id) => Play(id, "UI", 0, 0);

	public static void PlayMusic(string id, float fade = 1.2f) => Instance?.PlayMusicInternal(id, fade);

	void PlayInternal(string id, string bus, float volumeDb, float pitchJitter)
	{
		if (!_streams.TryGetValue(id, out var stream))
		{
			GD.PushWarning($"Unknown sound: {id}");
			return;
		}
		var now = Time.GetTicksMsec() / 1000.0;
		if (_lastPlayed.TryGetValue(id, out var last) && now - last < MinRepeat)
			return;
		_lastPlayed[id] = now;
		var player = _players[_next];
		_next = (_next + 1) % PoolSize;
		player.Stream = stream;
		player.Bus = bus;
		player.VolumeDb = volumeDb;
		player.PitchScale = 1 + (float)GD.RandRange(-pitchJitter, pitchJitter);
		player.Play();
	}

	void PlayMusicInternal(string id, float fade)
	{
		if (id == _musicId || !Music.ContainsKey(id))
			return;
		_musicId = id;
		var old = _music[_musicIndex];
		_musicIndex = 1 - _musicIndex;
		var next = _music[_musicIndex];
		var stream = GD.Load<AudioStream>(Music[id]);
		if (stream is AudioStreamOggVorbis ogg)
			ogg.Loop = true;
		next.Stream = stream;
		next.VolumeDb = -40;
		next.Play();
		var tween = CreateTween().SetParallel();
		tween.TweenProperty(next, "volume_db", 0, fade);
		if (old.Playing)
		{
			tween.TweenProperty(old, "volume_db", -40, fade);
			tween.Chain().TweenCallback(Callable.From(old.Stop));
		}
	}
}
