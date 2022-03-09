using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using IDict;

using DataStructures.ViliWonka.KDTree;

using static WorldData;

namespace WorldGeneration.Maps
{
    // Approach:
    // - For continents vs oceans, get random distribution of points a set distance from one another.
    // - Decrement height from the continent centers, which will be considered mountains
    
    public class TectonicPlateMap : Map
    {
        private static float[] distBetweenCenters = { .2f, .5f, .7f, .9f, 1.2f };

        public class FaultData
        {
            public FaultData() { }

            private static Color DEFAULT_COLOR = Color.yellow;
            public Color defaultColor { get { return DEFAULT_COLOR; } }
            public LineRenderer[] lines { get; set; }
            public Color[] colors { get; set; }
            public Color[] weightedColors { get; set; }

        }

        public FaultData faultData { get; private set; }

        private World.Parameters.Plates parameters;
        public TectonicPlateMap(World world, SaveData saveData) : base(world)
        {
            this.world = world;
            this.saveData = saveData;

            parameters = world.parameters.plates;
        }

        public override void Build()
        {
            faultData = new FaultData();
            BuildTectonicPlates();
            BuildGameObject();
        }

        private void BuildTectonicPlates()
        {
            world.worldData.triangles = GetTriangles();
            Vector3[] plateCenters = GeneratePlateCenters();
            world.worldData.plates = GeneratePlates(plateCenters);
        }

        private void BuildGameObject()
        {
            WorldData.Plate[] plates = world.worldData.plates;

            GameObject parentObj = new GameObject(World.MapDisplay.TectonicPlateMap.ToString());
            parentObj.transform.parent = world.transform;

            GameObject platesObj = new GameObject("Plates");
            platesObj.transform.parent = parentObj.transform;

            GameObject faultLinesObj = new GameObject("Fault Lines");
            faultLinesObj.transform.parent = parentObj.transform;

            List<LineRenderer> lines = new List<LineRenderer>();
            List<Color> colors = new List<Color>();
            List<Color> weightedColors = new List<Color>();


            for (int i = 0; i < world.worldData.plates.Length; i++)
            {
                GameObject obj = new GameObject("Plate " + i);
                obj.transform.parent = platesObj.transform;

                MeshFilter meshFilter = obj.AddComponent<MeshFilter>();
                meshFilter.sharedMesh = new Mesh();

                meshFilter.sharedMesh.vertices = plates[i].mesh.vertices;
                meshFilter.sharedMesh.triangles = plates[i].mesh.triangles;

                Color randomColor = world.worldData.plates[i].color;
                Color[] clr = new Color[plates[i].mesh.vertices.Length];

                for (int j = 0; j < clr.Length; j++)
                {
                    clr[j] = randomColor;
                }

                meshFilter.sharedMesh.colors = clr;

                meshFilter.sharedMesh.RecalculateNormals();

                obj.AddComponent<MeshRenderer>().material = materials.map;

                List<LineRenderer>[] linesList;
                List<Color>[] color;

                // Build fault lines of plate
                BuildBoundaries(faultLinesObj.transform, i, out linesList, out color);

                // Collapse line and colors
                for (int a = 0; a < linesList.Length; a++)
                {
                    for (int b = 0; b < linesList[a].Count; b++)
                    {
                        lines.Add(linesList[a][b]);
                        colors.Add(color[a][b]);
                    }
                }

                // Get weighted color
                foreach (FaultLine faultLine in world.worldData.plates[i].faultLines)
                {
                    Color wc = FaultLine.FColor(faultLine);
                    for (int b = 0; b < faultLine.edges.Length; b++)
                    {
                        weightedColors.Add(wc);
                    }
                }

            }

            faultData.lines = lines.ToArray();
            faultData.colors = colors.ToArray();
            faultData.weightedColors = weightedColors.ToArray();
        }

        private void BuildBoundaries(Transform parent, int ind, out List<LineRenderer>[] linesList, out List<Color>[] color)
        {
            // List for lines
            FaultLine[] faultLines = world.worldData.plates[ind].faultLines;

            linesList = new List<LineRenderer>[faultLines.Length];
            color = new List<Color>[faultLines.Length];

            // Iterate through FaultLines
            for (int a = 0; a < faultLines.Length; a++)
            {
                GameObject faultLineObj = new GameObject("Fault Line");
                faultLineObj.transform.parent = parent;

                linesList[a] = new List<LineRenderer>();
                color[a] = new List<Color>();

                Color c = Random.ColorHSV();

                // Get all relavent values
                for (int b = 0; b < faultLines[a].edges.Length; b++)
                {
                    Edge edge = faultLines[a].edges[b];
                    linesList[a].Add(BuildLineRenderer(edge, faultLineObj.transform));
                    color[a].Add(c);
                }
            }
        }

        private LineRenderer BuildLineRenderer(Edge edge, Transform parent)
        {
            Vector3[] vertices = new Vector3[10];
            GameObject lineObj = GameObject.Instantiate(Resources.Load<GameObject>("Prefabs/Line"));
            lineObj.transform.parent = parent;
            LineRenderer line = lineObj.GetComponent<LineRenderer>();

            vertices[0] = edge.edge[0].vertex.normalized;
            vertices[vertices.Length - 1] = edge.edge[1].vertex.normalized;

            for (int b = 1; b < vertices.Length - 1; b++)
            {
                vertices[b] = Vector3.Lerp(vertices[0], vertices[vertices.Length - 1], (float)b / vertices.Length).normalized;
            }

            line.positionCount = vertices.Length;
            line.SetPositions(vertices);

            line.startColor = faultData.defaultColor;
            line.endColor = faultData.defaultColor;

            return line;
        }

        /*--- ---*/
        // Empty for now
        public void Load()
        {
            GameObject parentObj = new GameObject(World.MapDisplay.TectonicPlateMap.ToString());
            parentObj.transform.parent = world.transform;

            GameObject platesObj = new GameObject("Plates");
            platesObj.transform.parent = parentObj.transform;

            GameObject faultLinesObj = new GameObject("Fault Lines");
            faultLinesObj.transform.parent = parentObj.transform;
        }



        /* ---  --- */

        private Triangle[] GetTriangles()
        {
            List<Triangle> ts = new List<Triangle>();
            MeshData meshData = world.worldData.meshData;

            for (int a = 0; a < meshData.triangles.Length; a += 3)
            {
                Point[] pts = new Point[3];
                int[] tris = new int[3];

                tris[0] = meshData.triangles[a + 0];
                tris[1] = meshData.triangles[a + 1];
                tris[2] = meshData.triangles[a + 2];

                pts[0] = world.worldData.points[tris[0]];
                pts[1] = world.worldData.points[tris[1]];
                pts[2] = world.worldData.points[tris[2]];

                ts.Add(new Triangle(pts, tris));
            }

            return ts.ToArray();
        }

        // Generation of plate centers
        private Vector3[] GeneratePlateCenters()
        {
            List<Vector3> centers = new List<Vector3>();

            centers.Add(Random.onUnitSphere);

            float minDist = distBetweenCenters[(int)parameters.plateSize];
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
                    float dist = Vector3.Distance(centers[i], c);

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
        private Plate[] GeneratePlates(Vector3[] plateCenters)
        {
            World.Parameters.Plates parameters = world.parameters.plates;

            // Initialization
            Plate[] plates = new Plate[plateCenters.Length];

            // Build plates
            for (int i = 0; i < plateCenters.Length; i++)
            {
                plates[i] = new Plate(plateCenters[i], parameters.continentalVsOceanic, i);
            }

            KDQueryCenters(plates);

            GetPlatePointsAndTriangles(plates);
            GenerateFaultLines(plates);
            return plates;
        }

        // Finds majority of Centers using radius query, any remaining triangles are found and assigned later
        private void KDQueryCenters(Plate[] plates)
        {
            Triangle[] triangles = world.worldData.triangles;
            List<Point> pointCloud = new List<Point>();

            for (int a = 0; a < triangles.Length; a++) 
            { 
                pointCloud.Add(new Point(triangles[a].triangleCenter, a)); 
            }

            // KD - Tree to find majority of plates
            KDTree kdTree = new KDTree(pointCloud.ToArray(), 1);

            KDQuery query = new KDQuery();

            // K-Nearest Query
            int avgPlateSize = world.parameters.resolution / plates.Length;

            float[] plateModifier = new float[plates.Length];
            float total = 0f;
            for(int a = 0; a < plates.Length; a++) 
            { 
                plateModifier[a] = Random.Range(0.5f, 1.5f);
                total += plateModifier[a];
            }
            for(int a = 0; a < plates.Length; a++)
            {
                int modifier = Mathf.RoundToInt(plateModifier[a] / total * avgPlateSize);

                List<int> resultInds = new List<int>();
                query.KNearest(kdTree, plates[a].center, modifier, resultInds);
                for(int b = 0; b < resultInds.Count; b++)
                {
                    if (triangles[resultInds[b]].plate == null) { triangles[resultInds[b]].plate = plates[a]; }
                }
            }

            /* 
            // Radius Query
            float radius = distBetweenCenters[(int)parameters.plateSize] / 1.25f;
            for(int a = 0; a < plates.Length; a++)
            {
                List<int> resultIdxs = new List<int>();
                query.Radius(kdTree, plates[a].center, radius, resultIdxs);
                foreach(int ind in resultIdxs) 
                {
                    if (triangles[ind].plate == null)
                    {
                        triangles[ind].plate = plates[a];
                    }
                }
            }
            */

            FindRemainingClosestCenters(plates, triangles);
        }

        private void FindRemainingClosestCenters(Plate[] plates, Triangle[] triangles)
        {

            SortedList<float, Triangle>[] map = new SortedList<float, Triangle>[plates.Length];
            for(int a = 0; a < plates.Length; a++) { map[a] = new SortedList<float, Triangle>(); }

            for(int a = 0; a < triangles.Length; a++)
            {
                if (triangles[a].plate == null)
                {
                    for (int b = 0; b < plates.Length; b++)
                    {
                        float dist = Vector3.Distance(triangles[a].triangleCenter, plates[b].center);
                        while (map[b].ContainsKey(dist)) { dist += .00001f; }
                        map[b].Add(dist, triangles[a]);
                    }
                }
            }

            while(true)
            {
                int cnt = 0;
                for (int a = 0; a < map.Length; a++)
                {
                    if (map[a].Count > 0)
                    {
                        Triangle triangle = map[a].Values[0];

                        if (triangle.plate == null) { triangle.plate = plates[a]; }

                        map[a].RemoveAt(0);
                    }
                    else { cnt += 1; }
                }

                if (cnt == map.Length) { break; }
            }

            /*
            List<int> ids = new List<int>();
            List<Triangle> centers = new List<Triangle>();
            for (int a = 0; a < plates.Length; a++)
            {
                int id = Random.Range(0,triangles.Length - 1);
                while (ids.Contains(id))
                {
                    ids.Remove(id);
                    id = Random.Range(0, triangles.Length - 1);
                    ids.Add(id);
                }

                centers.Add(triangles[id]);
            }

            // Iterative Floodfill
            Queue<Triangle> queue = new Queue<Triangle>();
            for (int a = 0; a < plates.Length; a++)
            {
                centers[a].plate = plates[a];
                queue.Enqueue(centers[a]);
            }

            while (queue.Count > 0)
            {
                Triangle t = queue.Dequeue();
                foreach (Triangle neighbor in t.neighbors)
                {
                    if (neighbor.plate == null)
                    {
                        neighbor.plate = t.plate;
                        queue.Enqueue(neighbor);
                    }
                }
            }
            */

            // Assign triangles
            List<Triangle>[] tArrs = new List<Triangle>[plates.Length];
            for (int a = 0; a < tArrs.Length; a++)
            {
                tArrs[a] = new List<Triangle>();
            }

            foreach (Triangle triangle in triangles)
            {
                tArrs[triangle.plate.id].Add(triangle);
            }

            for (int a = 0; a < plates.Length; a++)
            {
                plates[a].triangles = tArrs[a].ToArray();
            }
        }   

        private void GetPlatePointsAndTriangles(Plate[] plates)
        {
            int plateInd = 0;
            foreach (Plate plate in plates)
            {
                Dictionary<Point, int> map = new Dictionary<Point, int>();
                Dictionary<int[], int> triMap = new Dictionary<int[], int>();

                for (int a = 0; a < plate.triangles.Length; a++)
                {
                    Triangle triangle = plate.triangles[a];
                    for (int b = 0; b < triangle.points.Length; b++)
                    {
                        triangle.points[b].plateId = plateInd;
                        if (map.ContainsKey(triangle.points[b])) { map[triangle.points[b]] += 1; }
                        else { map.Add(triangle.points[b], 1); }
                    }

                    triMap.Add(triangle.triangles, 1);
                }

                List<Point> points = new List<Point>();
                foreach (KeyValuePair<Point, int> kvp in map)
                {
                    points.Add(kvp.Key);
                }

                List<int> triangles = new List<int>();
                foreach (KeyValuePair<int[], int> kvp in triMap)
                {
                    int[] tris = kvp.Key;
                    triangles.Add(tris[0]);
                    triangles.Add(tris[1]);
                    triangles.Add(tris[2]);
                }

                plate.mesh = new Plate.Mesh(world.worldData.meshData.vertices, triangles.ToArray());
                plateInd++;
            }
        }

        // Populates the Plates with fault lines containing all edges
        public void GenerateFaultLines(Plate[] plates)
        {
            Edge[][] boundaryEdges = new Edge[plates.Length][];

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
                    edges.Add(boundaryEdges[a][b]);
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
                        currEdge.edge[1] = edges[b].edge[0];
                        // Sort by edgeOf[0]
                        if (currEdge.edgeOf[0] > currEdge.edgeOf[1])
                        {
                            int temp = currEdge.edgeOf[0];
                            currEdge.edgeOf[0] = currEdge.edgeOf[1];
                            currEdge.edgeOf[1] = temp;

                            Point tempI = currEdge.edge[0];
                            currEdge.edge[0] = currEdge.edge[1];
                            currEdge.edge[1] = tempI;
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
                foreach (Edge edge in edgeList)
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

            foreach (List<Edge> edgeList in faultEdges)
            {
                Plate p1 = plates[edgeList[0].edgeOf[0]];
                Plate p2 = plates[edgeList[0].edgeOf[1]];
                faultLines.Add(new FaultLine(edgeList.ToArray(), p1, p2));
            }

            edgeMap = null;

            // Organize fault lines into seperate lists for each plate
            List<FaultLine>[] faultLineMap = new List<FaultLine>[plates.Length];
            // Initialize
            for (int a = 0; a < faultLineMap.Length; a++) { faultLineMap[a] = new List<FaultLine>(); }

            // Sort fault lines
            foreach (FaultLine faultLine in faultLines)
            {
                int lineOf = faultLine.edges[0].edgeOf[0];

                faultLineMap[lineOf].Add(faultLine);
            }

            // Assign values
            for (int a = 0; a < plates.Length; a++)
            {
                plates[a].faultLines = faultLineMap[a].ToArray();
            }

            // Make plates determine their type
            foreach (Plate plate in plates)
            {
                foreach (FaultLine faultLine in plate.faultLines)
                {
                    // world reference for plates
                    faultLine.DetermineFaultLineType(world);
                }
            }
        }

        // Edge custom sorting function
        private int SortAscendingByIndexOne(Edge a, Edge b)
        {
            int aa = a.edgeOf[1];
            int bb = b.edgeOf[1];

            if (aa > bb) { return 1; }
            else if (aa == bb) { return 0; }
            else { return -1; }
        }


        // Finds the boundary edges for a given plate

        private Edge[] FindBoundaryEdges(Plate plate)
        {

            List<Edge> edges = new List<Edge>();
            Dictionary<Edge, int> map = new Dictionary<Edge, int>(new EdgeCompareOverride());
            foreach (Triangle triangle in plate.triangles)
            {
                Edge[] es = new Edge[] { new Edge (triangle.points[0], triangle.points[1]),
                                                new Edge (triangle.points[1], triangle.points[2]),
                                                new Edge (triangle.points[2], triangle.points[0]) };

                Edge[] esInv = new Edge[] { new Edge (triangle.points[1], triangle.points[0]),
                                                new Edge (triangle.points[2], triangle.points[1]),
                                                new Edge (triangle.points[0], triangle.points[2]) };

                for (int j = 0; j < es.Length; j++)
                {
                    if (map.ContainsKey(es[j])) { map[es[j]] += 1; }
                    else if (map.ContainsKey(esInv[j])) { map[esInv[j]] += 1; }
                    else { map.Add(es[j], 1); }
                }
            }

            // Add
            foreach (KeyValuePair<Edge, int> entry in map)
            {
                entry.Key.edge[0].plate = plate;
                entry.Key.edge[1].plate = plate;

                uint num = uint.Parse(string.Join("", entry.Value));
                if (num == 1) { edges.Add(entry.Key); }
            }

            // Reorder edges so that they connect
            for (int i = 0; i < edges.Count - 1; i++)
            {
                for (int j = i + 2; j < edges.Count; j++)
                {
                    if (edges[j].edge[0] == edges[i].edge[1])
                    {
                        Edge tempEdge = edges[j];
                        edges[j] = edges[i + 1];
                        edges[i + 1] = tempEdge;
                        continue;
                    }
                }
            }

            return edges.ToArray();
        }
    }
}


