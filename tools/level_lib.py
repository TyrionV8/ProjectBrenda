import json
COLS, ROWS = 21, 16
DEFAULT = {"chip":(2,2),"bga":(3,3),"header":(1,4),"connector":(2,2),"capacitor":(1,1),"smd":(1,1),"resistor":(2,1),
 "led":(1,1),"crystal":(2,1),"inductor":(1,1),"mosfet":(1,1),"ram_slot":(8,1),"pcie_slot":(10,1),"sata":(1,1),"port":(2,2),
 "battery":(2,2),"heatsink":(3,3),"shield":(3,2),"switch":(1,1),"jumper":(1,1),"fingers":(8,1),"hole":(1,1),"label":(3,1)}
NONBLOCK = {"label"}

def seg_cells(a, b):
    out=[]; steps=max(abs(b[0]-a[0]), abs(b[1]-a[1]))
    for s in range(steps+1):
        t = s/max(steps,1)
        out.append((round(a[0]+(b[0]-a[0])*t), round(a[1]+(b[1]-a[1])*t)))
    return out

class Level:
    def __init__(self, title, subtitle, theme, money, music, path, core="CPU"):
        self.d = dict(title=title, subtitle=subtitle, theme=theme, core_label=core, start_money=money, music=music,
                      path=[list(p) for p in path], components=[], waves=[])
        self.route=set()
        for i in range(len(path)-1):
            for c in seg_cells(path[i], path[i+1]):
                if 0<=c[0]<COLS and 0<=c[1]<ROWS: self.route.add(c)
        cx,cy = path[-1]
        self.cpu={(cx+dx,cy+dy) for dx in (-1,0,1) for dy in (-1,0,1)}
        self.used={}
        self.errors=[]
    def add(self, t, x, y, w=None, h=None, color=None, label=None, soft=False):
        dw,dh = DEFAULT[t]
        w = dw if w is None else w; h = dh if h is None else h
        cells=[(x+i,y+j) for i in range(w) for j in range(h)]
        if t not in NONBLOCK:
            bad=[c for c in cells if not (0<=c[0]<COLS and 0<=c[1]<ROWS) or c in self.route or c in self.cpu or c in self.used]
            if bad:
                if soft: return False
                self.errors.append(f"{t} at {(x,y)} {w}x{h} conflicts {bad[:3]}")
                return False
            for c in cells: self.used[c]=t
        comp={"type":t,"cell":[x,y],"size":[w,h]}
        if color: comp["color"]=color
        if label: comp["label"]=label
        self.d["components"].append(comp)
        return True
    def fill(self, t, cells, **kw):
        for (x,y) in cells: self.add(t,x,y,soft=True,**kw)
    def sprinkle(self, seed, density=0.5, keep_route_free=True):
        """Scatter small passives over empty board areas (not next to the route, so turrets fit)."""
        import random
        rng = random.Random(seed)
        near = set()
        for (x,y) in self.route | self.cpu:
            for dx in (-1,0,1):
                for dy in (-1,0,1):
                    near.add((x+dx,y+dy))
        cells = [c for c in self.free_cells() if not (keep_route_free and c in near)]
        rng.shuffle(cells)
        target = int(len(cells) * density)
        placed = 0
        kinds = ["smd"]*6 + ["resistor","capacitor","capacitor","mosfet","inductor","smd","crystal"]
        for (x,y) in cells:
            if placed >= target: break
            if (x,y) in self.used: continue
            # leave some breathing room: skip if 3+ neighbours are already used
            busy = sum((x+dx,y+dy) in self.used for dx,dy in ((1,0),(-1,0),(0,1),(0,-1)))
            if busy >= 3: continue
            t = rng.choice(kinds)
            if t == "resistor":
                ok = self.add("resistor", x, y, 2, 1, soft=True) or self.add("resistor", x, y, 1, 2, soft=True)
            elif t == "crystal":
                ok = self.add("crystal", x, y, 2, 1, soft=True)
            elif t == "capacitor":
                ok = self.add("capacitor", x, y, color=rng.choice([None, None, "1d2026", "e0a93c"]), soft=True)
            else:
                ok = self.add(t, x, y, soft=True)
            placed += 1 if ok else 0
        return placed

    def wave(self, spawns):
        self.d["waves"].append(sorted([{"enemy":e,"time":round(t,2)} for e,t in spawns], key=lambda s:s["time"]))
    def free_cells(self):
        return [(x,y) for x in range(COLS) for y in range(ROWS) if (x,y) not in self.used and (x,y) not in self.route and (x,y) not in self.cpu]
    def adjacent_free(self):
        f=set(self.free_cells())
        return [c for c in f if any((c[0]+dx,c[1]+dy) in self.route for dx,dy in ((1,0),(-1,0),(0,1),(0,-1)))]
    def save(self, path):
        assert not self.errors, self.errors
        c=lambda v: json.dumps(v,separators=(', ',': '))
        d=self.d
        out=['{']
        for k in ["title","subtitle","theme","core_label","start_money","music"]:
            out.append(f' "{k}": {c(d[k])},')
        out.append(f' "path": {c(d["path"])},')
        out.append(' "components": [')
        out.append(',\n'.join('  '+c(x) for x in d["components"]))
        out.append(' ],')
        out.append(' "waves": [')
        out.append(',\n'.join('  [\n'+',\n'.join('   '+c(s) for s in w)+'\n  ]' for w in d["waves"]))
        out.append(' ]')
        out.append('}')
        open(path,'w').write('\n'.join(out)+'\n')

def series(enemy, n, start, every):
    return [(enemy, start+i*every) for i in range(n)]
