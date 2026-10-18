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

`welcome.turrets` includes the five original weapons and the support ids `antivirus`,
`firewall`, `backup` and `technician`; `build.turret` accepts all nine. Campaign unlocks
filter the local game's inventory only, including its versus and custom modes. They do
not restrict remote commands or the simulation.

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
| `mode` | `campaign` or `versus`; custom levels use the campaign wave runner. |
| `state` | `playing`, `defender_won` or `attacker_won`. |
| `money` | Defender's money. |
| `energy`, `energy_rate` | Attacker's energy and its regeneration per second. |
| `cpu_hp` | 10 at the start; the attacker wins at 0 in the same simulation step. Boss escape damage is 5/7/9 on boards 1–3 and 10 on boards 4–8, including Kernel Panic sent in versus. Packets cause zero damage; other minions retain their ordinary damage. |
| `time_left` | Seconds until the defender wins in versus; zero in campaign. |
| `wave`, `wave_count` | Current zero-based campaign wave index and total wave count. |
| `wave_state`, `countdown` | `countdown`, `spawning`, `waiting` or `done`; `countdown` is the remaining preparation time while in the countdown state. |
| `upcoming` | Enemy ids still scheduled in the current campaign wave, in spawn order. |
| `stars` | Zero until a defender victory, then 1–3 from remaining component integrity. Campaign saves use this rating; it is also present in versus snapshots without granting campaign credit. |
| `integrity` | Share of component HP still standing. Components take chewing damage only during a bug's detour, never automatically from passing bugs on the route. |
| `bugs` | `[{id, type, x, y, angle, hp, slowed, chewing, off_route, progress, cloaked, exposed, revealed}]` — `x`, `y` and `angle` are the real position/direction. `off_route` is true while approaching a component, chewing, or returning. `progress` stays frozen at the exit point until the bug returns exactly there. `chewing` is true only for actual chewing, not travel; `cloaked`: a rootkit hiding (can't be targeted or hurt); `exposed`: revealed by an active scanner in range, making effective `cloaked` false without resetting its internal cloak cycle; `revealed`: a trojan permanently without its disguise. |
| `turrets` | `[{id, type, cell, direction, head, locked, immune}]` — `locked`: encrypted (ransomware) or crashed (kernel panic), it can't fire or provide external support; `immune`: briefly protected against new locks after backup-assisted recovery. |
| `projectiles` | `[{id, kind (bullet/bomb/orb), source, x, y, h, a, protected}]` — `protected`: a firewall covered its firing origin or some part of its travelled segment, before interception checks. Protection stays true after leaving that range or a later firewall lock. |
| `components` | Remaining HP of each `level.components[i]`, in exactly the level's order and with the same indices (-1 = destroyed). Partial rebuilding remains -1; complete repair changes it back to full HP and restores its appearance/light. Includes ordinary `led` and `led_5mm` components; no extra components are derived from the route. LEDs count for `integrity` and award component energy like other components. Their light is visual only. |

## Events

`state.events` lists what happened since the previous update: `{kind, id, x, y, value, type}`.
Kinds: `bug_spawned`, `bug_died` (value = reward), `bug_escaped`, `turret_built`,
`turret_fired`, `bullet_hit`, `explosion` (value = radius), `orb_popped`, `cpu_hit`
(value = CPU HP left), `component_destroyed` (id = component index), `chew`, `match_ended`,
`boss_ability` (id = the boss, type = what it did: `steal`, `intercept`, `flood`, `cloak`,
`uncloak`, `encrypt` (value = radius), `panic`, `reveal`, `payload`, `fork`).

`turret_ability` identifies the support turret in `id` and uses `type` = `scan`, `block`,
`restore` or `repair`. Its position is the scanner centre for `scan`, otherwise the target
position. `value` is the support radius for scan/block/restore, or the component index for
repair. Component revival is reflected by `snapshot.components`, without a separate event.

`wave_started` carries the zero-based index of the wave that began; its UI wave number
is `id + 1`. `wave_countdown` carries the zero-based index of the next wave during its
preparation countdown; its `id` also equals the number of waves just completed.
`match_ended.id` is 1 for a defender win and 2 for an
attacker win; its `value` is the final `stars` field (zero on defeat).

Language selection affects local presentation only. Field names, ids, role names and
protocol error messages retain their original values; clients can translate messages
when displaying them. Campaign discoveries and shown tutorial tips are local save data,
not additions to the network protocol.

## Rules

`welcome.rules` (from [`data/versus.json`](../data/versus.json)):

* The attacker starts with `start_energy`, earns `energy_rate + energy_rate_growth * time`
  per second (up to `energy_cap`) and `component_energy` per HP of every component its bugs
  destroy. Each enemy has a `versus_cost` and is locked until `versus_unlock` seconds;
  enemies with `versus_cost` 0 (the level bosses and their minions) can't be sent.
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
