using System.Collections.Generic;
using UnityEngine;

/*
 * Fibonacci Distribution on Sphere
 * - https://medium.com/@vagnerseibert/distributing-points-on-a-sphere-6b593cc05b42
 * 
 * Delaunay triangulation on sphere
 * - https://fsu.digital.flvc.org/islandora/object/fsu:182663/datastream/PDF/view
 * - https://en.wikipedia.org/wiki/Bowyer%E2%80%93Watson_algorithm
 *
 * Octahedron Sphere triangulation
 * - https://www.youtube.com/watch?v=lctXaT9pxA0&t=194s&ab_channel=SebastianLague
*/

// TODO Fix the fibonacci delaunay triangulation. Probably broken in the conversion of 3d to 2d points

namespace MeshGenerator
{
    public abstract class CustomMesh
    {
        public MeshFilter[] meshFilters { get; protected set; }
        public abstract void Build();  
    }

    public class OctahedronSphere : CustomMesh
    {
        private Vector3[] ups   = new Vector3[] { Vector3.up, Vector3.down };
        private Vector3[] sides = new Vector3[] { Vector3.forward, Vector3.right, Vector3.back, Vector3.left, Vector3.forward };

        private Transform parent;
        private int resolution;
        private bool normalize;

        public OctahedronSphere(Transform parent, int resolution, bool normalize = true)
        {
            this.parent     = parent;
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
                    GameObject obj       = new GameObject("Face " + index);
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
            int[] triangles    = new int[CalculateTriangles()];

            // Jagged array
            Vector3[][] tempVertices = new Vector3[resolution + 1][];
            for (int ind = 0; ind < tempVertices.Length; ind++)
            {
                tempVertices[ind] = new Vector3[ind + 1];
            }
            
            // Populate the jagged array
            float step         = 1f / resolution;
            tempVertices[0][0] = localUp;
            for(int ind = 1; ind < tempVertices.Length; ind++)
            {
                float dist1       = step * ind;
                Vector3 leftMost  = (dist1 * localLeft + (1 - dist1) * localUp);
                Vector3 rightMost = (dist1 * localRight + (1 - dist1) * localUp);
                
                // Assign left and right vertices
                tempVertices[ind][0]                            = leftMost;
                tempVertices[ind][tempVertices[ind].Length - 1] = rightMost;

                // Populate the vertices inbetween the left and right
                float step2 =  1f / (tempVertices[ind].Length - 1);
                for (int ind2 = 1; ind2 < tempVertices[ind].Length - 1; ind2++)
                {
                    float dist2             = step2 * ind2;
                    tempVertices[ind][ind2] = (dist2 * rightMost + (1 - dist2) * leftMost);
                }
            }

            // Flatten jagged array into 1D
            int index = 0;
            for (int ind = 0; ind < tempVertices.Length; ind++)
            {
                for(int ind2 = 0; ind2 < tempVertices[ind].Length; ind2++)
                {
                    // Assign vertices
                    vertices[index] = (normalize) ? tempVertices[ind][ind2].normalized : tempVertices[ind][ind2];
                    index++;
                }
            }

            // Triangle
            int triIndex = 0;
            for(int row = 0; row < tempVertices.Length - 1; row++)
            {
                int topVertex    = ((row + 1) * (row + 1) - row - 1) / 2;
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
            for(int i = 1; i <= resolution; i++) { num += (i - 1) * 2 + 1; }
            return num * 3;
        }
    }



// --------------------------------------------------------------------------------------------------------------------------------------



    public class CubeSphere
    {
        private Vector3[] directions = new Vector3[6]
        {
            Vector3.up, Vector3.down, Vector3.left, Vector3.forward, Vector3.right, Vector3.back
        };
    }



// --------------------------------------------------------------------------------------------------------------------------------------



    public class FibonacciSphere : CustomMesh
    {
        private Transform parent;
        private int numPoints;
        private bool normalize;

        public FibonacciSphere(Transform parent, int numPoints, bool normalize = true)
        {
            this.parent = parent;
            this.numPoints = numPoints;
            this.normalize = normalize;
            meshFilters = new MeshFilter[1];
        }

        public override void Build()
        {
            GameObject meshObj = new GameObject("Mesh");
            meshObj.transform.parent = parent;

            Vector3[] vertices = new Vector3[numPoints];

            // Easy vertices using golden ratio
            for (int i = 0; i < numPoints; i++)
            {
                float k = i + .5f;

                float phi = Mathf.Acos(1f - 2f * k / numPoints);
                float theta = Mathf.PI * (1 + Mathf.Sqrt(5)) * k;

                float x = Mathf.Cos(theta) * Mathf.Sin(phi);
                float y = Mathf.Sin(theta) * Mathf.Sin(phi);
                float z = Mathf.Cos(phi);

                vertices[i] = new Vector3(x, y, z);
            }

            meshObj.AddComponent<MeshRenderer>().sharedMaterial = Resources.Load<Material>("Materials/Surface");
            meshFilters[0] = meshObj.AddComponent<MeshFilter>();
            meshFilters[0].sharedMesh = new Mesh();

            meshFilters[0].sharedMesh.vertices = vertices;

            // Delaunay Triangulation
            // Approach: divide sphere into two hemispheres, flatten, triangulate, stitch the sides
            Triangulate(meshFilters[0].sharedMesh);

            meshFilters[0].sharedMesh.RecalculateNormals();
            meshFilters[0].sharedMesh.Optimize();
        }

        private void Triangulate(Mesh mesh)
        {
            Vector3[] vertices = mesh.vertices;
            List<int[]> triangles = new List<int[]>();


            // Project onto plane
            Vector2[] planarProjection = new Vector2[vertices.Length + 3];
            for (int i = 0; i < vertices.Length; i++) { planarProjection[i] = SphereToPlane(vertices[i]); }

            // Add the "infinitely" large triangle that contains all points
            Vector2[] superTriangle = GetSuperTriangle(planarProjection);
            planarProjection[vertices.Length + 0] = superTriangle[0];
            planarProjection[vertices.Length + 1] = superTriangle[1];
            planarProjection[vertices.Length + 2] = superTriangle[2];
            triangles.Add(new int[] { vertices.Length + 0, vertices.Length + 1, vertices.Length + 2 });

            // Triangulate
            // For all real vertices, add to triangles
            for (int i = 0; i < vertices.Length; i++)
            {
                List<int[]> badTriangles = new List<int[]>();

                for(int tri = 0; tri < triangles.Count; tri++)
                {
                    // Current triangle
                    int[] triangle = triangles[tri];

                    // If the point is in the triangle, add the triangle to the bad triangles
                    if (TriangleContainsPoint(planarProjection, triangle, i))
                    {
                        badTriangles.Add(triangle);
                    }
                }

                List<int[]> polygon = new List<int[]>();

                // Get the polygon formed from the edges of bad triangles that enclose the point
                for (int tri = 0; tri < badTriangles.Count; tri++)
                {
                    int[][] edges = new int[3][] { new int[]{ badTriangles[tri][0], badTriangles[tri][1] },
                                                   new int[]{ badTriangles[tri][1], badTriangles[tri][2] },
                                                   new int[]{ badTriangles[tri][2], badTriangles[tri][0] }};

                    // For each edge in triangle, if the edge is not shared by any other triangle in badTriangles,
                    // Add to the polygon list
                    for (int e = 0; e < edges.Length; e++)
                    {
                        if (!EdgeIsShared(edges[e], badTriangles, tri)) { polygon.Add(edges[e]); }
                    }
                }

                // Remove the bad triangles from the triangles list
                foreach(int[] triangle in badTriangles) { triangles.Remove(triangle); }

                
                // Re-triangulate using polygon edges (clockwise)
                foreach (int[] edge in polygon)
                {
                    List<int> verts = new List<int>();
                    verts.Add(edge[0]);
                    verts.Add(edge[1]);
                    verts.Add(i);
                    // Edge = 2 vertex indices, i = current vertex index. a = highest, b = rightmost, c = leftmost

                    // Find greatest y
                    int a = planarProjection[edge[0]].y > planarProjection[edge[1]].y ? edge[0] : edge[1];
                    a = planarProjection[i].y > planarProjection[a].y ? i : a;

                    // Remove from the list
                    verts.Remove(a);

                    // Find rightmost, then remove
                    int b = planarProjection[verts[0]].x > planarProjection[verts[1]].x ? verts[0] : verts[1];
                    verts.Remove(b);

                    //Remainder is leftmost
                    int c = verts[0];

                    // Add clockwise oriented triangle
                    triangles.Add(new int[3] { a, b, c });
                }
            }

            // Delete the triangles created by the super triangle
            List<int[]> deleteMe = new List<int[]>();
            foreach (int[] triangle in triangles)
            {
                // Check for if any vertex is the ones tacked on at the end (the super triangle's)
                if (triangle[0] >= vertices.Length || triangle[1] >= vertices.Length || triangle[2] >= vertices.Length)
                {
                    deleteMe.Add(triangle);
                }
            }

            foreach (int[] triangle in deleteMe)
            {
                triangles.Remove(triangle);
            }

            int[] flattenedTriangles = new int[triangles.Count * 3];

            // Assign to 1d triangles array
            int ind = 0;
            for (int a = 0; a < triangles.Count; a++)
            {
                for (int b = 0; b < triangles[a].Length; b++)
                {
                    flattenedTriangles[ind] = triangles[a][b];
                    ind++;
                }
            }

            // Assign to mesh
            mesh.triangles = flattenedTriangles;
        }

        private bool TriangleContainsPoint(Vector2[] pp, int[] tri, int i)
        {
            Vector2[] t = new Vector2[3] { pp[tri[0]], pp[tri[2]], pp[tri[1]] };
            Vector2   p = pp[i];

            // determinant of counterclockwise triangle circumcircle + point
            // if positive, point is inside of the circumcircle

            // matrix
            float a1 = t[0].x - p.x, a2 = t[0].y - p.y, a3 = (Mathf.Pow(t[0].x, 2f) - Mathf.Pow(p.x, 2f)) + (Mathf.Pow(t[0].y, 2f) - Mathf.Pow(p.y,2f));
            float b1 = t[1].x - p.x, b2 = t[1].y - p.y, b3 = (Mathf.Pow(t[1].x, 2f) - Mathf.Pow(p.x, 2f)) + (Mathf.Pow(t[1].y, 2f) - Mathf.Pow(p.y, 2f));
            float c1 = t[2].x - p.x, c2 = t[2].y - p.y, c3 = (Mathf.Pow(t[2].x, 2f) - Mathf.Pow(p.x, 2f)) + (Mathf.Pow(t[2].y, 2f) - Mathf.Pow(p.y, 2f));

            // returns true if determinant of the matrix above is greater than 0
            return a1*(b2*c3 - b3*c2) - a2*(b1*c3 - b3*c1) + a3*(b1*c2 - b2*c1) > 0;
        }

        private bool EdgeIsShared(int[] edge, List<int[]> badTriangles, int skip)
        {
            for(int i = 0; i < badTriangles.Count; i++)
            {
                if (i != skip)
                {
                    int[][] edges = new int[3][] { new int[]{ badTriangles[i][0], badTriangles[i][1] },
                                                   new int[]{ badTriangles[i][1], badTriangles[i][2] },
                                                   new int[]{ badTriangles[i][2], badTriangles[i][0] } };

                    foreach(int[] e in edges) { if (edge == e) { return true; } }
                }
            }
            return false;
        }
        
        // Returns planar projection of the point
        private Vector2 SphereToPlane(Vector3 v3)
        {
            return new Vector2(v3.x/ (1f - v3.z), v3.y / (1f - v3.z));
        }

        // Returns spherical projection of the planar projected point
        private Vector3 PlaneToSphere(Vector2 v2)
        {
            float divisor = 1f + Mathf.Pow(v2.x, 2f) + Mathf.Pow(v2.y, 2f);
            float x = (2f * v2.x) / divisor;
            float y = (2f * v2.y) / divisor;
            float z = (-1f + Mathf.Pow(v2.x, 2f) + Mathf.Pow(v2.y, 2f)) / divisor;
            return new Vector3(x,y,z);
        }

        private Vector2[] GetSuperTriangle(Vector2[]pp)
        {
            float xMax = pp[0].x, xMin = pp[0].x, yMax = pp[0].y, yMin = pp[0].y;
            for (int i = 1; i < pp.Length - 3; i++)
            {
                xMax = pp[i].x > xMax ? pp[i].x : xMax;
                xMin = pp[i].x < xMin ? pp[i].x : xMin;
                yMax = pp[i].y > yMax ? pp[i].y : yMax;
                yMin = pp[i].y < yMin ? pp[i].y : yMin;
            }

            float margin = 500;
            Vector2 a = new Vector2(0.5f * xMax, -2f * xMax - margin);
            Vector2 b = new Vector2(-2f * yMax - margin, 2f * yMax + margin);
            Vector2 c = new Vector2(2 * xMax + yMax + margin, 2f * yMax + margin);
            return new Vector2[3] { a, b, c };
        }
    }
}