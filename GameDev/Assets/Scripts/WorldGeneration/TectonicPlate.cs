using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using IDict;

// TODO: Reorder boundary Vertices

public class TectonicPlate
{
    // Private set
    public Vector3[] Vertices { get; private set; }
    public int[] Triangles { get; private set; }
    public Vector3 Direction { get; private set; }
    public Vector3 Center { get; private set; }
    public Color[] Colors { get; private set; }
    public Vector3[] Neighbors { get; private set; }
    public Mesh SharedMesh { get; private set; }
    public Vector3[] BoundaryVertices { get; private set; }
    public int[][] BoundaryEdges { get; private set; }
    public bool IsContinental { get; private set; }

    // Public set
    public LineRenderer Boundary { get; set; }
    public int[] BoundaryNeighbors { get; set; }    // Size of boundary edges, labels the edge with corresponding neighbor

    // Mapping parts
    private float height, moisture, temperature;

    // Constructor
    public TectonicPlate(Vector3 center, Vector3[] vertices = null, int[] triangles = null, Color[] colors = null)
    {
        Center = center;
        Vertices = vertices;
        Triangles = triangles;
        if (colors != null) { Colors = colors; }

        SharedMesh = new Mesh();

        // Random assign of direction
        Direction = Vector3.Lerp(Center, (GetRandomDirection() + Center), .15f);

        // Random assign if continental or oceanic
        IsContinental = Random.Range(0f, 1f) > 0.5f ? true : false;

        // Build mesh if vertices and triangles arent null
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

    /* Constructor Functions */
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
        for(int i = 0; i < BoundaryEdges.Length; i++)
        {
            verts.Add(Vertices[BoundaryEdges[i][0]]);
        }

        BoundaryVertices = verts.Distinct().ToArray();
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

    /*------------------------------------------------------------------------------------*/
    
    /* Public Functions */

    // Sets color of the entire mesh (mono)
    public void SetColors(Color color)
    {
        List<Color> colors = new List<Color>();
        for(int i = 0; i < Vertices.Length; i++) { colors.Add(color); }
        Colors = colors.ToArray();

        SharedMesh.colors = Colors;
    }
}