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

    [Header("Player")]
    [Tooltip("Moved back to the middle of the first room every time the level is randomized.")]
    public Transform player;

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
    public Vector2Int minRoomBlocks = new Vector2Int(4, 3);
    public Vector2Int maxRoomBlocks = new Vector2Int(9, 6);

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

    void Start()
    {
        // Keep the level that was saved with the scene; only build one if there is nothing there
        if (floorTilemap.GetUsedTilesCount() == 0) Randomize();
    }

    public void Randomize()
    {
        floorTilemap.ClearAllTiles();
        wallTilemap.ClearAllTiles();
        ClearRoomObjects();

        List<Door> doors = new List<Door>();
        List<RectInt> rooms = LayOutRooms(doors);

        foreach (RectInt room in rooms)
        {
            BuildFloor(room);
            BuildWalls(room);
        }

        foreach (Door door in doors) CutDoor(door);

        for (int i = 0; i < rooms.Count; i++)
        {
            PlaceProps(rooms[i], doors, i == 0);
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
            RectInt from = rooms[Random.Range(0, rooms.Count)];
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
        // Two floors share the room in big noise patches, with the odd random block mixed in
        FloorBlock first = RandomFloorBlock();
        FloorBlock second = RandomFloorBlock();
        Vector2 noiseOffset = new Vector2(Random.value, Random.value) * 100f;

        for (int blockX = 0; blockX < room.width / 2; blockX++)
        {
            for (int blockY = 0; blockY < room.height / 2; blockY++)
            {
                float noise = Mathf.PerlinNoise(noiseOffset.x + blockX * patchScale, noiseOffset.y + blockY * patchScale);
                FloorBlock block = noise < 0.5f ? first : second;
                if (Random.value < accentChance) block = RandomFloorBlock();

                SetFloorBlock(room.xMin + blockX * 2, room.yMin + blockY * 2, block);
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

        // Nothing may land on the player's spawn point or in front of a doorway
        List<RectInt> taken = new List<RectInt>();
        if (hasPlayer) taken.Add(new RectInt(-1, -1, 2, 2));
        foreach (Door door in doors) taken.Add(door.cells);

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
                RectInt padded = new RectInt(spot.xMin - propSpacing, spot.yMin - propSpacing, width + propSpacing * 2, height + propSpacing * 2);

                if (taken.Exists(other => other.Overlaps(padded))) continue;

                for (int row = 0; row < height; row++)
                {
                    for (int column = 0; column < width; column++)
                    {
                        Vector3Int cell = new Vector3Int(spot.xMin + column, spot.yMax - 1 - row, 0);
                        wallTilemap.SetTile(cell, prop.tiles[row * width + column]);
                    }
                }

                taken.Add(spot);
                break;
            }
        }
    }

    // ---------- Room objects ----------

    void ClearRoomObjects()
    {
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
