using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MeshGenerator;

public class World : MonoBehaviour
{
    [SerializeField] private int resolution = 1;
    [SerializeField] private bool convertToSphere = false;

    CustomMesh sphere;


    // Start is called before the first frame updates
    void Start()
    {
        sphere = new FibonacciSphere(transform, resolution, convertToSphere);
        sphere.Build();

        // GetComponent<MeshFilter>().sharedMesh = sphere.SharedMesh;
    }
}
