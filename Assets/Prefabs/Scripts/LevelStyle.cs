using System;
using UnityEngine;
using UnityEngine.Tilemaps;

// A small picture made of tiles: 'width' tiles per row, rows listed from the top. Empty entries leave
// that cell alone, so odd shapes (a round well) can be stamped without painting their corners.
[Serializable]
public class TileStamp
{
    public int width;
    public TileBase[] tiles;

    public int Height => width > 0 && tiles != null ? tiles.Length / width : 0;
    public Vector2Int Size => new Vector2Int(width, Height);

    // x from the left, rowFromTop from the top
    public TileBase At(int x, int rowFromTop)
    {
        if (tiles == null || width <= 0) return null;
        int i = rowFromTop * width + x;
        return i >= 0 && i < tiles.Length ? tiles[i] : null;
    }
}

// One colour family of floor: three 2x2 patterns mixed by noise, plus the bigger stamps in the same colour
[Serializable]
public class FloorPalette
{
    public string name;
    public LevelRandomizer.FloorBlock[] blocks;
    [Tooltip("A 2x3 smooth slab stamped on top of the floor now and then.")]
    public TileStamp slab;
    [Tooltip("A 4x4 plain ground patch stamped on top of the floor now and then.")]
    public TileStamp ground;
}

// The art of one tileset for the level randomizer: floors, pits, grates, the stairs exit, decorations.
// One per art pack; built for the Catacombs pack by Tools > Catacombs > Build Level Style.
[CreateAssetMenu(menuName = "gyarte/Level Style")]
public class LevelStyle : ScriptableObject
{
    [Header("Floors")]
    [Tooltip("Each room picks one of these colour families.")]
    public FloorPalette[] palettes;
    [Range(0f, 1f)] [Tooltip("Chance that a room gets one or two slab or ground stamps.")]
    public float stampChance = 0.5f;

    [Header("Pits (block movement)")]
    public TileStamp[] pits;
    [Range(0f, 1f)] public float pitChance = 0.45f;
    [Tooltip("Cells kept clear between a pit and the room's walls.")]
    public int pitMargin = 3;

    [Header("Grates (walk over)")]
    public TileStamp[] grates;
    [Range(0f, 1f)] public float grateChance = 0.35f;

    [Header("Stairs exit")]
    [Tooltip("The stairs-down archway, built against the top wall of the farthest room.")]
    public TileStamp stairs;
    [Tooltip("Cells of the stairs stamp (x from left, row from top) that are walkable steps; the rest is solid frame.")]
    public Vector2Int[] stairsWalkable;
    [Tooltip("The barred gate drawn over the stairs while the exit is closed.")]
    public TileStamp bars;

    [Header("Decorations")]
    public RoomTheme startTheme;
    public RoomTheme[] themes;
    public Decoration torch;
    public Vector2Int torchesPerRoom = new Vector2Int(1, 3);
}
