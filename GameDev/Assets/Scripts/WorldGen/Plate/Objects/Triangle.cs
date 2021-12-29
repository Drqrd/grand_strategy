using UnityEngine;

namespace WorldGeneration.TectonicPlate.Objects
{
    // Triangle Object for flood fill
    public class Triangle
    {
        public int PlateCenter { get; set; }
        public int[] Triangles { get; private set; }
        public Vector3[] Vertices { get; private set; }
        public int[] VertexIndices { get; private set; }
        public Vector3 TriangleCenter { get; private set; }
        public Triangle(int[] triangles, Vector3[] vertices, int[] vertexIndices, Vector3 triangleCenter)
        {
            Triangles = triangles;
            Vertices = vertices;
            VertexIndices = vertexIndices;
            TriangleCenter = triangleCenter;

            PlateCenter = -1;
        }
    }
}
