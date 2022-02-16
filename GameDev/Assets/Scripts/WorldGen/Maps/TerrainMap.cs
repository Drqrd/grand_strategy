using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using WorldGeneration.TectonicPlate.Objects;
using static WorldGeneration.HLZS;

namespace WorldGeneration.Maps
{
    public class TerrainMap : Map
    {
        private MeshFilter[] meshFilters;

        private Vector3[][] vertices;
        private Color[][] colors;

        public TerrainMap(World world) : base(world)
        {
            this.world = world;

            meshFilters = new MeshFilter[world.Sphere.meshFilters.Length];
            vertices = new Vector3[meshFilters.Length][];
            colors = new Color[meshFilters.Length][];

            for (int i = 0; i < meshFilters.Length; i++)
            {
                // Initialize
                vertices[i] = new Vector3[world.Sphere.meshFilters[i].sharedMesh.vertexCount];
                colors[i] = new Color[world.Sphere.meshFilters[i].sharedMesh.vertexCount];
            }
        }

        public override void Build()
        {
            GameObject parentObj = new GameObject(World.MapDisplay.Terrain.ToString());
            parentObj.transform.parent = world.transform;
            GameObject obj = new GameObject("Mesh");
            obj.transform.parent = parentObj.transform;

            meshFilters[0] = obj.AddComponent<MeshFilter>();
            meshFilters[0].sharedMesh = new Mesh();
            vertices[0] = world.Sphere.meshFilters[0].sharedMesh.vertices;
            meshFilters[0].sharedMesh.vertices = vertices[0];
            meshFilters[0].sharedMesh.triangles = world.Sphere.meshFilters[0].sharedMesh.triangles;

            meshFilters[0].sharedMesh.RecalculateNormals();

            obj.AddComponent<MeshRenderer>().material = Resources.Load<Material>("Materials/WorldGen/Map");

            Colorize();
            Elevate();

            meshFilters[0].sharedMesh.vertices = vertices[0];
            meshFilters[0].sharedMesh.colors = colors[0];
        }

        private void Colorize()
        {
            for(int a = 0; a < colors.Length; a++)
            {
                for(int b = 0;  b < colors[a].Length; b++)
                {
                    Point._Height height = world._HeightMap.Map[a][b].Height;
                    colors[a][b] = world.Gradients.Terrain.Evaluate(height.Space);
                }
            }
        }

        private void Elevate()
        {
            for (int a = 0; a < vertices.Length; a++)
            {
                for (int b = 0; b < vertices[a].Length; b++)
                {
                    Point._Height height = world._HeightMap.Map[a][b].Height;

                    vertices[a][b] += vertices[a][b] * height.Space;
                }
            }
        }
    }
}


