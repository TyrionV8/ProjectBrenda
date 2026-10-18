using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Godot;

namespace HardwareDefence.UI;

/// <summary>Translate presentation text without changing catalog data or protocol messages.
/// Bindings retain the source text so an open screen can change language in place.</summary>
public static class Loc
{
	const string Source = "i18n_source";
	const string TipSource = "i18n_tooltip";
	static readonly string[] Formats =
	{
		"{0}. {1}",
		"next: {0}. {1}",
		"Start Level {0}",
		"Unlock ${0}",
		"Locked turret. Buy once for ${0}; upgrades are sold separately.",
		"{0} - {1}: {2}  Now: {3}",
		"{0}s between shots",
		"{0} damage/s",
		"{0}% speed",
		"{0} pellets",
		"{0} waves  {1} bugs",
		"{0} waves  -  {1}",
		"{0} (custom)",
		"Your address: {0}",
		"HP: {0}   |   Speed: {1} px/s",
		"CPU damage: {0}",
		"Detour chance: {0}%   |   Reach: {1} cells",
		"Chewing: {0} s   |   Cooldown: {1} s",
		"Build cost: ${0}   |   Base range: {1} px",
		"Permanent unlock: ${0}",
		"{0}  -  click a free cell to build  -  right click to cancel",
		"{0} bugs",
		"Board integrity: {0}%",
		"Remaining: ${0}  |  record: ${1}",
		"+${0} for upgrades  |  balance: ${1}",
		"+{0} more",
		"HP {0}  speed {1}  chews {2}/s",
		"{0} points, {1} cells",
		"{0} components\n{1} free cells for turrets",
		"This wave: {0} bugs, {1} HP, ${2} rewards, {3}s\nLevel: {4} waves, {5} bugs",
		"Saved {0}",
		"Could not open {0}",
		"Opened {0}",
		"x{0}  hp {1}",
		"Connecting to {0}...",
		"Could not connect to {0} ({1})",
		"Could not reach {0}.",
		"Unlocks in {0}s",
		"Waiting for the {0}...\nAsk them to join  {1}",
		"port {0}",
		"Could not open port {0} ({1})",
		"Protocol version {0} not supported (host uses {1})",
		"The {0} role is not available",
		"Unknown message type '{0}'",
		"Route segment {0} is not horizontal or vertical.",
		"Route point {0} repeats the previous one.",
		"{0} #{1} is outside the board.",
		"{0} #{1} is on the route.",
		"{0} #{1} overlaps the CPU.",
		"{0} #{1} overlaps component #{2}.",
		"Unknown bug type '{0}'.",
		"NEXT: {0}. {1}",
		"START LEVEL {0}",
		"UNLOCK ${0}",
		"LOCKED TURRET. BUY ONCE FOR ${0}; UPGRADES ARE SOLD SEPARATELY.",
		"{0} - {1}: {2}  NOW: {3}",
		"{0}S BETWEEN SHOTS",
		"{0} DAMAGE/S",
		"{0}% SPEED",
		"{0} PELLETS",
		"{0} WAVES  {1} BUGS",
		"{0} WAVES  -  {1}",
		"{0} (CUSTOM)",
		"YOUR ADDRESS: {0}",
		"HP: {0}   |   SPEED: {1} PX/S",
		"CPU DAMAGE: {0}",
		"DETOUR CHANCE: {0}%   |   REACH: {1} CELLS",
		"CHEWING: {0} S   |   COOLDOWN: {1} S",
		"BUILD COST: ${0}   |   BASE RANGE: {1} PX",
		"PERMANENT UNLOCK: ${0}",
		"{0}  -  CLICK A FREE CELL TO BUILD  -  RIGHT CLICK TO CANCEL",
		"WAVE {0} CLEARED",
		"WAVE {0}",
		"{0} BUGS",
		"BOARD INTEGRITY: {0}%",
		"REMAINING: ${0}  |  RECORD: ${1}",
		"+${0} FOR UPGRADES  |  BALANCE: ${1}",
		"+{0} MORE",
		"NEXT\n{0}",
		"HP {0}  SPEED {1}  CHEWS {2}/S",
		"{0} PROBLEM{1}",
		"{0} POINTS, {1} CELLS",
		"{0} COMPONENTS\n{1} FREE CELLS FOR TURRETS",
		"THIS WAVE: {0} BUGS, {1} HP, ${2} REWARDS, {3}S\nLEVEL: {4} WAVES, {5} BUGS",
		"SAVED {0}",
		"COULD NOT OPEN {0}",
		"OPENED {0}",
		"X{0}  HP {1}",
		"CONNECTING TO {0}...",
		"COULD NOT CONNECT TO {0} ({1})",
		"COULD NOT REACH {0}.",
		"UNLOCKS IN {0}S",
		"WAITING FOR THE {0}...\nASK THEM TO JOIN  {1}",
		"PORT {0}",
		"COULD NOT OPEN PORT {0} ({1})",
		"PROTOCOL VERSION {0} NOT SUPPORTED (HOST USES {1})",
		"THE {0} ROLE IS NOT AVAILABLE",
		"UNKNOWN MESSAGE TYPE '{0}'",
		"ROUTE SEGMENT {0} IS NOT HORIZONTAL OR VERTICAL.",
		"ROUTE POINT {0} REPEATS THE PREVIOUS ONE.",
		"{0} #{1} IS OUTSIDE THE BOARD.",
		"{0} #{1} IS ON THE ROUTE.",
		"{0} #{1} OVERLAPS THE CPU.",
		"{0} #{1} OVERLAPS COMPONENT #{2}.",
		"UNKNOWN BUG TYPE '{0}'.",
	};
	static readonly (string Key, Regex Pattern)[] Patterns = Formats
		.Select(key => (key, new Regex(key == "{0}. {1}" ? @"^(\d+)\. (.*?)$" : "^" + Regex.Replace(Regex.Escape(key), @"\\\{\d+}", "(.*?)") + "$", RegexOptions.Singleline | RegexOptions.CultureInvariant))).ToArray();

	public static string S(string source)
	{
		if (string.IsNullOrEmpty(source)) return source ?? "";
		var translated = TranslationServer.Translate(source).ToString();
		if (translated != source) return translated;
		foreach (var (key, pattern) in Patterns)
		{
			var match = pattern.Match(source);
			if (!match.Success) continue;
			var target = TranslationServer.Translate(key).ToString();
			if (target == key && TranslationServer.GetLocale().StartsWith("en")) continue;
			// Captures may include catalogue names or nested messages. Keep all protocol ids intact.
			return Regex.Replace(target, @"\{(\d+)\}", m =>
			{
				var index = int.Parse(m.Groups[1].Value);
				var value = match.Groups[index + 1].Value;
				// Network addresses, file names and player-provided values are opaque. Only these
				// authored presentation slots contain other localizable text.
				var nested = key == "{0}. {1}" && index == 1
					|| key.StartsWith("next:") && index == 1
					|| key.StartsWith("{0} - {1}:")
					|| key.Contains("click a free cell") && index == 0
					|| key.StartsWith("Waiting for the") && index == 0
					|| key.StartsWith("The {0} role") && index == 0
					|| key.StartsWith("{0} #{1}") && index == 0;
				if (key.StartsWith("Waiting for the") && index == 1)
				{
					if (value.StartsWith("port ")) return S(value);
					return value.Replace("  or  ", "  " + TranslationServer.Translate("HOST_ADDRESS_OR") + "  ");
				}
				return nested ? S(value) : value;
			});
		}
		if (source.Length > 2 && char.IsDigit(source[0]) && source[1] == ' ') return source[..2] + S(source[2..]);
		if (source.Contains('\n')) return string.Join("\n", source.Split('\n').Select(S));
		// Composite captions use separators around individually translatable catalogue fields.
		foreach (var separator in new[] { "  -  ", " - ", ": ", "  ->  " })
			if (source.Contains(separator)) return string.Join(separator, source.Split(separator).Select(S));
		return source;
	}
	public static void Raw(Control control, string source)
	{
		if (control.HasMeta(Source)) control.RemoveMeta(Source);
		control.AutoTranslateMode = Node.AutoTranslateModeEnum.Disabled;
		if (control is Label label) label.Text = source;
		else if (control is Button button) button.Text = source;
	}
	public static void Set(Control control, string source)
	{
		source ??= "";
		if (control.HasMeta(Source) && control.GetMeta(Source).AsString() == source
			&& control.HasMeta("i18n_locale") && control.GetMeta("i18n_locale").AsString() == TranslationServer.GetLocale()) return;
		control.SetMeta(Source, source);
		control.SetMeta("i18n_locale", TranslationServer.GetLocale());
		control.AutoTranslateMode = Node.AutoTranslateModeEnum.Disabled;
		if (control is Label label) label.Text = S(source);
		else if (control is Button button) button.Text = S(source);
	}
	public static void Tooltip(Control control, string source)
	{
		control.SetMeta(TipSource, source ?? "");
		control.TooltipText = S(source);
	}
	public static void AddItem(OptionButton control, string source)
	{
		var index = control.ItemCount;
		control.AddItem(S(source));
		control.SetMeta("i18n_item_" + index, source);
	}
	public static void Refresh(Node node)
	{
		if (node is Control control)
		{
			if (control.HasMeta(Source)) Set(control, control.GetMeta(Source).AsString());
			if (control.HasMeta(TipSource)) Tooltip(control, control.GetMeta(TipSource).AsString());
			if (control is OptionButton options)
				for (var i = 0; i < options.ItemCount; i++)
					if (control.HasMeta("i18n_item_" + i)) options.SetItemText(i, S(control.GetMeta("i18n_item_" + i).AsString()));
			control.QueueRedraw();
		}
		foreach (var child in node.GetChildren()) Refresh(child);
	}
}
