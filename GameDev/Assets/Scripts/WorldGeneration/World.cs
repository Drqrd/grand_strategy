using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MeshGenerator;
using MapGenerator;

// TODO: Create a get seed function to input into the map generators

public class World : MonoBehaviour
{

    public enum SphereType
    {
        Octahedron,
        Cube,
        Fibonacci
    }

    public enum MapDisplay
    {
        Mesh,
        Terrain,
        HeightMap,
        MoistureMap,
        TemperatureMap
    }

    [Header("Parameters")]
    [SerializeField] private SphereType sphereType;
    [SerializeField] private HeightMap.ContinentSize continentSize;
    [SerializeField] private int resolution = 1;
    [SerializeField] private bool convertToSphere = false;

    [Header("Display")]
    [SerializeField] private MapDisplay mapDisplay;
    private MapDisplay previousDisplay;

    private CustomMesh sphere;
    private HeightMap heightMap;

    // Start is called before the first frame updates
    private void Start()
    {
        BuildMesh();
        BuildMaps();
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
                sphere = new FibonacciSphere(transform, resolution, convertToSphere);
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
        BuildHeightMap();
        BuildMoistureMap();
        BuildTemperatureMap();
    }

    private void BuildHeightMap()
    {
        heightMap = new HeightMap(transform, sphere.meshFilters);
        heightMap.continentSize = continentSize;
        heightMap.Build();
    }

    private void BuildMoistureMap()
    {

    }

    private void BuildTemperatureMap()
    {

    }

    private void ChangeMap()
    {
        switch (mapDisplay)
        {
            case MapDisplay.Mesh:
                previousDisplay = MapDisplay.Mesh;
                transform.Find("Mesh").gameObject.SetActive(true);
                transform.Find("Terrain").gameObject.SetActive(false);
                transform.Find("Height Map").gameObject.SetActive(false);
                transform.Find("Moisture Map").gameObject.SetActive(false);
                transform.Find("Temperature Map").gameObject.SetActive(false);
                break;
            case MapDisplay.Terrain:
                previousDisplay = MapDisplay.Terrain;
                transform.Find("Mesh").gameObject.SetActive(false);
                transform.Find("Terrain").gameObject.SetActive(true);
                transform.Find("Height Map").gameObject.SetActive(false);
                transform.Find("Moisture Map").gameObject.SetActive(false);
                transform.Find("Temperature Map").gameObject.SetActive(false);
                break;
            case MapDisplay.HeightMap:
                previousDisplay = MapDisplay.Mesh;
                transform.Find("Mesh").gameObject.SetActive(false);
                transform.Find("Terrain").gameObject.SetActive(false);
                transform.Find("Height Map").gameObject.SetActive(true);
                transform.Find("Moisture Map").gameObject.SetActive(false);
                transform.Find("Temperature Map").gameObject.SetActive(false);
                break;
            case MapDisplay.MoistureMap:
                previousDisplay = MapDisplay.Mesh;
                transform.Find("Mesh").gameObject.SetActive(false);
                transform.Find("Terrain").gameObject.SetActive(false);
                transform.Find("Height Map").gameObject.SetActive(false);
                transform.Find("Moisture Map").gameObject.SetActive(true);
                transform.Find("Temperature Map").gameObject.SetActive(false);
                break;
            case MapDisplay.TemperatureMap:
                previousDisplay = MapDisplay.Mesh;
                transform.Find("Mesh").gameObject.SetActive(false);
                transform.Find("Terrain").gameObject.SetActive(false);
                transform.Find("Height Map").gameObject.SetActive(false);
                transform.Find("Moisture Map").gameObject.SetActive(true);
                transform.Find("Temperature Map").gameObject.SetActive(false);
                break;
            default:
                previousDisplay = MapDisplay.Mesh;
                transform.Find("Mesh").gameObject.SetActive(true);
                transform.Find("Terrain").gameObject.SetActive(false);
                transform.Find("Height Map").gameObject.SetActive(false);
                transform.Find("Moisture Map").gameObject.SetActive(false);
                transform.Find("Temperature Map").gameObject.SetActive(false);
                break;
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (heightMap != null)
        {
            foreach (Vector3 center in heightMap.continentCenters)
            {
                Gizmos.color = Color.blue;
                Gizmos.DrawSphere(center, .01f);
            }
        }
    }
}
