using System.Collections.Generic;
using UnityEngine;
using WorldGeneration.Objects;
using WorldGeneration.TectonicPlate.Objects;
using System.Linq;

namespace WorldGeneration
{
    public struct Functions
    {
        /* Late Public Constructor Functions */
        /* --- Listed by order of use ---*/

        // Generation of plates with plate centers, meant to be called by World
        public static Vector3[] GeneratePlateCenters(World world)
        {
            List<Vector3> centers = new List<Vector3>();

            centers.Add(Random.onUnitSphere);

            float minDist = world.DistBetweenCenters[(int)world._PlateSize];
            bool addToCenters;
            int MAX_TRIES = 999, tries = 0;

            while (tries < MAX_TRIES)
            {
                // Get random point
                addToCenters = true;
                Vector3 c = Random.onUnitSphere;

                // iterate through
                for (int i = 0; i < centers.Count; i++)
                {
                    // If distance is larger than allowed, add to tries, tell it to not execute last bit, and break
                    float dist = IMath.DistanceBetweenPoints(centers[i], c);

                    if (dist <= minDist)
                    {
                        tries += 1;
                        addToCenters = false;
                    }
                }

                if (addToCenters)
                {
                    centers.Add(c);
                    tries = 0;
                }

            };

            return centers.ToArray();
        }

        public static Plate[] GeneratePlates(World world)
        {
            // Initialization
            Plate[] plates = new Plate[world.PlateCenters.Length];
            List<int>[] plateTriangles = new List<int>[world.PlateCenters.Length];
            List<Vector3>[] plateVertices = new List<Vector3>[world.PlateCenters.Length];

            for (int i = 0; i < world.PlateCenters.Length; i++)
            {
                plateTriangles[i] = new List<int>();
                plateVertices[i] = new List<Vector3>();
            }

            if (world.PlateDeterminationType == World.PlateDetermination.ClosestCenter)
            {
                GetCenterClosestCenter(plateTriangles, plateVertices, out plateTriangles, out plateVertices, world);
            }
            else if (world.PlateDeterminationType == World.PlateDetermination.FloodFill)
            {
                GetCenterFloodFill(plateTriangles, plateVertices, out plateTriangles, out plateVertices, world);
            }

            // Build plates
            for (int i = 0; i < world.PlateCenters.Length; i++)
            {
                plates[i] = new Plate(world.PlateCenters[i], plateVertices[i].ToArray(), plateTriangles[i].ToArray());
            }


            foreach (Plate plate in plates)
            {
                // Do colors
                // Percent on gradient
                float onGradient = IMath.FloorFloat(Random.Range(0f, 1f), 0.1f);
                Color color = plate.IsContinental ? world.Continental.Evaluate(onGradient) : world.Oceanic.Evaluate(onGradient);
                plate.SetColors(color);
            }

            return plates;
        }

        // Find neighbors : get distinct list of edges...
        public static Edge[] GetBoundaryEdges(World world)
        {
            Plate[] plates = world.Plates;
            List<Edge> edges = new List<Edge>();

            for (int a = 0; a < plates.Length; a++)
            {
                for (int b = 0; b < plates[a].BoundaryEdges.Length; b++)
                {
                    int[] bE = plates[a].BoundaryEdges[b];
                    Edge e = new Edge(plates[a].Vertices[bE[0]], plates[a].Vertices[bE[1]], a);

                    edges.Add(e);
                }
            }

            // return edges.Distinct(new TectonicPlateEdgeCompareOverride()).ToList().ToArray();

            List<Edge> trueEdges = new List<Edge>();

            for (int a = 0; a < edges.Count; a++)
            {
                Edge currEdge = edges[a];
                for (int b = a + 1; b < edges.Count; b++)
                {
                    if (currEdge == edges[b])
                    {
                        currEdge.edgeOf[1] = edges[b].edgeOf[0];

                        // Sort by edgeOf[0]
                        if (currEdge.edgeOf[0] > currEdge.edgeOf[1])
                        {
                            int temp = currEdge.edgeOf[0];
                            currEdge.edgeOf[0] = currEdge.edgeOf[1];
                            currEdge.edgeOf[1] = temp;
                        }

                        trueEdges.Add(currEdge);
                        break;
                    }
                }
            }

            // Sort by edgeOf[1]
            List<List<Edge>> edgeMap = new List<List<Edge>>();
            edgeMap.Add(new List<Edge>());
            int ind = 0;
            int prevInd = trueEdges[0].edgeOf[0];
            // Seperate edges based on index 0
            foreach (Edge edge in trueEdges)
            {
                if (edge.edgeOf[0] != prevInd)
                {
                    ind += 1;
                    edgeMap.Add(new List<Edge>());
                    prevInd = edge.edgeOf[0];
                }

                edgeMap[ind].Add(edge);
            }
            // Sort edges based on index 1
            foreach(List<Edge> edgeList in edgeMap)
            {
                edgeList.Sort(SortAscendingByIndexOne);    
            }

            trueEdges.Clear();
            // Compress to 1d
            foreach(List<Edge> edgeList in edgeMap)
            {
                foreach(Edge edge in edgeList)
                {
                    trueEdges.Add(edge);
                }
            }

            // Clear data
            edgeMap.Clear();

            return trueEdges.ToArray();
        }

        public static int GetUniqueEdges(Edge[] edges)
        {
            int uniqueEdges = 1;
            int[] curr = edges[0].edgeOf;
            for (int a = 1; a < edges.Length; a++)
            {
                if (edges[a].edgeOf[0] != curr[0] || edges[a].edgeOf[1] != curr[1]) 
                { 
                    curr = edges[a].edgeOf; 
                    uniqueEdges += 1; 
                }
            }

            return uniqueEdges;
        }

        public static float[] GetEdgeWeights(World world)
        {
            Edge[] edges = world.PlateBoundaries.Edges;
            Plate[] plates = world.Plates;
            float[] weights = new float[edges.Length];

            for (int a = 0; a < edges.Length; a++)
            {
                Vector3 dirA = plates[edges[a].edgeOf[0]].Direction - plates[edges[a].edgeOf[0]].Center * plates[edges[a].edgeOf[0]].Speed;
                Vector3 dirB = plates[edges[a].edgeOf[1]].Direction - plates[edges[a].edgeOf[1]].Center * plates[edges[a].edgeOf[1]].Speed;
                
                weights[a] = (dirA - dirB).magnitude;
                edges[a].SetDirectionSign((int)Mathf.Sign(weights[a]));
            }

            // Normalize values
            float max = weights[0], min = weights[0];
            for (int a = 1; a < edges.Length; a++)
            {
                max = weights[a] > max ? weights[a] : max;
                min = weights[a] < min ? weights[a] : min;
            }

            for(int a = 0; a < weights.Length; a++)
            {
                weights[a] /= (max + min);
            }

            max = weights[0];
            min = weights[0];
            for (int a = 1; a < edges.Length; a++)
            {
                max = weights[a] > max ? weights[a] : max;
                min = weights[a] < min ? weights[a] : min;
            }

            return weights;
        }


        /*------------------------------------------------------------------------------------*/

        /* Private Functions Called By Late Constructor Functions */

        private static void GetCenterClosestCenter(List<int>[] plateTriangles, List<Vector3>[] plateVertices, out List<int>[] pt, out List<Vector3>[] pv, World world)
        {
            // For each vertex in each meshfilter...
            for (int i = 0; i < world.Sphere.meshFilters.Length; i++)
            {
                int[] t = world.Sphere.meshFilters[i].mesh.triangles;
                Vector3[] v = world.Sphere.meshFilters[i].mesh.vertices;
                for (int j = 0; j < t.Length; j += 3)
                {
                    Vector3 centroid = IMath.TriangleCentroid(v[t[j + 0]], v[t[j + 1]], v[t[j + 2]]);
                    float dist = IMath.DistanceBetweenPoints(centroid, world.PlateCenters[0]);
                    int distInd = 0;

                    // Find the closest plate center
                    for (int k = 1; k < world.PlateCenters.Length; k++)
                    {
                        float newDist = IMath.DistanceBetweenPoints(centroid, world.PlateCenters[k]);
                        if (dist > newDist)
                        {
                            dist = newDist;
                            distInd = k;
                        }
                    }

                    // Add to vertices
                    for (int k = 0; k < 3; k++)
                    {
                        plateVertices[distInd].Add(v[t[j + k]]);
                    }
                }
            }

            // Do triangles
            for (int i = 0; i < world.PlateCenters.Length; i++)
            {
                for (int j = 0; j < plateVertices[i].Count; j++)
                {
                    plateTriangles[i].Add(j);
                }
            }

            // Squash vertices
            if (world.SmoothMapSurface)
            {
                for (int i = 0; i < plateVertices.Length; i++)
                {
                    CondenseVerticesAndTriangles(plateVertices[i], plateTriangles[i], out plateVertices[i], out plateTriangles[i]);
                }
            }

            pt = plateTriangles;
            pv = plateVertices;
        }

        private static void GetCenterFloodFill(List<int>[] plateTriangles, List<Vector3>[] plateVertices, out List<int>[] pt, out List<Vector3>[] pv, World world)
        {
            Vector3[][] v = new Vector3[world.Sphere.meshFilters.Length][];
            int[][] t = new int[world.Sphere.meshFilters.Length][];
            for (int i = 0; i < world.Sphere.meshFilters.Length; i++)
            {
                v[i] = world.Sphere.meshFilters[i].mesh.vertices;
                t[i] = world.Sphere.meshFilters[i].mesh.triangles;
            }

            // Loop through all triangles and create a triangle object with relevant information
            List<Triangle> triangles = new List<Triangle>();
            for (int i = 0; i < t.Length; i++)
            {
                for (int j = 0; j < t[i].Length; j += 3)
                {
                    int[] tri = new int[] { t[i][j + 0], t[i][j + 1], t[i][j + 2] };
                    Vector3[] ver = new Vector3[] { v[i][tri[0]], v[i][tri[1]], v[i][tri[2]] };
                    Vector3 triCenter = IMath.TriangleCentroid(ver[0], ver[1], ver[2]);
                    triangles.Add(new Triangle(tri, ver, triCenter));
                }
            }

            int ind = 0;
            List<Triangle> tempCenters = new List<Triangle>();
            // Set triangle centers as random triangles in the triangles list
            while (ind < world.PlateCenters.Length && ind < triangles.Count)
            {
                tempCenters.Add(triangles[Random.Range(0, triangles.Count - 1)]);
                if (tempCenters.Count != tempCenters.Distinct().Count()) { tempCenters.RemoveAt(tempCenters.Count - 1); }
                else { ind++; }
            }

            // Resize
            world.PlateCenters = new Vector3[tempCenters.Count];

            // Proper plate centers
            for (int i = 0; i < tempCenters.Count; i++)
            {
                world.PlateCenters[i] = tempCenters[i].TriangleCenter;
                tempCenters[i].PlateCenter = i;
            }

            // Floodfill esque algorithm:
            // - Create 2d array of closest to farthest centers for each center
            // - Iterate through from closest to farthest, from first to last center and assign to center if the val is -1

            // closest to farthest map
            SortedList<float, int>[] distanceMap = new SortedList<float, int>[world.PlateCenters.Length];
            for (int i = 0; i < world.PlateCenters.Length; i++)
            {
                distanceMap[i] = new SortedList<float, int>();
                for (int j = 0; j < triangles.Count; j++)
                {
                    float dist = IMath.DistanceBetweenPoints(world.PlateCenters[i], triangles[j].TriangleCenter);
                    // If dist key already exists, adjust until its right behind
                    while (distanceMap[i].ContainsKey(dist)) { dist += .00001f; }
                    distanceMap[i].Add(dist, j);
                }
            }

            // iterate through each plate center by way of distance map, and if it hasnt been touched yet, assign it to the plate
            for (int i = 0; i < triangles.Count; i++)
            {
                for (int j = 0; j < world.PlateCenters.Length; j++)
                {
                    if (triangles[distanceMap[j].Values[i]].PlateCenter == -1)
                    {
                        triangles[distanceMap[j].Values[i]].PlateCenter = j;
                    }
                }
            }

            // With assigned plate centers, assign to the proper arrays
            for (int i = 0; i < triangles.Count; i++)
            {
                int[] tri = triangles[i].Triangles;
                Vector3[] ver = triangles[i].Vertices;
                for (int j = 0; j < tri.Length; j++)
                {
                    plateTriangles[triangles[i].PlateCenter].Add(tri[j]);
                    plateVertices[triangles[i].PlateCenter].Add(ver[j]);
                }
            }

            // Squash vertices
            if (world.SmoothMapSurface)
            {
                for (int i = 0; i < plateVertices.Length; i++)
                {
                    CondenseVerticesAndTriangles(plateVertices[i], plateTriangles[i], out plateVertices[i], out plateTriangles[i]);
                }
            }

            // Assign
            pt = plateTriangles;
            pv = plateVertices;
        }

        // Each triangle has their own set of vertices, the triangles should share vertices with neighboring triangles
        // to smooth the surface of the Sphere
        // Approach: Loop through each vertex element, find matching vertices, get their indices into a list and delete afterwards
        private static void CondenseVerticesAndTriangles(List<Vector3> v, List<int> t, out List<Vector3> vertices, out List<int> triangles)
        {
            // Get distinct members of v
            vertices = v.Distinct().ToList();
            for (int i = 0; i < vertices.Count; i++)
            {
                // For each vertex, if they match the current comparison vertex, change the triangle to the proper index
                for (int j = i; j < v.Count; j++)
                {
                    if (vertices[i] == v[j])
                    {
                        t[j] = i;
                    }
                }
            }

            triangles = t;
        }

        // Edge custom sorting function
        private static int SortAscendingByIndexOne(Edge a, Edge b)
        {
            int aa = a.edgeOf[1];
            int bb = b.edgeOf[1];

            if (aa > bb) { return 1; }
            else if (aa == bb) { return 0; }
            else { return -1; }
        }
    }   
}
