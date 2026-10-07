using UnityEngine;

// Where in a room a decoration may go
public enum Placement
{
    AgainstTopWall,   // on the top floor row, back against the wall (coffins, statues, hanging things)
    Corner,           // in one of the four corners (urns)
    AlongWall,        // touching any wall (crates, urns)
    Anywhere,         // anywhere on the floor away from the walls (candles)
    Centre,           // in the middle part of the room (spike fields)
    OnTopWall         // on the top wall itself (torches)
}

// One kind of decoration the level randomizer can place: which prefab, where it may go,
// how many floor cells it takes, and whether it blocks or hurts.
[CreateAssetMenu(menuName = "gyarte/Decoration")]
public class Decoration : ScriptableObject
{
    public GameObject prefab;
    [Tooltip("Optional look-alikes picked at random instead of Prefab (e.g. the five brown urns).")]
    public GameObject[] variants;
    public Placement rule = Placement.Anywhere;
    [Tooltip("Floor cells it stands on (tall things only take their base).")]
    public Vector2Int footprint = Vector2Int.one;
    [Tooltip("Blocks movement: its cells are solid and the level must stay walkable around it.")]
    public bool blocks;
    [Tooltip("Hurts whoever stands on it (spikes). Nothing else is placed on its cells.")]
    public bool hazard;

    public GameObject PickPrefab()
    {
        if (variants != null && variants.Length > 0) return variants[Random.Range(0, variants.Length)];
        return prefab;
    }
}
