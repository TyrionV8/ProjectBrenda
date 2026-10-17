using System.Collections.Generic;
using System.Linq;
using Brenda.Data;
using Brenda.Game;
using Brenda.Sim;
using Godot;

namespace Brenda.Net;

/// <summary>
/// Plays a versus match hosted by someone else. Receives state updates, interpolates them a
/// little behind real time for smooth motion, and sends this player's commands to the host.
/// </summary>
public sealed class RemoteFeed : IMatchFeed
{
	const float Delay = 0.1f;

	readonly WebSocketPeer _ws = new() { InboundBufferSize = Protocol.BufferSize, OutboundBufferSize = Protocol.BufferSize };
	readonly string _name;
	readonly PlayerSide _side;
	readonly List<Snapshot> _buffer = new();
	readonly List<SimEvent> _events = new();
	bool _helloSent;
	float _renderTime = -1;
	string _error;

	public RemoteFeed(string address, PlayerSide side, string name)
	{
		_side = side;
		_name = name;
		var url = address.StartsWith("ws://") || address.StartsWith("wss://") ? address : $"ws://{address}";
		if (!url[6..].Contains(':'))
			url += $":{Protocol.DefaultPort}";
		Address = url;
		var err = _ws.ConnectToUrl(url);
		if (err != Error.Ok)
			_error = $"Could not connect to {url} ({err})";
	}

	public string Address { get; }
	public LevelData Level { get; private set; }
	public MatchMode Mode => MatchMode.Versus;
	public PlayerSide LocalSide => _side;
	public Snapshot Current { get; private set; }
	public bool IsLocalOnly => false;
	/// <summary>True once the host accepted us (the level is known).</summary>
	public bool Joined => Level != null;
	public string LastError => _error;

	public string Status
	{
		get
		{
			if (_error != null && (Current == null || Current.MatchState == MatchState.Playing))
				return _error;
			if (!Joined)
				return $"Connecting to {Address}...";
			return Current == null ? "Connected. Waiting for the match to start..." : "";
		}
	}

	public void Process(float delta)
	{
		_ws.Poll();
		switch (_ws.GetReadyState())
		{
			case WebSocketPeer.State.Open:
				if (!_helloSent)
				{
					_helloSent = true;
					Send(new NetMessage { Type = "hello", Version = Protocol.Version, Role = Protocol.RoleName(_side), Name = _name });
				}
				while (_ws.GetAvailablePacketCount() > 0)
					Handle(Protocol.Decode(_ws.GetPacket()));
				break;
			case WebSocketPeer.State.Closed:
				_error ??= _helloSent ? "Connection lost." : $"Could not reach {Address}.";
				break;
		}
		Interpolate(delta);
	}

	void Handle(NetMessage m)
	{
		switch (m?.Type)
		{
			case "welcome":
				Level = m.Level;
				Level?.SortWaves();
				break;
			case "state" when m.Snapshot != null:
				_buffer.Add(m.Snapshot);
				if (_buffer.Count > 40)
					_buffer.RemoveAt(0);
				foreach (var e in m.Events ?? new List<NetEvent>())
					if (e.TryToSim(out var sim))
						_events.Add(sim);
				break;
			case "error":
				// Rejected commands (not enough energy...) are expected; only fatal ones end the session.
				if (!Joined)
					_error = m.Message;
				break;
		}
	}

	void Interpolate(float delta)
	{
		if (_buffer.Count == 0)
			return;
		var latest = _buffer[^1].Time;
		if (_renderTime < 0 || _renderTime < latest - 0.5f || _renderTime > latest)
			_renderTime = latest - Delay;
		else
			_renderTime += delta;
		_renderTime = Mathf.Min(_renderTime, latest);
		var b = _buffer.FindIndex(s => s.Time >= _renderTime);
		if (b <= 0)
		{
			Current = _buffer[Mathf.Max(b, 0)];
			return;
		}
		var a = _buffer[b - 1];
		var next = _buffer[b];
		var span = next.Time - a.Time;
		Current = Snapshot.Lerp(a, next, span <= 0 ? 1 : (_renderTime - a.Time) / span);
		if (next.MatchState != MatchState.Playing)
			Current = _buffer[^1];
	}

	public List<SimEvent> DrainEvents()
	{
		var list = _events.ToList();
		_events.Clear();
		return list;
	}

	void Send(NetMessage m)
	{
		if (_ws.GetReadyState() == WebSocketPeer.State.Open)
			_ws.SendText(Protocol.Encode(m));
	}

	public void Build(TurretDef def, Vector2I cell, float direction) =>
		Send(new NetMessage { Type = "build", Turret = def.Id, Cell = new[] { cell.X, cell.Y }, Direction = direction });

	public void SendBug(EnemyDef def) => Send(new NetMessage { Type = "send_bug", Bug = def.Id });

	public string CannotSend(EnemyDef def)
	{
		var s = _buffer.Count > 0 ? _buffer[^1] : null;
		if (s == null)
			return "Not connected";
		if (s.MatchState != MatchState.Playing)
			return "The match is over";
		if (s.Time < def.VersusUnlock)
			return $"Unlocks in {Mathf.CeilToInt(def.VersusUnlock - s.Time)}s";
		return s.Energy < def.VersusCost ? "Not enough energy" : null;
	}

	public Dictionary<string, float> StatsFor(TurretDef def) => def.ComputeStats(null);

	public void Close() => _ws.Close();
}
