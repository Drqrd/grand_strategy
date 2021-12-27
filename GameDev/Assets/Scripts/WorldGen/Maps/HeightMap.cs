using UnityEngine;
using System.Collections.Generic;
using System.Linq;

using WorldGeneration.Objects;
using WorldGeneration.TectonicPlate.Objects;

using static WorldGeneration.HLZS;

namespace WorldGeneration.Maps
{
    // All values for height are stored as surface and converted to space to avoid rounding errors.
    public class HeightMap : Map
    {
        private const float NULL_VAL = -1f;

        private MeshFilter[] meshFilters;

        private Vector3[][] globalVertices;
        private int[][] globalTriangles;

        private Point[][] map;
        private Color[][] colors;

        public Point[][] Map { get { return map; } }
        public Color[][] Colors { get { return colors; } }

        public HeightMap(World world) : base(world)
        {
            this.world = world;

            meshFilters = new MeshFilter[world.PlateCenters.Length];

            globalVertices = new Vector3[meshFilters.Length][];
            globalTriangles = new int[meshFilters.Length][];
            map = new Point[meshFilters.Length][];
            colors = new Color[meshFilters.Length][];

            for (int i = 0; i < meshFilters.Length; i++)
            {
                // Duplicate vertices and triangles
                globalVertices[i] = new Vector3[world.Plates[i].SharedMesh.vertexCount];
                globalVertices[i] = world.Plates[i].SharedMesh.vertices;
                globalTriangles[i] = new int[world.Plates[i].SharedMesh.triangles.Length];
                globalTriangles[i] = world.Plates[i].SharedMesh.triangles;

                // Initialize
                map[i] = new Point[globalVertices[i].Length];
                colors[i] = new Color[globalVertices[i].Length];

                for (int j = 0; j < map[i].Length; j++)
                {
                    map[i][j] = world.Plates[i].Points[j];
                    map[i][j].Height.Surface = NULL_VAL;
                    map[i][j].Height.Space = NULL_VAL;
                    map[i][j].Height.NeighborRefValue = NULL_VAL;
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
            // FloodSampleSurfaceHeights();
            // CalculateSpaceHeights();
            EvaluateColors(world.HeightMapGradient);

            // Set the colors
            for (int i = 0; i < world.Plates.Length; i++)
            {
                meshFilters[i].sharedMesh.colors = colors[i];
            }
        }

        private void FloodSampleSurfaceHeights()
        {
            Queue<Point> floodQueue = new Queue<Point>();

            // Enqueue the fault lines
            for (int a = 0; a < map.Length; a++)
            {
                for(int b = 0; b < map[a].Length; b++)
                {
                    if (map[a][b].Height.Surface != NULL_VAL) { floodQueue.Enqueue(map[a][b]); }
                }
            }

            // Flood enqueues neighbors
            while (floodQueue.Count > 1) 
            { 
                Point point = floodQueue.Dequeue();
                float falloff = CalculateFalloff() * Random.Range(0.75f,1f);

                // if is null_val, will always have a reference value
                if (point.Height.Surface == NULL_VAL) 
                {
                    point.Height.Surface = point.Height.NeighborRefValue - falloff > MIN_HEIGHT ? point.Height.NeighborRefValue - falloff : MIN_HEIGHT; 
                }

                foreach (Point neighbor in point.Neighbors) 
                {
                    if (neighbor.Height.NeighborRefValue != NULL_VAL) 
                    {
                        floodQueue.Enqueue(neighbor);
                        neighbor.Height.NeighborRefValue = point.Height.Surface;
                    }
                }        
            }
        }

        private float CalculateFalloff()
        {
            if (world.Resolution < 100) { return (float)world.Resolution / 1.5f; }
            else if (world.Resolution < 1000) { return Mathf.Sqrt(world.Resolution); }
            else if (world.Resolution < 10000) { return Mathf.Pow(world.Resolution, .4f); }
            else if (world.Resolution < 100000) { return Mathf.Pow(world.Resolution, 1f / 3f); }
            else { return Mathf.Pow(world.Resolution, .25f); }
        }

        private void CalculateSpaceHeights()
        {
            // Get max / min
            float min = map[0][0].Height.Space, max = map[0][0].Height.Space;
            for (int a = 0; a < map.Length; a++)
            {
                for (int b = 0; b < map[a].Length; b++)
                {
                    max = map[a][b].Height.Surface > max ? map[a][b].Height.Surface : max;
                    min = map[a][b].Height.Surface < min ? map[a][b].Height.Surface : min;
                }
            }

            // Normalize 0 - 1
            for (int a = 0; a < map.Length; a++)
            {
                for (int b = 0; b < map[a].Length; b++)
                {
                    map[a][b].Height.Space = (map[a][b].Height.Surface - min) / (max - min);
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
                map[fl.FaultOf[0]][fl.VertexIndices0[a]].Height.Surface = val;
                map[fl.FaultOf[1]][fl.VertexIndices1[a]].Height.Surface = val;
            }
        }


        public void EvaluateColors(Gradient gradient)
        {
            for (int a = 0; a < map.Length; a++)
            {
                for (int b = 0; b < map[a].Length; b++)
                {
                    colors[a][b] = gradient.Evaluate(map[a][b].Height.Surface);
                }
            }
        }
    }
}

