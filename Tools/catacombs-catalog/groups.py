# What every tile of mainlevbuild.png is. Each group is a set of rectangles on the sheet (in 16px cells,
# counted from the top-left), a role that becomes a Unity asset label, and a description.
# Usage: python groups.py   -> checks every tile belongs to a group, prints counts, and writes
#                              tile-labels.txt for Unity's menu "Tools > Catacombs > Apply Tile Labels".
import json, os, sys
from tiles import sprites
HERE = os.path.dirname(os.path.abspath(__file__))

def tile_positions():
    # sprite index -> (column, row) of its 16px cell, rows counted from the top of the 640px-high sheet
    out = {}
    for name, x, y, w, h in sprites("mainlevbuild.png"):
        out[int(name.split("_")[1])] = (int(x // 16), int((640 - y - 16) // 16))
    return out

pos = tile_positions()

# (label, role, description, [rectangles as (col0,row0,col1,row1) inclusive, in sheet cells from the top-left])
# Roles become Unity asset labels. Groups are matched in order; the first group containing a tile wins,
# so small groups that sit inside bigger rectangles (the void) come first.
GROUPS = [
    ("Void", "Void", "Pure black darkness inside the big wall outline: the unlit space beyond a wall",
        [(5, 4, 14, 6)]),
    ("WallCaps-A", "WallTop", "Small cap/corner pieces that sit on top of the big wall outline (set A)",
        [(4, 1, 15, 2)]),
    ("WallOutline-Big", "WallTop", "Thick wall outline seen from above, wrapped around the void; a whole north wall template",
        [(4, 3, 15, 7)]),
    ("WallFace-Brick-Archway", "WallFace", "Brick wall face under the outline, with a dark archway on its right half",
        [(4, 8, 15, 11)]),
    ("WallNarrow-A", "Wall", "Narrow vertical wall segment with an end cap (left of the big outline)",
        [(1, 3, 3, 11)]),
    ("WallNarrow-B", "Wall", "Second narrow vertical wall segment",
        [(16, 3, 18, 11)]),
    ("WallFrame-A", "Wall", "Arch frame: top beam 15-19, bottom beam 210-214, columns and caps. THIS is the room wall the level randomizer uses",
        [(19, 1, 23, 6)]),
    ("WallFrame-A-Legs", "WallColumn", "Column pieces that extend frame A downward",
        [(19, 7, 19, 11), (23, 7, 23, 11)]),
    ("WallFrame-B", "Wall", "Second arch frame with stone columns (not used yet)",
        [(25, 1, 29, 6)]),
    ("Pillar-Square-A", "Pillar", "Square standing pillar, 1x5 (randomizer prop)",
        [(25, 7, 25, 11)]),
    ("Pillar-Round", "Pillar", "Round standing column, 1x5 (randomizer prop)",
        [(27, 7, 27, 11)]),
    ("Pillar-Square-B", "Pillar", "Square standing pillar with carving, 1x5 (randomizer prop)",
        [(29, 7, 29, 11)]),
    ("Column-Tall", "Pillar", "Tall carved column, 1x5",
        [(31, 2, 31, 6), (36, 2, 36, 6)]),
    ("WallStub", "Wall", "Short horizontal wall end piece",
        [(31, 8, 31, 8)]),
    ("Gate-Bars", "Gate", "Iron-barred gate / portcullis in a stone frame, 4x5",
        [(32, 2, 35, 6)]),
    ("Doorway-Narrow", "Door", "Narrow stone doorway or wall alcove, 2x5",
        [(37, 2, 38, 6)]),
    ("Archway-Large", "Door", "Large dark archway (passage), 5x6",
        [(40, 0, 44, 5)]),
    ("Doorway-Small-A", "Door", "Small dark doorway, 3x4",
        [(45, 3, 47, 6)]),
    ("Doorway-Small-B", "Door", "Second small dark doorway, with the thin edge pieces to its left",
        [(48, 3, 52, 6)]),
    ("Stairs-Down", "Door", "Archway with stairs going down into darkness, 4x7 (a level exit)",
        [(55, 0, 58, 6)]),
    ("WallLedge", "Wall", "Low horizontal wall ledge, 3x1",
        [(32, 8, 34, 8)]),
    ("Pillar-Wide", "Pillar", "Wide block pillar, 2x4 (randomizer prop)",
        [(37, 8, 38, 11)]),
    ("Pillar-Tall", "Pillar", "Tall pillar, 1x5 (randomizer prop)",
        [(40, 7, 40, 11)]),
    ("Pillar-Short", "Pillar", "Short pillar, 1x3 (randomizer prop)",
        [(42, 9, 42, 11)]),
    ("Pillar-Block", "Pillar", "Block pillar, 2x3 (randomizer prop)",
        [(44, 9, 45, 11)]),
    ("BurialWall-Brick", "WallFace", "Brick catacomb wall with burial niches: skeleton bones, skulls and cobwebs, in a frame",
        [(3, 12, 16, 16)]),
    ("BurialNiche-Brick", "WallFace", "Brick burial niche chunks with bones, 4x3 each",
        [(5, 17, 8, 19), (11, 17, 14, 19)]),
    ("WallFace-Brick-Windows", "WallFace", "Brick wall with dark barred windows",
        [(17, 13, 21, 15)]),
    ("WallFace-Cracked", "WallFace", "Dark wall with cracks and roots",
        [(23, 13, 26, 15)]),
    ("WallFace-Brick", "WallFace", "Plain brick wall fill, 10x3 variants",
        [(17, 17, 26, 19)]),
    ("Empty", "Unused", "Almost or fully transparent slice",
        [(26, 20, 26, 20)]),
    ("Grate-Large", "Grate", "Big iron floor grate in a stone frame (over a pit or drain)",
        [(30, 12, 35, 17), (31, 18, 31, 19), (32, 18, 34, 19)]),
    ("Grate-Water", "Grate", "Iron grate over green water (sewer)",
        [(31, 21, 34, 22)]),
    ("Pit-Square", "Pit", "Square pit with stone rim and round tunnel openings on all four sides",
        [(37, 13, 44, 22)]),
    ("Pit-Round", "Pit", "Round hole / well",
        [(39, 24, 42, 27)]),
    ("BurialWall-Rubble", "WallFace", "Rubble-stone catacomb wall with burial niches, bones and cobwebs (two variants)",
        [(1, 21, 15, 25), (3, 26, 15, 30)]),
    ("BurialNiche-Rubble", "WallFace", "Rubble burial niche chunks with bones",
        [(5, 31, 14, 33)]),
    ("WallEdge-Rubble", "WallColumn", "Columns and edge pieces of the rubble wall",
        [(16, 21, 18, 25), (16, 26, 16, 30)]),
    ("WallFace-Rubble", "WallFace", "Rough rubble stone fill",
        [(19, 21, 22, 23), (19, 25, 27, 27)]),
    ("WallFace-Rubble-Plaques", "WallFace", "Rubble stone with inset tomb plaques",
        [(24, 21, 27, 23)]),
    ("Floor-Slab", "Floor", "Smooth floor slabs, 2x3 blocks, in six colours (brown, teal, dark, olive, green, moss)",
        [(46, 13, 63, 15)]),
    ("Floor-TileA", "Floor", "Square stone floor tiles, 2x2 blocks in six colours. Randomizer floor row 1 (starts at 581)",
        [(46, 17, 63, 18)]),
    ("Floor-TileB", "Floor", "Second stone floor tile pattern, 2x2 blocks in six colours. Randomizer floor row 2 (starts at 670)",
        [(46, 20, 63, 21)]),
    ("Floor-Rough", "Floor", "Rough broken floor, 2x2 blocks in six colours. Randomizer floor row 3 (starts at 786)",
        [(46, 23, 63, 24)]),
    ("Floor-Ground", "Floor", "Large plain ground patches (2x4 and 4x4) in brown, teal and green",
        [(43, 26, 63, 29)]),
]

def assign():
    owner = {}
    for i, (c, r) in pos.items():
        for g in GROUPS:
            if any(c0 <= c <= c1 and r0 <= r <= r1 for (c0, r0, c1, r1) in g[3]):
                owner[i] = g[0]
                break
    return owner


# ---------- Level style export ----------
# The tiles the level randomizer's LevelStyle uses, by rectangle on the sheet. Read by the Unity menu
# "Tools > Catacombs > Build Level Style". Each line: key|width|tile numbers row by row from the top,
# -1 for a cell that stays empty (e.g. the cut-off corners of the round well).
COLOURS = 6  # the floor families come in six colours, side by side, 3 cells apart

def stamp(c0, r0, w, h):
    inv = {v: k for k, v in pos.items()}
    return [inv.get((c, r), -1) for r in range(r0, r0 + h) for c in range(c0, c0 + w)]

def write_style_tiles():
    out = []
    def add(key, w, tiles): out.append(f"{key}|{w}|{','.join(str(t) for t in tiles)}")
    for k in range(COLOURS):
        c = 46 + 3 * k
        add(f"floor.{k}.tileA", 2, stamp(c, 17, 2, 2))
        add(f"floor.{k}.tileB", 2, stamp(c, 20, 2, 2))
        add(f"floor.{k}.rough", 2, stamp(c, 23, 2, 2))
        add(f"slab.{k}", 2, stamp(c, 13, 2, 3))
    for g in range(3):
        add(f"ground.{g}", 4, stamp(46 + 6 * g, 26, 4, 4))
    add("pit.square", 6, stamp(38, 16, 6, 6))
    add("pit.round", 4, stamp(39, 24, 4, 4))
    add("grate.large", 6, stamp(30, 12, 6, 6))
    add("grate.small", 2, stamp(31, 18, 2, 2))
    add("stairs", 4, stamp(55, 1, 4, 6))
    add("bars", 4, stamp(32, 2, 4, 5))
    open(os.path.join(HERE, "style-tiles.txt"), "w").write("\n".join(out))
    print("wrote style-tiles.txt")


if __name__ == "__main__":
    owner = assign()
    missing = sorted(set(pos) - set(owner))
    print("assigned", len(owner), "of", len(pos))
    print("unassigned:", [(i, pos[i]) for i in missing])
    counts = {}
    for i, g in owner.items(): counts[g] = counts.get(g, 0) + 1
    for g in GROUPS: print(f"{g[0]:26} {g[1]:10} {counts.get(g[0], 0)}")
    role = {grp[0]: grp[1] for grp in GROUPS}
    lines = []
    for i in sorted(owner):
        labels = ["Catacombs", role[owner[i]], owner[i]]
        # every wall role also gets the plain "Wall" label, so l:Wall finds all of them
        if role[owner[i]] in ("WallTop", "WallFace", "WallColumn"): labels.append("Wall")
        lines.append(f"{i}|{','.join(labels)}")
    open(os.path.join(HERE, "tile-labels.txt"), "w").write("\n".join(lines))
    print("wrote tile-labels.txt")
    write_style_tiles()
