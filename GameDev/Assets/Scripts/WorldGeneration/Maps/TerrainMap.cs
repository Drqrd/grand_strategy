using System.Collections.Generic;
using System.Linq;
using UnityEngine;

using static WorldData;

namespace WorldGeneration.Maps
{
    public class TerrainMap : Map
    {
        private World.Parameters.Terrain parameters;

        public TerrainMap(World world, SaveData saveData) : base(world)
        {
            this.world = world;
            this.saveData = saveData;
            parameters = world.parameters.terrain;
        }

        public override void Build()
        {
            GameObject parentObj = new GameObject(World.MapDisplay.Terrain.ToString());
            parentObj.transform.parent = world.transform;

            CreateGameObject(parentObj);
            CreateOceanShader(parentObj);
        }

        private void CreateGameObject(GameObject parentObj)
        {
            GameObject obj = new GameObject("Mesh");
            obj.transform.parent = parentObj.transform;

            MeshFilter meshFilter = obj.AddComponent<MeshFilter>();
            meshFilter.sharedMesh = new Mesh();

            obj.AddComponent<MeshRenderer>().material = materials.map;

            Color[] colors = Colorize();
            Vector3[] vertices = Elevate();

            meshFilter.sharedMesh.vertices = vertices;
            meshFilter.sharedMesh.triangles = world.worldData.meshData.triangles;
            meshFilter.sharedMesh.colors = colors;

            meshFilter.sharedMesh.RecalculateNormals();
        }

        private void CreateOceanShader(GameObject parentObj)
        {
            GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            obj.name = "OceanShader";
            obj.transform.parent = parentObj.transform;

            Object.Destroy(obj.GetComponent<SphereCollider>());

            obj.GetComponent<MeshRenderer>().material = materials.ocean;

            float scaleVal = 2f + 2f * 0.7f / 5f;
            obj.transform.localScale = new Vector3(scaleVal, scaleVal, scaleVal);
        }


        private Color[] Colorize()
        {
            Color[] colors = new Color[world.worldData.points.Length];
            for (int a = 0; a < world.worldData.points.Length; a++)
            {
                Point p = world.worldData.points[a];
                colors[a] = parameters.gradient.Evaluate(p.heightData.space);
            }
            return colors;
        }

        private Vector3[] Elevate()
        {
            List<Vector3> vs = new List<Vector3>();
            for (int a = 0; a < world.worldData.points.Length; a++)
            {
                Point p = world.worldData.points[a];
                Vector3 v = p.vertex + p.vertex * p.heightData.space / 5f;
                p.terrainData = new Point.Terrain();
                p.terrainData.vertex = v;
                vs.Add(v);
            }

            return vs.ToArray();
        }
    }
}


