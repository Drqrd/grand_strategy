using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MeshGenerator;

public class World : MonoBehaviour
{

    public enum SphereType
    {
        Octahedron,
        Cube,
        Fibonacci
    }

    [SerializeField] private SphereType sphereType;
    [SerializeField] private HeightMap.ContinentSize continentSize;
    [SerializeField] private int resolution = 1;
    [SerializeField] private bool convertToSphere = false;
    
    private CustomMesh sphere;
    private HeightMap heightMap;

    // Start is called before the first frame updates
    void Start()
    {
        BuildMesh();
        BuildHeightMap();
        
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

    private void BuildHeightMap()
    {
        heightMap = new HeightMap(sphere.meshFilters);
        heightMap.continentSize = continentSize;
        heightMap.Build();
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
