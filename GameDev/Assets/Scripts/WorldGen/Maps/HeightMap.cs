using UnityEngine;
using System.Collections;
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
        private float FALLOFF;

        private MeshFilter[] meshFilters;

        private Point[][] map;
        private Color[][] colors;

        public Point[][] Map { get { return map; } }
        public Color[][] Colors { get { return colors; } }

        public HeightMap(World world) : base(world)
        {
            this.world = world;
            this.FALLOFF = MAX_HEIGHT / Mathf.Pow(Mathf.Log10(world.Resolution), 2.5f);
            Debug.Log(FALLOFF);

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
                    map[0][gPos].Height.NeighborRefValue = NULL_VAL;
                }
            }

            for (int a = 0; a < map[0].Length; a++)
            {
                if (map[0][a] == null)
                {
                    Debug.Log(a);
                }
            }

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
            meshFilters[0].sharedMesh.Optimize();

            obj.AddComponent<MeshRenderer>().material = Resources.Load<Material>("Materials/WorldGen/Map");
            
            EvaluateFaultLines();
            FloodSampleSurfaceHeights();
            CalculateSpaceHeights();
            EvaluateColors(world.HeightMapGradient);

            // Set the colors
            meshFilters[0].sharedMesh.colors = colors[0];
        }

        private void FloodSampleSurfaceHeights()
        {
            List<Point> flood = new List<Point>();

            // Enqueue the fault lines
            for (int a = 0; a < map.Length; a++)
            {
                for(int b = 0; b < map[a].Length; b++)
                {
                    if (map[a][b].Height.Surface != NULL_VAL) { flood.Add(map[a][b]); }
                }
            }
            // Flood enqueues neighbors
            while (flood.Count > 1) 
            {
                int index = Random.Range(0, flood.Count - 1);
                Point point = flood[index];
                flood.RemoveAt(index);

                float falloff = FALLOFF * Sample(point.Pos);

                point.Height.Surface = point.Height.NeighborRefValue - falloff > MIN_HEIGHT ? point.Height.NeighborRefValue - falloff : MIN_HEIGHT;

                // if neighbors have null_val for reference height, set the reference height and enqueue neighbor,
                foreach (Point neighbor in point.Neighbors) 
                {
                    if (neighbor.Height.NeighborRefValue == NULL_VAL) 
                    {
                        neighbor.Height.NeighborRefValue = point.Height.Surface;
                        flood.Add(neighbor);
                    }
                }
            }
        }

        private void CalculateSpaceHeights()
        {
            for(int a = 0; a < map.Length; a++)
            {
                for(int b = 0; b < map[a].Length; b++)
                {
                    map[a][b].Height.Space = map[a][b].Height.Surface / MAX_HEIGHT;
                    //map[a][b].Height.Space = Sample(map[a][b].Pos);
                }
            }

            /*
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
            */
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
                    f = MAX_HEIGHT / 1.5f;
                    break;
                case 0:
                    f = MAX_HEIGHT / 2f;
                    break;
                case -1:
                    f = AVG_HEIGHT;
                    break;
                case -2:
                    f = AVG_HEIGHT / 1.5f;
                    break;
                default:
                    f = AVG_HEIGHT /2f;
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
                map[0][pos].Height.NeighborRefValue = val;
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
    }
}

