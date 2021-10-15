using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MeshGenerator;

public class World : MonoBehaviour
{
    [Range(1,1000)]
    [SerializeField] private int resolution = 1;
    [SerializeField] private bool convertToSphere = false;

    // Start is called before the first frame updates
    void Start()
    {
        OctahedronSphere sphere = new OctahedronSphere(transform, resolution, convertToSphere);
        sphere.Build();

        // GetComponent<MeshFilter>().sharedMesh = sphere.SharedMesh;
    }
}
