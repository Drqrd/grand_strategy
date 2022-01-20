using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using IDict;

using WorldGeneration.Objects;
using WorldGeneration.TectonicPlate.Objects;

namespace WorldGeneration.TectonicPlate
{
    public struct Functions
    {
        /* Late Public Constructor Functions */
        /* --- Listed by order of use ---*/

        // Generation of plate centers
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

        // Generates the actual plates
        public static Plate[] GeneratePlates(World world)
        {
            // Initialization
            Plate[] plates = new Plate[world.PlateCenters.Length];
            List<int>[] plateTriangles = new List<int>[world.PlateCenters.Length];
            List<Vector3>[] plateVertices = new List<Vector3>[world.PlateCenters.Length];
            List<int>[] globalVertices = new List<int>[world.PlateCenters.Length];

            for (int i = 0; i < world.PlateCenters.Length; i++)
            {
                plateTriangles[i] = new List<int>();
                plateVertices[i] = new List<Vector3>();
                globalVertices[i] = new List<int>();
            }

            if (world.PlateDeterminationType == World.PlateDetermination.ClosestCenter)
            {
                GetCenterClosestCenter(plateTriangles, plateVertices, out plateTriangles, out plateVertices, world);
            }
            else if (world.PlateDeterminationType == World.PlateDetermination.FloodFill)
            {
                GetCenterFloodFill(plateTriangles, plateVertices, globalVertices, out plateTriangles, out plateVertices, out globalVertices, world);
            }

            // Build plates
            for (int i = 0; i < world.PlateCenters.Length; i++)
            {
                plates[i] = new Plate(world.PlateCenters[i], i, world.CVO, plateVertices[i].ToArray(), globalVertices[i].ToArray(), plateTriangles[i].ToArray());
            }


            foreach (Plate plate in plates)
            {
                // Do colors
                // Percent on gradient
                float onGradient = IMath.FloorFloat(Random.Range(0f, 1f), 0.1f);
                Color color = plate.PlateType == Plate.TectonicPlateType.Continental ? world.Gradients.Continental.Evaluate(onGradient) : world.Gradients.Oceanic.Evaluate(onGradient);
                plate.SetColors(color);
            }

            return plates;
        }

        /* -------------------------------------------------------------------------------------------- */



        /// <summary>
        /// 
        /// FAULT LINE FUNCTIONS
        ///
        /// </summary>



        // Populates the Plates with fault lines containing all edges
        public static void GenerateFaultLines(World world)
        {
            Plate[] plates = world.Plates;
            int[][][] boundaryEdges = new int[plates.Length][][];

            // Find all boundary Edges
            for (int a = 0; a < plates.Length; a++)
            {
                boundaryEdges[a] = FindBoundaryEdges(plates[a]);
            }

            List<Edge> edges = new List<Edge>();
            for (int a = 0; a < boundaryEdges.Length; a++)
            {
                for (int b = 0; b < boundaryEdges[a].Length; b++)
                {
                    int[] bE = boundaryEdges[a][b];
                    Edge e = new Edge(plates[a].Points[bE[0]].Pos, plates[a].Points[bE[1]].Pos, bE, a);

                    edges.Add(e);
                }
            }

            List<Edge> trueEdges = new List<Edge>();

            for (int a = 0; a < edges.Count; a++)
            {
                Edge currEdge = edges[a];
                for (int b = a + 1; b < edges.Count; b++)
                {
                    if (currEdge == edges[b])
                    {
                        currEdge.edgeOf[1] = edges[b].edgeOf[0];
                        currEdge.vertexIndices1 = edges[b].vertexIndices0;
                        // Sort by edgeOf[0]
                        if (currEdge.edgeOf[0] > currEdge.edgeOf[1])
                        {
                            int temp = currEdge.edgeOf[0];
                            currEdge.edgeOf[0] = currEdge.edgeOf[1];
                            currEdge.edgeOf[1] = temp;

                            int[] tempI = currEdge.vertexIndices0;
                            currEdge.vertexIndices0 = currEdge.vertexIndices1;
                            currEdge.vertexIndices1 = tempI;
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

            trueEdges = null;

            // Sort edges based on index 1
            foreach (List<Edge> edgeList in edgeMap)
            {
                edgeList.Sort(SortAscendingByIndexOne);
            }

            // Iterate through, create a fault line for each list
            List<FaultLine> faultLines = new List<FaultLine>();
            int[] uniqueEdge = edgeMap[0][0].edgeOf;
            ind = 0;
            
            // Sort edges into unique faults
            List<List<Edge>> faultEdges = new List<List<Edge>>();
            faultEdges.Add(new List<Edge>());
            foreach (List<Edge> edgeList in edgeMap)
            {
                List<Edge> uniqueEdges = new List<Edge>();
                foreach(Edge edge in edgeList)
                {
                    if (edge.edgeOf[0] != uniqueEdge[0] || edge.edgeOf[1] != uniqueEdge[1])
                    {
                        uniqueEdge = edge.edgeOf;
                        faultEdges.Add(new List<Edge>());
                        ind++;
                    }
                    faultEdges[ind].Add(edge);
                }    
            }

            foreach(List<Edge> edgeList in faultEdges)
            {
                faultLines.Add(new FaultLine(edgeList.ToArray()));
            }

            edgeMap = null;

            // Organize fault lines into seperate lists for each plate
            List<FaultLine>[] faultLineMap = new List<FaultLine>[world.Plates.Length];
            // Initialize
            for(int a = 0; a < faultLineMap.Length; a++) { faultLineMap[a] = new List<FaultLine>();}
            
            // Sort fault lines
            foreach (FaultLine faultLine in faultLines)
            {
                int lineOf = faultLine.Edges[0].edgeOf[0];

                faultLineMap[lineOf].Add(faultLine);
            }

            // Assign values
            for (int a = 0; a < world.Plates.Length; a++)
            {
                world.Plates[a].FaultLines = faultLineMap[a].ToArray();
            }

            // Make plates determine their type
            foreach(Plate plate in world.Plates)
            {
                foreach(FaultLine faultLine in plate.FaultLines)
                {
                    // world reference for plates
                    faultLine.DetermineFaultLineType(world);
                }
            }


            faultLineMap = null;
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
            return new float[] { 1f };
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


        /*------------------------------------------------------------------------------------*/



        /// <summary>
        /// 
        ///  Private Functions Called By Late Constructor Functions
        /// 
        /// </summary>



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
            /*
            // Squash vertices
            if (world.SmoothMapSurface)
            {
                for (int i = 0; i < plateVertices.Length; i++)
                {
                    CondenseVerticesAndTriangles(plateVertices[i], plateTriangles[i], out plateVertices[i], out plateTriangles[i]);
                }
            }
            */
            pt = plateTriangles;
            pv = plateVertices;
        }

        private static void GetCenterFloodFill(List<int>[] plateTriangles, List<Vector3>[] plateVertices, List<int>[] globalVertices, out List<int>[] pt, out List<Vector3>[] pv, out List<int>[] pg,  World world)
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
                    // Global indices
                    int[] vertexIndices = new int[] { tri[0], tri[1], tri[2] };
                    Vector3 triCenter = IMath.TriangleCentroid(ver[0], ver[1], ver[2]);
                    triangles.Add(new Triangle(tri, ver, vertexIndices, triCenter));
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
                int[] vis = triangles[i].VertexIndices;
                for (int j = 0; j < tri.Length; j++)
                {
                    plateTriangles[triangles[i].PlateCenter].Add(tri[j]);
                    plateVertices[triangles[i].PlateCenter].Add(ver[j]);
                    globalVertices[triangles[i].PlateCenter].Add(vis[j]);
                }
            }

            // Squash vertices
            if (world.SmoothMapSurface)
            {
                for (int i = 0; i < plateVertices.Length; i++)
                {
                    CondenseVerticesAndTriangles(plateVertices[i], plateTriangles[i], globalVertices[i], out plateVertices[i], out plateTriangles[i], out globalVertices[i]);
                }
            }

            // Assign
            pt = plateTriangles;
            pv = plateVertices;
            pg = globalVertices;
        }

        // Finds the boundary edges for a given plate
        private static int[][] FindBoundaryEdges(Plate plate)
        {
            List<int[]> edges = new List<int[]>();
            Dictionary<int[], int> mp = new Dictionary<int[], int>(new IntArrCompareOverride());
            for (int i = 0; i < plate.Triangles.Length; i += 3)
            {
                int[][] es = new int[][] { new int[] { plate.Triangles[i + 0], plate.Triangles[i + 1] },
                                       new int[] { plate.Triangles[i + 1], plate.Triangles[i + 2] },
                                       new int[] { plate.Triangles[i + 2], plate.Triangles[i + 0] } };

                int[][] esInv = new int[][] { new int[] { plate.Triangles[i + 1], plate.Triangles[i + 0] },
                                          new int[] { plate.Triangles[i + 2], plate.Triangles[i + 1] },
                                          new int[] { plate.Triangles[i + 0], plate.Triangles[i + 2] } };

                for (int j = 0; j < es.Length; j++)
                {
                    if (mp.ContainsKey(es[j])) { mp[es[j]] += 1; }
                    else if (mp.ContainsKey(esInv[j])) { mp[esInv[j]] += 1; }
                    else { mp.Add(es[j], 1); }
                }
            }

            // Add
            foreach (KeyValuePair<int[], int> entry in mp)
            {
                uint num = uint.Parse(string.Join("", entry.Value));
                if (num == 1) { edges.Add(entry.Key); }
            }

            // Reorder edges so that they connect
            for (int i = 0; i < edges.Count - 1; i++)
            {

                for (int j = i + 2; j < edges.Count; j++)
                {
                    if (edges[j][0] == edges[i][1])
                    {
                        int[] tempEdge = edges[j];
                        edges[j] = edges[i + 1];
                        edges[i + 1] = tempEdge;
                        continue;
                    }
                }
            }

            return edges.ToArray();
        }

        // Each triangle has their own set of vertices, the triangles should share vertices with neighboring triangles
        // to smooth the surface of the Sphere
        // Approach: Loop through each vertex element, find matching vertices, get their indices into a list and delete afterwards
        private static void CondenseVerticesAndTriangles(List<Vector3> v, List<int> t, List<int> g, out List<Vector3> vertices, out List<int> triangles, out List<int> globals)
        {
            // Get distinct members of v
            vertices = v.Distinct().ToList();
            globals = g.Distinct().ToList();
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


        /*------------------------------------------------------------------------------------*/
    }
}

