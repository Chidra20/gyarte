using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

// Gives every Catacombs tile asset the labels that say what it is (Floor, WallFace, Door, Pit...).
// The list comes from Tools/catacombs-catalog/tile-labels.txt, which groups.py writes from its
// hand-made map of the sprite sheet. Existing labels are kept; ours are added.
// Search the Project window with l:Floor, l:Door, l:Floor-TileA ... to find tiles by what they are.
public static class CatacombsLabels
{
    const string ListPath = "Tools/catacombs-catalog/tile-labels.txt";
    const string TileFolder = "Assets/Art/Pallates/";

    [MenuItem("Tools/Catacombs/Apply Tile Labels")]
    public static void Apply()
    {
        if (!File.Exists(ListPath))
        {
            Debug.LogError("Catacombs labels: " + ListPath + " not found. Run groups.py in Tools/catacombs-catalog first.");
            return;
        }

        int done = 0, missing = 0;
        AssetDatabase.StartAssetEditing();
        try
        {
            foreach (string line in File.ReadAllLines(ListPath))
            {
                string[] parts = line.Split('|');
                if (parts.Length != 2) continue;

                Object tile = AssetDatabase.LoadMainAssetAtPath(TileFolder + "mainlevbuild_" + parts[0] + ".asset");
                if (tile == null)
                {
                    missing++;
                    continue;
                }

                var labels = new List<string>(AssetDatabase.GetLabels(tile));
                foreach (string label in parts[1].Split(','))
                {
                    if (!labels.Contains(label)) labels.Add(label);
                }
                AssetDatabase.SetLabels(tile, labels.ToArray());
                done++;
            }
        }
        finally
        {
            AssetDatabase.StopAssetEditing();
        }
        AssetDatabase.SaveAssets();
        Debug.Log("Catacombs labels: labelled " + done + " tiles, " + missing + " not found.");
    }
}
