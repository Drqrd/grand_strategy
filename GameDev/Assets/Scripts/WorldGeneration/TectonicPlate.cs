=using System.Collections.Generic;
using UnityEngine;

// TODO: Reorder boundary vertices

public class TectonicPlate
{
    public Vector3[] Vertices { get { return vertices; } set { vertices = value; } }
    public Vector3[] BoundaryVertices { get { return boundaryVertices; } set { boundaryVertices = value; } }
    public int[] Triangles { get { return triangles; } set { triangles = value; } }
    public Vector3 Direction { get { return direction; } set { direction = value; } }
    public Vector3 Center { get { return center; } }
    public Color[] Colors { get { return colors; } set { colors = value; } }
    public Vector3[] Neighbors { get { return neighbors; } }
    public Mesh SharedMesh { get { return mesh; } set { mesh = value; } }
    public LineRenderer Boundary { get { return boundary; } set { boundary = value; } }


    private Vector3[] vertices;
    private Vector3[] boundaryVertices;
    private int[] triangles;
    private Vector3 direction;
    private Vector3 center;
    private Color[] colors;
    private Vector3[] neighbors;

    private Mesh mesh;
    private LineRenderer boundary;

    // Mapping parts
    private float height, moisture, temperature;

    public TectonicPlate(Vector3 center, Vector3[] vertices = null, int[] triangles = null, Color[] colors = null)
    {
        this.center = center;
        this.vertices = vertices;
        this.triangles = triangles;
        this.colors = colors;

        mesh = new Mesh();

        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.colors = colors;
        mesh.RecalculateNormals();
        mesh.Optimize();

        direction = Vector3.Lerp(center,(GetRandomDirection() + center), .15f);

        boundaryVertices = FindBoundaryVertices();
    }

    private Vector3 GetRandomDirection()
    {
        Vector3 tangent = Vector3.Cross(center, Vector3.up);
        if (tangent.sqrMagnitude < float.Epsilon) { tangent = Vector3.Cross(center, Vector3.forward); }
        tangent.Normalize();

        Quaternion rotation = Quaternion.AngleAxis(Random.Range(0f, 360f), center);
        return rotation * tangent;
    }

    // Finds the boundary vertices through locating vertices attached to only one triangle
    private Vector3[] FindBoundaryVertices()
    {
        List<Vector3> bv = new List<Vector3>();
        Dictionary<Vector3, int> mp = new Dictionary<Vector3, int>();

        // Store all the elements in the map with
        // their occurrence
        for (int i = 0; i < triangles.Length; i++)
        {
            if (mp.ContainsKey(vertices[triangles[i]])) { mp[vertices[triangles[i]]] = 1 + mp[vertices[triangles[i]]]; }
            else { mp.Add(vertices[triangles[i]], 1); }
        }

        // Traverse the map and print all the
        // elements with occurrence 1
        foreach (KeyValuePair<Vector3, int> entry in mp)
        {
            if (uint.Parse(string.Join("", entry.Value)) <= 2) { bv.Add(entry.Key); }
        }

        // Reorder boundary vertices into a counterclockwise manner, to draw a circle
        
        return bv.ToArray();
    }
}
