
using UnityEngine;

using static WorldData;

namespace WorldGeneration.Maps
{
    
    public class MoistureMap : Map
    {
        private MeshFilter meshFilter;
        private World.Parameters.Moisture parameters;
        public MoistureMap(World world, Save save) : base(world)
        {
            this.world = world;
            this.save = save;
            this.parameters = world.parameters.moisture;
        }

        public override void Build()
        {
            /*
            WorldData.Mesh meshData = world.worldData.mesh;

            GameObject parentObj = new GameObject(World.MapDisplay.MoistureMap.ToString());
            parentObj.transform.parent = world.transform;

            GameObject obj = new GameObject("Mesh");
            obj.transform.parent = parentObj.transform;


            meshFilter = obj.AddComponent<MeshFilter>();
            meshFilter.sharedMesh = new UnityEngine.Mesh();
            meshFilter.sharedMesh.vertices = meshData.vertices;
            meshFilter.sharedMesh.triangles = meshData.triangles;
            meshFilter.sharedMesh.RecalculateNormals();

            meshFilter.sharedMesh.RecalculateNormals();
            // meshFilters[0].sharedMesh.Optimize(); Messes with triangulation

            obj.AddComponent<MeshRenderer>().material = materials.map;

            GetValues();

            for (int a = 0; a < world.worldData.points.Length; a++)
            {
                Point point = world.worldData.points[a];
                Blend(point);
            }

            Color[] colors = EvaluateColors(parameters.gradient);

            // Set the colors
            meshFilter.sharedMesh.colors = colors;
            */
        }
        /*
        private void GetValues()
        {
            Vector3 randVector = IMath.RandomVector3();

            for (int a = 0; a < world.worldData.points.Length; a++)
            {
                Point point = world.worldData.points[a];
                point.moistureData = new Point.Moisture(); 
                point.moistureData.value = Sample(point.vertex + randVector);
            }
        }
        private Color[] EvaluateColors(Gradient gradient)
        {
            Color[] colors = new Color[world.worldData.points.Length];
            for (int a = 0; a < world.worldData.points.Length; a++)
            {
                Point point = world.worldData.points[a];
                if (world.worldData.plates[point.plateId].plateType == Plate.Type.Continental) { colors[a] = gradient.Evaluate(point.moistureData.value); }
                else { colors[a] = Color.black; }
            }

            return colors;
        }

        private void Blend(Point point, int depth = 0)
        {
            if (depth < parameters.blendDepth)
            {
                float avg = point.moistureData.value;
                int cnt = 0;

                // Blend the neighbors, get avg
                for (int a = 0; a < point.neighbors.Length; a++)
                {
                    if (point.neighbors[a].plate.plateType == Plate.Type.Continental)
                    {
                        avg += point.neighbors[a].moistureData.value;
                        Blend(point.neighbors[a], depth + 1);
                        cnt++;
                    }
                }
                // Blend current point 
                avg /= cnt + 1;
                point.moistureData.value = avg;
            }
        }
        */
    }
}