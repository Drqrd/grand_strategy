using UnityEngine;

/*
 *  
 * Octahedron Sphere triangulation
 * - https://www.youtube.com/watch?v=lctXaT9pxA0&t=194s&ab_channel=SebastianLague
 * 
 */

namespace WorldGeneration.Meshes
{
    public class OctahedronSphere : CustomMesh
    {
        private Vector3[] ups = new Vector3[] { Vector3.up, Vector3.down };
        private Vector3[] sides = new Vector3[] { Vector3.forward, Vector3.right, Vector3.back, Vector3.left, Vector3.forward };

        private Transform parent;
        private bool normalize;

        public OctahedronSphere(Transform parent, int resolution, float jitter, bool normalize = true) : base(resolution, jitter)
        {
            this.parent = parent;
            this.resolution = resolution;
            this.normalize = normalize;
            meshFilters = new MeshFilter[8];
        }

        // Constructs the mesh
        public override void Build()
        {
            GameObject parentObj = new GameObject("Mesh");
            parentObj.transform.parent = parent;

            // For each the top and the bottom (2)
            for (int up = 0; up < ups.Length; up++)
            {
                // For each side (4)
                for (int side = 0; side < sides.Length - 1; side++)
                {
                    int index = up * (sides.Length - 1) + side;

                    // GameObject for heirarchy control
                    GameObject obj = new GameObject("Face " + index);
                    obj.transform.parent = parentObj.transform;

                    // Add mesh components
                    obj.AddComponent<MeshRenderer>().sharedMaterial = Resources.Load<Material>("Materials/Surface");
                    meshFilters[index] = obj.AddComponent<MeshFilter>();
                    meshFilters[index].sharedMesh = new Mesh();

                    // Draw the actual vertices and triangles
                    DrawFace(meshFilters[index].sharedMesh, ups[up], sides[side], sides[side + 1]);
                    meshFilters[index].sharedMesh.RecalculateNormals();
                    meshFilters[index].sharedMesh.Optimize();
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
            float step = 1f / resolution;
            tempVertices[0][0] = localUp;
            for (int ind = 1; ind < tempVertices.Length; ind++)
            {
                float dist1 = step * ind;
                Vector3 leftMost = (dist1 * localLeft + (1 - dist1) * localUp);
                Vector3 rightMost = (dist1 * localRight + (1 - dist1) * localUp);

                // Assign left and right vertices
                tempVertices[ind][0] = leftMost;
                tempVertices[ind][tempVertices[ind].Length - 1] = rightMost;

                // Populate the vertices inbetween the left and right
                float step2 = 1f / (tempVertices[ind].Length - 1);
                for (int ind2 = 1; ind2 < tempVertices[ind].Length - 1; ind2++)
                {
                    float dist2 = step2 * ind2;
                    tempVertices[ind][ind2] = (dist2 * rightMost + (1 - dist2) * leftMost);
                }
            }

            // Flatten jagged array into 1D
            int index = 0;
            for (int ind = 0; ind < tempVertices.Length; ind++)
            {
                for (int ind2 = 0; ind2 < tempVertices[ind].Length; ind2++)
                {
                    // Assign vertices
                    vertices[index] = (normalize) ? tempVertices[ind][ind2].normalized : tempVertices[ind][ind2];
                    index++;
                }
            }

            // Trianglulation
            int triIndex = 0;
            for (int row = 0; row < tempVertices.Length - 1; row++)
            {
                int topVertex = ((row + 1) * (row + 1) - row - 1) / 2;
                int bottomVertex = ((row + 2) * (row + 2) - row - 2) / 2;

                int numTrianglesInRow = 1 + 2 * row;
                for (int column = 0; column < numTrianglesInRow; column++)
                {
                    int v1, v2, v3;

                    if (column % 2 == 0)
                    {
                        v1 = topVertex;
                        v2 = bottomVertex + 1;
                        v3 = bottomVertex;

                        topVertex++;
                        bottomVertex++;
                    }
                    else
                    {
                        v1 = topVertex;
                        v2 = bottomVertex;
                        v3 = topVertex - 1;
                    }

                    triangles[triIndex + 0] = v1;
                    triangles[triIndex + 1] = (localUp.y > 0f) ? v3 : v2;
                    triangles[triIndex + 2] = (localUp.y > 0f) ? v2 : v3;

                    triIndex += 3;
                }
            }

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
            for (int i = 1; i <= resolution; i++) { num += (i - 1) * 2 + 1; }
            return num * 3;
        }
    }
}
