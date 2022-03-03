using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(World))]
public class WorldEditor : Editor
{
    public override void OnInspectorGUI()
    {
        World world = (World)target;

        if (GUILayout.Button("Generate")) { world.GenerateWorld(); }

        base.OnInspectorGUI();
    }
}
