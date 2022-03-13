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
            for (int i = 0; i < vertices.Length; i++) { planarProjection[i] = SphereToPlane(vertices[i]); }
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

        // Don't look at this REFACTOR MEEEEEEE
        private void AddFinalPoint(Delaunator delaunay)
        {
            Dictionary<Edge, int> edgeDict = new Dictionary<Edge, int>();
            IPoint[] IPts = delaunay.GetHullPoints();

            HashSet<IPoint> hullHash = new HashSet<IPoint>();
            for (int a = 0; a < IPts.Length; a++) { hullHash.Add(IPts[a]); }
            /* ------ */

            IEdge[] ves = delaunay.GetVoronoiEdgesBasedOnCentroids().ToArray();
            Vector3[][] evPoints = new Vector3[ves.Length][];
            for (int a = 0; a < ves.Length; a++)
            {
                evPoints[a] = new Vector3[] { PlaneToSphere(ves[a].P), PlaneToSphere(ves[a].Q) };
            }

            // Get hull triangles
            List<ITriangle> debugTris = new List<ITriangle>();
            delaunay.ForEachTriangle(triangle =>
            {
                if (hullHash.Overlaps(triangle.Points)) { debugTris.Add(triangle); }
            });

            HashSet<IPoint> centroidHash = new HashSet<IPoint>();
            for (int a = 0; a < debugTris.Count; a++)
            {
                centroidHash.Add(delaunay.GetCentroid(debugTris[a].Index));
            }

            List<IVoronoiCell> cells = new List<IVoronoiCell>();
            delaunay.ForEachVoronoiCellBasedOnCentroids(cell =>
            {
                if (cell.Points.Length > 3 && centroidHash.Overlaps(cell.Points)) { cells.Add(cell); }
            });


            Dictionary<IPoint, int> pointMap = new Dictionary<IPoint, int>();
            for(int a = 0; a < cells.Count; a++)
            {
                for(int b = 0; b < cells[a].Points.Length; b++)
                {
                    if(pointMap.ContainsKey(cells[a].Points[b])) { pointMap[cells[a].Points[b]] += 1; }
                    else { pointMap.Add(cells[a].Points[b], 1); }
                }
            }

            List<IPoint> debugCellPoints = new List<IPoint>();
            foreach (KeyValuePair<IPoint, int> point in pointMap)
            {
                if (point.Value == 1 && centroidHash.Contains(point.Key)) { debugCellPoints.Add(point.Key); }
            }

                List<Vector3> debugVoronoi = new List<Vector3>();
            for(int a = 0; a < debugCellPoints.Count; a++)
            {
                debugVoronoi.Add(PlaneToSphere(debugCellPoints[a]));
            }

            Vector3[] points = new Vector3[delaunay.Points.Count() + 1];
            for (int a = 0; a < delaunay.Points.Count(); a++) { points[a] = PlaneToSphere(delaunay.Points[a]); }
            points[points.Length - 1] = Vector3.forward;

            delaunay.ForEachTriangleEdge(edge =>
            {
                Edge e = (Edge)edge;
                if (edgeDict.ContainsKey(e)) { edgeDict[e] += 1; }
                else { edgeDict.Add(e, 1); };
            });

            List<Edge> edges = edgeDict.Keys.ToList();

            Vector3[][] etPoints = new Vector3[edges.Count + IPts.Length][];
            for (int a = 0; a < edges.Count; a++)
            {
                etPoints[a] = new Vector3[] { PlaneToSphere(edges[a].P), PlaneToSphere(edges[a].Q) };
            }
            for (int a = 0; a < IPts.Length; a++)
            {
                etPoints[a + edges.Count] = new Vector3[] { PlaneToSphere(IPts[a]), Vector3.forward };
            }

            Vector3[] debugCenters = new Vector3[IPts.Length];
            for (int a = 0; a < IPts.Length - 1; a++)
            {
                debugCenters[a] = IMath.Triangle.Centroid(PlaneToSphere(IPts[a]), PlaneToSphere(IPts[a + 1]), Vector3.forward);
            }
            debugCenters[IPts.Length - 1] = IMath.Triangle.Centroid(PlaneToSphere(IPts[0]), PlaneToSphere(IPts[IPts.Length - 1]), Vector3.forward);

            Vector3[][] finalCell = new Vector3[IPts.Length][];
            for (int a = 0; a < finalCell.Length - 1; a++)
            {
                finalCell[a] = new Vector3[] { debugCenters[a], debugCenters[a + 1] };
            }
            finalCell[finalCell.Length - 1] = new Vector3[] { debugCenters[0], debugCenters[debugCenters.Length - 1] };

            List<Vector3> finalCellPoints = debugCenters.ToList();
            List<Vector3[]> newVoronoiEdges = new List<Vector3[]>();
            for(int a = 0; a < debugVoronoi.Count; a++)
            {
                Vector3 currBest = finalCellPoints[0];
                float dist = Vector3.Distance(debugVoronoi[a], finalCellPoints[0]);
                for(int b = 1; b < finalCellPoints.Count; b++)
                {
                    float tempDist = Vector3.Distance(debugVoronoi[a], finalCellPoints[b]);
                    if(dist > tempDist) 
                    { 
                        dist = tempDist;
                        currBest = finalCellPoints[b];
                    }
                }
                newVoronoiEdges.Add(new Vector3[] { currBest, debugVoronoi[a]});
            }

            parent.GetComponent<World>().worldData.delaunayData = new WorldData.DelaunayData(etPoints.ToArray(), evPoints, points);
            parent.GetComponent<World>().worldData.delaunayData.debug = debugCenters;
            parent.GetComponent<World>().worldData.delaunayData.finalCell = finalCell;
            parent.GetComponent<World>().worldData.delaunayData.debugVoronoi = debugVoronoi.ToArray();
            parent.GetComponent<World>().worldData.delaunayData.debugNewVoronoiEdges = newVoronoiEdges.ToArray();
        }

        private bool TriDeterminant(Vector2 a, Vector2 b, Vector2 c)
        {
            return (b.x - a.x) * (c.y - a.y) - (c.x - a.x) * (b.y - a.y) > 0;
        }

        private Point SphereToPlane(Vector3 v3)
        {
            Point p = new Point(v3.x / (1f - v3.z), v3.y / (1f - v3.z));
            return p;
        }
        private Vector3 PlaneToSphere(IPoint pt)
        {
            float x = (float)pt.X;
            float y = (float)pt.Y;
            float divisor = (float)(1f + x * x + y * y);
            return new Vector3(2f * x / divisor, 2f * y / divisor, (-1f + x * x + y * y) / divisor);
        }

        private Edge Inverse(Edge edge)
        {
            return new Edge(edge.Index, edge.Q, edge.P);
        }
    }
}

