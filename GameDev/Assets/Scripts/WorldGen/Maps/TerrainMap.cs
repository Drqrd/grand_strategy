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

        private Point[][] map;
        private Color[][] colors;

        public TerrainMap(World world) : base(world)
        {
            this.world = world;

            meshFilters = new MeshFilter[world.Sphere.meshFilters.Length];
            colors = new Color[meshFilters.Length][];

            for (int i = 0; i < meshFilters.Length; i++)
            {
                // Initialize
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
            Vector3[] vertices = world.Sphere.meshFilters[0].sharedMesh.vertices;
            meshFilters[0].sharedMesh.vertices = vertices;
            meshFilters[0].sharedMesh.triangles = world.Sphere.meshFilters[0].sharedMesh.triangles;

            meshFilters[0].sharedMesh.RecalculateNormals();

            obj.AddComponent<MeshRenderer>().material = Resources.Load<Material>("Materials/WorldGen/Map");

            Colorize();


        }

        private void Colorize()
        {
            for(int a = 0; a < colors.Length; a++)
            {
                for(int b = 0;  b < colors.Length; b++)
                {
                    Point._Height height = world._HeightMap.Map[a][b].Height;
                    // Using height values from heightMap and the oceanic / plate gradients, create first pass color
                    FirstPass(a, b, height);
                }
            }

            // Set the colors
            meshFilters[0].sharedMesh.colors = colors[0];
        }

        private void FirstPass(int a, int b, Point._Height height)
        {

            if (height.Surface >= SEA_LEVEL)
            {
                Debug.Log(EvaluateContinentalScale(height.Surface));
                colors[a][b] = world.Gradients.Continental.Evaluate(EvaluateContinentalScale(height.Surface));
            }
            else
            {
                colors[a][b] = world.Gradients.Oceanic.Evaluate(EvaluateOceanicScale(height.Surface));
            }
        }
    }
}


