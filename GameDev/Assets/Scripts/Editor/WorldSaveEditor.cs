using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(WorldSave))]
public class WorldSaveEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        WorldSave worldSave = (WorldSave)target;

        if (GUILayout.Button("Save World")) { worldSave.SaveWorld(worldSave.world, worldSave.WorldName); }
        if (GUILayout.Button("Load World")) { worldSave.LoadWorld(worldSave.WorldName); }
    }
}
