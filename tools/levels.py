# Ricochet solver: checks that every level in Assets/Levels can be beaten.
# Level file: first line "NAME|SHOTS", then 18 rows of 30 tiles.
# '#' wall tile, '.' empty, 'B' baby start (feet in this cell), 'T' target.
# Run: python3 tools/levels.py
import glob, math, os, sys

def load():
    out = []
    here = os.path.dirname(os.path.abspath(__file__))
    for path in sorted(glob.glob(os.path.join(here, "..", "Assets", "Levels", "Level*.txt"))):
        lines = open(path).read().strip().split("\n")
        name, shots = lines[0].split("|")
        out.append((name, int(shots), "\n".join(lines[1:])))
    return out

LEVELS = load()

W, H = 30, 18

def parse(text):
    rows = [r for r in text.strip().split("\n")]
    assert len(rows) == H and all(len(r) == W for r in rows), [(i, len(r)) for i, r in enumerate(rows) if len(r) != W] or len(rows)
    solid = [[c == '#' for c in r] for r in rows]
    baby = None; targets = []
    for y, r in enumerate(rows):
        for x, c in enumerate(r):
            if c == 'B': baby = (x, y)
            if c == 'T': targets.append((x, y))
    return rows, solid, baby, targets

# Work in map space: x right, y DOWN, one unit per tile, cell (cx, cy) covers [cx, cx+1) x [cy, cy+1).
def standing_cells(solid, baby):
    def ok(x, y):  # feet cell y, head cell y-1, ground y+1
        return 0 < x < W-1 and 1 < y < H-1 and not solid[y][x] and not solid[y-1][x] and solid[y+1][x]
    seen = {baby}; stack = [baby]
    while stack:
        x, y = stack.pop()
        for dx in (-1, 1):
            nx = x + dx
            if solid[y][nx]:
                # step up one tile (needs headroom above current cell too)
                if ok(nx, y-1) and not solid[y-2][x]: c = (nx, y-1)
                else: continue
            elif ok(nx, y): c = (nx, y)
            else:
                # walk off a ledge and fall
                if solid[y-1][nx]: continue
                ny = y
                while not solid[ny+1][nx]: ny += 1
                c = (nx, ny) if ok(nx, ny) else None
                if c is None: continue
            if c not in seen: seen.add(c); stack.append(c)
    return seen

MUZZLE = 0.7; MAXB = 12; MAXLEN = 128.0; TR = 0.5

def shoot(solid, targets, ox, oy, ang):
    """Returns {target index: bounces at hit} for one shot. ang in degrees, 0 = right, 90 = up."""
    dx, dy = math.cos(math.radians(ang)), -math.sin(math.radians(ang))
    x, y = ox + dx*MUZZLE, oy + dy*MUZZLE
    if solid[int(y)][int(x)]: return {}
    hits = {}; b = 0; travelled = 0.0
    while travelled < MAXLEN:
        cx, cy = int(math.floor(x)), int(math.floor(y))
        tx = ((cx + (dx > 0)) - x) / dx if dx else 1e9
        ty = ((cy + (dy > 0)) - y) / dy if dy else 1e9
        t = min(tx, ty)
        # targets along this segment
        for i, (ax, ay) in enumerate(targets):
            if i in hits: continue
            px, py = ax + 0.5 - x, ay + 0.5 - y
            s = max(0.0, min(t, px*dx + py*dy))
            if (px - dx*s)**2 + (py - dy*s)**2 <= TR*TR: hits[i] = b
        nx, ny = x + dx*t, y + dy*t
        travelled += t
        if tx < ty: ncx, ncy = cx + (1 if dx > 0 else -1), cy
        else: ncx, ncy = cx, cy + (1 if dy > 0 else -1)
        if solid[ncy][ncx]:
            if b >= MAXB: break
            b += 1
            if tx < ty: dx = -dx
            else: dy = -dy
            x, y = nx + dx*1e-6, ny + dy*1e-6
        else:
            x, y = nx + (1e-6 if tx < ty else 0)*(1 if dx > 0 else -1), ny + (1e-6 if ty <= tx else 0)*(1 if dy > 0 else -1)
    return hits

def analyse(name, shots, text, verbose=True):
    rows, solid, baby, targets = parse(text)
    cells = standing_cells(solid, baby)
    best = {}  # target -> (bounces, window_deg, x, y, angle)
    STEP = 0.25
    for (cx, cy) in sorted(cells):
        for off in (0.5,):
            ox, oy = cx + off, cy  # gun pivot = sprite centre = feet + 1 unit; feet at cy+1
            run = {}
            a = -30.0
            while a <= 210.0:
                h = shoot(solid, targets, ox, oy, a)
                for i in range(len(targets)):
                    if i in h:
                        k = (i, h[i])
                        st = run.get(k)
                        run[k] = (st[0], a) if st and abs(st[1] - (a - STEP)) < 1e-9 else (a, a)
                        s0, s1 = run[k]
                        width = s1 - s0 + STEP
                        cur = best.get(i)
                        # prefer fewest bounces among windows at least 1 degree wide
                        if width >= 1.0 and (cur is None or h[i] < cur[0] or (h[i] == cur[0] and width > cur[1])):
                            best[i] = (h[i], width, ox, oy, (s0 + s1)/2)
                a += STEP
    ok = all(i in best for i in range(len(targets)))
    if verbose:
        print(f"{name}: {len(targets)} targets, {shots} shots, {len(cells)} standing cells, {'SOLVABLE' if ok else 'NOT SOLVABLE'}")
        for i, t in enumerate(targets):
            if i in best:
                bnc, w, ox, oy, ang = best[i]
                print(f"   target at col {t[0]} row {t[1]}: easiest = {bnc} bounce(s), {w:.2f} deg window, stand col {ox-0.5:.0f} row {oy:.0f}, aim {ang:.1f} deg")
            else: print(f"   target at col {t[0]} row {t[1]}: no reliable shot found")
    return ok, best

if __name__ == "__main__":
    allok = True
    for name, shots, text in LEVELS:
        ok, _ = analyse(name, shots, text); allok &= ok
    sys.exit(0 if allok else 1)
