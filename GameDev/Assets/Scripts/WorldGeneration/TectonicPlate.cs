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
    public float Speed { get; private set; }
    public Vector3 Center { get; private set; }
    public Color[] Colors { get; private set; }
    public Mesh SharedMesh { get; private set; }
    public Vector3[] BoundaryVertices { get; private set; }
    public int[][] BoundaryEdges { get; private set; }
    public bool IsContinental { get; private set; }

    // Public set
    public LineRenderer Boundary { get; set; }
    public LineRenderer[] NeighborsBoundary { get; set; }

    // For speed
    private const float minSpeed = 0.1f;
    private const float maxSpeed = 2.0f;

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

        // Random assign speed
        Speed = GetRandomSpeed();

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

    private float GetRandomSpeed()
    {
        return Random.Range(minSpeed, maxSpeed);
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


    /*------------------------------------------------------------------------------------*/

    /* Late Public Constructor Functions */
    /* --- Listed by order of use ---*/

    // Generation of plates with plate centers, meant to be called by World
    public static Vector3[] GeneratePlateCenters(World world)
    {
        List<Vector3> centers = new List<Vector3>();

        centers.Add(Random.onUnitSphere);

        float minDist = world.distBetweenCenters[(int)world.plateSize];
        bool addToCenters;
        int MAX_TRIES = 999, tries = 0;

        while (tries < MAX_TRIES)
        {
            // Get random point
            addToCenters = true;
            Vector3 c = Random.onUnitSphere;

            // iterate through
            for (int i = 0; i < centers.Count; i++)
            {
                // If distance is larger than allowed, add to tries, tell it to not execute last bit, and break
                float dist = IMath.DistanceBetweenPoints(centers[i], c);

                if (dist <= minDist)
                {
                    tries += 1;
                    addToCenters = false;
                }
            }

            if (addToCenters)
            {
                centers.Add(c);
                tries = 0;
            }

        };

        return centers.ToArray();
    }

    public static TectonicPlate[] GeneratePlates(Vector3[] plateCenters, World world)
    {
        // Initialization
        TectonicPlate[] plates = new TectonicPlate[plateCenters.Length];
        List<int>[] plateTriangles = new List<int>[plateCenters.Length];
        List<Vector3>[] plateVertices = new List<Vector3>[plateCenters.Length];

        for (int i = 0; i < plateCenters.Length; i++)
        {
            plateTriangles[i] = new List<int>();
            plateVertices[i] = new List<Vector3>();
        }

        if (world.plateDeterminationType == World.PlateDetermination.ClosestCenter)
        {
            GetCenterClosestCenter(plateTriangles, plateVertices, out plateTriangles, out plateVertices, world);
        }
        else if (world.plateDeterminationType == World.PlateDetermination.FloodFill)
        {
            GetCenterFloodFill(plateTriangles, plateVertices, out plateTriangles, out plateVertices, world);
        }

        // Build plates
        for (int i = 0; i < plateCenters.Length; i++)
        {
            plates[i] = new TectonicPlate(plateCenters[i], plateVertices[i].ToArray(), plateTriangles[i].ToArray());
        }


        foreach (TectonicPlate plate in plates)
        {
            // Do colors
            // Percent on gradient
            float onGradient = IMath.FloorFloat(Random.Range(0f, 1f), 0.1f);
            Color color = plate.IsContinental ? world.continental.Evaluate(onGradient) : world.oceanic.Evaluate(onGradient);
            plate.SetColors(color);
        }

        // Find the neighbors of all plates
        Edge[] edges = FindEdges(plates);

        return plates;
    }

    /*------------------------------------------------------------------------------------*/

    /* Private Functions Called By Late Constructor Functions */
    
    private static void GetCenterClosestCenter(List<int>[] plateTriangles, List<Vector3>[] plateVertices, out List<int>[] pt, out List<Vector3>[] pv, World world)
    {
        // For each vertex in each meshfilter...
        for (int i = 0; i < world.sphere.meshFilters.Length; i++)
        {
            int[] t = world.sphere.meshFilters[i].mesh.triangles;
            Vector3[] v = world.sphere.meshFilters[i].mesh.vertices;
            for (int j = 0; j < t.Length; j += 3)
            {
                Vector3 centroid = IMath.TriangleCentroid(v[t[j + 0]], v[t[j + 1]], v[t[j + 2]]);
                float dist = IMath.DistanceBetweenPoints(centroid, world.plateCenters[0]);
                int distInd = 0;

                // Find the closest plate center
                for (int k = 1; k < world.plateCenters.Length; k++)
                {
                    float newDist = IMath.DistanceBetweenPoints(centroid, world.plateCenters[k]);
                    if (dist > newDist)
                    {
                        dist = newDist;
                        distInd = k;
                    }
                }

                // Add to vertices
                for (int k = 0; k < 3; k++)
                {
                    plateVertices[distInd].Add(v[t[j + k]]);
                }
            }
        }

        // Do triangles
        for (int i = 0; i < world.plateCenters.Length; i++)
        {
            for (int j = 0; j < plateVertices[i].Count; j++)
            {
                plateTriangles[i].Add(j);
            }
        }

        // Squash vertices
        if (world.smoothMapSurface)
        {
            for (int i = 0; i < plateVertices.Length; i++)
            {
                CondenseVerticesAndTriangles(plateVertices[i], plateTriangles[i], out plateVertices[i], out plateTriangles[i]);
            }
        }

        pt = plateTriangles;
        pv = plateVertices;
    }

    private static void GetCenterFloodFill(List<int>[] plateTriangles, List<Vector3>[] plateVertices, out List<int>[] pt, out List<Vector3>[] pv, World world)
    {
        Vector3[][] v = new Vector3[world.sphere.meshFilters.Length][];
        int[][] t = new int[world.sphere.meshFilters.Length][];
        for (int i = 0; i < world.sphere.meshFilters.Length; i++)
        {
            v[i] = world.sphere.meshFilters[i].mesh.vertices;
            t[i] = world.sphere.meshFilters[i].mesh.triangles;
        }

        // Loop through all triangles and create a triangle object with relevant information
        List<Triangle> triangles = new List<Triangle>();
        for (int i = 0; i < t.Length; i++)
        {
            for (int j = 0; j < t[i].Length; j += 3)
            {
                int[] tri = new int[] { t[i][j + 0], t[i][j + 1], t[i][j + 2] };
                Vector3[] ver = new Vector3[] { v[i][tri[0]], v[i][tri[1]], v[i][tri[2]] };
                Vector3 triCenter = IMath.TriangleCentroid(ver[0], ver[1], ver[2]);
                triangles.Add(new Triangle(tri, ver, triCenter));
            }
        }

        int ind = 0;
        List<Triangle> tempCenters = new List<Triangle>();
        // Set triangle centers as random triangles in the triangles list
        while (ind < world.plateCenters.Length && ind < triangles.Count)
        {
            tempCenters.Add(triangles[Random.Range(0, triangles.Count - 1)]);
            if (tempCenters.Count != tempCenters.Distinct().Count()) { tempCenters.RemoveAt(tempCenters.Count - 1); }
            else { ind++; }
        }

        // Resize
        world.plateCenters = new Vector3[tempCenters.Count];

        // Proper plate centers
        for (int i = 0; i < tempCenters.Count; i++)
        {
            world.plateCenters[i] = tempCenters[i].TriangleCenter;
            tempCenters[i].PlateCenter = i;
        }

        // Floodfill esque algorithm:
        // - Create 2d array of closest to farthest centers for each center
        // - Iterate through from closest to farthest, from first to last center and assign to center if the val is -1

        // closest to farthest map
        SortedList<float, int>[] distanceMap = new SortedList<float, int>[world.plateCenters.Length];
        for (int i = 0; i < world.plateCenters.Length; i++)
        {
            distanceMap[i] = new SortedList<float, int>();
            for (int j = 0; j < triangles.Count; j++)
            {
                float dist = IMath.DistanceBetweenPoints(world.plateCenters[i], triangles[j].TriangleCenter);
                // If dist key already exists, adjust until its right behind
                while (distanceMap[i].ContainsKey(dist)) { dist += .00001f; }
                distanceMap[i].Add(dist, j);
            }
        }

        // iterate through each plate center by way of distance map, and if it hasnt been touched yet, assign it to the plate
        for (int i = 0; i < triangles.Count; i++)
        {
            for (int j = 0; j < world.plateCenters.Length; j++)
            {
                if (triangles[distanceMap[j].Values[i]].PlateCenter == -1)
                {
                    triangles[distanceMap[j].Values[i]].PlateCenter = j;
                }
            }
        }

        // With assigned plate centers, assign to the proper arrays
        for (int i = 0; i < triangles.Count; i++)
        {
            int[] tri = triangles[i].Triangles;
            Vector3[] ver = triangles[i].Vertices;
            for (int j = 0; j < tri.Length; j++)
            {
                plateTriangles[triangles[i].PlateCenter].Add(tri[j]);
                plateVertices[triangles[i].PlateCenter].Add(ver[j]);
            }
        }

        // Squash vertices
        if (world.smoothMapSurface)
        {
            for (int i = 0; i < plateVertices.Length; i++)
            {
                CondenseVerticesAndTriangles(plateVertices[i], plateTriangles[i], out plateVertices[i], out plateTriangles[i]);
            }
        }

        // Assign
        pt = plateTriangles;
        pv = plateVertices;
    }

    // Find neighbors : get distinct list of edges...
    private static Edge[] FindEdges(TectonicPlate[] plates)
    {
        List<Edge> edges = new List<Edge>();

        for (int a = 0; a < plates.Length; a++)
        {
            for (int b = 0; b < plates[a].BoundaryEdges.Length; b++)
            {
                int[] bE = plates[a].BoundaryEdges[b];
                Edge e = new Edge(plates[a].Vertices[bE[0]], plates[a].Vertices[bE[1]]);

                edges.Add(e);
            }
        }

        return edges.Distinct(new TectonicPlateEdgeCompareOverride()).ToList().ToArray();
    }

    // Each triangle has their own set of vertices, the triangles should share vertices with neighboring triangles
    // to smooth the surface of the sphere
    // Approach: Loop through each vertex element, find matching vertices, get their indices into a list and delete afterwards
    private static void CondenseVerticesAndTriangles(List<Vector3> v, List<int> t, out List<Vector3> vertices, out List<int> triangles)
    {
        // Get distinct members of v
        vertices = v.Distinct().ToList();
        for (int i = 0; i < vertices.Count; i++)
        {
            // For each vertex, if they match the current comparison vertex, change the triangle to the proper index
            for (int j = i; j < v.Count; j++)
            {
                if (vertices[i] == v[j])
                {
                    t[j] = i;
                }
            }
        }

        triangles = t;
    }

    /*------------------------------------------------------------------------------------*/

    /* Other Classes */

    // Triangle Object for flood fill
    public class Triangle
    {
        public int PlateCenter { get; set; }
        public int[] Triangles { get; private set; }
        public Vector3[] Vertices { get; private set; }
        public Vector3 TriangleCenter { get; private set; }
        public Triangle(int[] triangles, Vector3[] vertices, Vector3 triangleCenter)
        {
            Triangles = triangles;
            Vertices = vertices;
            TriangleCenter = triangleCenter;

            PlateCenter = -1;
        }
    }

    // Edge for boundary stuff
    public class Edge
    {
        public Vector3[] edge { get; private set; }
        public Vector3[] invEdge { get; private set; }

        // Tectonic plates are stored in an array, edgeOf is an index array of that tectonic plate array
        // Describes the index of the plate the edge is a part of, should always be 2 long
        public int[] edgeOf{ get; set; }

        public Edge(Vector3 v1, Vector3 v2, int i = -1)
        {
            edge = new Vector3[] { v1, v2 };
            invEdge = new Vector3[] { v2, v1 };

            // -1 placeholder value
            edgeOf = new int[2] { -1, -1 };
            edgeOf[0] = i;
        }
    }
}