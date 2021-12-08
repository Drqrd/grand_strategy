using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using MeshGenerator;
using MapGenerator;

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

    public float[] distBetweenCenters = new float[] { .2f, .5f, .7f, .9f, 1.2f };

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
        All,
        Neighbors,
        Weighted
    }

    [SerializeField] private SphereType sphereType;

    [Header("General Parameters")]
    [SerializeField] public PlateSize plateSize;
    [SerializeField] public PlateDetermination plateDeterminationType;
    [SerializeField] [Range(1, 65534)] private int resolution = 1;
    [SerializeField] private bool convertToSphere = false;
    [SerializeField] public bool smoothMapSurface = true;
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
    [SerializeField] public Gradient continental;
    [SerializeField] public Gradient oceanic;

    public Vector3[] plateCenters;
    public TectonicPlate[] plates;
    public CustomMesh sphere;
    private HeightMap heightMap;
    private TectonicPlateMap plateMap;
    private MoistureMap moistureMap;
    private TemperatureMap temperatureMap;

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
        plateCenters = GeneratePlateCenters(this);
        plates = GeneratePlates(plateCenters, this);

        int ind = 0;
        foreach (TectonicPlate plate in plates)
        {
            ind += plate.BoundaryEdges.Length;
        }
        Debug.Log(ind);

        BuildTectonicPlateMap();
    }

    private void BuildTectonicPlateMap()
    {
        plateMap = new TectonicPlateMap(transform, plates, boundaryDisplay);
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
        Transform plateRef = transform.Find(MapDisplay.Plates.ToString());
        foreach (Transform plate in plateRef)
        {
            Transform boundaryRef = plate.Find("Boundary");
            foreach (Transform boundary in boundaryRef)
            {
                if (boundary.name.Contains(boundaryDisplay.ToString())) { boundary.gameObject.SetActive(true); }
                else { boundary.gameObject.SetActive(false); }
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
