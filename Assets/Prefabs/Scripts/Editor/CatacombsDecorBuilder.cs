using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

// Builds the Catacombs decorations: one prefab per prop sprite (numbers from the Catacombs Asset Catalog),
// the animated torch, candles and spike fields, the Decoration assets that say where each may go,
// and the room themes. Run through Tools > Catacombs > Build Level Style. Safe to run again.
public static class CatacombsDecorBuilder
{
    const string PackDir = "Assets/Art/RF_Catacombs_v1.0/";
    const string PrefabDir = "Assets/Prefabs/Level/Decor";
    const string DecorationDir = "Assets/Prefabs/Level/Decorations";
    const string ThemeDir = "Assets/Prefabs/Level/Themes";
    const string GlowSprite = "Assets/Art/Placeholders/SoftGlow.png";

    static readonly Color FlameGlow = new Color(1f, 0.6f, 0.25f, 0.35f);

    static Dictionary<int, Sprite> props;

    public static void Build(LevelStyle style)
    {
        CatacombsStyleBuilder.EnsureFolder(PrefabDir);
        CatacombsStyleBuilder.EnsureFolder(DecorationDir);
        CatacombsStyleBuilder.EnsureFolder(ThemeDir);

        props = new Dictionary<int, Sprite>();
        foreach (Object o in AssetDatabase.LoadAllAssetsAtPath(PackDir + "decorative.png"))
        {
            if (o is Sprite s) props[int.Parse(s.name.Substring(s.name.LastIndexOf('_') + 1))] = s;
        }

        // ---- Decorations: which sprites, where they go, footprint, blocking (sprite numbers from the catalogue) ----
        Decoration skullPost = Deco("Skull Post", Placement.Anywhere, 1, 1, true, Props("Skull Post", 0));
        Decoration brazierPost = Deco("Brazier Post", Placement.Anywhere, 1, 1, true, Props("Brazier Post", 1));
        Decoration posts = Deco("Wooden Posts", Placement.AlongWall, 1, 1, true, Props("Post", 3, 4, 14));
        Decoration skullOrnament = Deco("Skull Ornament", Placement.OnTopWall, 1, 1, false, Props("Skull Ornament", 2));
        Decoration hangingBasin = Deco("Hanging Basin", Placement.AgainstTopWall, 1, 1, false, Props("Hanging Basin", 5, 6));
        Decoration hangingChains = Deco("Hanging Chains", Placement.AgainstTopWall, 1, 1, false, Props("Hanging Chains", 7, 8, 9));
        Decoration candleStubs = Deco("Candle Stubs", Placement.Anywhere, 1, 1, false, Props("Candle Stubs", 10, 11, 12, 13));
        Decoration sarcophagus = Deco("Sarcophagus", Placement.Anywhere, 3, 2, true, Props("Sarcophagus", 24, 43));
        Decoration brokenSarcophagus = Deco("Broken Sarcophagus", Placement.AlongWall, 3, 2, true, Props("Broken Sarcophagus", 15, 29, 37, 49));
        Decoration uprightSarcophagus = Deco("Upright Sarcophagus", Placement.AgainstTopWall, 2, 1, true, Props("Upright Sarcophagus", 16, 17, 25));
        Decoration coffin = Deco("Wooden Coffin", Placement.AgainstTopWall, 1, 1, true, Props("Coffin", 18, 19, 20, 21, 22, 23));
        Decoration bigUrn = Deco("Large Urn", Placement.Corner, 1, 1, true, Props("Large Urn", 26, 38));
        Decoration urns = Deco("Urns", Placement.AlongWall, 1, 1, false, Props("Urn", 27, 28, 30, 31, 39, 40, 41, 42));
        Decoration brokenUrns = Deco("Broken Urns", Placement.Anywhere, 1, 1, false, Props("Broken Urn", 32, 33, 34, 35, 36, 44, 45, 46, 47, 48));

        // Torch glow is kept small and low: torches hang on the top wall, and a big glow would show
        // through the wall in the room behind it
        Decoration torch = Deco("Torch", Placement.OnTopWall, 1, 1, false, new[] { Animated("Torch", "torch_", 1, 4, 8f, 2.2f, 1, -0.35f) });
        Decoration candleA = Deco("Candle", Placement.Anywhere, 1, 1, false, new[] { Animated("Candle A", "candleA_0", 1, 4, 7f, 1.6f, 0) });
        Decoration candleB = Deco("Candle Pair", Placement.AlongWall, 1, 1, false, new[] { Animated("Candle B", "candleB_0", 1, 4, 7f, 1.8f, 0) });
        Decoration spikes2 = Deco("Spikes 2x2", Placement.Centre, 2, 2, false, new[] { Spikes(2, 2) }, hazard: true);
        Decoration spikes3 = Deco("Spikes 3x2", Placement.Centre, 3, 2, false, new[] { Spikes(3, 2) }, hazard: true);

        // ---- Room themes ----
        style.startTheme = Theme("Start", false, E(candleA, 1, 2), E(urns, 1, 2));
        style.themes = new[]
        {
            Theme("Crypt", false, E(sarcophagus, 1, 2), E(brokenSarcophagus, 0, 1), E(uprightSarcophagus, 1, 2), E(coffin, 1, 2),
                E(candleB, 1, 2), E(candleStubs, 1, 3)),
            Theme("Storeroom", false, E(bigUrn, 1, 3), E(urns, 3, 5), E(brokenUrns, 1, 3), E(coffin, 0, 1), E(posts, 0, 1)),
            Theme("Shrine", false, E(skullPost, 1, 2), E(brazierPost, 1, 2), E(hangingBasin, 1, 2), E(hangingChains, 1, 3),
                E(skullOrnament, 0, 1), E(candleA, 2, 3), E(candleStubs, 1, 2)),
            Theme("Trap Room", true, E(spikes2, 1, 2), E(spikes3, 0, 1), E(brokenUrns, 1, 2), E(candleStubs, 0, 1)),
        };
        style.torch = torch;
        EditorUtility.SetDirty(style);
    }

    static RoomTheme.Entry E(Decoration d, int min, int max)
    {
        return new RoomTheme.Entry { decoration = d, min = min, max = max };
    }

    static RoomTheme Theme(string name, bool trap, params RoomTheme.Entry[] entries)
    {
        string path = ThemeDir + "/" + name + ".asset";
        RoomTheme theme = AssetDatabase.LoadAssetAtPath<RoomTheme>(path);
        if (theme == null)
        {
            theme = ScriptableObject.CreateInstance<RoomTheme>();
            AssetDatabase.CreateAsset(theme, path);
        }
        theme.themeName = name;
        theme.isTrap = trap;
        theme.entries = entries;
        EditorUtility.SetDirty(theme);
        return theme;
    }

    static Decoration Deco(string name, Placement rule, int w, int h, bool blocks, GameObject[] prefabs, bool hazard = false)
    {
        string path = DecorationDir + "/" + name + ".asset";
        Decoration d = AssetDatabase.LoadAssetAtPath<Decoration>(path);
        if (d == null)
        {
            d = ScriptableObject.CreateInstance<Decoration>();
            AssetDatabase.CreateAsset(d, path);
        }
        d.rule = rule;
        d.footprint = new Vector2Int(w, h);
        d.blocks = blocks;
        d.hazard = hazard;
        d.prefab = prefabs.Length > 0 ? prefabs[0] : null;
        d.variants = prefabs.Length > 1 ? prefabs : new GameObject[0];

        // Blocking props get a solid box over their footprint, tagged wall like every other obstacle
        if (blocks)
        {
            foreach (GameObject prefab in prefabs) AddBlocker(prefab, w, h);
        }
        EditorUtility.SetDirty(d);
        return d;
    }

    // One prefab per sprite number: a root at the bottom centre of the footprint (sorting point),
    // the sprite on a child lifted so its bottom edge sits on the root
    static GameObject[] Props(string baseName, params int[] numbers)
    {
        var list = new List<GameObject>();
        foreach (int n in numbers)
        {
            if (!props.TryGetValue(n, out Sprite sprite)) continue;
            var root = new GameObject(baseName + " " + n);
            root.AddComponent<SortingGroup>().sortingOrder = 0;
            AddSprite(root.transform, sprite, 0);
            list.Add(Save(root, baseName + " " + n));
        }
        return list.ToArray();
    }

    static SpriteRenderer AddSprite(Transform parent, Sprite sprite, int order)
    {
        var child = new GameObject("Sprite");
        child.transform.SetParent(parent, false);
        child.transform.localPosition = new Vector3(0f, sprite.bounds.extents.y - sprite.bounds.center.y, 0f);
        var sr = child.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingOrder = order;
        return sr;
    }

    // Torch and candles: flame frames on a FrameAnimator plus a flickering glow behind them.
    // Torches sit on the wall row and draw above the wall (order 1)
    static GameObject Animated(string name, string filePrefix, int first, int last, float fps, float glowSize, int groupOrder, float glowShift = 0f)
    {
        var frames = new List<Sprite>();
        for (int i = first; i <= last; i++)
        {
            Sprite s = AssetDatabase.LoadAssetAtPath<Sprite>(PackDir + filePrefix + i + ".png");
            if (s != null) frames.Add(s);
        }

        var root = new GameObject(name);
        root.AddComponent<SortingGroup>().sortingOrder = groupOrder;

        var glow = new GameObject("Glow");
        glow.transform.SetParent(root.transform, false);
        glow.transform.localPosition = new Vector3(0f, (frames.Count > 0 ? frames[0].bounds.size.y * 0.7f : 0.5f) + glowShift, 0f);
        glow.transform.localScale = Vector3.one * glowSize;
        var glowRenderer = glow.AddComponent<SpriteRenderer>();
        glowRenderer.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(GlowSprite);
        glowRenderer.color = FlameGlow;
        glowRenderer.sortingOrder = -1;
        glow.AddComponent<GlowFlicker>().glow = glowRenderer;

        if (frames.Count > 0)
        {
            SpriteRenderer flame = AddSprite(root.transform, frames[0], 0);
            var animator = flame.gameObject.AddComponent<FrameAnimator>();
            animator.target = flame;
            animator.frames = frames.ToArray();
            animator.framesPerSecond = fps;
        }
        return Save(root, name);
    }

    // A w x h field of spikes sharing one SpikeTrap; lies flat on the floor under the characters
    static GameObject Spikes(int w, int h)
    {
        var frames = new List<Sprite>();
        for (int i = 0; i <= 4; i++)
        {
            Sprite s = AssetDatabase.LoadAssetAtPath<Sprite>(PackDir + "spike_" + i + ".png");
            if (s != null) frames.Add(s);
        }

        string name = "Spikes " + w + "x" + h;
        var root = new GameObject(name);
        root.AddComponent<SortingGroup>().sortingOrder = -1;
        var spikes = new List<SpriteRenderer>();
        for (int x = 0; x < w; x++)
        {
            for (int y = 0; y < h; y++)
            {
                var cell = new GameObject("Spike");
                cell.transform.SetParent(root.transform, false);
                cell.transform.localPosition = new Vector3(x - (w - 1) / 2f, y + 0.5f, 0f);
                var sr = cell.AddComponent<SpriteRenderer>();
                sr.sprite = frames.Count > 0 ? frames[0] : null;
                spikes.Add(sr);
            }
        }
        var trap = root.AddComponent<SpikeTrap>();
        trap.frames = frames.ToArray();
        trap.spikes = spikes.ToArray();
        trap.areaSize = new Vector2(w, h);
        trap.areaOffset = new Vector2(0f, h / 2f);
        return Save(root, name);
    }

    static void AddBlocker(GameObject prefab, int w, int h)
    {
        string path = AssetDatabase.GetAssetPath(prefab);
        GameObject root = PrefabUtility.LoadPrefabContents(path);
        if (root.GetComponent<BoxCollider2D>() == null)
        {
            var box = root.AddComponent<BoxCollider2D>();
            box.size = new Vector2(w * 0.9f, h * 0.9f);
            box.offset = new Vector2(0f, h / 2f);
        }
        root.tag = "wall";
        PrefabUtility.SaveAsPrefabAsset(root, path);
        PrefabUtility.UnloadPrefabContents(root);
    }

    static GameObject Save(GameObject root, string name)
    {
        string path = PrefabDir + "/" + name + ".prefab";
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);
        return prefab;
    }
}
