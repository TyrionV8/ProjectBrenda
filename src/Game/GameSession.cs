using System;
using HardwareDefence.Autoload;
using HardwareDefence.Data;
using HardwareDefence.Sim;

namespace HardwareDefence.Game;

/// <summary>How the next game screen should start (set by menus before changing scene).</summary>
public static class GameSession
{
	public const string GameScene = "res://scenes/game.tscn";
	public const string MenuScene = "res://scenes/main_menu.tscn";

	public enum Kind
	{
		Campaign,
		/// <summary>A level played from the editor; returns to the editor.</summary>
		TestPlay,
		/// <summary>A custom level picked from the list (no campaign progress).</summary>
		Custom,
		Versus,
	}

	public static Kind Current = Kind.Campaign;
	public static Func<IMatchFeed> CreateFeed;
	/// <summary>Where "back"/"continue" goes after the match.</summary>
	public static string ReturnScene = MenuScene;

	public static void StartCampaign(int level)
	{
		GameState.CurrentLevel = level;
		Current = Kind.Campaign;
		ReturnScene = "res://scenes/upgrade_screen.tscn";
		CreateFeed = () => new LocalFeed(Catalog.Levels[GameState.CurrentLevel], MatchMode.Campaign, PlayerSide.Defender, true);
		Transition.Go(GameScene);
	}

	public static void StartCustom(LevelData level, Kind kind, string returnScene)
	{
		Current = kind;
		ReturnScene = returnScene;
		CreateFeed = () => new LocalFeed(level, MatchMode.Campaign, PlayerSide.Defender, false);
		Transition.Go(GameScene);
	}

	/// <summary>Versus on this machine: you defend, a bot attacks.</summary>
	public static void StartVersusBot(LevelData level)
	{
		Current = Kind.Versus;
		ReturnScene = MenuScene;
		CreateFeed = () => new LocalFeed(level, MatchMode.Versus, PlayerSide.Defender, false) { AttackerBot = new AttackerBot((int)DateTime.Now.Ticks) };
		Transition.Go(GameScene);
	}

	public static void StartOnline(Func<IMatchFeed> feed)
	{
		Current = Kind.Versus;
		ReturnScene = MenuScene;
		CreateFeed = feed;
		Transition.Go(GameScene);
	}
}
