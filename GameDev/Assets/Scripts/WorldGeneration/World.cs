using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using MeshGenerator;
using MapGenerator;

using TectonicPlateObjects;
using static WorldGeneration.TP;

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

    public enum MapDisplay
    {
        Mesh,
        Plates,
        Terrain,
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
        Weighted
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
    private MapDisplay previousMapDisplay;

    [SerializeField] private BoundaryDisplay boundaryDisplay;
    private BoundaryDisplay previousBoundaryDisplay;

    [Header("Gradients")]
    [SerializeField] private Gradient continental;
    [SerializeField] private Gradient oceanic;

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
    public float[] DistBetweenCenters { get { return distBetweenCenters; } }

    // Pure properties
    public Vector3[] PlateCenters { get; set; }
    public TectonicPlate[] Plates { get; private set; }
    public CustomMesh Sphere { get; private set; }
    public TectonicPlateBoundaries PlateBoundaries { get; private set; }

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
            if (boundaryDisplay != previousBoundaryDisplay && mapDisplay == MapDisplay.Plates) { ChangeBoundaryDisplay(); }
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
        Edge[] edges = GetBoundaryEdges(this);

        PlateBoundaries = new TectonicPlateBoundaries(edges, GetUniqueEdges(edges));

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
        Transform boundaryRef = transform.Find(MapDisplay.Plates.ToString()).GetChild(0);

        foreach (Transform boundary in boundaryRef)
        {
            switch (boundaryDisplay)
            {
                case BoundaryDisplay.Default:
                    foreach(LineRenderer line in plateMap.Lines)
                    {
                        line.startColor = PlateBoundaries.DefaultColor;
                        line.endColor = PlateBoundaries.DefaultColor;
                    }
                    break;
                case BoundaryDisplay.FaultLines:
                    for (int a = 0; a < plateMap.Lines.Length; a++)
                    {
                        plateMap.Lines[a].startColor = PlateBoundaries.FaultColors[a];
                        plateMap.Lines[a].endColor = PlateBoundaries.FaultColors[a];
                    }
                    break;
                case BoundaryDisplay.Weighted:
                    break;
                default:
                    break;
            }
        }
    }

    /* ------------------------------------------------------------------------------------------------------------------------------------------- */

    private void OnDrawGizmosSelected()
    {
        if (Application.isPlaying)
        {
            if (mapDisplay == MapDisplay.Plates && Plates != null)
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

    /* ------------------------------------------------------------------------------------------------------------------------------------------- */

    // For the boundaries
    public class TectonicPlateBoundaries
    {
        // Boundary Properties
        public Edge[] Edges { get; private set; }
        public Color DefaultColor { get; private set; }
        public Color[] FaultColors { get; private set; }
        public Color[] WeightedFaultColors { get; private set; }

        // Constructors
        public TectonicPlateBoundaries(Edge[] Edges, int uniqueEdges)
        {
            this.Edges = Edges;
            this.DefaultColor = Color.red;
            this.FaultColors = new Color[Edges.Length];
            this.WeightedFaultColors = new Color[Edges.Length];

            GenerateColors(uniqueEdges);
        }

        private void GenerateColors(int uniqueEdges)
        {
            Color[] uniqueColors = new Color[uniqueEdges];

            for (int a = 0; a < uniqueEdges; a++)
            {
                // Generate a random color
                uniqueColors[a] = Random.ColorHSV();

                // Make sure the colors are different from previous colors
                for(int b = 0; b < a; b++)
                {
                    // If color is the same...
                    if (IMath.ColorApproximately(uniqueColors[a], uniqueColors[b]))
                    {
                        // Generate a new random color
                        uniqueColors[a] = Random.ColorHSV();
                        // Compare again
                        b = 0;
                    }
                }
            }

            // Assign the colors
            int ind = 0;
            int[] curr = Edges[0].edgeOf;
            FaultColors[0] = uniqueColors[ind];
            for(int a = 1; a < FaultColors.Length; a++)
            {
                if (Edges[a].edgeOf[0] != curr[0] || Edges[a].edgeOf[1] != curr[1])
                {
                    curr = Edges[a].edgeOf;
                    ind += 1;
                }
                FaultColors[a] = uniqueColors[ind];
            }
        }

        public void SetFaultColors(Color[] FaultColors)
        {
            if (FaultColors.Length == this.FaultColors.Length) { this.FaultColors = FaultColors; }
            else { Debug.LogError("INVALID SET."); }
        }

        public void SetWeightedFaultColors(Color[] WeightedFaultColors)
        {
            if (WeightedFaultColors.Length == this.WeightedFaultColors.Length) { this.WeightedFaultColors = WeightedFaultColors; }
            else { Debug.LogError("INVALID SET."); }
        }
    }
}
