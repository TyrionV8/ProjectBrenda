using System.Collections.Generic;
using System.Linq;
using HardwareDefence.Data;
using HardwareDefence.Game;
using HardwareDefence.Sim;

namespace HardwareDefence.Net;

/// <summary>
/// Hosts a versus match: runs the simulation locally (like <see cref="LocalFeed"/>) and serves
/// it to remote players over WebSocket. The match starts once every remote role is taken.
/// </summary>
public sealed class HostFeed : LocalFeed
{
	public readonly NetHost Host;
	readonly List<SimEvent> _events = new();
	readonly PlayerSide[] _remote;
	bool _started;

	public HostFeed(LevelData level, int port, PlayerSide localSide, params PlayerSide[] remoteRoles)
		: base(level, MatchMode.Versus, localSide, false)
	{
		_remote = remoteRoles;
		Host = new NetHost(Match, port, remoteRoles);
		Host.Left += role =>
		{
			if (_started)
				Match.Forfeit(role);
			OpponentLeft = _started;
		};
	}

	public bool OpponentLeft { get; private set; }

	public override bool IsLocalOnly => false;

	protected override bool Running => _started;

	public override string Status
	{
		get
		{
			if (!Host.Listening)
				return Host.Error;
			if (_started)
				return "";
			var waiting = string.Join(" and ", _remote.Where(r => Host.NameOf(r) == null).Select(Protocol.RoleName));
			var ips = Protocol.LocalAddresses();
			var where = ips.Count > 0 ? string.Join("  or  ", ips.Select(ip => $"{ip}:{Host.Port}")) : $"port {Host.Port}";
			return $"Waiting for the {waiting}...\nAsk them to join  {where}";
		}
	}

	public override void Process(float delta)
	{
		Host.Poll(delta);
		if (!_started && Host.AllJoined)
			_started = true;
		base.Process(delta);
		if (Match.Events.Count == 0)
			return;
		Host.Forward(Match.Events);
		_events.AddRange(Match.Events);
		Match.Events.Clear();
	}

	public override List<SimEvent> DrainEvents()
	{
		var list = _events.ToList();
		_events.Clear();
		return list;
	}

	public override void Close() => Host.Close();
}
