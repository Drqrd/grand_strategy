using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using WorldGeneration.Objects;
using WorldGeneration.TectonicPlate.Objects;
using DataStructures.ViliWonka.KDTree;

using static WorldGeneration.HLZS;

namespace WorldGeneration.Maps
{
    // All values for height are stored as surface and converted to space to avoid rounding errors.
    public class HeightMap : Map
    {
        private const float NULL_VAL = -1f;

        private MeshFilter[] meshFilters;

        private Point[][] map;
        private Color[][] colors;

        public Point[][] Map { get { return map; } }

        public HeightMap(World world) : base(world)
        {
            this.world = world;

            meshFilters = new MeshFilter[world.Sphere.meshFilters.Length];
            map = new Point[meshFilters.Length][];
            colors = new Color[meshFilters.Length][];

            for (int i = 0; i < meshFilters.Length; i++)
            {
                // Initialize
                map[i] = new Point[world.Sphere.meshFilters[i].sharedMesh.vertexCount];
                colors[i] = new Color[world.Sphere.meshFilters[i].sharedMesh.vertexCount];
            }

            for(int a = 0; a < world.Plates.Length; a++)
            {
                for(int b = 0; b < world.Plates[a].Points.Length; b++)
                {
                    Point p = world.Plates[a].Points[b];
                    int gPos = p.GlobalPosition;

                    map[0][gPos] = p;
                    map[0][gPos].Height.Surface = NULL_VAL;
                    map[0][gPos].Height.Space = NULL_VAL;
                }
            }

            FindPointNeighbors();
        }

        public override void Build()
        {
            GameObject parentObj = new GameObject(World.MapDisplay.HeightMap.ToString());
            parentObj.transform.parent = world.transform;
            GameObject obj = new GameObject("Mesh");
            obj.transform.parent = parentObj.transform;

            meshFilters[0] = obj.AddComponent<MeshFilter>();
            meshFilters[0].sharedMesh = new Mesh();
            Vector3[] vertices = world.Sphere.meshFilters[0].sharedMesh.vertices;
            meshFilters[0].sharedMesh.vertices = vertices;
            meshFilters[0].sharedMesh.triangles = world.Sphere.meshFilters[0].sharedMesh.triangles;

            meshFilters[0].sharedMesh.RecalculateNormals();
            // meshFilters[0].sharedMesh.Optimize(); Messes with triangulation

            obj.AddComponent<MeshRenderer>().material = Resources.Load<Material>("Materials/WorldGen/Map");


            SampleSurfaceHeights();
            EvaluateFaultLines();

            List<Point> faultLinePoints = new List<Point>();
            for(int a = 0; a < map.Length; a++)
            {
                for (int b = 0; b < map[a].Length; b++)
                {
                    if(map[a][b].Height.Surface != NULL_VAL) { faultLinePoints.Add(map[a][b]); }
                }
            }

            
            foreach(Point point in faultLinePoints) { Blend(point); }
            CalculateSpaceHeights();
            EvaluateColors(world.Gradients.Height);

            // Set the colors
            meshFilters[0].sharedMesh.colors = colors[0];

            AssignWorldPointValues();
        }

        private void SampleSurfaceHeights()
        {
            for(int a = 0; a < map.Length; a++)
            {
                for (int b = 0; b < map[a].Length; b++)
                {
                    map[a][b].Height.Surface = GetHeight(world.Plates[map[a][b].PlateId].PlateType) + (MAX_HEIGHT * Sample(map[a][b].Pos) / 3f);
                }
            }
        }
        private float GetHeight(Plate.TectonicPlateType plateType)
        {
            return plateType == Plate.TectonicPlateType.Continental ? (MAX_HEIGHT * 0.5f * world.HMParams.CMultiplier) 
                                                                    : (MAX_HEIGHT * 0.2f * world.HMParams.OMultiplier);
        }

        // Recursive blend function
        private void Blend(Point point, int depth = 0)
        {
            if (depth < world.HMParams.BlendDepth)
            {
                float avg = point.Height.Surface;
                // Blend the neighbors, get avg
                for (int a = 0; a < point.Neighbors.Length; a++)
                {
                    avg += point.Neighbors[a].Height.Surface;
                    Blend(point.Neighbors[a], depth + 1); 
                }
                // Blend current point 
                avg /= point.Neighbors.Length + 1;
                point.Height.Surface = avg;
            }
        }

        private void CalculateSpaceHeights()
        {
            for(int a = 0; a < map.Length; a++)
            {
                for(int b = 0; b < map[a].Length; b++)
                {
                    map[a][b].Height.Space = map[a][b].Height.Surface / MAX_HEIGHT;
                }
            }
        }

        public static float ScaleSurfaceToSpace(float input)
        {
            if (input < 0f)
            {
                Debug.LogError("ERROR INPUT MUST BE NEGATIVE.");
                return -1;
            }
            else { return input / MAX_HEIGHT; }
        }

        public void EvaluateFaultLines()
        {
            // Populate the fault lines
            Plate[] plates = world.Plates;
            for (int a = 0; a < plates.Length; a++)
            {
                FaultLine[] faultLines = plates[a].FaultLines;

                // Get the average faultLine heights
                for (int b = 0; b < faultLines.Length; b++)
                {
                    float faultHeights = EvaluateFaultLineHeight(faultLines[b], plates);
                    AssignFaultValues(faultHeights, a, b);
                }
            }
        }

        public float EvaluateFaultLineHeight(FaultLine faultLine, Plate[] plates)
        {
            int index = 0;
            float f;
            if (faultLine.Type == FaultLine.FaultLineType.Convergent) { index += 1; }
            else if (faultLine.Type == FaultLine.FaultLineType.Divergent) { index -= 1; }

            index += plates[faultLine.FaultOf[0]].PlateType == Plate.TectonicPlateType.Continental ? 1 : -1;
            index += plates[faultLine.FaultOf[1]].PlateType == Plate.TectonicPlateType.Continental ? 1 : -1;

            switch (index)
            {
                case 3:
                    f = MAX_HEIGHT;
                    break;
                case 2:
                    f = MAX_HEIGHT * 0.9f;
                    break;
                case 1:
                    f = MAX_HEIGHT * 0.8f;
                    break;
                case 0:
                    f = MAX_HEIGHT * 0.7f;
                    break;
                case -1:
                    f = MAX_HEIGHT * 0.3f;
                    break;
                case -2:
                    f = MAX_HEIGHT * 0.1f;
                    break;
                default:
                    f = MIN_HEIGHT;
                    break;
            }

            return f;
        }

        private void AssignFaultValues(float val, int plateInd, int faultInd)
        {
            FaultLine fl = world.Plates[plateInd].FaultLines[faultInd];
            int[] inds = fl.VertexIndices0.ToList().Distinct().ToArray();
            for(int a = 0; a < inds.Length; a++)
            {
                int pos = world.Plates[plateInd].Points[inds[a]].GlobalPosition;
                map[0][pos].Height.Surface = val;
            }
        }

        public void EvaluateColors(Gradient gradient)
        {
            for (int a = 0; a < map.Length; a++)
            {
                for (int b = 0; b < map[a].Length; b++)
                {
                    colors[a][b] = gradient.Evaluate(map[a][b].Height.Space);
                }
            }
        }

        private void FindPointNeighbors()
        {
            // KD - Tree
            Point[] points = map[0];

            KDTree kdTree = new KDTree(points, 1);

            KDQuery query = new KDQuery();

            foreach (Point point in points)
            {
                List<int> results = new List<int>();
                query.KNearest(kdTree, point.Pos, world.HMParams.NeighborNumber, results);

                List<Point> nn = new List<Point>();
                foreach (int ind in results)
                {
                    nn.Add(points[ind]);
                }

                point.SetNearestNeighbors(nn.ToArray());
            }
            

            // Naive approach
            /*
            Point[] points = map[0];

            List<KeyValuePair<float, int>>[] distanceMap = new List<KeyValuePair<float, int>>[points.Length];

            for (int a = 0; a < distanceMap.Length; a++) { distanceMap[a] = new List<KeyValuePair<float, int>>(); }

            // Map distances
            for (int a = 0; a < points.Length; a++)
            {
                for (int b = a + 1; b < points.Length; b++)
                {
                    float dist = SquareDistance(points[a].Pos, points[b].Pos);
                    distanceMap[a].Add(new KeyValuePair<float, int>(dist, b));
                    distanceMap[b].Add(new KeyValuePair<float, int>(dist, a));
                }
            }

            foreach (List<KeyValuePair<float, int>> d in distanceMap)
            {
                d.Sort(new DuplicateKeyComparer());
            }

            // Set nearest neighbors
            for (int a = 0; a < points.Length; a++)
            {
                Point[] neighbors = new Point[world.HMParams.NeighborNumber];

                for (int b = 0; b < world.HMParams.NeighborNumber; b++)
                {
                    if (b < distanceMap[a].Count)
                    {
                        neighbors[b] = points[distanceMap[a][b].Value];
                    }
                }

                points[a].SetNearestNeighbors(neighbors);
            }
            */
        }

        private void AssignWorldPointValues()
        {
            for (int a = 0; a < world.Plates.Length; a++)
            {
                for (int b = 0; b < world.Plates[a].Points.Length; b++)
                {
                    Point p = world.Plates[a].Points[b];
                    world.Plates[a].Points[b] = map[0][p.GlobalPosition];
                }
            }
        }

        public class DuplicateKeyComparer : IComparer<KeyValuePair<float, int>>
        {
            public int Compare(KeyValuePair<float, int> a, KeyValuePair<float, int> b)
            {
                if (a.Key < b.Key) { return -1; }
                else if (a.Key > b.Key) { return 1; }
                else
                {
                    if (a.Value < b.Value) { return -1; }
                    return 1;
                }
            }
        }

        public static float SquareDistance(Vector3 a, Vector3 b)
        {
            return Mathf.Pow(a.x - b.x, 2f) + Mathf.Pow(a.y - b.y, 2f) + Mathf.Pow(a.z - b.z, 2f);
        }
    }
}

