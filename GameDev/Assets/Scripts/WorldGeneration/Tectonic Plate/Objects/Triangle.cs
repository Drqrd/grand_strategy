using UnityEngine;

namespace TectonicPlateObjects
{
    // Triangle Object for flood fill
    public class Triangle
    {
        public int PlateCenter { get; set; }
        public int[] Triangles { get; private set; }
        public Vector3[] Vertices { get; private set; }
        public Vector3 TriangleCenter { get; private set; }
        public Triangle(int[] triangles, Vector3[] vertices, Vector3 triangleCenter)
        {
            Triangles = triangles;
            Vertices = vertices;
            TriangleCenter = triangleCenter;

            PlateCenter = -1;
        }
    }
}
