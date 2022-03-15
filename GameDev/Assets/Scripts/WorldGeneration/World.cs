using UnityEngine;

using WorldGeneration.Meshes;
using WorldGeneration.Maps;

using System.Collections.Generic;

using DelaunatorSharp;
using DataStructures.ViliWonka.KDTree;

using static WorldData;

public class World : MonoBehaviour
{
    public enum MapDisplay
    {
        Mesh,
        Terrain,
        TectonicPlateMap,
        HeightMap,
        MoistureMap,
        TemperatureMap,
    }

    public enum PlateDetermination
    {
        FloodFill
    }

    public enum BoundaryDisplay
    {
        Default,
        FaultLines,
        FaultLineType
    }

    [Header("Debug Parameters")]
    [SerializeField] private bool enableDebugOnGeneration;

    [Header("Debug Delaunay")]
    [SerializeField] private bool enableDelaunayDebug;
    [SerializeField] private bool d_triangleCenters;
    [SerializeField] private bool d_triangleCentroids; 
    [SerializeField] private bool d_triangleEdges;
    [SerializeField] private bool d_voronoiEdges;
    [SerializeField] private bool d_constructedTriangleCentroids;
    [SerializeField] private bool d_constructedVoronoiEdges;
    [SerializeField] private bool d_finalCell;


    [Header("General Parameters")]
    [SerializeField] [Range(10, 50)] private int plateNumber = 20;
    [SerializeField] private PlateDetermination plateDeterminationType;
    [SerializeField] [Range(2, 100000)] private int resolution = 2;
    [SerializeField] [Range(0f, 1f)] private float continentalVsOceanic = 0.5f;
    [SerializeField] [Range(1,16)] private int chunks = 8;

    [Header("Fibonacci Exclusive Parameters")]
    [SerializeField] [Range(0f, 1f)] private float jitter = 0;
    [SerializeField] private bool alterFibonacciLattice = true;

    [Header("HeightMap Parameters")]
    [SerializeField] [Range(1, 8)] private int neighborNumber = 3;
    [SerializeField] [Range(0, 8)] private int h_blendDepth = 4;
    [SerializeField] [Range(0f, 2f)] private float continentHeightMutiplier = 1f;
    [SerializeField] [Range(0f, 1f)] private float oceanDepthMultiplier = 1f;

    [Header("MoistureMap Parameters")]
    [SerializeField] [Range(0, 4)] private int m_blendDepth = 2;

    [Header("Display")]
    [SerializeField] private MapDisplay mapDisplay;
    private MapDisplay previousMapDisplay;

    [SerializeField] private bool displayVertices = false;

    [Header("Plate Exclusive Display")]
    [SerializeField] private BoundaryDisplay boundaryDisplay;
    private BoundaryDisplay previousBoundaryDisplay;

    [SerializeField] private bool displayPlateCenters = false;
    [SerializeField] private bool displayPlateDirections = false;

    [Header("Gradients")]
    [SerializeField] private Gradient terrainMapGradient;
    [SerializeField] private Gradient temperatureMapGradient;
    [SerializeField] private Gradient moistureMapGradient;
    [SerializeField] private Gradient heightMapGradient;

    // Maps
    private TectonicPlateMap plateMap = null;
    private HeightMap heightMap = null;
    private MoistureMap moistureMap = null;
    private TemperatureMap temperatureMap = null;
    private TerrainMap terrainMap = null;

    // Parameters & shared data
    public Parameters parameters { get; private set; }
    public WorldData worldData { get; private set; }

    // Pure properties
    public FibonacciSphere Sphere { get; private set; }

    /* ------------------------------------------------------------------------------------------------------------------------------------------- */

    private void Update()
    {
        if (mapDisplay != previousMapDisplay) { ChangeMapDisplay(); }
        if (boundaryDisplay != previousBoundaryDisplay && mapDisplay == MapDisplay.TectonicPlateMap) { ChangeBoundaryDisplay(); }
    }
    

    /* ------------------------------------------------------------------------------------------------------------------------------------------- */

    // Generate world from button press
    public void GenerateWorld()
    {
        while (transform.childCount > 0) { DestroyImmediate(transform.GetChild(0).gameObject); }

        InitializeParameterProperties();

        worldData = new WorldData();

        BuildMesh();
        transform.GetChild(0).gameObject.SetActive(false);

        if (enableDebugOnGeneration) { DebugWorld(); }

        FindCellNeighbors();
        BuildMaps();

        // If the display is the last value, make prev - 1, otherwise + 1
        previousMapDisplay = (int)mapDisplay == System.Enum.GetValues(typeof(MapDisplay)).Length ? (MapDisplay)((int)mapDisplay + 1) : (MapDisplay)((int)mapDisplay - 1);
        previousBoundaryDisplay = (int)boundaryDisplay == System.Enum.GetValues(typeof(BoundaryDisplay)).Length ? (BoundaryDisplay)((int)mapDisplay + 1) : (BoundaryDisplay)((int)mapDisplay - 1);
    }

    // Loading from saved data
    public void LoadWorld(Save save)
    {
        BuildMaps(save);
    }

    /* ------------------------------------------------------------------------------------------------------------------------------------------- */
    private void InitializeParameterProperties()
    {
        parameters = new Parameters(resolution);
        parameters.customMesh = new Parameters.CustomMesh(chunks);
        parameters.plates = new Parameters.Plates(plateDeterminationType, plateNumber, continentalVsOceanic, neighborNumber);
        parameters.height = new Parameters.Height(h_blendDepth, continentHeightMutiplier, oceanDepthMultiplier, heightMapGradient);
        parameters.moisture = new Parameters.Moisture(m_blendDepth, moistureMapGradient);
        parameters.temperature = new Parameters.Temperature(temperatureMapGradient);
        parameters.terrain = new Parameters.Terrain(terrainMapGradient);
    }

    private void BuildMesh()
    {
        Sphere = new FibonacciSphere(this, transform, resolution, jitter, alterFibonacciLattice);
        // Sphere = new CubeSphere(transform, resolution, jitter);
        Sphere.Build();

        Mesh mesh = Sphere.meshFilter.sharedMesh;
        worldData.mesh = new Mesh();
        worldData.mesh.vertices = mesh.vertices;
        worldData.mesh.triangles = mesh.triangles;
        worldData.mesh.normals = mesh.normals;
    }

    private void DebugWorld()
    {
        if (enableDelaunayDebug) { DebugDelaunay(); }
    }

    private void DebugDelaunay()
    {
        transform.GetChild(0).gameObject.SetActive(false);
        WorldData.Debug.Delaunay d = worldData.debug.delaunay;
        float scale = 0.01f;
        Vector3 cubeSize = new Vector3(scale,scale,scale);

        GameObject delaunayDebug = new GameObject("Delaunay Debug");
        delaunayDebug.transform.parent = this.transform;

        GameObject triangleCenters = new GameObject("Triangle Centers");
        triangleCenters.transform.parent = delaunayDebug.transform;

        for(int a = 0; a < d.triangleCenters.Length; a++)
        {
            DrawCube(d.triangleCenters[a], cubeSize, triangleCenters.transform);
        }

        triangleCenters.SetActive(d_triangleCenters);

        GameObject voronoiPoints = new GameObject("Triangle Centroids");
        voronoiPoints.transform.parent = delaunayDebug.transform;
        for(int a = 0; a < d.triangleCentroids.Length; a++)
        {
            DrawCube(d.triangleCentroids[a], cubeSize, voronoiPoints.transform);
        }

        voronoiPoints.SetActive(d_triangleCentroids);

        GameObject triangleEdges = new GameObject("Triangle Edges");
        triangleEdges.transform.parent = delaunayDebug.transform;

        for(int a = 0; a < d.triangleEdges.Length; a++)
        {
            DrawLine(d.triangleEdges[a][0], d.triangleEdges[a][1], triangleEdges.transform, Color.gray);
        }

        triangleEdges.SetActive(d_triangleEdges);

        GameObject voronoiEdges = new GameObject("Voronoi Edges");
        voronoiEdges.transform.parent = delaunayDebug.transform;

        for(int a = 0; a < d.voronoiEdges.Length; a++)
        {
            DrawLine(d.voronoiEdges[a][0], d.voronoiEdges[a][1], voronoiEdges.transform, Color.blue);
        }

        voronoiEdges.SetActive(d_voronoiEdges);

        GameObject constructedTriangleCentroids = new GameObject("Constructed Triangle Centroids");
        constructedTriangleCentroids.transform.parent = delaunayDebug.transform;

        for(int a = 0; a < d.constructedTriangleCentroids.Length; a++)
        {
            DrawCube(d.constructedTriangleCentroids[a], cubeSize * 3f, constructedTriangleCentroids.transform);
        }

        constructedTriangleCentroids.SetActive(d_constructedTriangleCentroids);

        GameObject constructedVoronoiEdges = new GameObject("Constructed Voronoi Edges");
        constructedVoronoiEdges.transform.parent = delaunayDebug.transform;

        for (int a = 0; a < d.constructedVoronoiEdges.Length; a++)
        {
            DrawLine(d.constructedVoronoiEdges[a][0], d.constructedVoronoiEdges[a][1], constructedVoronoiEdges.transform, Color.cyan);
        }

        constructedVoronoiEdges.SetActive(d_constructedVoronoiEdges);

        GameObject finalCell = new GameObject("Final Cell");
        finalCell.transform.parent = delaunayDebug.transform;

        for(int a = 0; a < d.finalCell.Length; a++)
        {
            DrawLine(d.finalCell[a][0], d.finalCell[a][1], finalCell.transform, Color.green);
        }

        finalCell.SetActive(d_finalCell);
    }

    private void FindCellNeighbors()
    {
        Vector3[] pointCloud = new Vector3[worldData.cells.Length];
        for (int a = 0; a < worldData.cells.Length; a++)
        {
            pointCloud[a] = worldData.cells[a].center;
        }
        KDTree kdTree = new KDTree(pointCloud, 1);
        KDQuery query = new KDQuery();

        for(int a = 0; a < worldData.cells.Length; a++)
        {
            List<int> neighbors = new List<int>();
            query.KNearest(kdTree, worldData.cells[a].center, worldData.cells[a].points.Length + 2, neighbors);
            Cell[] cells = new Cell[neighbors.Count];
            for(int b = 0; b < neighbors.Count; b++) { cells[b] = worldData.cells[neighbors[b]]; }
            worldData.cells[a].neighbors = cells;
        }
    }

    private void BuildMaps(Save save = null)
    {
        BuildTectonicPlateMap(save);
        /*
        BuildHeightMap(save);
        BuildMoistureMap(save);
        BuildTemperatureMap(save);
        BuildTerrainMap(save);
        */
    }

    private void BuildTectonicPlateMap(Save save = null)
    {
        plateMap = new TectonicPlateMap(this, save);
        if (save == null) { plateMap.Build(); }
        else { plateMap.Load(); }
    }
   
    private void BuildHeightMap(Save save = null)
    {
       heightMap = new HeightMap(this, save);
       if (save == null) { heightMap.Build(); }
       //else { heightMap.Load(); }
    }


    private void BuildMoistureMap(Save save = null)
    {
        moistureMap = new MoistureMap(this, save);
        moistureMap.Build();
    }

    private void BuildTemperatureMap(Save save = null)
    {
        temperatureMap = new TemperatureMap(this, save);
        temperatureMap.Build();
    }

    private void BuildTerrainMap(Save save = null)
    {
        terrainMap = new TerrainMap(this, save);
        terrainMap.Build();
    }

    /* ------------------------------------------------------------------------------------------------------------------------------------------- */

    private void ChangeMapDisplay()
    {
        previousMapDisplay = mapDisplay;
        foreach (Transform child in transform)
        {
            if (!child.gameObject.name.Contains("Camera"))
            {
                if (child.gameObject.name.Contains(mapDisplay.ToString())) { child.gameObject.SetActive(true); }
                else { child.gameObject.SetActive(false); }
            }
        }
    }

    private void ChangeBoundaryDisplay()
    {
        previousBoundaryDisplay = boundaryDisplay;
        Transform boundaryRef = transform.Find(MapDisplay.TectonicPlateMap.ToString()).GetChild(0);
        TectonicPlateMap.FaultData faultData = plateMap.faultData;

        foreach (Transform boundary in boundaryRef)
        {
            switch (boundaryDisplay)
            {
                case BoundaryDisplay.Default:
                    foreach(LineRenderer line in faultData.lines)
                    {
                        line.startColor = faultData.defaultColor;
                        line.endColor = faultData.defaultColor;
                    }
                    break;
                case BoundaryDisplay.FaultLines:
                    for (int a = 0; a < faultData.lines.Length; a++)
                    {
                        faultData.lines[a].startColor = faultData.colors[a];
                        faultData.lines[a].endColor = faultData.colors[a];
                    }
                    break;
                case BoundaryDisplay.FaultLineType:
                    for (int a = 0; a < faultData.lines.Length; a++)
                    {
                        faultData.lines[a].startColor = faultData.weightedColors[a];
                        faultData.lines[a].endColor = faultData.weightedColors[a];
                    }
                    break;
                default:
                    break;
            }
        }
    }

    /* ------------------------------------------------------------------------------------------------------------------------------------------- */

    private void OnDrawGizmos()
    {
        if (Application.isPlaying)
        {
            if (mapDisplay == MapDisplay.TectonicPlateMap && worldData.plates != null)
            {
                Gizmos.color = Color.red;
                for (int i = 0; i < worldData.plates.Length; i++)
                {
                    if (displayPlateDirections) { Gizmos.DrawLine(worldData.plates[i].center.center, worldData.plates[i].direction); }
                    if (displayPlateCenters) { Gizmos.DrawCube(worldData.plates[i].center.center, new Vector3(0.1f,0.1f,0.1f)); }
                }
            }

            if (displayVertices)
            {
                for (int i = 0; i < worldData.mesh.vertices.Length; i++)
                {
                    foreach (Vector3 v in worldData.mesh.vertices)
                    {
                        Gizmos.DrawCube(v, new Vector3(0.1f, 0.1f, 0.1f));
                    }
                }
            }
        }
    }

    // Parameters class for clean code, part of refactoring
    public class Parameters
    {
        public int resolution { get; private set; }
        public CustomMesh customMesh { get; set; }
        public Plates plates { get; set; }
        public Height height { get; set; }
        public Moisture moisture { get; set; }
        public Temperature temperature { get; set; }
        public Terrain terrain { get; set; }

        public Parameters(int r)
        {
            resolution = r + 1;
        }

        public class CustomMesh
        {
            public CustomMesh(int cs)
            {
                chunks = cs;
            }

            public int chunks { get; private set; }
        }

        public class Plates
        {
            public Plates(PlateDetermination pdt, int pn, float cvo, int nn)
            {
                plateDeterminationType = pdt;
                plateNumber = pn;
                continentalVsOceanic = cvo;
                neighborNumber = nn;
            }

            public PlateDetermination plateDeterminationType { get; private set; }
            public int plateNumber { get; private set; }
            public float continentalVsOceanic { get; private set; }
            public int neighborNumber { get; private set; }
            public int threadNumber { get; private set; }
        }


        public class Height
        {
            public Height(int bd, float cm, float om, Gradient g)
            {
                blendDepth = bd;
                cMultiplier = cm;
                oMultiplier = om;
                gradient = g;
            }

            public int blendDepth { get; private set; }
            public float cMultiplier { get; private set; }
            public float oMultiplier { get; private set; }
            public Gradient gradient { get; private set; }
        }

        public class Moisture
        {
            public Moisture(int bd, Gradient g)
            {
                blendDepth = bd;
                gradient = g;
            }

            public int blendDepth { get; private set; }
            public Gradient gradient { get; private set; }
        }

        public class Temperature
        {
            public Temperature(Gradient g)
            {
                gradient = g;
            }

            public Gradient gradient { get; private set; }
        }

        public class Terrain
        {
            public Terrain(Gradient g)
            {
                gradient = g;
            }

            public Gradient gradient { get; private set; }
        }
    }

    private void DrawCube(Vector3 center, Vector3 scale, Transform parent)
    {
        GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
        obj.transform.position = center;
        obj.transform.localScale = scale;
        obj.transform.parent = parent;
        obj.GetComponent<MeshRenderer>().sharedMaterial = Resources.Load<Material>("Materials/WorldGen/Centers");
    }

    private void DrawLine(Vector3 pt1, Vector3 pt2, Transform parent, Color color)
    {
        GameObject obj = GameObject.Instantiate(Resources.Load<GameObject>("Prefabs/Line"));
        obj.transform.parent = parent;

        LineRenderer line = obj.GetComponent<LineRenderer>();
        line.positionCount = 2;
        line.SetPositions(new Vector3[] { pt1, pt2 });
        line.startWidth = 0.005f;
        line.endWidth = 0.005f;
        line.startColor = color;
        line.endColor = color;
    }
}