using System.Collections.Generic;
using System.Collections;
using System.Linq;
using UnityEngine;
using MeshGenerator;
using MapGenerator;

// TODO: - Create a get seed function to input into the map generators
//       - Add jitter functionality to Fibonacci

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

    [Header("Parameters")]
    [SerializeField] private SphereType sphereType;
    [SerializeField] private PlateSize plateSize;
    [SerializeField] private PlateDetermination plateDeterminationType;
    [SerializeField] [Range(1, 65534)] private int resolution = 1;
    [SerializeField] private bool convertToSphere = false;
    [SerializeField] private bool smoothMapSurface = true;
    [SerializeField] private bool displayVertices = false;

    [Header("Fibonacci Exclusive Parameters")]
    [SerializeField] [Range(0f, 1f)] private float jitter = 0;
    [SerializeField] private bool alterFibonacciLattice = true;

    [Header("Display")]
    [SerializeField] private MapDisplay mapDisplay;
    private MapDisplay previousDisplay;

    private Vector3[] plateCenters;
    private TectonicPlate[] plates;
    private CustomMesh sphere;
    private HeightMap heightMap;
    private TectonicPlateMap plateMap;
    private MoistureMap moistureMap;
    private TemperatureMap temperatureMap;

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

    // First sort the points into their nearest plates
    private void GeneratePlates()
    {
        // Initialization
        List<int>[] plateTriangles = new List<int>[plateCenters.Length];
        List<Vector3>[] plateVertices = new List<Vector3>[plateCenters.Length];
        List<Color>[] plateColors = new List<Color>[plateCenters.Length];

        for (int i = 0; i < plateCenters.Length; i++)
        {
            plateTriangles[i] = new List<int>();
            plateVertices[i] = new List<Vector3>();
            plateColors[i] = new List<Color>();
        }

        if (plateDeterminationType == PlateDetermination.ClosestCenter)
        {
            GetCenterClosestCenter(plateTriangles, plateVertices, plateColors, out plateTriangles, out plateVertices, out plateColors);
        }
        else if (plateDeterminationType == PlateDetermination.FloodFill)
        {
            GetCenterFloodFill(plateTriangles, plateVertices, plateColors, out plateTriangles, out plateVertices, out plateColors);
        }


        plates = new TectonicPlate[plateCenters.Length];
        for (int i = 0; i < plateCenters.Length; i++)
        {
            plates[i] = new TectonicPlate(plateCenters[i], plateVertices[i].ToArray(), plateTriangles[i].ToArray(), plateColors[i].ToArray());
        }
    }

    private void GetCenterClosestCenter(List<int>[] plateTriangles, List<Vector3>[] plateVertices, List<Color>[] plateColors, out List<int>[] pt, out List<Vector3>[] pv, out List<Color>[] pc)
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

        // Do colors
        for (int i = 0; i < plateCenters.Length; i++)
        {
            Color color = Random.ColorHSV();
            for (int j = 0; j < plateVertices[i].Count; j++)
            {
                plateColors[i].Add(color);
            }
        }

        pt = plateTriangles;
        pv = plateVertices;
        pc = plateColors;
    }

    private void GetCenterFloodFill(List<int>[] plateTriangles, List<Vector3>[] plateVertices, List<Color>[] plateColors, out List<int>[] pt, out List<Vector3>[] pv, out List<Color>[] pc)
    {
        // queue initialization
        Queue<Triangle> floodQueue = new Queue<Triangle>();
        List<Triangle> triangles = new List<Triangle>();


        // Loop through all triangles and create a triangle object with relevant information
        for (int i = 0; i < plateTriangles.Length; i++)
        {
            for (int j = 0; j < plateTriangles.Length; j += 3)
            {
                int[] tri = new int[] { plateTriangles[i][j + 0], plateTriangles[i][j + 1], plateTriangles[i][j + 2] };
                Vector3 triCenter = IMath.TriangleCentroid(plateVertices[i][tri[0]], plateVertices[i][tri[1]], plateVertices[i][tri[2]]);
                triangles.Add(new Triangle(tri, triCenter));
            }
        }

        int ind = 0;
        List<Triangle> tempCenters = new List<Triangle>();
        // Set triangle centers as random triangles in the triangles list
        while (ind < plateCenters.Length)
        {
            tempCenters.Add(triangles[Random.Range(0, triangles.Count - 1)]);
            if (tempCenters.Count != tempCenters.Distinct().Count()) { tempCenters.RemoveAt(tempCenters.Count - 1); }
            else { ind++; }
        }
        
        // Enqueue the centers of the plates
        for (int i = 0; i < tempCenters.Count; i++) 
        {
            plateCenters[i] = tempCenters[i].TriangleCenter;
            tempCenters[i].PlateCenter = i;
            floodQueue.Enqueue(tempCenters[i]); 
        }

        // Start the floodfill(4 way), add to end of queue to check


        // Assign
        pt = plateTriangles;
        pv = plateVertices;
        pc = plateColors;
    }

    // Triangle Object for flood fill
    private class Triangle
    {
        public int PlateCenter { get; set; }
        public int[] Triangles { get; private set; }
        public Vector3 TriangleCenter { get; private set; }
        public Triangle(int[] triangles, Vector3 triangleCenter)
        {
            Triangles = triangles;
            TriangleCenter = triangleCenter;

            PlateCenter = -1;
        }
    }   

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

    private void OnDrawGizmosSelected()
    {
        if (Application.isPlaying)
        {
            if (mapDisplay == MapDisplay.Plates && plates != null)
            {
                Gizmos.color = Color.red;
                for (int i = 0; i < plates.Length; i++)
                {
                    Gizmos.DrawLine(plates[i].Center, plates[i].Direction);
                    Gizmos.DrawSphere(plates[i].Center, 0.01f);
                }
            }

            if (displayVertices)
            {
                for(int i = 0; i < sphere.meshFilters.Length; i++)
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
