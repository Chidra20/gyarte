using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

// Builds a random level out of the Catacombs tiles: a handful of walled rooms joined by small doorways,
// each with a patchy floor and a few pillars. Every room also gets a room object for RoomManager,
// which keeps the rooms the player is not in dark.
// Hook Randomize() up to a UI button to build a new level.
public class LevelRandomizer : MonoBehaviour
{
    // The floor art is drawn in 2x2 tile blocks, so the floor is placed one block at a time
    [System.Serializable]
    public struct FloorBlock
    {
        public TileBase topLeft;
        public TileBase topRight;
        public TileBase bottomLeft;
        public TileBase bottomRight;
    }

    [System.Serializable]
    public class Prop
    {
        public int width = 1;
        [Tooltip("Tiles listed row by row from the top, left to right.")]
        public TileBase[] tiles;
    }

    // A 2x2 hole through the two walls between neighbouring rooms
    private struct Door
    {
        public RectInt cells;
        public bool sideways;
    }

    private const int DoorSize = 2;

    [Header("Tilemaps")]
    public Tilemap floorTilemap;
    [Tooltip("Needs a TilemapCollider2D so the walls and pillars block the player.")]
    public Tilemap wallTilemap;
    [Tooltip("Walk-over pieces drawn on top of the floor without replacing it (grates, the stairs' steps). No collider. Uses the floor tilemap when empty.")]
    public Tilemap detailTilemap;

    [Header("Player")]
    [Tooltip("Moved back to the middle of the first room every time the level is randomized.")]
    public Transform player;

    [Tooltip("Build a level when the scene starts if none is saved in it. Off when something else (the game loop) decides when to build.")]
    public bool buildOnStart = true;
    [Tooltip("The art set: floor colours, pits, grates, the stairs exit and decorations. Without one, floors use the Floor Blocks list below and rooms stay bare.")]
    public LevelStyle style;
    [Tooltip("Place themed decorations, torches and traps (needs a style).")]
    public bool decorate = true;
    [Tooltip("Multiplies how many decorations each room gets. 0 = none, 1 = the themes' numbers.")]
    [Range(0f, 3f)] public float decorationDensity = 1f;

    [Header("Rooms")]
    public int minRooms = 3;
    public int maxRooms = 6;
    [Tooltip("The room objects are created under this. Its RoomManager darkens every room the player is not in.")]
    public Transform roomsParent;
    [Tooltip("Stretched over a room to hide it. Any plain square sprite works.")]
    public Sprite darknessSprite;
    public Color darknessColor = Color.black;
    [Tooltip("Has to be above everything that should be hidden inside a dark room.")]
    public int darknessSortingOrder = 10;

    [Header("Room Size (in 2x2 floor blocks)")]
    public Vector2Int minRoomBlocks = new Vector2Int(7, 5);
    public Vector2Int maxRoomBlocks = new Vector2Int(12, 8);

    [Header("Floor")]
    public FloorBlock[] floorBlocks;
    [Tooltip("Smaller values make bigger patches of the same floor.")]
    public float patchScale = 0.3f;
    [Tooltip("Chance for a block to be a random floor instead of matching its patch.")]
    [Range(0f, 1f)] public float accentChance = 0.12f;

    [Header("Walls")]
    public TileBase topLeftCorner;
    public TileBase topRightCorner;
    public TileBase bottomLeftCorner;
    public TileBase bottomRightCorner;
    [Tooltip("Repeated from left to right.")]
    public TileBase[] topWall;
    [Tooltip("Repeated from left to right.")]
    public TileBase[] bottomWall;
    [Tooltip("Placed right under the top corners and under sideways doorways.")]
    public TileBase leftWallCap;
    public TileBase rightWallCap;
    [Tooltip("Repeated from top to bottom.")]
    public TileBase[] leftWall;
    [Tooltip("Repeated from top to bottom.")]
    public TileBase[] rightWall;

    [Header("Pillars (per room)")]
    public Prop[] props;
    public int minProps = 0;
    public int maxProps = 3;
    [Tooltip("Free cells kept around every pillar so the player can always walk past it.")]
    public int propSpacing = 2;

    // The last level built: each room's floor area in cells, and which rooms a door joins.
    // Room 0 is where the player starts. Empty until Randomize has run in this session.
    public IReadOnlyList<RectInt> Rooms => rooms;
    public IReadOnlyList<Vector2Int> RoomLinks => roomLinks;
    public int StartRoomIndex => 0;

    private readonly List<RectInt> rooms = new List<RectInt>();
    private readonly List<Vector2Int> roomLinks = new List<Vector2Int>();
    private readonly List<Door> doorList = new List<Door>();
    private readonly List<string> roomThemeNames = new List<string>();
    private Transform decorationsParent;

    // Which cells of the last level are taken (pillars, pits, props, spikes, doorways, spawn)
    public LevelOccupancy Occupancy { get; } = new LevelOccupancy();

    // The 2x2 area around cell (0,0) where the player appears
    private static readonly RectInt SpawnArea = new RectInt(-1, -1, 2, 2);

    // The stairs-down exit of the last level, if one fitted (see BuildStairsExit)
    public bool HasStairsExit { get; private set; }
    public RectInt ExitCells { get; private set; }
    public Vector2 ExitPoint { get; private set; }
    public int ExitRoomIndex { get; private set; } = -1;
    private Vector2Int exitFront;

    void Start()
    {
        // Keep the level that was saved with the scene; only build one if there is nothing there
        if (buildOnStart && floorTilemap.GetUsedTilesCount() == 0) Randomize();
    }

    // A random spot on a room's floor with no wall or pillar tile in the 3x3 cells around it,
    // so a character placed there is not stuck. Tiles are checked rather than colliders because
    // the wall collider is only rebuilt after a level is built.
    public bool TryGetRandomFloorPoint(int roomIndex, out Vector2 point)
    {
        point = Vector2.zero;
        if (roomIndex < 0 || roomIndex >= rooms.Count) return false;

        RectInt room = rooms[roomIndex];
        for (int attempt = 0; attempt < 20; attempt++)
        {
            var cell = new Vector3Int(Random.Range(room.xMin + 1, room.xMax - 1), Random.Range(room.yMin + 1, room.yMax - 1), 0);
            if (!ClearAround(cell) || NearOccupied(cell)) continue;

            point = floorTilemap.GetCellCenterWorld(cell);
            return true;
        }
        return false;
    }

    bool ClearAround(Vector3Int cell)
    {
        for (int x = -1; x <= 1; x++)
        {
            for (int y = -1; y <= 1; y++)
            {
                Vector3Int near = cell + new Vector3Int(x, y, 0);
                if (wallTilemap.HasTile(near) || !floorTilemap.HasTile(near)) return false;
            }
        }
        return true;
    }

    // Enemies and the key never go on or right next to something solid or a trap
    bool NearOccupied(Vector3Int cell)
    {
        for (int x = -1; x <= 1; x++)
        {
            for (int y = -1; y <= 1; y++)
            {
                var near = new Vector2Int(cell.x + x, cell.y + y);
                if (Occupancy.IsBlocked(near) || Occupancy.IsHazard(near)) return true;
            }
        }
        return false;
    }

    // Can the player still walk from the spawn to every doorway (and the exit)? Used after placing
    // anything that blocks, so a pit or a big prop never seals part of the level off
    public bool StillConnected(IEnumerable<Vector2Int> extraTargets = null)
    {
        var walkable = new HashSet<Vector2Int>();
        foreach (RectInt room in rooms)
            foreach (Vector2Int cell in room.allPositionsWithin) walkable.Add(cell);
        foreach (Door door in doorList)
            foreach (Vector2Int cell in door.cells.allPositionsWithin) walkable.Add(cell);
        walkable.RemoveWhere(cell => Occupancy.IsBlocked(cell) || wallTilemap.HasTile(new Vector3Int(cell.x, cell.y, 0)));

        var targets = new List<Vector2Int>();
        foreach (Door door in doorList) targets.Add(door.cells.min);
        if (HasStairsExit) targets.Add(exitFront);
        if (extraTargets != null) targets.AddRange(extraTargets);
        return LevelOccupancy.AllReachable(walkable, Vector2Int.zero, targets);
    }

    // Which room a world position is in, counting the room's own walls (so a doorway belongs to a room). -1 if none
    public int RoomIndexAt(Vector2 worldPoint)
    {
        Vector3Int cell = floorTilemap.WorldToCell(worldPoint);
        var point = new Vector2Int(cell.x, cell.y);
        for (int i = 0; i < rooms.Count; i++)
        {
            if (rooms[i].Contains(point)) return i;
        }
        for (int i = 0; i < rooms.Count; i++)
        {
            if (WithWalls(rooms[i]).Contains(point)) return i;
        }
        return -1;
    }

    // World position of a room's centre
    public Vector2 RoomCenter(int roomIndex)
    {
        RectInt room = rooms[roomIndex];
        Vector3 min = floorTilemap.CellToWorld(new Vector3Int(room.xMin, room.yMin, 0));
        Vector3 max = floorTilemap.CellToWorld(new Vector3Int(room.xMax, room.yMax, 0));
        return (min + max) / 2f;
    }

    public void Randomize()
    {
        floorTilemap.ClearAllTiles();
        wallTilemap.ClearAllTiles();
        if (detailTilemap != null) detailTilemap.ClearAllTiles();
        ClearRoomObjects();

        List<Door> doors = doorList;
        doors.Clear();
        roomLinks.Clear();
        rooms.Clear();
        Occupancy.Clear();
        rooms.AddRange(LayOutRooms(doors));

        // Doorways, the cells in front of them and the spawn always stay clear
        foreach (Door door in doors) Occupancy.Reserve(new RectInt(door.cells.xMin - 1, door.cells.yMin - 1, door.cells.width + 2, door.cells.height + 2));
        Occupancy.Reserve(SpawnArea);

        foreach (RectInt room in rooms)
        {
            BuildFloor(room);
            BuildWalls(room);
        }

        foreach (Door door in doors) CutDoor(door);

        BuildStairsExit();

        roomThemeNames.Clear();
        bool previousWasTrap = false;
        for (int i = 0; i < rooms.Count; i++)
        {
            PlaceProps(rooms[i], doors, i == 0);
            if (i != 0) PlaceStructures(rooms[i]);
            previousWasTrap = Decorate(i, previousWasTrap);
            CreateRoomObject(rooms[i], i);
        }

        MovePlayerToCenter();
    }

    // ---------- Layout ----------

    // Returns the floor area of every room. The walls sit one cell outside of it.
    List<RectInt> LayOutRooms(List<Door> doors)
    {
        // The first room is centred on cell (0, 0) so the middle is always a safe place for the player
        Vector2Int firstSize = RandomRoomSize();
        List<RectInt> rooms = new List<RectInt> { new RectInt(-firstSize.x / 2, -firstSize.y / 2, firstSize.x, firstSize.y) };

        int roomCount = Random.Range(minRooms, maxRooms + 1);

        // Grow the level by attaching each new room to a random side of a room that is already there
        for (int attempt = 0; attempt < 200 && rooms.Count < roomCount; attempt++)
        {
            int fromIndex = Random.Range(0, rooms.Count);
            RectInt from = rooms[fromIndex];
            Vector2Int size = RandomRoomSize();
            RectInt room;
            Door door;

            if (Random.value < 0.5f)
            {
                // Next to each other. The doorway stays clear of the top and bottom rows of both rooms
                // so the wall above and below it is still a plain wall.
                bool toTheRight = Random.value < 0.5f;
                int doorY = Random.Range(from.yMin + 1, from.yMax - DoorSize);
                int y = Random.Range(doorY + DoorSize + 1 - size.y, doorY);
                int x = toTheRight ? from.xMax + 2 : from.xMin - 2 - size.x;

                room = new RectInt(x, y, size.x, size.y);
                door.cells = new RectInt(toTheRight ? from.xMax : from.xMin - 2, doorY, 2, DoorSize);
                door.sideways = true;
            }
            else
            {
                // On top of each other
                bool above = Random.value < 0.5f;
                int doorX = Random.Range(from.xMin, from.xMax - DoorSize + 1);
                int x = Random.Range(doorX + DoorSize - size.x, doorX + 1);
                int y = above ? from.yMax + 2 : from.yMin - 2 - size.y;

                room = new RectInt(x, y, size.x, size.y);
                door.cells = new RectInt(doorX, above ? from.yMax : from.yMin - 2, DoorSize, 2);
                door.sideways = false;
            }

            // Walls of different rooms may touch but never overlap
            RectInt walls = WithWalls(room);
            if (rooms.Exists(other => WithWalls(other).Overlaps(walls))) continue;

            roomLinks.Add(new Vector2Int(fromIndex, rooms.Count));
            rooms.Add(room);
            doors.Add(door);
        }

        return rooms;
    }

    Vector2Int RandomRoomSize()
    {
        // Rooms need at least 2x2 blocks so there is space for a doorway in every wall
        int blocksX = Mathf.Max(2, Random.Range(minRoomBlocks.x, maxRoomBlocks.x + 1));
        int blocksY = Mathf.Max(2, Random.Range(minRoomBlocks.y, maxRoomBlocks.y + 1));
        return new Vector2Int(blocksX * 2, blocksY * 2);
    }

    RectInt WithWalls(RectInt room)
    {
        return new RectInt(room.xMin - 1, room.yMin - 1, room.width + 2, room.height + 2);
    }

    // ---------- Tiles ----------

    void BuildFloor(RectInt room)
    {
        // With a style, the whole room uses one colour family; otherwise any block from the list
        FloorPalette palette = style != null && style.palettes != null && style.palettes.Length > 0
            ? style.palettes[Random.Range(0, style.palettes.Length)] : null;
        FloorBlock[] pool = palette != null ? palette.blocks : floorBlocks;

        // Two floors share the room in big noise patches, with the odd random block mixed in
        FloorBlock first = pool[Random.Range(0, pool.Length)];
        FloorBlock second = pool[Random.Range(0, pool.Length)];
        Vector2 noiseOffset = new Vector2(Random.value, Random.value) * 100f;

        for (int blockX = 0; blockX < room.width / 2; blockX++)
        {
            for (int blockY = 0; blockY < room.height / 2; blockY++)
            {
                float noise = Mathf.PerlinNoise(noiseOffset.x + blockX * patchScale, noiseOffset.y + blockY * patchScale);
                FloorBlock block = noise < 0.5f ? first : second;
                if (Random.value < accentChance) block = pool[Random.Range(0, pool.Length)];

                SetFloorBlock(room.xMin + blockX * 2, room.yMin + blockY * 2, block);
            }
        }

        if (palette != null && Random.value < style.stampChance) StampFloor(room, palette);
    }

    // One or two bigger floor pieces (a smooth slab, a plain ground patch) laid over the pattern
    void StampFloor(RectInt room, FloorPalette palette)
    {
        int count = Random.Range(1, 3);
        for (int i = 0; i < count; i++)
        {
            TileStamp stamp = Random.value < 0.5f ? palette.slab : palette.ground;
            if (stamp == null || stamp.Height == 0) continue;

            for (int attempt = 0; attempt < 10; attempt++)
            {
                // On the 2-cell grid so the stamp lines up with the floor blocks around it
                int x = room.xMin + 2 * Random.Range(0, Mathf.Max(1, (room.width - stamp.width) / 2 + 1));
                int y = room.yMin + 2 * Random.Range(0, Mathf.Max(1, (room.height - stamp.Height) / 2 + 1));
                var area = new RectInt(x, y, stamp.width, stamp.Height);
                if (area.xMax > room.xMax || area.yMax > room.yMax) continue;

                Paint(floorTilemap, stamp, x, y);
                break;
            }
        }
    }

    // Paints a stamp with its bottom-left cell at (xMin, yMin). Empty stamp cells are left alone
    void Paint(Tilemap map, TileStamp stamp, int xMin, int yMin)
    {
        for (int row = 0; row < stamp.Height; row++)
        {
            for (int x = 0; x < stamp.width; x++)
            {
                TileBase tile = stamp.At(x, row);
                if (tile != null) map.SetTile(new Vector3Int(xMin + x, yMin + stamp.Height - 1 - row, 0), tile);
            }
        }
    }

    FloorBlock RandomFloorBlock()
    {
        return floorBlocks[Random.Range(0, floorBlocks.Length)];
    }

    void SetFloorBlock(int x, int y, FloorBlock block)
    {
        floorTilemap.SetTile(new Vector3Int(x, y + 1, 0), block.topLeft);
        floorTilemap.SetTile(new Vector3Int(x + 1, y + 1, 0), block.topRight);
        floorTilemap.SetTile(new Vector3Int(x, y, 0), block.bottomLeft);
        floorTilemap.SetTile(new Vector3Int(x + 1, y, 0), block.bottomRight);
    }

    void BuildWalls(RectInt room)
    {
        // The frame sits one cell outside the floor on every side
        int left = room.xMin - 1;
        int right = room.xMax;
        int bottom = room.yMin - 1;
        int top = room.yMax;

        for (int x = room.xMin; x < room.xMax; x++)
        {
            wallTilemap.SetTile(new Vector3Int(x, top, 0), Repeat(topWall, x - room.xMin));
            wallTilemap.SetTile(new Vector3Int(x, bottom, 0), Repeat(bottomWall, x - room.xMin));
        }

        wallTilemap.SetTile(new Vector3Int(left, top - 1, 0), leftWallCap);
        wallTilemap.SetTile(new Vector3Int(right, top - 1, 0), rightWallCap);

        for (int y = top - 2; y > bottom; y--)
        {
            wallTilemap.SetTile(new Vector3Int(left, y, 0), Repeat(leftWall, top - 2 - y));
            wallTilemap.SetTile(new Vector3Int(right, y, 0), Repeat(rightWall, top - 2 - y));
        }

        wallTilemap.SetTile(new Vector3Int(left, top, 0), topLeftCorner);
        wallTilemap.SetTile(new Vector3Int(right, top, 0), topRightCorner);
        wallTilemap.SetTile(new Vector3Int(left, bottom, 0), bottomLeftCorner);
        wallTilemap.SetTile(new Vector3Int(right, bottom, 0), bottomRightCorner);
    }

    TileBase Repeat(TileBase[] tiles, int index)
    {
        return tiles[index % tiles.Length];
    }

    void CutDoor(Door door)
    {
        foreach (Vector2Int cell in door.cells.allPositionsWithin)
        {
            wallTilemap.SetTile(new Vector3Int(cell.x, cell.y, 0), null);
        }

        // Doorways use the plain block list; they sit between two rooms of possibly different colours
        SetFloorBlock(door.cells.xMin, door.cells.yMin, RandomFloorBlock());

        if (door.sideways)
        {
            // Finish the two wall columns under the doorway the same way they start under the top corners.
            // The left column is the right wall of the room on the left, and the other way around.
            wallTilemap.SetTile(new Vector3Int(door.cells.xMin, door.cells.yMin - 1, 0), rightWallCap);
            wallTilemap.SetTile(new Vector3Int(door.cells.xMin + 1, door.cells.yMin - 1, 0), leftWallCap);
        }
    }

    void PlaceProps(RectInt room, List<Door> doors, bool hasPlayer)
    {
        if (props.Length == 0) return;

        // Doorways and the spawn are already reserved in the occupancy, so a free spot is never on them
        int count = Random.Range(minProps, maxProps + 1);

        for (int i = 0; i < count; i++)
        {
            Prop prop = props[Random.Range(0, props.Length)];
            int width = prop.width;
            int height = prop.tiles.Length / width;

            int maxX = room.xMax - propSpacing - width;
            int maxY = room.yMax - propSpacing - height;
            int minX = room.xMin + propSpacing;
            int minY = room.yMin + propSpacing;
            if (maxX < minX || maxY < minY) continue;

            // Try a few random spots and take the first one that is far enough from everything else
            for (int attempt = 0; attempt < 20; attempt++)
            {
                RectInt spot = new RectInt(Random.Range(minX, maxX + 1), Random.Range(minY, maxY + 1), width, height);
                if (!Occupancy.IsFree(spot, propSpacing)) continue;

                for (int row = 0; row < height; row++)
                {
                    for (int column = 0; column < width; column++)
                    {
                        Vector3Int cell = new Vector3Int(spot.xMin + column, spot.yMax - 1 - row, 0);
                        wallTilemap.SetTile(cell, prop.tiles[row * width + column]);
                    }
                }

                Occupancy.Block(spot);
                break;
            }
        }
    }

    // ---------- Structures ----------

    Tilemap Details => detailTilemap != null ? detailTilemap : floorTilemap;

    // Bottom-left corners where a structure of 'size' fits inside the room with 'margin' free cells to every wall
    public static List<Vector2Int> StructureCandidates(RectInt room, Vector2Int size, int margin)
    {
        var anchors = new List<Vector2Int>();
        for (int x = room.xMin + margin; x + size.x <= room.xMax - margin; x++)
            for (int y = room.yMin + margin; y + size.y <= room.yMax - margin; y++)
                anchors.Add(new Vector2Int(x, y));
        return anchors;
    }

    // Left x positions where stairs 'width' wide fit along the room's top wall, one cell from the
    // side walls and not touching a doorway in the top wall ('doorXs' = each doorway's left cell, 2 wide)
    public static List<int> StairsCandidates(RectInt room, int width, IEnumerable<int> doorXs)
    {
        var xs = new List<int>();
        for (int x = room.xMin + 1; x + width <= room.xMax - 1; x++)
        {
            bool clear = true;
            foreach (int door in doorXs)
            {
                // the stairs grown by one cell on each side must miss the door's two cells
                if (x - 1 <= door + 1 && x + width >= door) clear = false;
            }
            if (clear) xs.Add(x);
        }
        return xs;
    }

    // At most one pit (blocks movement, painted on the walls layer) and one grate (walk-over, on the
    // floor layer) per room. A pit that would cut the level apart is taken back out again
    void PlaceStructures(RectInt room)
    {
        if (style == null) return;

        if (style.pits != null && style.pits.Length > 0 && Random.value < style.pitChance)
        {
            TileStamp pit = style.pits[Random.Range(0, style.pits.Length)];
            foreach (Vector2Int anchor in Shuffled(StructureCandidates(room, pit.Size, style.pitMargin), 10))
            {
                var area = new RectInt(anchor, pit.Size);
                if (!Occupancy.IsFree(area, 1)) continue;

                Paint(wallTilemap, pit, area.xMin, area.yMin);
                Occupancy.Block(area);
                if (StillConnected()) break;

                Erase(wallTilemap, pit, area.xMin, area.yMin);
                Occupancy.Unblock(area);
            }
        }

        if (style.grates != null && style.grates.Length > 0 && Random.value < style.grateChance)
        {
            TileStamp grate = style.grates[Random.Range(0, style.grates.Length)];
            foreach (Vector2Int anchor in Shuffled(StructureCandidates(room, grate.Size, 1), 10))
            {
                var area = new RectInt(anchor, grate.Size);
                if (!Occupancy.IsFree(area, 0)) continue;

                Paint(Details, grate, area.xMin, area.yMin);
                // Nothing else goes on a grate: spikes under it would be hidden but still hurt
                Occupancy.Reserve(area);
                break;
            }
        }
    }

    // The stairs-down archway against the top wall of the room farthest from the start. Its frame is
    // solid (walls layer), its steps are floor. Without a spot, HasStairsExit stays false and the game
    // loop falls back to its plain gate
    void BuildStairsExit()
    {
        HasStairsExit = false;
        ExitRoomIndex = -1;
        if (style == null || style.stairs == null || style.stairs.Height == 0 || rooms.Count < 2) return;

        int roomIndex = LevelGraph.FarthestRoom(rooms.Count, roomLinks, rooms, StartRoomIndex);
        RectInt room = rooms[roomIndex];
        TileStamp stairs = style.stairs;
        if (room.height < stairs.Height + 3) return;

        var topDoors = new List<int>();
        foreach (Door door in doorList)
        {
            if (!door.sideways && door.cells.yMin == room.yMax) topDoors.Add(door.cells.xMin);
        }

        int yMin = room.yMax - stairs.Height;
        foreach (int x in Shuffled(StairsCandidates(room, stairs.width, topDoors), 20))
        {
            var area = new RectInt(x, yMin, stairs.width, stairs.Height);
            if (!Occupancy.IsFree(area, 0)) continue;

            var walkable = new HashSet<Vector2Int>(style.stairsWalkable ?? new Vector2Int[0]);
            for (int row = 0; row < stairs.Height; row++)
            {
                for (int column = 0; column < stairs.width; column++)
                {
                    var cell = new Vector3Int(x + column, yMin + stairs.Height - 1 - row, 0);
                    TileBase tile = stairs.At(column, row);
                    if (walkable.Contains(new Vector2Int(column, row)))
                    {
                        // On the details layer, so the floor stays underneath the steps' see-through parts
                        Details.SetTile(cell, tile);
                    }
                    else
                    {
                        wallTilemap.SetTile(cell, tile);
                        Occupancy.Block(new RectInt(cell.x, cell.y, 1, 1));
                    }
                }
            }

            // The steps and the two rows in front of them stay clear
            Occupancy.Reserve(new RectInt(x, yMin - 2, stairs.width, stairs.Height + 2));
            exitFront = new Vector2Int(x + 1, yMin - 1);

            ExitCells = area;
            // World centre of the steps (the two middle columns, below the arch top)
            Vector3 low = floorTilemap.CellToWorld(new Vector3Int(x + 1, yMin, 0));
            Vector3 high = floorTilemap.CellToWorld(new Vector3Int(x + 3, yMin + stairs.Height - 1, 0));
            ExitPoint = (low + high) / 2f;
            ExitRoomIndex = roomIndex;
            HasStairsExit = true;
            return;
        }
    }

    void Erase(Tilemap map, TileStamp stamp, int xMin, int yMin)
    {
        for (int row = 0; row < stamp.Height; row++)
            for (int x = 0; x < stamp.width; x++)
                if (stamp.At(x, row) != null) map.SetTile(new Vector3Int(xMin + x, yMin + stamp.Height - 1 - row, 0), null);
    }

    // Up to 'count' entries in random order
    static List<T> Shuffled<T>(List<T> items, int count)
    {
        for (int i = items.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (items[i], items[j]) = (items[j], items[i]);
        }
        if (items.Count > count) items.RemoveRange(count, items.Count - count);
        return items;
    }

    // ---------- Decorations ----------

    // The theme each room got in the last build ("" for none), for the Test Menu
    public string RoomThemeAt(int roomIndex)
    {
        return roomIndex >= 0 && roomIndex < roomThemeNames.Count ? roomThemeNames[roomIndex] : "";
    }

    // Gives the room a theme and places its decorations, plus torches on the top wall.
    // Returns whether the room became a trap room (so the next room isn't one too)
    bool Decorate(int roomIndex, bool previousWasTrap)
    {
        if (style == null || !decorate || style.themes == null)
        {
            roomThemeNames.Add("");
            return false;
        }

        RectInt room = rooms[roomIndex];
        var rng = new System.Random(Random.Range(int.MinValue, int.MaxValue));
        RoomTheme theme = RoomTheme.Pick(style.themes, roomIndex == StartRoomIndex, roomIndex == ExitRoomIndex, previousWasTrap, style.startTheme, rng);
        roomThemeNames.Add(theme != null ? theme.themeName : "");

        if (theme != null && theme.entries != null)
        {
            foreach (RoomTheme.Entry entry in theme.entries)
            {
                if (entry.decoration == null) continue;
                int count = Mathf.RoundToInt(Random.Range(entry.min, entry.max + 1) * decorationDensity);
                for (int i = 0; i < count; i++) PlaceDecoration(entry.decoration, room);
            }
        }

        if (style.torch != null && decorationDensity > 0f)
        {
            int torches = Random.Range(style.torchesPerRoom.x, style.torchesPerRoom.y + 1);
            for (int i = 0; i < torches; i++) PlaceDecoration(style.torch, room);
        }

        return theme != null && theme.isTrap;
    }

    void PlaceDecoration(Decoration decoration, RectInt room)
    {
        GameObject prefab = decoration.PickPrefab();
        if (prefab == null) return;

        foreach (RectInt area in Shuffled(DecorationPlacer.Candidates(decoration.rule, room, decoration.footprint), 20))
        {
            if (decoration.rule == Placement.OnTopWall)
            {
                // Only on plain wall: every cell under it and beside it is wall, so never in a doorway gap
                if (!WallRow(area.xMin - 1, area.xMax, area.yMin) || !Occupancy.IsFree(area, 1)) continue;
            }
            else if (!Occupancy.IsFree(area, decoration.blocks ? 1 : 0) || HasWallTile(area))
            {
                continue;
            }

            if (decoration.blocks)
            {
                Occupancy.Block(area);
                if (!StillConnected())
                {
                    Occupancy.Unblock(area);
                    continue;
                }
            }
            else if (decoration.hazard)
            {
                Occupancy.Hazard(area);
            }
            else
            {
                Occupancy.Reserve(area);
            }

            // Prefabs have their pivot at the bottom centre of their footprint
            Vector3 bottomLeft = floorTilemap.CellToWorld(new Vector3Int(area.xMin, area.yMin, 0));
            Vector3 spot = bottomLeft + new Vector3(area.width / 2f, 0f, 0f) * floorTilemap.layoutGrid.cellSize.x;
            if (player != null) spot.z = player.position.z;
            Instantiate(prefab, spot, Quaternion.identity, decorationsParent);
            return;
        }
    }

    bool HasWallTile(RectInt area)
    {
        foreach (Vector2Int cell in area.allPositionsWithin)
            if (wallTilemap.HasTile(new Vector3Int(cell.x, cell.y, 0))) return true;
        return false;
    }

    bool WallRow(int xFrom, int xTo, int y)
    {
        for (int x = xFrom; x <= xTo; x++)
            if (!wallTilemap.HasTile(new Vector3Int(x, y, 0))) return false;
        return true;
    }

    // ---------- Room objects ----------

    void ClearRoomObjects()
    {
        if (decorationsParent == null)
        {
            Transform existing = transform.Find("Decorations");
            decorationsParent = existing != null ? existing : new GameObject("Decorations").transform;
            decorationsParent.SetParent(transform, false);
        }
        for (int i = decorationsParent.childCount - 1; i >= 0; i--)
        {
            GameObject decoration = decorationsParent.GetChild(i).gameObject;
            if (Application.isPlaying) Destroy(decoration);
            else DestroyImmediate(decoration);
        }

        for (int i = roomsParent.childCount - 1; i >= 0; i--)
        {
            GameObject roomObject = roomsParent.GetChild(i).gameObject;

            // Destroy only works while the game is running
            if (Application.isPlaying) Destroy(roomObject);
            else DestroyImmediate(roomObject);
        }
    }

    // Same setup as the rooms in SampleScene: a trigger box that RoomManager checks the player against
    // and a black sprite that hides the room while the player is somewhere else
    void CreateRoomObject(RectInt room, int index)
    {
        RectInt area = WithWalls(room);
        Vector3 min = floorTilemap.CellToWorld(new Vector3Int(area.xMin, area.yMin, 0));
        Vector3 max = floorTilemap.CellToWorld(new Vector3Int(area.xMax, area.yMax, 0));
        Vector2 spriteSize = darknessSprite.bounds.size;

        GameObject roomObject = new GameObject("Room " + (index + 1));
        roomObject.transform.SetParent(roomsParent);
        roomObject.transform.position = (min + max) / 2f;
        roomObject.transform.localScale = new Vector3((max.x - min.x) / spriteSize.x, (max.y - min.y) / spriteSize.y, 1f);

        SpriteRenderer overlay = roomObject.AddComponent<SpriteRenderer>();
        overlay.sprite = darknessSprite;
        overlay.color = darknessColor;
        overlay.sortingOrder = darknessSortingOrder;

        // The room the player starts in is lit straight away. Outside of play mode nothing is hidden,
        // so the whole level can be seen in the editor.
        overlay.enabled = Application.isPlaying && index != 0;

        BoxCollider2D box = roomObject.AddComponent<BoxCollider2D>();
        box.isTrigger = true;
        box.size = spriteSize;
    }

    void MovePlayerToCenter()
    {
        if (player == null) return;

        Vector3 center = floorTilemap.CellToWorld(Vector3Int.zero);
        center.z = player.position.z;
        player.position = center;

        Rigidbody2D rb = player.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.position = center;
            rb.linearVelocity = Vector2.zero;
        }
    }
}
