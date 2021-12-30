using System.Collections.Generic;
using UnityEngine;

using WorldGeneration.TectonicPlate.Objects;

// TODO: Reorder boundary Vertices
namespace WorldGeneration.Objects
{
    public class Plate
    {
        public enum TectonicPlateType
        {
            Oceanic,
            Continental
        }

        // Private set
        public Point[] Points { get; private set; }         // Vertices with neighbors
        public int[] Triangles { get; private set; }
        public Vector3 Center { get; private set; }
        public Vector3 Direction { get; private set; }
        public float Speed { get; private set; }

        public Color[] Colors { get; private set; }
        public Mesh SharedMesh { get; private set; }
        public TectonicPlateType PlateType { get; private set; }


        // Object References
        public FaultLine[] FaultLines { get; set; }

        // Constants
        private const float MIN_SPEED = 0.01f;
        private const float MAX_SPEED = 2.0f;

        // Constructor
        public Plate(Vector3 center, int id, Vector3[] vertices = null, int[] vIndices = null, int[] triangles = null, Color[] colors = null)
        {
            Center = center;
            Points = new Point[vertices.Length];
            for (int a = 0; a < Points.Length; a++) { Points[a] = new Point(vertices[a], id, vIndices[a]); }
            Triangles = triangles;
            if (colors != null) { Colors = colors; }

            SharedMesh = new Mesh();

            // Random assign of direction
            Direction = Vector3.Lerp(Center, (GetRandomDirection() + Center), .15f) * 1.001f;

            // Random assign speed
            Speed = GetRandomSpeed();

            // Random assign if continental or oceanic
            PlateType = Random.Range(0f, 1f) > 0.5f ? TectonicPlateType.Continental : TectonicPlateType.Oceanic;

            // Build mesh if vertices and triangles arent null
            if (vertices != null && Triangles != null)
            {
                SharedMesh.vertices = vertices;
                SharedMesh.triangles = Triangles;
                if (colors != null) { SharedMesh.colors = Colors; }
                SharedMesh.RecalculateNormals();
                SharedMesh.Optimize();
            }
        }

        /* Constructor Functions */
        private Vector3 GetRandomDirection()
        {
            Vector3 tangent = Vector3.Cross(Center, Vector3.up);
            if (tangent.sqrMagnitude < float.Epsilon) { tangent = Vector3.Cross(Center, Vector3.forward); }
            tangent.Normalize();

            Quaternion rotation = Quaternion.AngleAxis(Random.Range(0f, 360f), Center);
            return rotation * tangent;
        }

        private static float GetRandomSpeed()
        {
            return Random.Range(MIN_SPEED, MAX_SPEED);
        }

        /*------------------------------------------------------------------------------------*/

        /* Public Functions */

        // Sets color of the entire mesh (mono)
        public void SetColors(Color color)
        {
            List<Color> colors = new List<Color>();
            for (int i = 0; i < Points.Length; i++) { colors.Add(color); }
            Colors = colors.ToArray();

            SharedMesh.colors = Colors;
        }

        public void FindNearestNeighbors()
        {

        }
    }
}