using UnityEngine;

public class SpaceSkybox : MonoBehaviour
{ 
    private void Start()
    {
        MeshFilter meshFilter = GetComponent<MeshFilter>();

        // Normalize the vertices
        Vector3[] vertices = meshFilter.sharedMesh.vertices;
        for(int a = 0; a < vertices.Length; a++) { vertices[a] = vertices[a].normalized; }
        meshFilter.sharedMesh.vertices = vertices;
    }
}
