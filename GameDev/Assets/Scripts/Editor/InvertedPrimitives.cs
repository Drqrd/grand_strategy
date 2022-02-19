using UnityEngine;
using UnityEditor;
using System.Linq;

public class InvertedPrimitives : Editor
{
    private static void InvertPrimitive(PrimitiveType primitive)
    {
        GameObject temp = GameObject.CreatePrimitive(primitive);
        MeshFilter tmf = temp.GetComponent<MeshFilter>();

        GameObject obj = new GameObject("Inverted " + primitive.ToString());
        MeshFilter meshFilter = obj.AddComponent<MeshFilter>();

        meshFilter.sharedMesh = new Mesh();
        meshFilter.sharedMesh.vertices = tmf.sharedMesh.vertices;
        meshFilter.sharedMesh.triangles = tmf.sharedMesh.triangles.Reverse().ToArray();
        meshFilter.sharedMesh.uv = tmf.sharedMesh.uv;
        meshFilter.sharedMesh.RecalculateNormals();
        meshFilter.sharedMesh.Optimize();

        MeshRenderer meshRenderer = obj.AddComponent<MeshRenderer>();
        meshRenderer.sharedMaterial = temp.GetComponent<MeshRenderer>().sharedMaterial;

        DestroyImmediate(temp);
    }

    [MenuItem("GameObject/Custom/Inverted 3D Objects/Cube", false)]
    private static void Cube()
    {
        InvertPrimitive(PrimitiveType.Cube);
    }

    [MenuItem("GameObject/Custom/Inverted 3D Objects/Sphere", false)]
    private static void Sphere()
    {
        InvertPrimitive(PrimitiveType.Sphere);
    }

    [MenuItem("GameObject/Custom/Inverted 3D Objects/Capsule", false)]
    private static void Capsule()
    {
        InvertPrimitive(PrimitiveType.Capsule);
    }

    [MenuItem("GameObject/Custom/Inverted 3D Objects/Cylinder", false)]
    private static void Cylinder()
    {
        InvertPrimitive(PrimitiveType.Cylinder);
    }
}
