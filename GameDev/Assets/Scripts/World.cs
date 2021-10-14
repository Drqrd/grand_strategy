using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MeshGenerator;


[RequireComponent(typeof(MeshRenderer))]
public class World : MonoBehaviour
{
    [Range(1,100)]
    [SerializeField] private int resolution = 1;

    // Start is called before the first frame updates
    void Start()
    {
        OctahedronSphere sphere = new OctahedronSphere(transform, resolution);
        sphere.Build();

        // GetComponent<MeshFilter>().sharedMesh = sphere.SharedMesh;
    }
}
