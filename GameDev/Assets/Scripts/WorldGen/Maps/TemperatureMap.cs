using UnityEngine;
using System.Collections.Generic;

using WorldGeneration.TectonicPlate.Objects;

namespace WorldGeneration.Maps
{
    public class TemperatureMap : Map
    {
        private MeshFilter[] meshFilters;

        private Point[][] map;
        private Color[][] colors;

        public TemperatureMap(World world) : base(world)
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
            GameObject parentObj = new GameObject(World.MapDisplay.TemperatureMap.ToString());
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
            
            EvaluateColors(world.Gradients.Temperature);

            // Set the colors
            meshFilters[0].sharedMesh.colors = colors[0];
        }
        public void EvaluateColors(Gradient gradient)
        {
            for (int a = 0; a < map.Length; a++)
            {
                for (int b = 0; b < map[a].Length; b++)
                {
                    float longitude = 1.1f - Mathf.Abs(map[a][b].Pos.y);
                    if (map[a][b].Height.Space >= 0.7f) { longitude -= .1f * map[a][b].Height.Space; }
                    else { longitude += 0.05f * map[a][b].Height.Space; }
                    colors[a][b] = gradient.Evaluate(longitude);
                }
            }
        }
    }
}

