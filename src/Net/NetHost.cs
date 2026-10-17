using System;
using System.Collections.Generic;
using System.Linq;
using Brenda.Data;
using Brenda.Sim;
using Godot;

namespace Brenda.Net;

/// <summary>
/// WebSocket server for versus matches. The host runs the simulation; remote players (people
/// or AIs) take the free roles, send commands and receive state updates.
/// </summary>
public sealed class NetHost
{
	public sealed class Peer
	{
		public WebSocketPeer Ws;
		public PlayerSide? Role;
		public string Name = "";
		public double ConnectedAt;
	}

	readonly TcpServer _server = new();
	readonly List<Peer> _peers = new();
	readonly Match _match;
	readonly HashSet<PlayerSide> _remoteRoles;
	readonly List<NetEvent> _pending = new();
	double _sendTimer;

	public int Port { get; }
	public string Error { get; }
	public event Action<PlayerSide, string> Joined;
	public event Action<PlayerSide> Left;

	/// <param name="remoteRoles">Roles played by remote clients; the match waits until all are taken.</param>
	public NetHost(Match match, int port, IEnumerable<PlayerSide> remoteRoles)
	{
		_match = match;
		_remoteRoles = new HashSet<PlayerSide>(remoteRoles);
		Port = port;
		var err = _server.Listen((ushort)port);
		if (err != Godot.Error.Ok)
			Error = $"Could not open port {port} ({err})";
	}

	public bool Listening => Error == null;

	public bool AllJoined => _remoteRoles.All(r => _peers.Any(p => p.Role == r));

	public IEnumerable<string> PlayerNames => _peers.Where(p => p.Role != null).Select(p => $"{p.Name} ({Protocol.RoleName(p.Role.Value)})");

	public string NameOf(PlayerSide role) => _peers.FirstOrDefault(p => p.Role == role)?.Name;

	/// <summary>Queue simulation events for the next state update.</summary>
	public void Forward(IEnumerable<SimEvent> events)
	{
		foreach (var e in events)
			_pending.Add(NetEvent.From(e));
	}

	public void Poll(double delta)
	{
		if (!Listening)
			return;
		while (_server.IsConnectionAvailable())
		{
			var ws = new WebSocketPeer { InboundBufferSize = Protocol.BufferSize, OutboundBufferSize = Protocol.BufferSize };
			ws.AcceptStream(_server.TakeConnection());
			_peers.Add(new Peer { Ws = ws, ConnectedAt = Time.GetTicksMsec() / 1000.0 });
		}
		foreach (var peer in _peers.ToList())
		{
			peer.Ws.Poll();
			var state = peer.Ws.GetReadyState();
			if (state == WebSocketPeer.State.Open)
			{
				while (peer.Ws.GetAvailablePacketCount() > 0)
					Handle(peer, Protocol.Decode(peer.Ws.GetPacket()));
			}
			else if (state == WebSocketPeer.State.Closed)
			{
				_peers.Remove(peer);
				if (peer.Role is { } role)
					Left?.Invoke(role);
			}
		}
		if (!AllJoined)
			return;
		_sendTimer -= delta;
		if (_sendTimer > 0)
			return;
		_sendTimer = 1 / Protocol.TickRate;
		var msg = Protocol.Encode(new NetMessage { Type = "state", Snapshot = Snapshot.From(_match), Events = _pending.ToList() });
		_pending.Clear();
		foreach (var peer in _peers.Where(p => p.Role != null))
			peer.Ws.SendText(msg);
	}

	void Send(Peer peer, NetMessage m) => peer.Ws.SendText(Protocol.Encode(m));

	void Handle(Peer peer, NetMessage m)
	{
		if (m == null)
		{
			Send(peer, new NetMessage { Type = "error", Message = "Invalid JSON" });
			return;
		}
		switch (m.Type)
		{
			case "hello":
				var wanted = Protocol.ParseRole(m.Role) ?? _remoteRoles.FirstOrDefault(r => _peers.All(p => p.Role != r));
				if (peer.Role != null)
					return;
				if (m.Version is { } v && v != Protocol.Version)
				{
					Send(peer, new NetMessage { Type = "error", Message = $"Protocol version {v} not supported (host uses {Protocol.Version})" });
					peer.Ws.Close();
					return;
				}
				if (!_remoteRoles.Contains(wanted) || _peers.Any(p => p.Role == wanted))
				{
					Send(peer, new NetMessage { Type = "error", Message = $"The {Protocol.RoleName(wanted)} role is not available" });
					peer.Ws.Close();
					return;
				}
				peer.Role = wanted;
				peer.Name = string.IsNullOrWhiteSpace(m.Name) ? "Player" : m.Name.Trim()[..Math.Min(m.Name.Trim().Length, 24)];
				Send(peer, new NetMessage
				{
					Type = "welcome", Version = Protocol.Version, You = Protocol.RoleName(wanted), Level = _match.Level, Rules = _match.Rules,
					Turrets = Catalog.Turrets, Enemies = Catalog.EnemyList,
				});
				Joined?.Invoke(wanted, peer.Name);
				break;
			case "send_bug":
				if (peer.Role != PlayerSide.Attacker)
				{
					Send(peer, new NetMessage { Type = "error", Message = "Only the attacker can send bugs" });
					return;
				}
				var bug = Catalog.Enemy(m.Bug ?? "");
				var reason = _match.CannotSend(bug);
				if (reason != null)
					Send(peer, new NetMessage { Type = "error", Message = reason });
				else
					_match.SendBug(bug);
				break;
			case "build":
				if (peer.Role != PlayerSide.Defender)
				{
					Send(peer, new NetMessage { Type = "error", Message = "Only the defender can build" });
					return;
				}
				var def = Catalog.Turret(m.Turret ?? "");
				if (def == null || m.Cell is not { Length: 2 } || !_match.Build(def, new Vector2I(m.Cell[0], m.Cell[1]), m.Direction ?? 0))
					Send(peer, new NetMessage { Type = "error", Message = "Can't build there" });
				break;
			case "ping":
				Send(peer, new NetMessage { Type = "pong" });
				break;
			default:
				Send(peer, new NetMessage { Type = "error", Message = $"Unknown message type '{m.Type}'" });
				break;
		}
	}

	public void Close()
	{
		foreach (var peer in _peers)
			peer.Ws.Close();
		_peers.Clear();
		_server.Stop();
	}
}
