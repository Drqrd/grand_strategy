using System.Collections.Generic;
using System.Linq;
using UnityEngine;

using static Extensions;

using DelaunatorSharp;

// Stopped using fibonnacci sphere due to how points are grouped, chunking for rendering optomization is not
// Feasible (currently)


/*
 * Fibonacci Distribution on Sphere
 * - https://medium.com/@vagnerseibert/distributing-points-on-a-sphere-6b593cc05b42
 * 
 * Delaunay triangulation on sphere
 * - https://fsu.digital.flvc.org/islandora/object/fsu:182663/datastream/PDF/view
 * - https://en.wikipedia.org/wiki/Bowyer%E2%80%93Watson_algorithm
 * - https://github.com/nol1fe/delaunator-sharp
 * 
 * Altered Fibonacci Lattice
 * - http://extremelearning.com.au/how-to-evenly-distribute-points-on-a-sphere-more-effectively-than-the-canonical-fibonacci-lattice/
*/

namespace WorldGeneration.Meshes
{
    public class FibonacciSphere : CustomMesh
    {
        private Transform parent;
        private bool alterFibonacciLattice;

        private World.Parameters.CustomMesh parameters;

        public FibonacciSphere(Transform parent, int resolution, float jitter, bool alterFibonacciLattice = true) : base(resolution, jitter)
        {
            parameters = parent.GetComponent<World>().parameters.customMesh;

            this.parent = parent;
            this.alterFibonacciLattice = alterFibonacciLattice;

            this.resolution = resolution;
            this.jitter = jitter;
        }

        public override void Build()
        {
            GameObject meshObj = new GameObject("Mesh");
            meshObj.transform.parent = parent;

            Vector3[] vertices = new Vector3[resolution];

            // Altered fibonacci lattice
            if (alterFibonacciLattice)
            {
                float epsilon;
                if (resolution >= 600000) { epsilon = 214f; }
                else if (resolution >= 400000) { epsilon = 75f; }
                else if (resolution >= 11000) { epsilon = 27f; }
                else if (resolution >= 890) { epsilon = 10f; }
                else if (resolution >= 177) { epsilon = 3.33f; }
                else if (resolution >= 24) { epsilon = 1.33f; }
                else { epsilon = .33f; }

                float goldenRatio = (1f + Mathf.Sqrt(5f)) / 2f;

                for (int i = 0; i < resolution; i++)
                {
                    float theta = 2f * Mathf.PI * i / goldenRatio;
                    float phi = Mathf.Acos(1f - 2f * (i + epsilon) / (resolution - 1f + 2f * epsilon));

                    float x = Mathf.Cos(theta) * Mathf.Sin(phi);
                    float y = Mathf.Sin(theta) * Mathf.Sin(phi);
                    float z = Mathf.Cos(phi);

                    vertices[i] = new Vector3(x, y, z);
                }
            }
            // Default vertices
            else
            {
                for (int i = 0; i < resolution; i++)
                {
                    float k = i + .5f;

                    float phi = Mathf.Acos(1f - 2f * k / resolution);
                    float theta = Mathf.PI * (1 + Mathf.Sqrt(5)) * k;

                    float x = Mathf.Cos(theta) * Mathf.Sin(phi);
                    float y = Mathf.Sin(theta) * Mathf.Sin(phi);
                    float z = Mathf.Cos(phi);

                    vertices[i] = new Vector3(x, y, z);
                }
            }

            Mesh mesh = new Mesh();

            // Delaunator Triangluation
            DelaunatorTriangulate(mesh, vertices);

            mesh.RecalculateNormals();

            meshObj.AddComponent<MeshRenderer>().sharedMaterial = Resources.Load<Material>("Materials/WorldGen/Map");
            meshFilter = meshObj.AddComponent<MeshFilter>();
            meshFilter.sharedMesh = mesh;
        }

        private void DelaunatorTriangulate(Mesh mesh, Vector3[] vertices)
        {
            mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;

            // Add jitter
            if (jitter > 0)
            {
                for (int i = 0; i < vertices.Length; i++) { vertices[i] = AddJitter(vertices[i]); }
            }

            // Delaunay wizardry
            IPoint[] planarProjection = new IPoint[vertices.Length];
            for (int i = 0; i < vertices.Length; i++) { planarProjection[i] = V2SphereToPlane(vertices[i]); }
            Delaunator delaunay = new Delaunator(planarProjection);

            // Figure out the final point in the back. Takes the convex hull given by delaunator and the final point added,
            // draws triangles
            List<Vector3> addTheNegZ = vertices.ToList();
            addTheNegZ.Add(Vector3.forward);

            List<IPoint> cv = delaunay.GetHullPoints().ToList();
            List<IPoint> v = delaunay.Points.ToList();

            AddFinalPoint(delaunay);

            int[] convexHull = new int[cv.Count];
            for (int i = 0; i < cv.Count; i++) { convexHull[i] = v.IndexOf(cv[i]); }

            int[] triangles = CloseMesh(addTheNegZ.ToArray(), delaunay.Triangles.ToList(), convexHull);

            mesh.vertices = addTheNegZ.ToArray();
            mesh.triangles = triangles;
        }


        private int[] CloseMesh(Vector3[] vertices, List<int> triangles, int[] convexHull)
        {
            int a = vertices.Length - 1;
            int b, c;
            for (int i = 0; i < convexHull.Length - 1; i++)
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

        /* Approach:
         * 1. Get the triangles which contain the hull points
         * 2. Get the triangle neighbors
         * 3. Find all centroid-based voronoi cells that are created from these triangles
         * 4. Add final point
         * 5. Create new triangle centroids from the final point + hull points
         * 6. Update neighboring voronoi cells with new centroid points
         * 7. Create final voronoi cell
         */
        private void AddFinalPoint(Delaunator delaunay)
        {
            // --- Get triangles that contain hull points
            IPoint[] IPts = delaunay.GetHullPoints();

            HashSet<IPoint> IHPSet = new HashSet<IPoint>();
            for(int a = 0; a < IPts.Length; a++) { IHPSet.Add(IPts[a]); }


            List<ITriangle> ITriangles = new List<ITriangle>();
            delaunay.ForEachTriangle(triangle =>
            {
                if (IHPSet.Overlaps(triangle.Points)) 
                { 
                    ITriangles.Add(triangle);
                }
            });

            /*
            // --- Get triangle neighbors
            List<ITriangle> neighbors = new List<ITriangle>();
            for(int a = 0; a < ITriangles.Count; a++)
            {
                IEnumerable<int> adj = delaunay.TrianglesAdjacentToTriangle(ITriangles[a].Index);
                for (int b = 0; b < adj.Count(); b++)
                {
                    if (!neighbors.Contains(delaunay.GetTriangles().ElementAt(adj.ElementAt(b))))
                    {
                        neighbors.Add(delaunay.GetTriangles().ElementAt(adj.ElementAt(b)));
                    }
                }
            }

            ITriangles = ITriangles.Concat(neighbors.Distinct()).ToList();
            */

            // --- Find all centroid-based voronoi cells that are created from these triangles
            HashSet<IPoint> centroids = new HashSet<IPoint>();
            for (int a = 0; a < ITriangles.Count; a++)
            {
                centroids.Add(delaunay.GetCentroid(ITriangles[a].Index));
            }

            List<IVoronoiCell> voronoiCells = new List<IVoronoiCell>();
            delaunay.ForEachVoronoiCellBasedOnCentroids(cell => 
            { 
                if (centroids.Overlaps(cell.Points)) { voronoiCells.Add(cell); }
            });

            // --- Add final point (done in triangle centroids
            Vector3[] hullPoints = new Vector3[IPts.Length]; // + 1
            for (int a = 0; a < IPts.Length; a++) { hullPoints[a] = PlaneToSphere(IPts[a]); }
            // hullPoints[hullPoints.Length - 1] = Vector3.forward;

            // --- Create new triangle centroids from the final point + hull points
            Vector3[] triangleCentroids = new Vector3[hullPoints.Length];
            for(int a = 0; a < hullPoints.Length - 1; a++) { triangleCentroids[a]}

            // --- Update neighboring voronoi cells with new centroid points
            // --- Create final voronoi cell
        }

        private bool TriDeterminant(Vector2 a, Vector2 b, Vector2 c)
        {
            return (b.x - a.x) * (c.y - a.y) - (c.x - a.x) * (b.y - a.y) > 0;
        }

        private V2 V2SphereToPlane(Vector3 v3)
        {
            V2 p = new V2(v3.x / (1f - v3.z), v3.y / (1f - v3.z));
            return p;
        }
        private Vector3 PlaneToSphere(IPoint pt)
        {
            float x = (float)pt.X;
            float y = (float)pt.Y;
            float divisor = (float)(1f + x * x + y * y);
            return new Vector3(2f * x / divisor, 2f * y / divisor, (-1f + x * x + y * y) / divisor);
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

