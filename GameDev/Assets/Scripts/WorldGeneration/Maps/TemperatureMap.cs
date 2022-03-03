using UnityEngine;

using static WorldData;

namespace WorldGeneration.Maps
{
    public class TemperatureMap : Map
    {
        private MeshFilter meshFilter;
        private World.Parameters.Temperature parameters;
        public TemperatureMap(World world, SaveData saveData) : base(world)
        {
            this.world = world;
            this.saveData = saveData;
            parameters = world.parameters.temperature;
        }

        public override void Build()
        {
            MeshData meshData = world.worldData.meshData;

            GameObject parentObj = new GameObject(World.MapDisplay.TemperatureMap.ToString());
            parentObj.transform.parent = world.transform;

            GameObject obj = new GameObject("Mesh");
            obj.transform.parent = parentObj.transform;

            
            meshFilter = obj.AddComponent<MeshFilter>();
            meshFilter.sharedMesh = new Mesh();
            meshFilter.sharedMesh.vertices = meshData.vertices;
            meshFilter.sharedMesh.triangles = meshData.triangles;
            meshFilter.sharedMesh.RecalculateNormals();

            meshFilter.sharedMesh.RecalculateNormals();
            // meshFilters[0].sharedMesh.Optimize(); Messes with triangulation

            obj.AddComponent<MeshRenderer>().material = materials.map;
            
            Color[] colors = EvaluateColors(parameters.gradient);

            // Set the colors
            meshFilter.sharedMesh.colors = colors;
        }
        public Color[] EvaluateColors(Gradient gradient)
        {
            Color[] colors = new Color[world.worldData.points.Length];
            for(int a = 0; a < world.worldData.points.Length; a++)
            {
                Point point = world.worldData.points[a];
                float longitude = 1.1f - Mathf.Abs(point.vertex.y);
                if (point.heightData.space >= 0.7f) { longitude -= .1f * point.heightData.space; }
                else { longitude += 0.05f * point.heightData.space; }
                colors[a] = gradient.Evaluate(longitude);
            }

            return colors;
        }
    }
}

