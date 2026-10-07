using System;
using UnityEngine;

// What kind of room this is (crypt, storeroom, shrine, trap room...) and so which decorations it gets
[CreateAssetMenu(menuName = "gyarte/Room Theme")]
public class RoomTheme : ScriptableObject
{
    [Serializable]
    public class Entry
    {
        public Decoration decoration;
        public int min = 1;
        public int max = 2;
    }

    public string themeName;
    [Tooltip("Rooms with a trap theme are never the exit room and never two in a row.")]
    public bool isTrap;
    public Entry[] entries;

    // The theme for one room: the start room always gets the start theme; trap themes are left out
    // for the exit room and right after another trap room
    public static RoomTheme Pick(System.Collections.Generic.IList<RoomTheme> themes, bool isStart, bool isExit, bool previousWasTrap,
        RoomTheme startTheme, System.Random rng)
    {
        if (isStart && startTheme != null) return startTheme;

        var allowed = new System.Collections.Generic.List<RoomTheme>();
        foreach (RoomTheme theme in themes)
        {
            if (theme == null) continue;
            if (theme.isTrap && (isExit || previousWasTrap)) continue;
            allowed.Add(theme);
        }
        return allowed.Count > 0 ? allowed[rng.Next(allowed.Count)] : null;
    }
}
