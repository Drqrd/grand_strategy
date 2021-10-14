using UnityEngine;

public class CubeMeshData
{
    private Vector3[] vertices;
    private int[] triangles;
    private Vector3[] normals;

    public CubeMeshData(Vector3[] vertices)
    {
        this.vertices = vertices;
    }

    public CubeMeshData(Vector3[] vertices, int[] triangles)
    {

    }

    public CubeMeshData(Vector3[] vertices, int[] triangles, Vector3[] normals)
    {
        this.vertices = vertices;
        this.triangles = triangles;
        this.normals = normals;
    }
}
