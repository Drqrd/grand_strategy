using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TectonicPlate
{
    public Vector3[] Vertices { get { return vertices; } set { vertices = value; } }
    public int[] Triangles { get { return triangles; } set { triangles = value; } }
    public Vector3[] Directions { get { return directions; } set { directions = value; } }
    public Color[] Colors { get { return colors; } set { colors = value; } }
    public Mesh SharedMesh { get { return mesh; } set { mesh = value; } }

    private Vector3[] vertices;
    private int[] triangles;
    private Vector3[] directions;
    private Vector3 center;
    private Color[] colors;

    private Mesh mesh;

    // Mapping parts
    private float height, moisture, temperature;

    public TectonicPlate(Vector3 center, Vector3[] vertices = null, int[] triangles = null, Color[] colors = null)
    {
        this.center = center;
        this.vertices = vertices;
        this.triangles = triangles;
        this.colors = colors;

        mesh = new Mesh();

        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.colors = colors;
        mesh.RecalculateNormals();
        mesh.Optimize();
    }
}
