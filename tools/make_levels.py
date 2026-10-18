from level_lib import *
import json, os

def original_waves(L, n):
    """Waves of the GameMaker prototype (converted from WaveData.txt)."""
    data = json.load(open(os.path.join(os.path.dirname(__file__), f"orig_{n}.json")))
    for w in data["waves"]:
        L.wave([(s["enemy"], s["time"]) for s in w])

def level1():
    L = Level("Dev Board", "A hobby board under attack", "blue", 30, "music_level_1",
              [(-1,2),(13,2),(13,5),(7,5),(7,10)], core="CPU")
    L.add("header",1,0,10,1); L.add("header",14,0,6,1)
    L.add("label",2,1,4,1,label="DIGITAL")
    L.add("port",0,4,label="usb"); L.add("chip",4,4,label="CH340")
    L.add("led",2,3,color="3dff6a"); L.add("led",10,3,color="ffd34d"); L.add("led",11,3,color="ffd34d")
    L.add("connector",0,7,color="1d2026"); L.add("mosfet",2,7); L.add("capacitor",3,7); L.add("capacitor",3,8); L.add("smd",2,8)
    L.add("chip",9,7,3,3,label="ATMEGA"); L.add("crystal",9,11,label="16.000")
    L.add("smd",12,7); L.add("capacitor",12,9,color="ffd34d"); L.add("smd",12,10)
    L.add("switch",3,12,color="d93b3b"); L.add("resistor",1,10); L.add("smd",4,10)
    L.add("header",1,15,6,1); L.add("header",11,15,8,1); L.add("label",11,14,5,1,label="ANALOG IN")
    L.add("header",15,8,2,3); L.add("label",15,11,3,1,label="ICSP")
    L.add("capacitor",17,4,2,2); L.add("smd",16,4); L.add("smd",19,7); L.add("resistor",17,7)
    L.add("chip",16,12,2,2,label="LM1117"); L.add("label",14,13,6,1,label="HWD UNO")
    L.add("hole",19,10); L.add("jumper",19,4,color="2f6fd6")
    L.sprinkle(101, 0.55)
    original_waves(L, 1)
    return L

def level2():
    L = Level("Keyboard", "Every key is a snack", "white", 20, "music_level_2",
              [(5,-1),(5,5),(3,5),(3,10),(6,10),(6,11),(15,11),(15,6),(11,6),(11,3),(17,3)], core="MCU")
    keys = "d93b3b"
    L.fill("switch",[(x,0) for x in range(0,21)],color=keys)
    L.fill("switch",[(x,y) for x in (0,1) for y in range(2,12)],color=keys)
    L.fill("switch",[(x,y) for x in range(8,14) for y in (8,9)],color="ffd34d")
    L.fill("switch",[(x,y) for x in (19,20) for y in range(5,13)],color=keys)
    L.fill("switch",[(x,y) for x in range(0,21) for y in (13,14)],color=keys)
    L.fill("switch",[(x,15) for x in list(range(0,7))+list(range(13,21))],color=keys)
    L.add("switch",7,15,6,1,color="2f6fd6")
    L.add("port",19,1,label="usb")
    L.add("led",19,3,color="3dff6a"); L.add("led",20,3,color="ffd34d")
    L.add("smd",7,2); L.add("smd",9,4); L.add("smd",17,7); L.add("smd",13,4)
    L.add("crystal",17,9,label="12.000")
    L.add("label",7,3,3,1,label="HWD-60")
    L.sprinkle(202, 0.3)
    original_waves(L, 2)
    return L

def level3():
    L = Level("Wi-Fi Router", "Packets, interrupted", "green", 25, "music_level_3",
              [(-1,2),(4,2),(4,8),(13,8),(13,10),(4,10),(4,13),(14,13)], core="SoC")
    L.add("shield",7,1,label="2.4G"); L.add("shield",11,1,label="5G"); L.add("shield",15,1,label="PA")
    L.add("hole",19,1); L.add("hole",19,4); L.add("label",17,0,3,1,label="ANT1  ANT2")
    L.add("smd",7,4); L.add("smd",9,4); L.add("inductor",11,4); L.add("inductor",12,4); L.add("smd",14,4)
    L.add("chip",8,5,2,2,label="DDR3"); L.add("chip",16,5,2,2,label="FLASH"); L.add("crystal",11,6,label="40.000")
    L.add("capacitor",0,4,2,2); L.add("chip",0,7,2,2,label="PMIC")
    L.add("led",6,9,color="3dff6a"); L.add("led",8,9,color="3dff6a"); L.add("led",10,9,color="ffd34d"); L.add("smd",12,9)
    L.add("chip",7,11,2,2,label="WIFI"); L.add("label",9,11,4,1,label="HWD AX3000")
    L.add("port",17,10,label="rj45"); L.add("port",19,10,label="rj45"); L.add("port",17,13,label="rj45"); L.add("port",19,13,label="rj45")
    L.add("connector",0,13,color="1d2026"); L.add("inductor",0,10); L.add("inductor",1,10); L.add("capacitor",0,11); L.add("capacitor",1,11)
    L.fill("led",[(x,15) for x in range(5,12,2)],color="3dff6a")
    L.add("switch",2,15,color="1d2026"); L.add("smd",16,8); L.add("smd",18,8); L.add("resistor",19,7)
    L.sprinkle(303, 0.55)
    original_waves(L, 3)
    return L

def level4():
    L = Level("Sound Card", "Bass-boosted bugs", "red", 40, "music_level_1",
              [(-1,3),(6,3),(6,8),(2,8),(2,12),(10,12),(10,6),(15,6),(15,11),(17,11)], core="DSP")
    gold = "e0a93c"
    L.add("capacitor",8,1,2,2,color=gold); L.add("capacitor",10,1,2,2,color=gold); L.add("capacitor",12,1,2,2,color="2f6fd6")
    L.add("header",15,1,4,1); L.add("label",15,2,4,1,label="FRONT AUDIO")
    L.add("port",19,1,label="audio"); L.add("port",19,4,label="audio"); L.add("port",19,7,label="audio")
    L.add("chip",8,4,2,1,label="OPA2134"); L.add("chip",8,7,2,1,label="OPA2134"); L.add("chip",8,9,2,1,label="NE5532")
    L.add("capacitor",7,5,color=gold); L.add("capacitor",9,5,color=gold); L.add("capacitor",7,10,color=gold)
    L.add("chip",12,8,2,2,label="CODEC"); L.add("crystal",11,10,label="24.576"); L.add("smd",14,9)
    L.add("capacitor",0,5,color=gold); L.add("capacitor",0,6,color=gold); L.add("smd",3,5); L.add("resistor",3,6)
    L.add("smd",0,10); L.add("capacitor",0,11,color="2f6fd6")
    L.fill("capacitor",[(x,13) for x in range(11,16)],color=gold)
    L.add("inductor",16,8); L.add("inductor",17,8); L.add("mosfet",16,6)
    L.add("fingers",2,15,15,1); L.add("label",3,14,6,1,label="HWD BLASTER")
    L.add("shield",17,13,3,1,label="EMI")
    L.sprinkle(404, 0.6)
    L.wave(series("bug",8,0,1.5))
    L.wave(series("bug",8,0,1)+series("spider",4,10,1.5))
    L.wave(series("spider",6,0,1.2)+series("beetle",2,9,3)+series("bug",8,14,0.6))
    L.wave(series("beetle",5,0,2.5)+series("spider",8,4,0.8)+series("bug",10,12,0.5))
    return L

def level5():
    L = Level("Graphics Card", "Overclocked infestation", "black", 50, "music_level_2",
              [(15,-1),(15,3),(4,3),(4,12),(16,12),(16,6),(13,6),(13,8),(10,8)], core="GPU")
    for (x,y) in [(6,6),(6,9),(9,4),(11,4),(9,10),(11,10)]:
        L.add("chip",x,y,2,2,label="GDDR6")
    L.add("hole",8,6); L.add("hole",12,9)
    L.fill("mosfet",[(x,13) for x in range(6,16,2)])
    L.fill("inductor",[(x,14) for x in range(5,16)])
    L.add("fingers",3,15,15,1)
    L.add("connector",18,0,2,3,color="1d2026"); L.add("connector",18,4,2,3,color="1d2026"); L.add("label",17,7,4,1,label="12V 8-PIN")
    L.add("port",0,1,label="hdmi"); L.add("port",0,4,label="hdmi"); L.add("port",0,7,label="hdmi"); L.add("port",0,10,label="hdmi")
    L.add("header",19,9,1,3); L.add("smd",17,9); L.add("smd",17,10); L.add("capacitor",18,13,2,2)
    L.add("smd",6,1); L.add("smd",9,1); L.add("chip",11,0,2,2,label="BIOS"); L.add("smd",13,1)
    L.add("label",5,11,6,1,label="HWD RTX")
    L.sprinkle(505, 0.5)
    L.wave(series("beetle",4,0,2)+series("bug",10,6,0.6))
    L.wave(series("spider",10,0,0.8)+series("tank",2,8,4))
    L.wave(series("tank",3,0,3)+series("beetle",6,2,1.5)+series("bug",12,10,0.4))
    L.wave(series("tank",4,0,2.5)+series("beetle",8,5,1)+series("spider",10,14,0.5))
    return L

def level6():
    L = Level("Laptop", "Thin, light and full of bugs", "blue", 50, "music_level_3",
              [(21,13),(17,13),(17,10),(10,10),(10,13),(3,13),(3,6),(8,6),(8,3),(14,3),(14,7)], core="CPU")
    L.add("ram_slot",0,0,7,1,color="16191e"); L.add("ram_slot",0,1,7,1,color="16191e"); L.add("label",0,2,4,1,label="SO-DIMM")
    L.add("heatsink",10,0,8,2,color="c87533")
    L.add("bga",16,5,3,3,label="PCH")
    L.add("pcie_slot",11,9,4,1,color="16191e")
    L.add("connector",19,0,2,3,color="e9eef3"); L.add("label",18,3,3,1,label="BATT")
    L.add("port",19,8,label="usb"); L.add("port",19,15,1,1,label="usb")
    L.add("port",0,14,label="usb"); L.add("port",0,8,label="hdmi")
    L.fill("smd",[(x,y) for (x,y) in [(4,4),(6,4),(10,4),(12,4),(5,8),(7,8),(9,8),(5,11),(7,11),(12,11),(14,11),(18,11),(19,12),(1,5),(1,11),(11,14),(13,14),(15,14)]])
    L.fill("inductor",[(4,7),(5,7),(6,7),(7,7)])
    L.fill("capacitor",[(11,5),(12,5),(11,6),(12,6)],color="1d2026")
    L.add("chip",4,9,2,2,label="EC"); L.add("chip",7,9,2,1,label="AUDIO"); L.add("crystal",12,13,label="32.768")
    L.add("battery",16,14,2,2); L.add("header",8,14,2,1); L.add("led",6,14,color="3dff6a")
    L.add("label",1,15,5,1,label="HWD BOOK")
    L.sprinkle(606, 0.6)
    L.wave(series("bug",12,0,0.5)+series("beetle",4,4,1.5))
    L.wave(series("runner",2,0,4)+series("spider",10,2,0.6))
    L.wave(series("tank",3,0,2)+series("runner",3,6,2.5)+series("beetle",8,10,0.8))
    L.wave(series("runner",5,0,1.8)+series("tank",4,3,2)+series("spider",14,8,0.4))
    L.wave(series("beetle",10,0,0.8)+series("runner",6,6,1.2)+series("tank",5,10,1.5)+series("bug",16,16,0.3))
    return L

def level7():
    L = Level("ATX Motherboard", "Full tower, full siege", "green", 60, "music_level_1",
              [(12,16),(12,13),(1,13),(1,11),(11,11),(11,7),(8,7),(8,4)], core="CPU")
    L.fill("mosfet",[(x,1) for x in range(5,12)])
    L.fill("inductor",[(x,2) for x in range(5,12) if x not in (7,8,9)]+[(6,y) for y in range(3,6)]+[(10,y) for y in range(3,6)])
    for x in (13,14,15,16):
        L.add("ram_slot",x,1,1,8,color="16191e" if x%2 else "2f6fd6")
    L.add("connector",18,2,2,6,color="e9eef3"); L.add("label",17,8,4,1,label="ATX 24-PIN")
    L.add("pcie_slot",2,10,9,1,color="16191e"); L.add("pcie_slot",2,12,10,1,color="e9eef3"); L.add("pcie_slot",2,14,9,1,color="16191e")
    L.add("heatsink",14,10,3,3,color="3d4c5c"); L.add("label",14,13,3,1,label="Z890")
    L.add("pcie_slot",14,14,6,1,color="16191e"); L.add("label",17,13,2,1,label="M.2")
    L.fill("sata",[(19,10),(20,10),(19,11),(20,11)])
    L.add("battery",2,7,2,2); L.add("chip",4,8,2,1,label="BIOS"); L.add("jumper",4,7,color="d93b3b")
    L.add("port",0,1,label="usb"); L.add("port",0,3,label="rj45"); L.add("port",0,5,label="audio")
    L.add("header",13,15,6,1); L.add("header",19,0,2,1); L.add("smd",12,4); L.add("smd",12,8); L.add("smd",9,9); L.add("crystal",6,9,label="25.000")
    L.add("led",20,14,color="3dff6a"); L.add("label",2,0,3,1,label="HWD")
    L.sprinkle(707, 0.45)
    L.wave(series("beetle",8,0,0.8)+series("tank",3,6,2))
    L.wave(series("runner",4,0,2)+series("spider",14,1,0.4)+series("beetle",6,8,0.7))
    L.wave(series("brute",2,0,4)+series("tank",4,2,1.5)+series("bug",20,8,0.25))
    L.wave(series("runner",6,0,1.2)+series("brute",3,4,3)+series("beetle",10,10,0.5))
    L.wave(series("brute",5,0,2)+series("tank",6,4,1)+series("runner",8,10,0.8)+series("spider",16,14,0.3))
    return L

def level8():
    L = Level("Server Board", "The last line of defence", "purple", 70, "ending",
              [(-1,14),(19,14),(19,11),(2,11),(2,2),(10,2),(10,8),(15,8),(15,5)], core="CPU0")
    L.add("bga",5,5,3,3,label="CPU1"); L.add("label",4,8,5,1,label="(EMPTY)")
    L.fill("ram_slot",[(x,3) for x in (4,)],color="2f6fd6")
    L.add("ram_slot",4,3,5,1,color="2f6fd6"); L.add("ram_slot",4,9,5,1,color="2f6fd6")
    for x in (18,19,20):
        L.add("ram_slot",x,0,1,7,color="16191e" if x%2 else "2f6fd6")
    L.fill("inductor",[(12,y) for y in range(0,7) if y not in (2,)]+[(11,4),(11,6)])
    L.fill("mosfet",[(13,y) for y in (0,1,3)])
    L.add("chip",12,9,2,2,label="BMC"); L.add("crystal",14,10,label="25.000"); L.add("smd",16,9); L.add("smd",17,10)
    L.add("pcie_slot",3,12,8,1,color="16191e"); L.add("pcie_slot",11,12,8,1,color="16191e")
    L.fill("sata",[(x,15) for x in range(1,20,2)],color="16191e")
    L.add("connector",0,4,2,2,color="e9eef3"); L.add("connector",0,7,2,2,color="e9eef3")
    L.add("port",0,0,label="rj45"); L.add("hole",17,7); L.add("smd",3,0); L.add("smd",5,0); L.add("smd",7,0); L.add("smd",9,0)
    L.add("label",13,13,6,1,label="HWD SERVER")
    L.sprinkle(808, 0.55)
    L.wave(series("tank",4,0,1.5)+series("runner",4,3,1.5)+series("beetle",10,6,0.6))
    L.wave(series("brute",3,0,2.5)+series("spider",20,2,0.3))
    L.wave(series("runner",8,0,0.9)+series("tank",6,4,1.2)+series("brute",2,10,3))
    L.wave(series("brute",6,0,1.5)+series("beetle",14,3,0.4)+series("runner",6,9,0.8))
    L.wave(series("tank",8,0,0.8)+series("brute",5,4,1.5)+series("runner",10,8,0.5))
    L.wave([("boss",0)]+series("brute",4,6,2)+series("runner",8,10,0.7)+series("beetle",16,14,0.3))
    return L

LEVELS=[level1,level2,level3,level4,level5,level6,level7,level8]
if __name__=="__main__":
    import sys
    out=sys.argv[1]
    for i,f in enumerate(LEVELS):
        L=f()
        if L.errors:
            print(f"LEVEL {i+1} ERRORS:"); [print("  ",e) for e in L.errors]
            continue
        adj=L.adjacent_free(); free=L.free_cells()
        bugs=sum(len(w) for w in L.d["waves"])
        print(f"L{i+1} {L.d['title']}: comps={len(L.d['components'])} free={len(free)} route-adjacent-free={len(adj)} bugs={bugs}")
        L.save(f"{out}/level_{i+1}.json")
