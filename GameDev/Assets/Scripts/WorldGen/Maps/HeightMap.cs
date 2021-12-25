using UnityEngine;
using System.Collections.Generic;
using System.Linq;

using WorldGeneration.Objects;
using WorldGeneration.TectonicPlate.Objects;
using WorldGeneration.TectonicPlate.KDTree;

using static WorldGeneration.HLZS;

namespace WorldGeneration.Maps
{
    // All values for height are stored as surface and converted to space to avoid rounding errors.
    public class HeightMap : Map
    {
        private const float NULL_VAL = 8001f;

        private MeshFilter[] meshFilters;

        private Vector3[][] globalVertices;
        private int[][] globalTriangles;

        private float[][] surfaceMap;
        private float[][] spaceMap;
        private Color[][] colors;

        public float[][] SurfaceMap { get { return surfaceMap; } }
        public float[][] SpaceMap { get { return spaceMap; } }
        public Color[][] Colors { get { return colors; } }

        public HeightMap(World world) : base(world)
        {
            this.world = world;

            meshFilters = new MeshFilter[world.PlateCenters.Length];

            globalVertices = new Vector3[meshFilters.Length][];
            globalTriangles = new int[meshFilters.Length][];
            surfaceMap = new float[meshFilters.Length][];
            spaceMap = new float[meshFilters.Length][];
            colors = new Color[meshFilters.Length][];

            for (int i = 0; i < meshFilters.Length; i++)
            {
                // Duplicate vertices and triangles
                globalVertices[i] = new Vector3[world.Plates[i].SharedMesh.vertexCount];
                globalVertices[i] = world.Plates[i].SharedMesh.vertices;
                globalTriangles[i] = new int[world.Plates[i].SharedMesh.triangles.Length];
                globalTriangles[i] = world.Plates[i].SharedMesh.triangles;

                // Initialize
                surfaceMap[i] = new float[globalVertices[i].Length];
                spaceMap[i] = new float[globalVertices[i].Length];
                colors[i] = new Color[globalVertices[i].Length];

                for (int j = 0; j < surfaceMap[i].Length; j++)
                {
                    surfaceMap[i][j] = NULL_VAL;
                    spaceMap[i][j] = NULL_VAL;

                }
            }
        }

        public override void Build()
        {
            GameObject parentObj = new GameObject(World.MapDisplay.HeightMap.ToString());
            parentObj.transform.parent = world.transform;

            for (int i = 0; i < world.Plates.Length; i++)
            {
                GameObject obj = new GameObject("Plate " + i);
                obj.transform.parent = parentObj.transform;

                meshFilters[i] = obj.AddComponent<MeshFilter>();
                meshFilters[i].sharedMesh = new Mesh();

                meshFilters[i].sharedMesh.vertices = globalVertices[i];
                meshFilters[i].sharedMesh.triangles = globalTriangles[i];

                meshFilters[i].sharedMesh.RecalculateNormals();
                meshFilters[i].sharedMesh.Optimize();

                obj.AddComponent<MeshRenderer>().material = Resources.Load<Material>("Materials/WorldGen/Map");
            }

            EvaluateFaultLines();
            FindPointNeighbors();
            // FloodSampleSurfaceHeights();
            CalculateSpaceHeights();
            EvaluateColors(world.HeightMapGradient);

            // Set the colors
            for (int i = 0; i < world.Plates.Length; i++)
            {
                meshFilters[i].sharedMesh.colors = colors[i];
            }
        }

        private void FindPointNeighbors()
        {
            foreach (Plate plate in world.Plates)
            {
                List<Point> points = plate.Points.ToList();

                for (int a = 0; a < points.Count; a++)
                {
                    List<Point> refPoints = points.Where(x => x != points[a]).ToList();

                }

                Point closestPoint = null;
                float closestDist = Mathf.Infinity;

                Node kdTree = KD.BuildTree(points);
                Point[] closestPoints;
            }
        }

        private void FloodSampleSurfaceHeights()
        {
            Queue<Point> floodQueue = new Queue<Point>();

            for (int a = 0; a < surfaceMap.Length; a++)
            {
                for (int b = 0; b < surfaceMap[a].Length; b++)
                {
                    if (surfaceMap[a][b] != NULL_VAL) { floodQueue.Enqueue(world.Plates[a].Points[b]); }
                }
            }
            
            // Start dequeueing, if there are any neighbors that are not assigned yet, enqueue
            // If enqueued obj is already assigned, do nothing
            while (floodQueue.Count > 0)
            {
                // Flooding
                Point v = floodQueue.Dequeue();
            }
        }

        private void CalculateSpaceHeights()
        {
            // Sample
            /*
            for (int a = 0; a < meshFilters.Length; a++)
            {
                surfaceMap[a] = new float[globalVertices[a].Length];

                for (int i = 0; i < globalVertices[a].Length; i++)
                {
                    surfaceMap[a][i] = Mathf.Clamp(Sample(globalVertices[a][i]), -1f, 1f);
                }
            }
            */

            // Get max / min
            float min = spaceMap[0][0], max = spaceMap[0][0];
            for (int a = 0; a < spaceMap.Length; a++)
            {
                for (int b = 0; b < spaceMap[a].Length; b++)
                {
                    max = surfaceMap[a][b] > max ? surfaceMap[a][b] : max;
                    min = surfaceMap[a][b] < min ? surfaceMap[a][b] : min;
                }
            }

            // Normalize 0 - 1
            for (int a = 0; a < surfaceMap.Length; a++)
            {
                for (int b = 0; b < surfaceMap[a].Length; b++)
                {
                    spaceMap[a][b] = (surfaceMap[a][b] - min) / (max - min);
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

        public static float ScaleSpaceToSurface(float input)
        {
            if (input < 0f || input > 8000f)
            {
                Debug.LogError("ERROR INPUT MUST BE < 0 or > 8000.");
                return -1;
            }
            else { return input * MAX_HEIGHT; }
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
            
            if (plates[faultLine.FaultOf[0]].PlateType == Plate.TectonicPlateType.Continental) { index += 1; }
            else if (plates[faultLine.FaultOf[0]].PlateType == Plate.TectonicPlateType.Oceanic) { index -= 1; }

            switch (index)
            {
                case 2:
                    f = MAX_HEIGHT;
                    break;
                case 1:
                    f = MAX_HEIGHT / 2f;
                    break;
                case 0:
                    f = AVG_HEIGHT;
                    break;
                case -1:
                    f = AVG_HEIGHT / 2f;
                    break;
                case -2:
                    f = MIN_HEIGHT;
                    break;
                default:
                    f = AVG_HEIGHT;
                    break;
            }

            return f;
        }

        private void AssignFaultValues(float val, int plateInd, int faultInd)
        {
            FaultLine fl = world.Plates[plateInd].FaultLines[faultInd];
            for (int a = 0; a < fl.VertexIndices0.Length; a++)
            {
                surfaceMap[fl.FaultOf[0]][fl.VertexIndices0[a]] = val;
                surfaceMap[fl.FaultOf[1]][fl.VertexIndices1[a]] = val;
            }
        }


        public void EvaluateColors(Gradient gradient)
        {
            for (int a = 0; a < surfaceMap.Length; a++)
            {
                for (int b = 0; b < surfaceMap[a].Length; b++)
                {
                    colors[a][b] = gradient.Evaluate(surfaceMap[a][b]);
                }
            }
        }
    }
}

