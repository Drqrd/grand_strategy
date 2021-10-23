using System.Collections.Generic;
using UnityEngine;

// TODO: Reorder boundary Vertices

public class TectonicPlate
{
    public Vector3[] Vertices { get; private set; }
    public int[] Triangles { get; private set; }
    public Vector3 Direction { get; private set; }
    public Vector3 Center { get; private set; }
    public Color[] Colors { get; private set; }
    public Vector3[] Neighbors { get; private set; }
    public Mesh SharedMesh { get; private set; }

    public Cell[] Cells { get; private set; }

    // Public set
    public LineRenderer Boundary { get; set; }
    public Vector3[] BoundaryVertices { get; private set; }
    public int[] BVMap { get; private set; }

    private const int BOUNDARY_VALUE = 2;


    // Mapping parts
    private float height, moisture, temperature;

    public TectonicPlate(Vector3 center, Vector3[] vertices = null, int[] triangles = null, Color[] colors = null)
    {
        Center = center;
        Vertices = vertices;
        Triangles = triangles;
        Colors = colors;

        SharedMesh = new Mesh();

        Direction = Vector3.Lerp(Center, (GetRandomDirection() + Center), .15f);

        if (Vertices != null && Triangles != null)
        {
            SharedMesh.vertices = Vertices;
            SharedMesh.triangles = Triangles;
            if (colors != null) { SharedMesh.colors = Colors; }
            SharedMesh.RecalculateNormals();
            SharedMesh.Optimize();

            FindBoundaryVertices();

            Cells = GenerateCells();            
        }
    }

    private Vector3 GetRandomDirection()
    {
        Vector3 tangent = Vector3.Cross(Center, Vector3.up);
        if (tangent.sqrMagnitude < float.Epsilon) { tangent = Vector3.Cross(Center, Vector3.forward); }
        tangent.Normalize();

        Quaternion rotation = Quaternion.AngleAxis(Random.Range(0f, 360f), Center);
        return rotation * tangent;
    }

    private void FindBoundaryVertices()
    {
        List<Vector3> bv = new List<Vector3>();
        List<int> bvMap = new List<int>();
        Dictionary<Vector3, int> mp = new Dictionary<Vector3, int>();

        // Store all the elements in the map with
        // their occurrence
        for (int i = 0; i < Triangles.Length; i++)
        {
            if (mp.ContainsKey(Vertices[Triangles[i]])) { mp[Vertices[Triangles[i]]] = 1 + mp[Vertices[Triangles[i]]]; }
            else { mp.Add(Vertices[Triangles[i]], 1); }
        }

        // Traverse the map and print all the
        // elements with occurrence of 2 or less
        foreach (KeyValuePair<Vector3, int> entry in mp)
        {
            int num = (int)uint.Parse(string.Join("", entry.Value));

            if (num <= BOUNDARY_VALUE) { bv.Add(entry.Key); }
            bvMap.Add(num);
        }

        // Reorder boundary Vertices into a counterclockwise manner, to draw a circle
        
        BoundaryVertices = bv.ToArray();
        BVMap = bvMap.ToArray();
    }

    // Finds the boundary Vertices through locating Vertices attached to only one triangle
    private Cell[] GenerateCells()
    {
        List<Cell> c = new List<Cell>();

        for (int i = 0; i < Triangles.Length; i += 3)
        {
            
            int[] tri = new int[] { Triangles[i], Triangles[i + 1], Triangles[i + 2] };
            c.Add(new Cell(this, tri));
        }

        return c.ToArray();
    }
}
