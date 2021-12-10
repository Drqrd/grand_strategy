using UnityEngine;

using WorldGeneration.Meshes;
using WorldGeneration.Maps;
using WorldGeneration.Objects;

using static WorldGeneration.TectonicPlate.Functions;

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

    public enum SphereType
    {
        Octahedron,
        Cube,
        Fibonacci
    }

    public enum CellType
    {
        Triangle,
        Circumcenter,
        Centroid
    }

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
        ClosestCenter,
        FloodFill
    }

    public enum BoundaryDisplay
    {
        Default,
        FaultLines,
        FaultLineType
    }

    [SerializeField] private SphereType sphereType;

    [Header("General Parameters")]
    [SerializeField] private PlateSize plateSize;
    [SerializeField] private PlateDetermination plateDeterminationType;
    [SerializeField] [Range(1, 65534)] private int resolution = 1;
    [SerializeField] private bool convertToSphere = false;

    [Header("Sphere Transformation Parameters")]
    [SerializeField] private bool smoothMapSurface = true;

    [Header("Fibonacci Exclusive Parameters")]
    [SerializeField] [Range(0f, 1f)] private float jitter = 0;
    // [SerializeField] private CellType cellType = CellType.Triangle;
    [SerializeField] private bool alterFibonacciLattice = true;

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
    [SerializeField] private Gradient continental;
    [SerializeField] private Gradient oceanic;
    [SerializeField] private Gradient heightMapGradient;

    private HeightMap heightMap;
    private TectonicPlateMap plateMap;
    private MoistureMap moistureMap;
    private TemperatureMap temperatureMap;

    private float[] distBetweenCenters = new float[] { .2f, .5f, .7f, .9f, 1.2f };

    // Public properties from parameters
    public PlateSize _PlateSize { get { return plateSize; } }
    public PlateDetermination PlateDeterminationType { get { return plateDeterminationType; } }
    public BoundaryDisplay _BoundaryDisplay { get { return boundaryDisplay; } }
    public bool SmoothMapSurface { get { return smoothMapSurface; } }
    public Gradient Continental { get { return continental; } }
    public Gradient Oceanic { get { return oceanic; } }
    public Gradient HeightMapGradient { get { return heightMapGradient; } }
    public float[] DistBetweenCenters { get { return distBetweenCenters; } }

    // Pure properties
    public Vector3[] PlateCenters { get; set; }
    public Plate[] Plates { get; private set; }
    public CustomMesh Sphere { get; private set; }

    /* ------------------------------------------------------------------------------------------------------------------------------------------- */

    // Start is called before the first frame updates
    private void Start()
    {
        BuildMesh();

        if (sphereType != SphereType.Octahedron)
        {
            BuildMaps();

            // If the display is the last value, make prev - 1, otherwise + 1
            previousMapDisplay = (int)mapDisplay == System.Enum.GetValues(typeof(MapDisplay)).Length ? (MapDisplay)((int)mapDisplay + 1) : (MapDisplay)((int)mapDisplay - 1);
            previousBoundaryDisplay = (int)boundaryDisplay == System.Enum.GetValues(typeof(BoundaryDisplay)).Length ? (BoundaryDisplay)((int)mapDisplay + 1) : (BoundaryDisplay)((int)mapDisplay - 1);
        }
    }

    private void Update()
    {
        if (mapDisplay != previousMapDisplay) { ChangeMapDisplay(); }
        if (boundaryDisplay != previousBoundaryDisplay && mapDisplay == MapDisplay.TectonicPlateMap) { ChangeBoundaryDisplay(); }
    }

    /* ------------------------------------------------------------------------------------------------------------------------------------------- */

    private void BuildMesh()
    {
        switch (sphereType)
        {
            case SphereType.Octahedron:
                Sphere = new OctahedronSphere(transform, resolution, convertToSphere);
                break;
            case SphereType.Cube:
                break;
            case SphereType.Fibonacci:
                Sphere = new FibonacciSphere(transform, resolution, jitter, alterFibonacciLattice);
                break;
            default:
                Sphere = null;
                break;
        }
        if (Sphere != null)
        {
            Sphere.Build();
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
        
        PlateCenters = GeneratePlateCenters(this);
        Plates = GeneratePlates(this);

        GenerateFaultLines(this);

        BuildTectonicPlateMap();
    }

    private void BuildTectonicPlateMap()
    {
        plateMap = new TectonicPlateMap(this);
        plateMap.Build();
    }

    private void BuildHeightMap()
    {
        heightMap = new HeightMap(this);
        heightMap.Build();
    }

    private void BuildMoistureMap()
    {
        moistureMap = new MoistureMap(this);
        moistureMap.Build();
    }

    private void BuildTemperatureMap()
    {
        temperatureMap = new TemperatureMap(this);
        temperatureMap.Build();
    }

    /* ------------------------------------------------------------------------------------------------------------------------------------------- */

    private void ChangeMapDisplay()
    {
        previousMapDisplay = mapDisplay;
        foreach (Transform child in transform)
        {
            if (child.gameObject.name.Contains(mapDisplay.ToString())) { child.gameObject.SetActive(true); }
            else { child.gameObject.SetActive(false); }
        }
    }

    private void ChangeBoundaryDisplay()
    {
        previousBoundaryDisplay = boundaryDisplay;
        Transform boundaryRef = transform.Find(MapDisplay.TectonicPlateMap.ToString()).GetChild(0);

        foreach (Transform boundary in boundaryRef)
        {
            switch (boundaryDisplay)
            {
                case BoundaryDisplay.Default:
                    foreach(LineRenderer line in plateMap.Lines)
                    {
                        line.startColor = TectonicPlateMap.DefaultColor;
                        line.endColor = TectonicPlateMap.DefaultColor;
                    }
                    break;
                case BoundaryDisplay.FaultLines:
                    for (int a = 0; a < plateMap.Lines.Length; a++)
                    {
                        plateMap.Lines[a].startColor = plateMap.FaultLineColors[a];
                        plateMap.Lines[a].endColor = plateMap.FaultLineColors[a];
                    }
                    break;
                case BoundaryDisplay.FaultLineType:
                    for (int a = 0; a < plateMap.Lines.Length; a++)
                    {
                        plateMap.Lines[a].startColor = plateMap.WeightedLineColors[a];
                        plateMap.Lines[a].endColor = plateMap.WeightedLineColors[a];
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
            if (mapDisplay == MapDisplay.TectonicPlateMap && Plates != null)
            {
                Gizmos.color = Color.red;
                for (int i = 0; i < Plates.Length; i++)
                {
                    if (displayPlateDirections) { Gizmos.DrawLine(Plates[i].Center, Plates[i].Direction); }
                    if (displayPlateCenters) { Gizmos.DrawSphere(Plates[i].Center, 0.01f); }
                }
            }

            if (displayVertices)
            {
                for (int i = 0; i < Sphere.meshFilters.Length; i++)
                {
                    foreach (Vector3 v in Sphere.meshFilters[i].sharedMesh.vertices)
                    {
                        Gizmos.DrawSphere(v, 0.01f);
                    }
                }
            }
        }
    }
}
