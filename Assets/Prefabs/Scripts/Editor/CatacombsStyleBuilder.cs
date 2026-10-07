using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

// Builds (or updates) the Catacombs Level Style from the tile numbers that groups.py exported to
// Tools/catacombs-catalog/style-tiles.txt, so no tile is ever picked by hand in C#. Safe to run again.
public static class CatacombsStyleBuilder
{
    public const string TileList = "Tools/catacombs-catalog/style-tiles.txt";
    public const string StylePath = "Assets/Prefabs/Level/Catacombs Style.asset";
    const string TileFolder = "Assets/Art/Pallates/";

    static readonly string[] ColourNames = { "Brown", "Dark Brown", "Teal", "Dark Teal", "Olive", "Green" };

    [MenuItem("Tools/Catacombs/Build Level Style")]
    public static void Build()
    {
        Dictionary<string, TileStamp> stamps = ReadStamps();
        if (stamps == null) return;

        EnsureFolder("Assets/Prefabs/Level");
        LevelStyle style = AssetDatabase.LoadAssetAtPath<LevelStyle>(StylePath);
        if (style == null)
        {
            style = ScriptableObject.CreateInstance<LevelStyle>();
            AssetDatabase.CreateAsset(style, StylePath);
        }

        // Six colour families; each gets its three 2x2 patterns, its slab, and the ground patch of its colour pair
        var palettes = new List<FloorPalette>();
        for (int k = 0; k < ColourNames.Length; k++)
        {
            palettes.Add(new FloorPalette
            {
                name = ColourNames[k],
                blocks = new[] { Block(stamps["floor." + k + ".tileA"]), Block(stamps["floor." + k + ".tileB"]), Block(stamps["floor." + k + ".rough"]) },
                slab = stamps["slab." + k],
                ground = stamps["ground." + (k / 2)],
            });
        }
        style.palettes = palettes.ToArray();
        style.pits = new[] { stamps["pit.square"], stamps["pit.round"] };
        style.grates = new[] { stamps["grate.large"], stamps["grate.small"] };
        style.stairs = stamps["stairs"];
        style.bars = stamps["bars"];

        // In the 4x6 stairs stamp the two middle columns below the arch top are the steps you walk on
        var walkable = new List<Vector2Int>();
        for (int row = 1; row < style.stairs.Height; row++)
        {
            walkable.Add(new Vector2Int(1, row));
            walkable.Add(new Vector2Int(2, row));
        }
        style.stairsWalkable = walkable.ToArray();

        BuildStairsGate(style);
        CatacombsDecorBuilder.Build(style);

        EditorUtility.SetDirty(style);
        AssetDatabase.SaveAssets();
        Debug.Log("Catacombs style built: " + palettes.Count + " floor colours, " + style.pits.Length + " pits, " + style.grates.Length + " grates, "
            + (style.themes != null ? style.themes.Length : 0) + " room themes.");
    }

    public const string StairsGatePath = "Assets/Prefabs/Stairs Gate.prefab";

    // The exit gate for the stairs: placed at the centre of the steps (2 wide, 5 tall). While closed,
    // the bars are drawn over the archway opening and a solid collider keeps the player off the steps.
    // The trigger reaches one row below the steps so walking up to the bars counts as touching the gate
    static void BuildStairsGate(LevelStyle style)
    {
        var root = new GameObject("Stairs Gate");
        root.AddComponent<Grid>();
        var trigger = root.AddComponent<BoxCollider2D>();
        trigger.isTrigger = true;
        trigger.size = new Vector2(2f, 6f);
        trigger.offset = new Vector2(0f, -0.5f);

        var bars = new GameObject("Bars");
        bars.transform.SetParent(root.transform, false);
        // The bars stamp is 4x5 and covers the archway's rows below the arch top; the root sits at the
        // centre of the 2x5 steps, so the stamp's bottom-left cell is 2 left and 2.5 down from it
        bars.transform.localPosition = new Vector3(-2f, -2.5f, 0f);
        bars.tag = "wall";
        var map = bars.AddComponent<Tilemap>();
        var render = bars.AddComponent<TilemapRenderer>();
        render.sortingOrder = 1;
        for (int row = 0; row < style.bars.Height; row++)
            for (int x = 0; x < style.bars.width; x++)
                map.SetTile(new Vector3Int(x, style.bars.Height - 1 - row, 0), style.bars.At(x, row));
        var solid = bars.AddComponent<BoxCollider2D>();
        solid.size = new Vector2(2f, 5f);
        solid.offset = new Vector2(2f, 2.5f);

        var gate = root.AddComponent<Gate>();
        gate.closedLook = bars;
        // Opening the bars isn't enough; the level ends when the player walks onto the steps
        gate.enterAreaSize = new Vector2(2f, 5f);

        PrefabUtility.SaveAsPrefabAsset(root, StairsGatePath);
        Object.DestroyImmediate(root);
    }

    static LevelRandomizer.FloorBlock Block(TileStamp stamp)
    {
        return new LevelRandomizer.FloorBlock
        {
            topLeft = stamp.At(0, 0),
            topRight = stamp.At(1, 0),
            bottomLeft = stamp.At(0, 1),
            bottomRight = stamp.At(1, 1),
        };
    }

    static Dictionary<string, TileStamp> ReadStamps()
    {
        if (!File.Exists(TileList))
        {
            Debug.LogError("Catacombs style: " + TileList + " not found. Run groups.py in Tools/catacombs-catalog first.");
            return null;
        }

        var stamps = new Dictionary<string, TileStamp>();
        foreach (string line in File.ReadAllLines(TileList))
        {
            string[] parts = line.Split('|');
            if (parts.Length != 3) continue;

            string[] numbers = parts[2].Split(',');
            var tiles = new TileBase[numbers.Length];
            for (int i = 0; i < numbers.Length; i++)
            {
                int n = int.Parse(numbers[i]);
                if (n >= 0) tiles[i] = AssetDatabase.LoadAssetAtPath<TileBase>(TileFolder + "mainlevbuild_" + n + ".asset");
            }
            stamps[parts[0]] = new TileStamp { width = int.Parse(parts[1]), tiles = tiles };
        }
        return stamps;
    }

    public static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        string parent = Path.GetDirectoryName(path).Replace('\\', '/');
        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
    }
}
