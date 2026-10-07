# Catacombs Asset Catalog

What every sprite in the Catacombs art pack is, how that was worked out, and how to find or relabel things. Part of [[Scenes and Assets]]; back to [[Home]].

**Files:** the pack is in `Assets/Art/RF_Catacombs_v1.0/`; its 1024 tile assets are in `Assets/Art/Pallates/`; the labelling tools are in `Tools/catacombs-catalog/`; the menu item is `Assets/Prefabs/Scripts/Editor/CatacombsLabels.cs`; the pictures in this note are in `Obsidian/Images/Catacombs/`.

Labelled on 7 Oct 2026 by a Claude Code session, at the user's request, after it turned out nobody had recorded how the level randomizer's floor and wall tiles had been picked.

## Why this was needed

The art pack ships pictures and nothing else. When the sheet was dragged into a Tile Palette, Unity only cut it into 1024 numbered 16×16 sprites (`mainlevbuild_0` … `mainlevbuild_1023`) and made a tile asset for each. Unity has no idea which ones are floor, wall or doorway, and the numbers don't run row by row across the sheet. Whatever needs to know "what is this tile" has to be told by a person. The [[Level Randomizer]]'s Inspector lists were the only record until now, and they covered only the few tiles it uses.

## How it was made

1. **Read Unity's slicing.** Every sprite's rectangle (position and size on the sheet) is in the `.meta` file next to the PNG. `tiles.py` reads those rectangles.
2. **Drew labelled pictures.** `tiles.py region …` cuts the sheet into 12 areas, enlarges them four times and writes each sprite's number on it (the `mainlevbuild_r_X_Y.png` images below). The same was done for `decorative.png`, and the animation frames were drawn side by side.
3. **Identified by eye.** Each picture was looked at and every structure named: which blocks are floor, which form a wall outline, an archway, a pit, a grate, and so on.
4. **Wrote the result as rectangles.** `groups.py` lists every group as one or more rectangles on the sheet, measured in 16px cells from the top-left. A rectangle is far less error-prone than typing a thousand numbers, because the numbers jump around.
5. **Checked coverage.** `groups.py` confirms that every one of the 1024 tiles falls into exactly one group (it did: 1024 of 1024, none left over). It then draws `roles.png`, the whole sheet coloured by role, which was checked by eye for anything in the wrong colour.
6. **Applied the labels in Unity** as asset labels (below), then confirmed by search: `l:Floor` finds the 156 floor tiles, `l:Door` 96, `l:Pit` 60.

**To redo or change it:** edit a rectangle in `Tools/catacombs-catalog/groups.py`, then run `python groups.py` (Python with Pillow, from that folder). It checks coverage and rewrites `tile-labels.txt`. Then in Unity choose **Tools > Catacombs > Apply Tile Labels**. Labels are only ever added, never removed, so to rename a group, remove the old label in Unity as well.

## How to use the labels

Asset labels are Unity's search tags. In the Project window's search box:

- `l:Floor`: every floor tile. `l:Wall`: every wall piece of any kind. `l:Door`, `l:Pit`, `l:Pillar`, `l:Grate`, `l:Gate`, `l:Void`.
- `l:Floor-TileA`, `l:Archway-Large`, `l:Pit-Square` …: one group exactly (the group names in the tables below).
- `l:Catacombs`: everything from this pack, so a second pack can be told apart later.

Each tile has three labels: `Catacombs`, its **role**, and its **group**. Wall pieces get a fourth, `Wall`. Selecting a tile asset shows its labels at the bottom of the Inspector.

Code does not read labels. They are for people finding tiles. The game still gets its tiles from the lists on components such as the [[Level Randomizer]], which hold the tiles themselves. A future decoration system ([[Roadmap]]) would use this catalogue to fill its lists.

Tile 593 already had a `Terrain` label before this. It was kept, though 593 is actually part of a brick burial-niche wall.

## The roles

| Role | Meaning for a level |
|---|---|
| Floor | Walkable ground; painted on the Floor tilemap |
| Wall | Wall structures in general: frames, narrow walls, ledges |
| WallTop | The top edge of a wall seen from above (the outline) |
| WallFace | The front face of a wall: brick, rubble, burial niches |
| WallColumn | The vertical side pieces of a wall structure |
| Void | Pure black: the darkness beyond a wall |
| Door | Archways, doorways and the stairs down: passages through a wall |
| Gate | A barred gate that can stand in a doorway |
| Pillar | Free-standing pillars and columns; the randomizer places some as props |
| Grate | Iron floor grates, one over green water |
| Pit | Holes in the floor: a square pit with tunnel mouths, and a round well |
| Unused | Blank slices |

![[roles.png]]

*The whole sheet coloured by role. Green floor, orange wall, yellow wall top, red wall face, pink wall column, lavender void, purple door, white gate, cyan pillar, grey grate, blue pit.*

## Every group on mainlevbuild.png

### Floor

| Group label | What it is | Tiles |
|---|---|---|
| `Floor-Slab` | Smooth floor slabs, 2x3 blocks, in six colours (brown, teal, dark, olive, green, moss) | 427–438, 470–481, 513–524 |
| `Floor-TileA` | Square stone floor tiles, 2x2 blocks in six colours. Randomizer floor row 1 (starts at 581) | 581–592, 622–633 |
| `Floor-TileB` | Second stone floor tile pattern, 2x2 blocks in six colours. Randomizer floor row 2 (starts at 670) | 670–681, 717–728 |
| `Floor-Rough` | Rough broken floor, 2x2 blocks in six colours. Randomizer floor row 3 (starts at 786) | 786–797, 818–829 |
| `Floor-Ground` | Large plain ground patches (2x4 and 4x4) in brown, teal and green | 886–897, 922–933, 948–959, 974–985 |

### Wall

| Group label | What it is | Tiles |
|---|---|---|
| `WallNarrow-A` | Narrow vertical wall segment with an end cap (left of the big outline) | 59, 100–101, 145–147, 192–194, 235–237, 259–261, 289–291, 318–320, 347–349 |
| `WallNarrow-B` | Second narrow vertical wall segment | 72, 114–115, 160–162, 207–209, 250–252, 274–276, 304–306, 333–335, 362–364 |
| `WallFrame-A` | Arch frame: top beam 15-19, bottom beam 210-214, columns and caps. THIS is the room wall the level randomizer uses | 15–19, 38–39, 73–74, 116–117, 163–164, 210–214 |
| `WallFrame-B` | Second arch frame with stone columns (not used yet) | 20–24, 40–41, 75–76, 118–119, 165–166, 215–216 |
| `WallStub` | Short horizontal wall end piece | 282 |
| `WallLedge` | Low horizontal wall ledge, 3x1 | 283–285 |

### WallTop

| Group label | What it is | Tiles |
|---|---|---|
| `WallCaps-A` | Small cap/corner pieces that sit on top of the big wall outline (set A) | 9–14, 34–37 |
| `WallOutline-Big` | Thick wall outline seen from above, wrapped around the void; a whole north wall template | 60–71, 102, 113, 148, 159, 195, 206, 238–249 |

### WallFace

| Group label | What it is | Tiles |
|---|---|---|
| `WallFace-Brick-Archway` | Brick wall face under the outline, with a dark archway on its right half | 262–273, 292–303, 321–332, 350–361 |
| `BurialWall-Brick` | Brick catacomb wall with burial niches: skeleton bones, skulls and cobwebs, in a frame | 376–389, 396–409, 439–452, 482–495, 525–538 |
| `BurialNiche-Brick` | Brick burial niche chunks with bones, 4x3 each | 551–558, 593–600, 634–641 |
| `WallFace-Brick-Windows` | Brick wall with dark barred windows | 410–414, 453–457, 496–500 |
| `WallFace-Cracked` | Dark wall with cracks and roots | 415–418, 458–461, 501–504 |
| `WallFace-Brick` | Plain brick wall fill, 10x3 variants | 559–568, 601–610, 642–651 |
| `BurialWall-Rubble` | Rubble-stone catacomb wall with burial niches, bones and cobwebs (two variants) | 682–696, 729–743, 760–774, 798–812, 830–844, 860–872, 898–910, 934–946, 960–972, 986–998 |
| `BurialNiche-Rubble` | Rubble burial niche chunks with bones | 1000–1023 |
| `WallFace-Rubble` | Rough rubble stone fill | 700–703, 747–750, 778–781, 848–855, 874–881, 912–919 |
| `WallFace-Rubble-Plaques` | Rubble stone with inset tomb plaques | 704–707, 751–754, 782–785 |

### WallColumn

| Group label | What it is | Tiles |
|---|---|---|
| `WallFrame-A-Legs` | Column pieces that extend frame A downward | 253–254, 277–278, 307–308, 336–337, 365–366 |
| `WallEdge-Rubble` | Columns and edge pieces of the rubble wall | 697–699, 744–746, 775–777, 813–815, 845–847, 873, 911, 947, 973, 999 |

### Void

| Group label | What it is | Tiles |
|---|---|---|
| `Void` | Pure black darkness inside the big wall outline: the unlit space beyond a wall | 103–112, 149–158, 196–205 |

### Door

| Group label | What it is | Tiles |
|---|---|---|
| `Doorway-Narrow` | Narrow stone doorway or wall alcove, 2x5 | 48–49, 83–84, 126–127, 173–174, 223–224 |
| `Archway-Large` | Large dark archway (passage), 5x6 | 0–4, 25–29, 50–54, 85–89, 128–132, 175–179 |
| `Doorway-Small-A` | Small dark doorway, 3x4 | 90–92, 133–135, 180–182, 225–227 |
| `Doorway-Small-B` | Second small dark doorway, with the thin edge pieces to its left | 93–95, 136–140, 183–187, 228–230 |
| `Stairs-Down` | Archway with stairs going down into darkness, 4x7 (a level exit) | 5–8, 30–33, 55–58, 96–99, 141–144, 188–191, 231–234 |

### Gate

| Group label | What it is | Tiles |
|---|---|---|
| `Gate-Bars` | Iron-barred gate / portcullis in a stone frame, 4x5 | 43–46, 78–81, 121–124, 168–171, 218–221 |

### Pillar

| Group label | What it is | Tiles |
|---|---|---|
| `Pillar-Square-A` | Square standing pillar, 1x5 (randomizer prop) | 255, 279, 309, 338, 367 |
| `Pillar-Round` | Round standing column, 1x5 (randomizer prop) | 256, 280, 310, 339, 368 |
| `Pillar-Square-B` | Square standing pillar with carving, 1x5 (randomizer prop) | 257, 281, 311, 340, 369 |
| `Column-Tall` | Tall carved column, 1x5 | 42, 47, 77, 82, 120, 125, 167, 172, 217, 222 |
| `Pillar-Wide` | Wide block pillar, 2x4 (randomizer prop) | 286–287, 312–313, 341–342, 370–371 |
| `Pillar-Tall` | Tall pillar, 1x5 (randomizer prop) | 258, 288, 314, 343, 372 |
| `Pillar-Short` | Short pillar, 1x3 (randomizer prop) | 315, 344, 373 |
| `Pillar-Block` | Block pillar, 2x3 (randomizer prop) | 316–317, 345–346, 374–375 |

### Grate

| Group label | What it is | Tiles |
|---|---|---|
| `Grate-Large` | Big iron floor grate in a stone frame (over a pit or drain) | 390–395, 419–424, 462–467, 505–510, 539–544, 569–574, 611–613, 652–654 |
| `Grate-Water` | Iron grate over green water (sewer) | 708–710, 755–757 |

### Pit

| Group label | What it is | Tiles |
|---|---|---|
| `Pit-Square` | Square pit with stone rim and round tunnel openings on all four sides | 425–426, 468–469, 511–512, 545–550, 575–580, 614–621, 655–662, 664–669, 711–716, 758–759 |
| `Pit-Round` | Round hole / well | 816–817, 856–859, 882–885, 920–921 |

### Unused

| Group label | What it is | Tiles |
|---|---|---|
| `Empty` | Almost or fully transparent slice | 663 |

**Which groups the [[Level Randomizer]] uses** (since 7 Oct 2026, through the Level Style):
- `WallFrame-A` for every room's walls and corners.
- All six colours of `Floor-TileA`, `Floor-TileB` and `Floor-Rough`, plus `Floor-Slab` and `Floor-Ground` as stamps.
- The pillar groups as props.
- The core of `Pit-Square` (6×6, without the tunnel mouths) and `Pit-Round`.
- `Grate-Large` and a 2×2 single grate.
- `Stairs-Down` (rows 1–6) as the exit, and `Gate-Bars` as its closed bars.

Still unused: the other walls (burial walls, rubble, brick faces), archways and doorways, `WallFrame-B`, `Grate-Water`. From `decorative.png` every prop is used ([[Decorations]]), as are the torch, both candles and the spikes.

### The labelled pictures

The sheet in 12 areas, each sprite with its number. The file name gives the area's top-left corner in pixels.

![[mainlevbuild_r_0_0.png]]
![[mainlevbuild_r_256_0.png]]
![[mainlevbuild_r_512_0.png]]
![[mainlevbuild_r_768_0.png]]
![[mainlevbuild_r_0_192.png]]
![[mainlevbuild_r_256_192.png]]
![[mainlevbuild_r_512_192.png]]
![[mainlevbuild_r_768_192.png]]
![[mainlevbuild_r_0_384.png]]
![[mainlevbuild_r_256_384.png]]
![[mainlevbuild_r_512_384.png]]
![[mainlevbuild_r_768_384.png]]

## decorative.png: the 50 props

These are sprites inside one PNG, not tile assets, so Unity can only label the file as a whole (`Catacombs`, `Decor`, `Prop`). This table is the per-sprite record. The numbers are the sprite names `decorative_0` … `decorative_49`.

| Sprites | What they are |
|---|---|
| 0 | Wooden post topped with a horned skull |
| 1 | Wooden post with a hanging basin (brazier stand) |
| 2 | Horned skull ornament on a crossbar (wall mounted) |
| 3 | Wooden post with a chain wrapped round it |
| 4 | Plain wooden post |
| 14 | Broken wooden post |
| 5 | Hanging basin on chains, empty |
| 6 | Hanging basin on chains, with water |
| 7 | Hanging double chains |
| 8, 9 | Hanging chain, short and long |
| 10, 11, 12, 13 | Candle stubs: a pair, a group, a single, a tiny stub (static, not animated) |
| 24, 43 | Stone sarcophagus, lying, closed |
| 15, 29, 37, 49 | Stone sarcophagus, lying, broken open (four variants) |
| 17 | Stone sarcophagus, upright, closed |
| 16, 25 | Stone sarcophagus, upright, broken open |
| 18, 20, 21, 23 | Wooden coffin, upright, closed (small to tall) |
| 19, 22 | Wooden coffin, upright, broken |
| 26, 27, 28, 30, 31 | Clay urns, brown, large to small |
| 32, 33, 34, 35, 36 | Clay urns, brown, broken |
| 38, 39, 40, 41, 42 | Clay urns, green, large to small |
| 44, 45, 46, 47, 48 | Clay urns, green, broken |

![[deco_top.png]]
![[deco_bottom.png]]

## The animated sprites

Separate PNG files, one per frame. Each file is labelled (`Animated`, plus `Torch`/`Candle`/`Light` or `Trap`/`Spikes`).

| Files | What | Frames |
|---|---|---|
| `torch_1` … `torch_4` | Burning torch on a wall bracket | 4, 16×16 |
| `candleA_01` … `candleA_04` | One tall candle, flickering | 4 |
| `candleB_01` … `candleB_04` | A tall and a short candle together (each frame was sliced into the two candles) | 4 |
| `spike_0` … `spike_4` | Floor spike trap: 0 is holes only (retracted), 1–4 rise to fully out (`spike_4` was sliced into its four single spikes) | 5 |

![[anims.png]]

*Left to right: the 4 torch frames, 4 candle A, 4 candle B, then spike 0 to 4.*

## Import settings

Fixed on 7 Oct 2026: `decorative.png`, the torch, candle and spike files are now imported like the tileset (16 pixels per unit, Point filter, no compression). The animation frames are single sprites again; `spike_4` and every `candleB` frame had been auto-sliced into several pieces.

## Links to other systems

- The [[Level Randomizer]] takes its floor, wall and pillar tiles from the groups marked above.
- Decorations (torches, urns, coffins, spikes) are planned as a data-driven step of level building; see [[Roadmap]].
