using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MeshGenerator
{
    public class OctahedronSphere
    {

        public Mesh SharedMesh { get; private set; }
        private const int NUM_FACES = 8;

        private Vector3[] ups = new Vector3[] { Vector3.up, Vector3.down };
        private Vector3[] sides = new Vector3[] { Vector3.forward, Vector3.right, Vector3.back, Vector3.left, Vector3.forward };

        private MeshFilter[] meshFilters = new MeshFilter[8];
        private Transform parent;
        private int resolution;

        public OctahedronSphere(Transform parent, int resolution)
        {
            this.parent = parent;
            this.resolution = resolution;
        }

        // Constructs the mesh
        public void Build()
        {
            // For each the top and the bottom (2)
            for (int up = 0; up < ups.Length; up++)
            {
                // For each side (4)
                for (int side = 0; side < sides.Length - 1; side++)
                {
                    int index = up * ups.Length + side;

                    // GameObject for heirarchy control
                    GameObject obj = new GameObject("Face " + index);
                    obj.transform.parent = parent;

                    // Add mesh components
                    obj.AddComponent<MeshRenderer>().sharedMaterial = Resources.Load<Material>("Materials/Surface");
                    meshFilters[index] = obj.AddComponent<MeshFilter>();
                    meshFilters[index].sharedMesh = new Mesh();

                    // Draw the actual vertices and triangles
                    DrawFace(meshFilters[index].sharedMesh, ups[up], sides[side], sides[side + 1]);
                }
            }
        }

        public void DrawFace(Mesh mesh, Vector3 localUp, Vector3 localLeft, Vector3 localRight)
        {
            // Variable initialization
            Vector3[] vertices = new Vector3[CalculateVertices()];
            int[] triangles = new int[CalculateTriangles()];

            // Jagged array
            Vector3[][] tempVertices = new Vector3[resolution + 1][];
            for (int ind = 0; ind < tempVertices.Length; ind++)
            {
                tempVertices[ind] = new Vector3[ind + 1];
            }

            
            // Populate the jagged array
            float step = 1 / resolution;
            tempVertices[0][0] = localUp;
            for(int ind = 1; ind < resolution + 1; ind++)
            {
                Vector3 leftMost = localLeft * step;
                Vector3 rightMost = localRight * step;

                // Assign left and right vertices
                tempVertices[ind][0] = leftMost;
                tempVertices[ind][tempVertices[ind].Length - 1] = rightMost;

                // Populate the vertices inbetween the left and right
                for (int ind2 = 1; ind2 < tempVertices[ind].Length - 1; ind2++)
                {
                    float dist = step * ind2;
                    tempVertices[ind][ind2] = (dist * rightMost + (1 - dist) * leftMost);
                }
            }

            // Flatten jagged array into 1D
            int index = 0;
            for (int ind = 0; ind < tempVertices.Length; ind++)
            {
                for(int ind2 = 0; ind2 < tempVertices[ind].Length; ind2++)
                {
                    vertices[index] = tempVertices[ind][ind2];
                    index++;
                }
            }
            

            // Triangle
            triangles[0] = 0;
            triangles[1] = 1;
            triangles[2] = 2;

            // Assign to mesh
            mesh.vertices = vertices;
            mesh.triangles = triangles;
        }

        // Calculates vertex number based on resolution
        public int CalculateVertices()
        {
            int num = 0;
            for (int i = 1; i <= resolution + 1; i++) { num += i; }
            return num;
        }

        // Calculates triangle number based on resolution
        public int CalculateTriangles()
        {
            int num = 0;
            for(int i = 1; i <= resolution; i++) { num += (i - 1) * 2 + 1; }
            return num * 3;
        }
    }

    public class CubeSphere
    {
        private Vector3[] directions = new Vector3[6]
        {
            Vector3.up, Vector3.down, Vector3.left, Vector3.forward, Vector3.right, Vector3.back
        };
    }

    public class FibonacciSphere
    {
    }
}
