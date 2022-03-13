using UnityEngine;

using WorldGeneration.Meshes;
using WorldGeneration.Maps;

using System.Collections.Generic;

using DelaunatorSharp;
using DataStructures.ViliWonka.KDTree;

using static WorldData;

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

    [Header("Debug")]
    public bool alter;

    [Header("General Parameters")]
    [SerializeField] private PlateSize plateSize;
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
    /*
    private void Update()
    {
        if (mapDisplay != previousMapDisplay) { ChangeMapDisplay(); }
        if (boundaryDisplay != previousBoundaryDisplay && mapDisplay == MapDisplay.TectonicPlateMap) { ChangeBoundaryDisplay(); }
    }
    */

    /* ------------------------------------------------------------------------------------------------------------------------------------------- */
    
    // Generate world from button press
    public void GenerateWorld()
    {
        while (transform.childCount > 0) { DestroyImmediate(transform.GetChild(0).gameObject); }

        InitializeParameterProperties();

        worldData = new WorldData();

        BuildMesh();

        transform.GetChild(0).gameObject.SetActive(false);

        TestDelaunay();

        // GenerateSaveData();
        // BuildMaps();

        // If the display is the last value, make prev - 1, otherwise + 1
        previousMapDisplay = (int)mapDisplay == System.Enum.GetValues(typeof(MapDisplay)).Length ? (MapDisplay)((int)mapDisplay + 1) : (MapDisplay)((int)mapDisplay - 1);
        previousBoundaryDisplay = (int)boundaryDisplay == System.Enum.GetValues(typeof(BoundaryDisplay)).Length ? (BoundaryDisplay)((int)mapDisplay + 1) : (BoundaryDisplay)((int)mapDisplay - 1);
    }

    // Loading from saved data
    public void LoadWorld(SaveData saveData)
    {
        BuildMaps(saveData);
    }

    /* ------------------------------------------------------------------------------------------------------------------------------------------- */
    private void InitializeParameterProperties()
    {
        parameters = new Parameters(resolution);
        parameters.customMesh = new Parameters.CustomMesh(chunks);
        parameters.plates = new Parameters.Plates(plateDeterminationType, plateSize, continentalVsOceanic, neighborNumber);
        parameters.height = new Parameters.Height(h_blendDepth, continentHeightMutiplier, oceanDepthMultiplier, heightMapGradient);
        parameters.moisture = new Parameters.Moisture(m_blendDepth, moistureMapGradient);
        parameters.temperature = new Parameters.Temperature(temperatureMapGradient);
        parameters.terrain = new Parameters.Terrain(terrainMapGradient);
    }


    private void BuildMesh()
    {
        Sphere = new FibonacciSphere(transform, resolution, jitter, alterFibonacciLattice);
        // Sphere = new CubeSphere(transform, resolution, jitter);
        Sphere.Build();
    }

    private void TestDelaunay()
    {
        WorldData.DelaunayData dd = worldData.delaunayData;
        float scale = 0.01f;
        Vector3 cubeSize = new Vector3(scale,scale,scale);

        GameObject triangleCenters = new GameObject("Triangle Centers");
        triangleCenters.transform.parent = this.transform;

        for(int a = 0; a < dd.triangleCenters.Length; a++)
        {
            DrawCube(dd.triangleCenters[a], cubeSize, triangleCenters.transform);
        }

        GameObject voronoiPoints = new GameObject("Voronoi Points");
        voronoiPoints.transform.parent = this.transform;
        for(int a = 0; a < dd.voronoiPoints.Length; a++)
        {
            DrawCube(dd.voronoiPoints[a], cubeSize, voronoiPoints.transform);
        }

        GameObject triangleLines = new GameObject("Triangle Lines");
        triangleLines.transform.parent = this.transform;

        for(int a = 0; a < dd.triangleEdges.Length; a++)
        {
            DrawLine(dd.triangleEdges[a][0], dd.triangleEdges[a][1], triangleLines.transform, Color.gray);
        }

        GameObject voronoiLines = new GameObject("Voronoi Lines");
        voronoiLines.transform.parent = this.transform;

        for(int a = 0; a < dd.voronoiEdges.Length; a++)
        {
            DrawLine(dd.voronoiEdges[a][0], dd.voronoiEdges[a][1], voronoiLines.transform, Color.blue);
        }

        GameObject debugCenters = new GameObject("Debug Centers");
        debugCenters.transform.parent = this.transform;

        for(int a = 0; a < dd.debug.Length; a++)
        {
            DrawCube(dd.debug[a], cubeSize, debugCenters.transform);
        }
        for(int a = 0; a < dd.debugVoronoi.Length; a++)
        {
            DrawCube(dd.debugVoronoi[a], cubeSize * 3f, debugCenters.transform);
        }


        GameObject finalCell = new GameObject("Final Cell");
        finalCell.transform.parent = this.transform;

        for(int a = 0; a < dd.finalCell.Length; a++)
        {
            DrawLine(dd.finalCell[a][0], dd.finalCell[a][1], finalCell.transform, Color.green);
        }

        GameObject newEdges = new GameObject("New Voronoi Edges");
        newEdges.transform.parent = this.transform;
        for(int a = 0; a < dd.debugNewVoronoiEdges.Length; a++)
        {
            DrawLine(dd.debugNewVoronoiEdges[a][0], dd.debugNewVoronoiEdges[a][1], newEdges.transform, Color.cyan);
        }

        
    }

    private void GenerateSaveData()
    {
        worldData = new WorldData();

        Mesh mesh = Sphere.meshFilter.sharedMesh;
        worldData.meshData = new WorldData.MeshData(mesh.vertices, mesh.triangles, mesh.normals);

        // GeneratePoints();
    }

    /*
    private void GeneratePoints()
    {
        Point[] points = new Point[worldData.meshData.vertices.Length];
        for (int a = 0; a < worldData.meshData.vertices.Length; a++)
        {
            points[a] = new Point(worldData.meshData.vertices[a], a);
        }

        worldData.points = points;

        // KD - Tree to find neighbors
        KDTree kdTree = new KDTree(worldData.points, 1);

        KDQuery query = new KDQuery();

        foreach (Point point in points)
        {
            List<int> results = new List<int>();
            query.KNearest(kdTree, point.vertex, neighborNumber, results);

            List<Point> nn = new List<Point>();
            foreach (int ind in results)
            {
                nn.Add(points[ind]);
            }

            point.neighbors = nn.ToArray();
        }
    }
    */

    private void BuildMaps(SaveData saveData = null)
    {
        /*
        BuildTectonicPlateMap(saveData);
        BuildHeightMap(saveData);
        BuildMoistureMap(saveData);
        BuildTemperatureMap(saveData);
        BuildTerrainMap(saveData);
        */
    }

    private void BuildTectonicPlateMap(SaveData saveData = null)
    {
        plateMap = new TectonicPlateMap(this, saveData);
        if (saveData == null) { plateMap.Build(); }
        else { plateMap.Load(); }
    }
   
    private void BuildHeightMap(SaveData saveData = null)
    {
       heightMap = new HeightMap(this, saveData);
       if (saveData == null) { heightMap.Build(); }
       else { heightMap.Load(); }
    }


    private void BuildMoistureMap(SaveData saveData = null)
    {
        moistureMap = new MoistureMap(this, saveData);
        moistureMap.Build();
    }

    private void BuildTemperatureMap(SaveData saveData = null)
    {
        temperatureMap = new TemperatureMap(this, saveData);
        temperatureMap.Build();
    }

    private void BuildTerrainMap(SaveData saveData = null)
    {
        terrainMap = new TerrainMap(this, saveData);
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
                    if (displayPlateDirections) { Gizmos.DrawLine(worldData.plates[i].center, worldData.plates[i].direction); }
                    if (displayPlateCenters) { Gizmos.DrawCube(worldData.plates[i].center, new Vector3(0.1f,0.1f,0.1f)); }
                }
            }

            if (displayVertices)
            {
                for (int i = 0; i < worldData.meshData.vertices.Length; i++)
                {
                    foreach (Vector3 v in worldData.meshData.vertices)
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
            public Plates(PlateDetermination pdt, PlateSize pz, float cvo, int nn)
            {
                plateDeterminationType = pdt;
                plateSize = pz;
                continentalVsOceanic = cvo;
                neighborNumber = nn;
            }

            public PlateDetermination plateDeterminationType { get; private set; }
            public PlateSize plateSize { get; private set; }
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