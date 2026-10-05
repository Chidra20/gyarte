using UnityEngine;

// Something the player can pick on the ability choice screen. Each ability is a small subclass
// with one asset; picking it again stacks it, up to maxStacks.
public abstract class Ability : ScriptableObject
{
    [Header("Shown on the choice card")]
    public string displayName;
    [TextArea] public string description;
    [Tooltip("How many times this can be picked in one run.")]
    public int maxStacks = 3;

    // Changes the player. Called once per pick
    public abstract void Apply(GameObject player);
}
