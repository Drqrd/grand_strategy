
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using DataStructures.ViliWonka.KDTree;

using static WorldGeneration.HLZS;

using static WorldData;

namespace WorldGeneration.Maps
{
    
    // All values for height are stored as surface and converted to space to avoid rounding errors.
    public class HeightMap : Map
    {
        private const float NULL_VAL = -1f;
        private World.Parameters.Height parameters;
        private MeshFilter meshFilter;

        public HeightMap(World world, Save save) : base(world)
        {
            this.world = world;
            parameters = world.parameters.height;  
            this.save = save;
        }

        public override void Build()
        {
            /*
            for (int a = 0; a < world.worldData.points.Length; a++)
            {
                Point point = world.worldData.points[a];
                point.heightData = new Point.Height(NULL_VAL);
            }

            SampleSurfaceHeights();

            Point[] faultLinePoints = EvaluateFaultLines(world.worldData.plates);
            foreach (Point point in faultLinePoints) { Blend(point); }

            CalculateSpaceHeights();

            BuildGameObject();
            */
        }

        private void BuildGameObject()
        {
            Mesh meshData = world.worldData.mesh;

            GameObject parentObj = new GameObject(World.MapDisplay.HeightMap.ToString());
            parentObj.transform.parent = world.transform;
            GameObject obj = new GameObject("Mesh");
            obj.transform.parent = parentObj.transform;

            meshFilter = obj.AddComponent<MeshFilter>();
            meshFilter.sharedMesh = new UnityEngine.Mesh();
            meshFilter.sharedMesh.vertices = meshData.vertices;
            meshFilter.sharedMesh.triangles = meshData.triangles;
            meshFilter.sharedMesh.RecalculateNormals();

            // meshFilter.sharedMesh.Optimize(); Messes with triangulation

            obj.AddComponent<MeshRenderer>().material = materials.map;

            // Color[] colors = EvaluateColors(parameters.gradient);
            // meshFilter.sharedMesh.colors = colors;
        }
        /*
        private void SampleSurfaceHeights()
        {
            Point[] points = world.worldData.points;
            for(int a = 0; a < points.Length; a++)
            {
                if (points[a].plate != null)
                {
                    points[a].heightData.surface = GetHeight(points[a].plate.plateType)
                                                 + (MAX_HEIGHT * Sample(points[a].vertex) / 3f);
                }
            }
        }
        private float GetHeight(Plate.Type plateType)
        {
            return plateType == Plate.Type.Continental ? (MAX_HEIGHT * 0.5f * parameters.cMultiplier) 
                                                       : (MAX_HEIGHT * 0.2f * parameters.oMultiplier);
        }

        // Recursive blend function
        private void Blend(Point point, int depth = 0)
        {
            if (depth < parameters.blendDepth)
            {
                float avg = point.heightData.surface;
                // Blend the neighbors, get avg
                for (int a = 0; a < point.neighbors.Length; a++)
                {
                    avg += point.neighbors[a].heightData.surface;
                    Blend(point.neighbors[a], depth + 1); 
                }
                // Blend current point 
                avg /= point.neighbors.Length + 1;
                point.heightData.surface = avg;
            }
        }

        private void CalculateSpaceHeights()
        {
            for(int a = 0; a < world.worldData.points.Length; a++)
            {
                Point point = world.worldData.points[a];
                point.heightData.space = point.heightData.surface / MAX_HEIGHT;
            }
        }

        public static float ScaleSurfaceToSpace(float input)
        {
            if (input < 0f)
            {
                UnityEngine.Debug.LogError("ERROR INPUT MUST BE NEGATIVE.");
                return -1;
            }
            else { return input / MAX_HEIGHT; }
        }

        public Point[] EvaluateFaultLines(Plate[] plates)
        {
            // Populate the fault lines
            List<Point> flp = new List<Point>();
            for (int a = 0; a < plates.Length; a++)
            {
                FaultLine[] faultLines = plates[a].faultLines;
                // Get the average faultLine heights
                for (int b = 0; b < faultLines.Length; b++)
                {
                    float faultHeights = EvaluateFaultLineHeight(faultLines[b], plates);
                    List<Point> tempFlp = AssignFaultValues(faultHeights, a, b);
                    flp = flp.Concat(tempFlp).ToList();
                }
            }

            return flp.Distinct().ToArray();
        }

        public float EvaluateFaultLineHeight(FaultLine faultLine, Plate[] plates)
        {
            int index = 0;
            float f;
            if (faultLine.type == FaultLine.Type.Convergent) { index += 1; }
            else if (faultLine.type == FaultLine.Type.Divergent) { index -= 1; }

            index += faultLine.faultOf[0].plateType == Plate.Type.Continental ? 1 : -1;
            index += faultLine.faultOf[1].plateType == Plate.Type.Continental ? 1 : -1;

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

        private List<Point> AssignFaultValues(float val, int plateInd, int faultInd)
        {
            List<Point> flp = new List<Point>();
            FaultLine fl = world.worldData.plates[plateInd].faultLines[faultInd];
            Edge[] edges = fl.edges;
            for(int a = 0; a < edges.Length; a++)
            {
                foreach(Point point in edges[a].edge) 
                { 
                    point.heightData.surface = val;
                    flp.Add(point);
                }
            }
            return flp;
        }

        public Color[] EvaluateColors(Gradient gradient)
        {
            Color[] colors = new Color[world.worldData.points.Length];
            for (int a = 0; a < colors.Length; a++)
            {
                colors[a] = gradient.Evaluate(world.worldData.points[a].heightData.space);
            }

            return colors;
        }

        public void Load()
        {
            GameObject parentObj = new GameObject(World.MapDisplay.HeightMap.ToString());
            parentObj.transform.parent = world.transform;
            GameObject obj = new GameObject("Mesh");
            obj.transform.parent = parentObj.transform;

            MeshFilter meshFilter = obj.AddComponent<MeshFilter>();
            meshFilter.sharedMesh = new UnityEngine.Mesh();
            Vector3[] vertices = new Vector3[1];
            meshFilter.sharedMesh.vertices = vertices;
            meshFilter.sharedMesh.triangles = new int[1];
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
        */
    }

}
