# Versus protocol (WebSocket + JSON)

Versus matches are played over a WebSocket. The **host** runs the simulation (it is
normally the defender's game); **clients** take the remote roles, send commands and receive
the match state 20 times per second. The same protocol is used by the game itself and by
AIs, so any language with a WebSocket library can play — see
[`tools/ai_attacker.py`](../tools/ai_attacker.py) for a complete example.

## Running a host

* In the game: *Versus → Host game* (you defend; one remote attacker).
* Headless, for AIs and experiments:

  ```
  godot --headless --path . -- --server --level=3 --port=7777 --defender=bot --attacker=remote
  ```

  Each side can be `remote` (waits for a WebSocket client) or `bot` (built-in AI). The
  server prints a short log and exits when the match ends.

Clients connect to `ws://HOST:PORT` (default port 7777). Over the internet the host needs
the port forwarded on their router (or a tunnel such as Tailscale/ZeroTier/playit.gg).

## Messages

Every message is one JSON object with a `type`. Keys are `snake_case`; keys that don't apply
are omitted.

### Client → host

| type | fields | meaning |
|------|--------|---------|
| `hello` | `version` (1), `role` (`attacker` or `defender`), `name` | Join the match. Must be the first message. |
| `send_bug` | `bug` (enemy id) | Attacker: spend energy to send a bug. |
| `build` | `turret` (turret id), `cell` ([x, y]), `direction` (radians, laser only) | Defender: build a turret. |
| `ping` | | The host answers `pong`. |

### Host → client

| type | fields | meaning |
|------|--------|---------|
| `welcome` | `you`, `level`, `rules`, `turrets`, `enemies` | Accepted. Contains everything needed to understand the match. |
| `state` | `snapshot`, `events` | Sent 20×/s once every remote role is taken. |
| `error` | `message` | A rejected command (`"Not enough energy"`, `"Can't build there"`...) or, before `welcome`, why the join failed (the connection is then closed). |
| `pong` | | |

The match starts as soon as all remote roles are taken; there is no separate start message —
the first `state` is the start. A player who disconnects during the match loses it.

## Board and units

The board is 21 × 16 cells of 48 px. Positions are in board pixels: `(0, 0)` is the top-left
corner of cell `(0, 0)`, so the centre of cell `(x, y)` is `(x * 48 + 24, y * 48 + 24)`.
Times are in seconds, speeds in pixels per second, angles in radians (0 = right, clockwise).

`level.path` lists the route's waypoint cells; bugs walk it from the first point (often just
outside the board) to the last one, where the CPU is. Turrets can't be built on the route, on
blocking components or on the 3 × 3 cells around the CPU.

## Snapshot

| field | meaning |
|-------|---------|
| `time` | Seconds since the start. |
| `state` | `playing`, `defender_won` or `attacker_won`. |
| `money` | Defender's money. |
| `energy`, `energy_rate` | Attacker's energy and its regeneration per second. |
| `cpu_hp` | 10 at the start; the attacker wins at 0. |
| `time_left` | Seconds until the defender wins. |
| `integrity` | Share of component HP still standing (bugs chew components next to the route). |
| `bugs` | `[{id, type, x, y, angle, hp, slowed, chewing, progress}]` — `progress` is the distance walked along the route. |
| `turrets` | `[{id, type, cell, direction, head}]` |
| `projectiles` | `[{id, kind (bullet/bomb/orb), source, x, y, h, a}]` |
| `components` | Remaining HP of each `level.components[i]` (-1 = destroyed). |

## Events

`state.events` lists what happened since the previous update: `{kind, id, x, y, value, type}`.
Kinds: `bug_spawned`, `bug_died` (value = reward), `bug_escaped`, `turret_built`,
`turret_fired`, `bullet_hit`, `explosion` (value = radius), `orb_popped`, `cpu_hit`
(value = CPU HP left), `component_destroyed` (id = component index), `chew`, `match_ended`.

## Rules

`welcome.rules` (from [`data/versus.json`](../data/versus.json)):

* The attacker starts with `start_energy`, earns `energy_rate + energy_rate_growth * time`
  per second (up to `energy_cap`) and `component_energy` per HP of every component its bugs
  destroy. Each enemy has a `versus_cost` and is locked until `versus_unlock` seconds.
  Bugs can be sent at most every `send_interval` seconds.
* The defender starts with the level's money plus `defender_bonus_money`, earns
  `defender_income` per second and `kill_reward_factor` of each kill's reward.
* The defender wins after `duration` seconds; the attacker wins when the CPU reaches 0 HP.

`godot --headless --path . -- --balance` plays the built-in bots against each other on
every level with the current rules, to help tuning them.

## Steam

The protocol doesn't depend on the transport. When the game has a Steam App ID, matches can
use Steam's networking (lobbies, friend invites, relay servers — no port forwarding) by
replacing the WebSocket peer in `src/Net/NetHost.cs`/`RemoteFeed.cs` with Steam Networking
Messages (for example through the GodotSteam GDExtension), keeping the same JSON messages.
