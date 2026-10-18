using System.Collections.Generic;
using HardwareDefence.Autoload;
using HardwareDefence.Data;
using Godot;

namespace HardwareDefence.UI;

/// <summary>Short non-modal hints. Queueing never consumes a hint; only showing it does.</summary>
public partial class TutorialTips : PanelContainer
{
	public const string Build = "first_build";
	public const string Wave = "first_wave";
	public const string Detour = "first_detour";
	public const string Led = "first_led";
	public const string Victory = "first_victory_balance";
	public const string VictoryText = "Your remaining money becomes upgrade balance. A better replay credits only the improvement.";
	public static readonly IReadOnlyDictionary<string, string> Texts = new Dictionary<string, string>
	{
		[Build] = "Pick a turret on the right, then click a free board cell to build. Protect the copper route!",
		[Wave] = "The wave is starting. Bugs follow the copper route toward your CPU (central processing unit).",
		[Detour] = "That bug left the route to chew a component. Your turrets can still hit it; cover the detour too.",
		[Led] = "An LED (light-emitting diode) burnt out. Repair Bot can rebuild it and restore the light.",
		[Victory] = VictoryText,
		["boss_trojan"] = "Trojan incoming! Antivirus Scanner reveals its disguise. Keep damage ready for the bugs it drops.",
		["boss_keylogger"] = "Keylogger steals money while alive. Focus your damage on it before your cash drains away.",
		["boss_mitm"] = "Man-in-the-Middle intercepts shots. Firewall protects fire from turrets inside or across its range.",
		["boss_ddos"] = "DDoS (distributed denial of service) floods the route with packets. Firewall deletes packets; area damage helps too.",
		["boss_rootkit"] = "Rootkit hides from weapons. Antivirus Scanner exposes it while it stays inside scan range.",
		["boss_ransomware"] = "Ransomware locks nearby turrets. Backup Drive speeds their recovery; cover your weapons with it.",
		["boss_worm"] = "Worm forks into copies as health falls. Spread damage coverage and use area weapons for the copies.",
		["boss_panic"] = "Kernel Panic freezes every turret. Backup Drive recovers itself faster, then helps nearby weapons reboot.",
	};

	readonly Queue<string> _pending = new();
	readonly HashSet<string> _queued = new();
	Label _text;
	float _remaining;
	float _delay = 3;
	public bool Suspended { get; set; }
	public string CurrentId { get; private set; }
	public static bool Enabled => Settings.Instance?.Tips ?? true;
	public static bool CanShow(string id) => Enabled && Texts.ContainsKey(id) && !GameState.ShownTips.Contains(id);

	public override void _Ready()
	{
		Name = "TutorialTips";
		Position = new Vector2(Grid.Origin.X + 96, 108);
		Size = new Vector2(816, 0);
		Visible = false;
		var box = Ui.VBox(4);
		AddChild(box);
		var header = Ui.HBox();
		var title = Ui.Label("FIELD TIP", "SmallHeader");
		title.SizeFlagsHorizontal = SizeFlags.ExpandFill;
		header.AddChild(title);
		header.AddChild(Ui.Button("Dismiss", Dismiss));
		box.AddChild(header);
		_text = Ui.Label("", "BodyLabel");
		_text.AutowrapMode = TextServer.AutowrapMode.WordSmart;
		box.AddChild(_text);
		GuiInput += e => { if (e is InputEventMouseButton { Pressed: true }) { Dismiss(); AcceptEvent(); } };
	}

	public void Enqueue(string id)
	{
		if (CanShow(id) && id != CurrentId && _queued.Add(id)) _pending.Enqueue(id);
	}
	public void Clear()
	{
		_pending.Clear();
		_queued.Clear();
		Dismiss();
	}
	public void Dismiss() { Visible = false; CurrentId = null; _remaining = 0; _delay = 0.8f; }
	public override void _Process(double delta)
	{
		if (Suspended) return;
		if (!Enabled) { Clear(); return; }
		if (Visible)
		{
			_remaining -= (float)delta;
			if (_remaining <= 0) Dismiss();
			return;
		}
		_delay -= (float)delta;
		if (_delay > 0 || _pending.Count == 0) return;
		var id = _pending.Dequeue();
		_queued.Remove(id);
		if (!CanShow(id)) return;
		Loc.Set(_text, Texts[id]);
		CurrentId = id;
		Visible = true;
		_remaining = 9;
		GameState.MarkTipShown(id);
	}
}
