using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using Brenda.Data;
using Brenda.Sim;
using Godot;

namespace Brenda.Net;

/// <summary>
/// One JSON message of the versus protocol (see docs/PROTOCOL.md). A single flat type keeps it
/// easy to read from any language: fields that don't apply are left out.
/// </summary>
public sealed class NetMessage
{
	public string Type { get; set; }

	// client -> host
	public int? Version { get; set; }
	public string Role { get; set; }
	public string Name { get; set; }
	public string Bug { get; set; }
	public string Turret { get; set; }
	public int[] Cell { get; set; }
	public float? Direction { get; set; }

	// host -> client
	public string You { get; set; }
	public LevelData Level { get; set; }
	public VersusRules Rules { get; set; }
	public IReadOnlyList<TurretDef> Turrets { get; set; }
	public IReadOnlyList<EnemyDef> Enemies { get; set; }
	public Snapshot Snapshot { get; set; }
	public List<NetEvent> Events { get; set; }
	public string Message { get; set; }
	public string Opponent { get; set; }
}

public sealed class NetEvent
{
	public string Kind { get; set; }
	public int Id { get; set; }
	public float X { get; set; }
	public float Y { get; set; }
	public float Value { get; set; }
	public string Type { get; set; }

	public static NetEvent From(SimEvent e) => new()
	{
		Kind = JsonNamingPolicy.SnakeCaseLower.ConvertName(e.Kind.ToString()), Id = e.Id,
		X = Mathf.Snapped(e.Pos.X, 0.1f), Y = Mathf.Snapped(e.Pos.Y, 0.1f), Value = e.Value, Type = e.Type,
	};

	public bool TryToSim(out SimEvent e)
	{
		foreach (EventKind kind in System.Enum.GetValues(typeof(EventKind)))
		{
			if (JsonNamingPolicy.SnakeCaseLower.ConvertName(kind.ToString()) != Kind)
				continue;
			e = new SimEvent(kind, Id, new Vector2(X, Y), Value, Type);
			return true;
		}
		e = default;
		return false;
	}
}

public static class Protocol
{
	public const int Version = 1;
	public const int DefaultPort = 7777;
	/// <summary>State updates per second sent to remote players.</summary>
	public const float TickRate = 20;
	public const int BufferSize = 1 << 21;

	public static string Encode(NetMessage m) => JsonIO.Write(m);

	public static NetMessage Decode(byte[] packet)
	{
		try
		{
			return JsonIO.Read<NetMessage>(Encoding.UTF8.GetString(packet));
		}
		catch (JsonException)
		{
			return null;
		}
	}

	public static string RoleName(PlayerSide side) => side == PlayerSide.Defender ? "defender" : "attacker";

	public static PlayerSide? ParseRole(string role) => role switch
	{
		"defender" => PlayerSide.Defender,
		"attacker" => PlayerSide.Attacker,
		_ => null,
	};

	/// <summary>This machine's LAN addresses, to show to the player hosting a game.</summary>
	public static List<string> LocalAddresses()
	{
		var list = new List<string>();
		foreach (var ip in IP.GetLocalAddresses())
			if (ip.Contains('.') && !ip.StartsWith("127.") && !ip.StartsWith("169.254"))
				list.Add(ip);
		return list;
	}
}
