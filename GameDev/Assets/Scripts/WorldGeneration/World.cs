using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using MeshGenerator;
using MapGenerator;
using IDict;

// TODO:
//  - Create a get seed function to input into the map generators
//  - Nearest neighor calculation for plate boundary work.
//      Determined by same (adjacent) vertices / sides
public class World : MonoBehaviour
{
    public enum PlateSize
    {
        Islands,
        Small,
        Medium,
        Large,
        Enormous
    };

    private float[] distBetweenCenters = new float[] { .2f, .5f, .7f, .9f, 1.2f };

    public enum SphereType
    {
        Octahedron,
        Cube,
        Fibonacci
    }

    public enum MapDisplay
    {
        Mesh,
        Plates,
        Terrain,
        HeightMap,
        MoistureMap,
        TemperatureMap
    }

    public enum PlateDetermination
    {
        ClosestCenter,
        FloodFill
    }


    [SerializeField] private SphereType sphereType;

    [Header("General Parameters")]
    [SerializeField] private PlateSize plateSize;
    [SerializeField] private PlateDetermination plateDeterminationType;
    [SerializeField] [Range(1, 65534)] private int resolution = 1;
    [SerializeField] private bool convertToSphere = false;
    [SerializeField] private bool smoothMapSurface = true;
    [SerializeField] private bool displayVertices = false;
    [SerializeField] private bool displayPlateCenters = false;
    [SerializeField] private bool displayPlateDirections = false;

    [Header("Fibonacci Exclusive Parameters")]
    [SerializeField] [Range(0f, 1f)] private float jitter = 0;
    [SerializeField] private bool alterFibonacciLattice = true;

    [Header("Display")]
    [SerializeField] private MapDisplay mapDisplay;
    private MapDisplay previousDisplay;

    [Header("Gradients")]
    [SerializeField] private Gradient continental;
    [SerializeField] private Gradient oceanic;

    private Vector3[] plateCenters;
    private TectonicPlate[] plates;
    private CustomMesh sphere;
    private HeightMap heightMap;
    private TectonicPlateMap plateMap;
    private MoistureMap moistureMap;
    private TemperatureMap temperatureMap;

    /* ------------------------------------------------------------------------------------------------------------------------------------------- */

    // Start is called before the first frame updates
    private void Start()
    {
        BuildMesh();
        BuildMaps();
        previousDisplay = MapDisplay.Plates;
    }

    private void Update()
    {
        if (mapDisplay != previousDisplay) { ChangeMap(); }
    }

    /* ------------------------------------------------------------------------------------------------------------------------------------------- */

    private void BuildMesh()
    {
        switch (sphereType)
        {
            case SphereType.Octahedron:
                sphere = new OctahedronSphere(transform, resolution, convertToSphere);
                break;
            case SphereType.Cube:
                break;
            case SphereType.Fibonacci:
                sphere = new FibonacciSphere(transform, resolution, jitter, alterFibonacciLattice);
                break;
            default:
                sphere = null;
                break;
        }
        if (sphere != null)
        {
            sphere.Build();
        }
    }

    private void BuildMaps()
    {
        BuildTectonicPlates();
        BuildHeightMap();
        BuildMoistureMap();
        BuildTemperatureMap();
    }

    private void BuildTectonicPlates()
    {
        GeneratePlateCenters();
        GeneratePlates();
        BuildTectonicPlateMap();
    }

    private void BuildTectonicPlateMap()
    {
        plateMap = new TectonicPlateMap(transform, plates);
        plateMap.Build();
    }

    private void BuildHeightMap()
    {
        heightMap = new HeightMap(transform, sphere.meshFilters);
        heightMap.Build();
    }

    private void BuildMoistureMap()
    {
        moistureMap = new MoistureMap(transform, sphere.meshFilters);
        moistureMap.Build();
    }

    private void BuildTemperatureMap()
    {
        temperatureMap = new TemperatureMap(transform, sphere.meshFilters);
        temperatureMap.Build();
    }

    /* ------------------------------------------------------------------------------------------------------------------------------------------- */

    private void ChangeMap()
    {
        if (previousDisplay != mapDisplay)
        {
            previousDisplay = mapDisplay;
            foreach (Transform child in transform)
            {
                if (child.gameObject.name.Contains(mapDisplay.ToString())) { child.gameObject.SetActive(true); }
                else { child.gameObject.SetActive(false); }
            }
        }
    }

    /* ------------------------------------------------------------------------------------------------------------------------------------------- */
    private void GeneratePlateCenters()
    {
        List<Vector3> centers = new List<Vector3>();

        centers.Add(Random.onUnitSphere);

        float minDist = distBetweenCenters[(int)plateSize];
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

        plateCenters = centers.ToArray();
    }

    /* ------------------------------------------------------------------------------------------------------------------------------------------- */

    // First sort the points into their nearest plates
    private void GeneratePlates()
    {
        // Initialization
        List<int>[] plateTriangles = new List<int>[plateCenters.Length];
        List<Vector3>[] plateVertices = new List<Vector3>[plateCenters.Length];

        for (int i = 0; i < plateCenters.Length; i++)
        {
            plateTriangles[i] = new List<int>();
            plateVertices[i] = new List<Vector3>();
        }

        if (plateDeterminationType == PlateDetermination.ClosestCenter)
        {
            GetCenterClosestCenter(plateTriangles, plateVertices, out plateTriangles, out plateVertices);
        }
        else if (plateDeterminationType == PlateDetermination.FloodFill)
        {
            GetCenterFloodFill(plateTriangles, plateVertices, out plateTriangles, out plateVertices);
        }

        // Build plates
        plates = new TectonicPlate[plateCenters.Length];
        for (int i = 0; i < plateCenters.Length; i++)
        {
            plates[i] = new TectonicPlate(plateCenters[i], plateVertices[i].ToArray(), plateTriangles[i].ToArray());
        }

        
        foreach(TectonicPlate plate in plates)
        {
            // Do colors
            // Percent on gradient
            float onGradient = IMath.FloorFloat(Random.Range(0f, 1f), 0.1f);
            Color color = plate.IsContinental ? continental.Evaluate(onGradient) : oceanic.Evaluate(onGradient);
            plate.SetColors(color);

            // Find adjacent boundaries / plates foreach plate
            FindNeighbors(plate);
        }
    }

    /* ------------------------------------------------------------------------------------------------------------------------------------------- */

    private void GetCenterClosestCenter(List<int>[] plateTriangles, List<Vector3>[] plateVertices, out List<int>[] pt, out List<Vector3>[] pv)
    {
        // For each vertex in each meshfilter...
        for (int i = 0; i < sphere.meshFilters.Length; i++)
        {
            int[] t = sphere.meshFilters[i].mesh.triangles;
            Vector3[] v = sphere.meshFilters[i].mesh.vertices;
            for (int j = 0; j < t.Length; j += 3)
            {
                Vector3 centroid = IMath.TriangleCentroid(v[t[j + 0]], v[t[j + 1]], v[t[j + 2]]);
                float dist = IMath.DistanceBetweenPoints(centroid, plateCenters[0]);
                int distInd = 0;

                // Find the closest plate center
                for (int k = 1; k < plateCenters.Length; k++)
                {
                    float newDist = IMath.DistanceBetweenPoints(centroid, plateCenters[k]);
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
        for (int i = 0; i < plateCenters.Length; i++)
        {
            for (int j = 0; j < plateVertices[i].Count; j++)
            {
                plateTriangles[i].Add(j);
            }
        }

        // Squash vertices
        if (smoothMapSurface)
        {
            for (int i = 0; i < plateVertices.Length; i++)
            {
                CondenseVerticesAndTriangles(plateVertices[i], plateTriangles[i], out plateVertices[i], out plateTriangles[i]);
            }
        }

        pt = plateTriangles;
        pv = plateVertices;
    }

    /* ------------------------------------------------------------------------------------------------------------------------------------------- */

    private void GetCenterFloodFill(List<int>[] plateTriangles, List<Vector3>[] plateVertices, out List<int>[] pt, out List<Vector3>[] pv)
    {
        Vector3[][] v = new Vector3[sphere.meshFilters.Length][];
        int[][] t = new int[sphere.meshFilters.Length][];
        for (int i = 0; i < sphere.meshFilters.Length; i++)
        {
            v[i] = sphere.meshFilters[i].mesh.vertices;
            t[i] = sphere.meshFilters[i].mesh.triangles;
        }
        
        // Loop through all triangles and create a triangle object with relevant information
        List<Triangle> triangles = new List<Triangle>();
        for (int i = 0; i < t.Length; i++)
        {
            for (int j = 0; j < t[i].Length; j += 3)
            {
                int[] tri = new int[] { t[i][j + 0], t[i][j + 1], t[i][j + 2] };
                Vector3[] ver = new Vector3[] { v[i][tri[0]], v[i][tri[1]], v[i][tri[2]]};
                Vector3 triCenter = IMath.TriangleCentroid(ver[0], ver[1], ver[2]);
                triangles.Add(new Triangle(tri, ver, triCenter));
            }
        }
        
        int ind = 0;
        List<Triangle> tempCenters = new List<Triangle>();
        // Set triangle centers as random triangles in the triangles list
        while (ind < plateCenters.Length && ind < triangles.Count)
        {
            tempCenters.Add(triangles[Random.Range(0, triangles.Count - 1)]);
            if (tempCenters.Count != tempCenters.Distinct().Count()) { tempCenters.RemoveAt(tempCenters.Count - 1); }
            else { ind++; }
        }

        // Resize
        plateCenters = new Vector3[tempCenters.Count];

        // Proper plate centers
        for (int i = 0; i < tempCenters.Count; i++)
        {
            plateCenters[i] = tempCenters[i].TriangleCenter;
            tempCenters[i].PlateCenter = i;
        }

        // Floodfill esque algorithm:
        // - Create 2d array of closest to farthest centers for each center
        // - Iterate through from closest to farthest, from first to last center and assign to center if the val is -1

        // closest to farthest map
        SortedList<float, int>[] distanceMap = new SortedList<float, int>[plateCenters.Length];
        for (int i = 0; i < plateCenters.Length; i++)
        {
            distanceMap[i] = new SortedList<float, int>();
            for (int j = 0; j < triangles.Count; j++)
            {
                float dist = IMath.DistanceBetweenPoints(plateCenters[i], triangles[j].TriangleCenter);
                // If dist key already exists, adjust until its right behind
                while (distanceMap[i].ContainsKey(dist)) { dist += .00001f; }
                distanceMap[i].Add(dist, j);
            }
        }
        
        // iterate through each plate center by way of distance map, and if it hasnt been touched yet, assign it to the plate
        for (int i = 0; i < triangles.Count; i++)
        {
            for (int j = 0; j < plateCenters.Length; j++)
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
        if (smoothMapSurface)
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

    // Triangle Object for flood fill
    private class Triangle
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

    /* ------------------------------------------------------------------------------------------------------------------------------------------- */

    // Each triangle has their own set of vertices, the triangles should share vertices with neighboring triangles
    // to smooth the surface of the sphere
    // Approach: Loop through each vertex element, find matching vertices, get their indices into a list and delete afterwards
    private void CondenseVerticesAndTriangles(List<Vector3> v, List<int> t, out List<Vector3> vertices, out List<int> triangles)
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

    /* ------------------------------------------------------------------------------------------------------------------------------------------- */

    // Find neighbors through comparing all boundary edges (not slow af?)
    // Approach:
    // - O(n): have a dictionary, if boundary edge exists (if is the edge or the inverse), append to the List, else add the edge 
    // - There should be only two indices per edge, and that will be the adjacent plates
    private void FindNeighbors(TectonicPlate plate)
    {

        // Find all existing edges, if edge is in Dictionary, append to list in value. 
        // Else, add to dictionary
        Dictionary<BoundaryEdge, List<int>> sharedBoundariesMap = new Dictionary<BoundaryEdge, List<int>>(new BoundaryEdgeCompareOverride());
        for (int i = 0; i < plates.Length; i++)
        {
            int[][] edgeIndices = plates[i].BoundaryEdges;
            Vector3[] edgeVertices = plates[i].Vertices;
            // For each edge in each plate
            for (int j = 0; j < plates[i].BoundaryEdges.Length; j++)
            {
                Vector3[] e = new Vector3[] { edgeVertices[edgeIndices[j][0]], edgeVertices[edgeIndices[j][1]] };
                BoundaryEdge edge = new BoundaryEdge(e);

                if (sharedBoundariesMap.ContainsKey(edge)) { sharedBoundariesMap[edge].Add(i); }
                else
                {
                    sharedBoundariesMap.Add(edge, new List<int>());
                    sharedBoundariesMap[edge].Add(i);
                }
            }
        }

        // Assign appropriate edges
        for (int i = 0; i < sharedBoundariesMap.Count; i++)
        {

        }
    }

    // Data class for easy readability in the neighbors searching algorithm
    public class BoundaryEdge
    {
        public Vector3[] Edge { get; private set; }
        public Vector3[] InverseEdge { get; private set; }

        public BoundaryEdge(Vector3[] edge)
        {
            if (edge.Length == 2)
            {
                Edge = edge;
                InverseEdge = new Vector3[2] { edge[1], edge[0] };
            }
            else
            {
                Edge = null;
                InverseEdge = null;
            }
        }
    }



    /* ------------------------------------------------------------------------------------------------------------------------------------------- */

    private void OnDrawGizmosSelected()
    {
        if (Application.isPlaying)
        {
            if (mapDisplay == MapDisplay.Plates && plates != null)
            {
                Gizmos.color = Color.red;
                for (int i = 0; i < plates.Length; i++)
                {
                    if (displayPlateDirections) { Gizmos.DrawLine(plates[i].Center, plates[i].Direction); }
                    if (displayPlateCenters) { Gizmos.DrawSphere(plates[i].Center, 0.01f); }
                }
            }

            if (displayVertices)
            {
                for (int i = 0; i < sphere.meshFilters.Length; i++)
                {
                    foreach (Vector3 v in sphere.meshFilters[i].sharedMesh.vertices)
                    {
                        Gizmos.DrawSphere(v, 0.01f);
                    }
                }
            }
        }
    }
}
