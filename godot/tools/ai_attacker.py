#!/usr/bin/env python3
"""Example AI for Project Brenda's versus mode: plays the attacker over WebSocket.

Start a host first (the game's Versus > Host game, or headless):
    godot --headless --path godot -- --server --level=3 --defender=bot
then:
    pip install websockets
    python3 tools/ai_attacker.py ws://127.0.0.1:7777

Strategy: save energy until it can afford a wave, then send the strongest unlocked bugs and
pad the wave with the cheapest ones. See docs/PROTOCOL.md for every message.
"""
import asyncio
import json
import sys

import websockets


async def play(url: str) -> None:
    async with websockets.connect(url, max_size=2**22) as ws:
        await ws.send(json.dumps({"type": "hello", "version": 1, "role": "attacker", "name": "python-ai"}))
        welcome = json.loads(await ws.recv())
        if welcome["type"] != "welcome":
            print("rejected:", welcome)
            return
        enemies = {e["id"]: e for e in welcome["enemies"]}
        print(f"playing {welcome['level']['title']} as {welcome['you']}")
        budget = 60.0
        last_report = -1
        async for raw in ws:
            msg = json.loads(raw)
            if msg["type"] == "error":
                continue  # rejected command (not enough energy, too fast...)
            if msg["type"] != "state":
                continue
            s = msg["snapshot"]
            if s["state"] != "playing":
                print(f"match over: {s['state']} at {s['time']:.0f}s, cpu {s['cpu_hp']}, board {s['integrity']:.0%}")
                return
            if int(s["time"]) // 30 != last_report:
                last_report = int(s["time"]) // 30
                print(f"t={s['time']:.0f}s energy={s['energy']:.0f} cpu={s['cpu_hp']} bugs={len(s['bugs'])} turrets={len(s['turrets'])}")
            if s["energy"] < budget:
                continue
            unlocked = sorted((e for e in enemies.values() if s["time"] >= e["versus_unlock"]), key=lambda e: -e["versus_cost"])
            energy = s["energy"]
            wave = []
            for e in unlocked[:2]:
                while e["versus_cost"] <= energy * 0.7:
                    wave.append(e["id"])
                    energy -= e["versus_cost"]
            cheap = unlocked[-1]
            while energy >= cheap["versus_cost"]:
                wave.append(cheap["id"])
                energy -= cheap["versus_cost"]
            for bug in wave:
                await ws.send(json.dumps({"type": "send_bug", "bug": bug}))
                await asyncio.sleep(welcome["rules"]["send_interval"] + 0.02)
            budget = min(60 + s["time"] * 1.2, welcome["rules"]["energy_cap"] * 0.9)


if __name__ == "__main__":
    asyncio.run(play(sys.argv[1] if len(sys.argv) > 1 else "ws://127.0.0.1:7777"))
