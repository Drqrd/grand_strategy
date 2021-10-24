using System.Collections;
using System.Collections.Generic;
using System.Linq;
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

    // Public set
    public LineRenderer Boundary { get; set; }
    public Vector3[] BoundaryVertices { get; private set; }
    public int[][] BoundaryEdges { get; private set; }
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

            FindBoundaryEdges();
            FindBoundaryVertices();
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
        List<Vector3> verts = new List<Vector3>();
        foreach (int[] edge in BoundaryEdges)
        {
            for (int i = 0; i < edge.Length; i++) { verts.Add(Vertices[edge[i]]); }
        }

        BoundaryVertices = verts.Distinct().ToArray();


        /*
        List<Vector3> bv = new List<Vector3>();
        List<int> bvMap = new List<int>();
        Dictionary<Vector3, int> mp = new Dictionary<Vector3, int>();

        // Store all the elements in the map with
        // their occurrence
        for (int i = 0; i < Triangles.Length; i++)
        {
            if (mp.ContainsKey(Vertices[Triangles[i]])) { mp[Vertices[Triangles[i]]] += 1; }
            else { mp.Add(Vertices[Triangles[i]], 1); }
        }

        // Traverse the map and add all the
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
        */
    }

    private void FindBoundaryEdges()
    {
        List<int[]> edges = new List<int[]>();
        Dictionary<int[], int> mp = new Dictionary<int[], int>(new IntArrCompareOverride());
        for (int i = 0; i < Triangles.Length; i += 3)
        {
            int[][] es = new int[][] { new int[] { Triangles[i + 0], Triangles[i + 1] },
                                       new int[] { Triangles[i + 1], Triangles[i + 2] },
                                       new int[] { Triangles[i + 2], Triangles[i + 0] } };

            int[][] esInv = new int[][] { new int[] { Triangles[i + 1], Triangles[i + 0] },
                                          new int[] { Triangles[i + 2], Triangles[i + 1] },
                                          new int[] { Triangles[i + 0], Triangles[i + 2] } };

            for (int j = 0; j < es.Length; j++)
            {
                if (mp.ContainsKey(es[j])) { mp[es[j]] += 1; }
                else if (mp.ContainsKey(esInv[j])) { mp[esInv[j]] += 1; }
                else { mp.Add(es[j], 1); }
            }
        }

        // Add
        foreach (KeyValuePair<int[], int> entry in mp)
        {
            uint num = uint.Parse(string.Join("", entry.Value));
            if (num == 1) { edges.Add(entry.Key); }
        }

        // Reorder edges so that they connect
        for (int i = 0; i < edges.Count - 1; i++)
        {

            for (int j = i + 2; j < edges.Count; j++)
            {
                if (edges[j][0] == edges[i][1])
                {
                    int[] tempEdge = edges[j];
                    edges[j] = edges[i + 1];
                    edges[i + 1] = tempEdge;
                    continue;
                }
            }
        }

        BoundaryEdges = edges.ToArray();
    }
}


class IntArrCompareOverride : IEqualityComparer<int[]>
{
    public bool Equals(int[] i1, int[] i2)
    {
        if (i1 == null && i2 == null) { return true; }
        if (i1 == null || i2 == null) { return false; }

        bool val = true;
        for (int ind = 0; ind < i1.Length; ind++) { if (i1[ind] != i2[ind]) { val = false; } }
        return val;
    }

    public int GetHashCode(int[] arr)
    {
        int hc = arr.Length;
        foreach (int val in arr) { hc = unchecked(hc * 314159 + val); }
        return hc;
    }
}