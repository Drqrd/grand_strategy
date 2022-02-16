using UnityEngine;

using WorldGeneration.TectonicPlate.Objects;

namespace WorldGeneration.Maps
{
    public class MoistureMap : Map
    {
        private MeshFilter[] meshFilters;

        private Point[][] map;
        private Color[][] colors;

        public MoistureMap(World world) : base(world)
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

            for (int a = 0; a < world.Plates.Length; a++)
            {
                for (int b = 0; b < world.Plates[a].Points.Length; b++)
                {
                    Point p = world.Plates[a].Points[b];
                    int gPos = p.GlobalPosition;

                    map[0][gPos] = p;
                }
            }
        }

        public override void Build()
        {
            GameObject parentObj = new GameObject(World.MapDisplay.MoistureMap.ToString());
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

            GetValues();

            for (int a = 0; a < map.Length; a++)
            {
                for (int b = 0; b < map[a].Length; b++)
                {
                    if (world.Plates[map[a][b].PlateId].PlateType == Objects.Plate.TectonicPlateType.Continental) { Blend(map[a][b]); }
                }
            }

            EvaluateColors(world.Gradients.Moisture);

            // Set the colors
            meshFilters[0].sharedMesh.colors = colors[0];

            AssignWorldPointValues();
        }

        private void GetValues()
        {
            Vector3 randVector = IMath.RandomVector3();

            for (int a = 0; a < map.Length; a++)
            {
                for (int b = 0; b < map[a].Length; b++)
                {
                    map[a][b].Moisture.Value = Sample(map[a][b].Pos + randVector);
                }
            }
        }
        private void EvaluateColors(Gradient gradient)
        {
            for (int a = 0; a < map.Length; a++)
            {
                for (int b = 0; b < map[a].Length; b++)
                {
                    if (world.Plates[map[a][b].PlateId].PlateType == Objects.Plate.TectonicPlateType.Continental) { colors[a][b] = gradient.Evaluate(map[a][b].Moisture.Value); }
                    else { colors[a][b] = Color.black; }
                }
            }
        }

        private void Blend(Point point, int depth = 0)
        {
            if (depth < world.MMParams.BlendDepth)
            {
                float avg = point.Moisture.Value;
                int cnt = 0;

                // Blend the neighbors, get avg
                for (int a = 0; a < point.Neighbors.Length; a++)
                {
                    if (world.Plates[point.Neighbors[a].PlateId].PlateType == Objects.Plate.TectonicPlateType.Continental)
                    {
                        avg += point.Neighbors[a].Moisture.Value;
                        Blend(point.Neighbors[a], depth + 1);
                        cnt++;
                    }
                }
                // Blend current point 
                avg /= cnt + 1;
                point.Moisture.Value = avg;
            }
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
    }
}