using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using DelaunatorSharp;

/*
 * Fibonacci Distribution on Sphere
 * - https://medium.com/@vagnerseibert/distributing-points-on-a-sphere-6b593cc05b42
 * 
 * Delaunay triangulation on sphere
 * - https://fsu.digital.flvc.org/islandora/object/fsu:182663/datastream/PDF/view
 * - https://en.wikipedia.org/wiki/Bowyer%E2%80%93Watson_algorithm
 * - https://github.com/nol1fe/delaunator-sharp
 *
 * Octahedron Sphere triangulation
 * - https://www.youtube.com/watch?v=lctXaT9pxA0&t=194s&ab_channel=SebastianLague
 * 
 * Altered Fibonacci Lattice
 * - http://extremelearning.com.au/how-to-evenly-distribute-points-on-a-sphere-more-effectively-than-the-canonical-fibonacci-lattice/
*/

// TODO Fix the fibonacci delaunay triangulation. Probably broken in the conversion of 3d to 2d points,
// Also stereopgraphic projection goes to inf at -1 and 1, so move the points from those

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
        private float jitter;

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

            // Trianglulation
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
        public Mesh PPMesh { get; private set; }

        private Transform parent;
        private int numPoints;
        private float jitter;
        private bool alterFibonacciLattice;

        public FibonacciSphere(Transform parent, int numPoints, float jitter = 0f, bool alterFibonacciLattice = true)
        {
            this.parent = parent;
            this.numPoints = numPoints;
            this.jitter = jitter;
            this.alterFibonacciLattice = alterFibonacciLattice;
            meshFilters = new MeshFilter[1];
        }

        public override void Build()
        {
            GameObject meshObj = new GameObject("Mesh");
            meshObj.transform.parent = parent;

            Vector3[] vertices = new Vector3[numPoints];

            // Altered fibonacci lattice
            if (alterFibonacciLattice)
            {
                float epsilon;
                if (numPoints >= 600000) { epsilon = 214f; }
                else if (numPoints >= 400000) { epsilon = 75f; }
                else if (numPoints >= 11000) { epsilon = 27f; }
                else if (numPoints >= 890) { epsilon = 10f; }
                else if (numPoints >= 177) { epsilon = 3.33f; }
                else if (numPoints >= 24) { epsilon = 1.33f; }
                else { epsilon = .33f; }

                float goldenRatio = (1f + Mathf.Sqrt(5f)) / 2f;

                for (int i = 0; i < numPoints; i++)
                {
                    float theta = 2f * Mathf.PI * i / goldenRatio;
                    float phi = Mathf.Acos(1f - 2f * (i + epsilon) / (numPoints - 1f + 2f * epsilon));

                    float x = Mathf.Cos(theta) * Mathf.Sin(phi);
                    float y = Mathf.Sin(theta) * Mathf.Sin(phi);
                    float z = Mathf.Cos(phi);

                    vertices[i] = new Vector3(x, y, z);
                }
            }
            // Default vertices
            else
            {
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
            }

            meshObj.AddComponent<MeshRenderer>().sharedMaterial = Resources.Load<Material>("Materials/Surface");
            meshFilters[0] = meshObj.AddComponent<MeshFilter>();
            meshFilters[0].sharedMesh = new Mesh();

            // Delaunator Triangluation
            Vector3[] pp = DelaunatorTriangulate(meshFilters[0].sharedMesh, vertices);

            meshFilters[0].sharedMesh.RecalculateNormals();
            meshFilters[0].sharedMesh.Optimize();
        }

        private Vector3[] DelaunatorTriangulate(Mesh mesh, Vector3[] vertices)
        {
            // Delaunay wizardry
            IPoint[] planarProjection = new IPoint[vertices.Length];
            for (int i = 0; i < vertices.Length; i++) { planarProjection[i] = V2SphereToPlane(vertices[i]); }
            Delaunator delaunay = new Delaunator(planarProjection);

            // For debug
            Vector3[] pp = new Vector3[vertices.Length];
            for (int i = 0; i < vertices.Length; i++) { pp[i] = SphereToPlane(vertices[i]); }
            

            // Figure out the final point in the back. Takes the convex hull given by delaunator and the final point added,
            // draws triangles
            List<Vector3> addTheNegZ = vertices.ToList();
            addTheNegZ.Add(Vector3.forward);

            // Get the indices of the convex hull points
            List<IPoint> cv = delaunay.GetHullPoints().ToList();
            List<IPoint> v = delaunay.Points.ToList();
            int[] convexHull = new int[cv.Count];
            for (int i = 0; i < cv.Count; i++) { convexHull[i] = v.IndexOf(cv[i]); }

            // Add jitter
            if (jitter > 0)
            {
                for (int i = 0; i < addTheNegZ.Count; i++)
                {
                    addTheNegZ[i] = AddJitter(addTheNegZ[i]);
                }
            }


            int[] triangles = CloseMesh(addTheNegZ.ToArray(), delaunay.Triangles.ToList(), convexHull);

            mesh.vertices = addTheNegZ.ToArray();
            mesh.triangles = triangles;

            return pp;
        }


        private int[] CloseMesh(Vector3[] vertices, List<int> triangles, int[] convexHull)
        {
            int a = vertices.Length - 1;
            int b, c;
            for(int i = 0; i < convexHull.Length - 1; i++)
            {
                if (TriDeterminant(vertices[a], vertices[convexHull[i]], vertices[convexHull[i + 1]]))
                {
                    b = convexHull[i];
                    c = convexHull[i + 1];
                }
                else
                {
                    b = convexHull[i + 1];
                    c = convexHull[i];
                }

                triangles.Add(a);
                triangles.Add(b);
                triangles.Add(c);
            }

            if (TriDeterminant(vertices[a], vertices[convexHull[convexHull.Length - 1]], vertices[convexHull[0]]))
            {
                b = convexHull[convexHull.Length - 1];
                c = convexHull[0];
            }
            else
            {
                b = convexHull[0];
                c = convexHull[convexHull.Length - 1];
            }

            triangles.Add(a);
            triangles.Add(b);
            triangles.Add(c);

            return triangles.ToArray();
        }
        
        private bool TriDeterminant(Vector2 a, Vector2 b, Vector2 c)
        {
            return (b.x - a.x) * (c.y - a.y) - (c.x - a.x) * (b.y - a.y) > 0;
        }

        private Vector2 SphereToPlane(Vector3 v3)
        {
            return new Vector2(v3.x / (1f - v3.z), v3.y / (1f - v3.z));
        }

        private V2 V2SphereToPlane(Vector3 v3)
        {
            V2 p = new V2(v3.x / (1f - v3.z), v3.y / (1f - v3.z));
            return p;
        }
        private Vector3 AddJitter(Vector3 v)
        {
            float j = jitter / Mathf.Sqrt(numPoints);
            Vector3 r = new Vector3(Random.Range(-1,1), Random.Range(-1, 1), Random.Range(-1, 1)) * Random.Range(-j,j);
            return (v + r).normalized;
        }
    }

    public class V2 : IPoint
    {
        public double X { get; set; }
        public double Y { get; set; }

        public V2(double X, double Y)
        {
            this.X = X;
            this.Y = Y;
        }
    }

}