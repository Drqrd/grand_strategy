using UnityEngine;

// Abstract class for meshes
namespace WorldGeneration.Meshes
{
    public abstract class CustomMesh
    {
        public MeshFilter meshFilter { get; protected set; }
        public MeshFilter[] meshFilters { get; protected set; }

        protected int resolution { get; set; }
        protected float jitter { get; set; }
        public abstract void Build();

        public CustomMesh(int resolution, float jitter)
        {
            this.resolution = resolution;
            this.jitter = jitter;
        }

        protected Vector3 AddJitter(Vector3 v)
        {
            float j = jitter / Mathf.Sqrt(resolution);
            Vector3 r = new Vector3(Random.Range(-1, 1), Random.Range(-1, 1), Random.Range(-1, 1)) * Random.Range(-j, j);
            return (v + r).normalized;
        }
    }

    public class MeshData
    {
        public MeshData(Vector3[] vertices, int[] triangles, Vector3[] normals)
        {
            this.vertices = vertices;
            this.triangles = triangles;
            this.normals = normals;
        }

        public Vector3[] vertices { get; private set; }
        public int[] triangles { get; private set; }
        public Vector3[] normals { get; private set; }
    }

}

