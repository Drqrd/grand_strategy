using System.Collections.Generic;
using System.Linq;
using UnityEngine;

using WorldGeneration.TectonicPlate.Objects;

namespace WorldGeneration.Maps
{
    public class TerrainMap : Map
    {
        private class MeshData
        {
            public Point[] Points { get; set; }
            public int[] Triangles { get; set; }
            public Color[] Colors { get; set; }
        }

        MeshData Oceans, Continents;

        public TerrainMap(World world) : base(world)
        {
            this.world = world;
            this.Oceans = new MeshData();
            this.Continents = new MeshData();
        }

        public override void Build()
        {
            GameObject parentObj = new GameObject(World.MapDisplay.Terrain.ToString());
            parentObj.transform.parent = world.transform;

            // For separating ocean and continents
            // DetermineOceanAndContinents();

            CreateMesh(parentObj);
            CreateOceanShader(parentObj);
        }

        public void Chunk()
        {
            
        }


        private void DetermineOceanAndContinents()
        {
            // Divide based on continent vs oceanic
            List<int> t_c = new List<int>(), t_o = new List<int>();
            List<Point> p_c = new List<Point>(), p_o = new List<Point>();
            for (int a = 0; a < world.Plates.Length; a++)
            {
                int[] tris = world.Plates[a].Triangles;
                Point[] points = world.Plates[a].Points;
                for (int b = 0; b < tris.Length; b+=3)
                {
                    int[] tri = new int[]{ tris[b], tris[b + 1], tris[b + 2] };
                    // If all point of triangle are above the ocean level, add to c, otherwise add to o
                    if (points[tri[0]].Height.Space >= 0.7f ||
                        points[tri[1]].Height.Space >= 0.7f ||
                        points[tri[2]].Height.Space >= 0.7f) 
                    { 
                        t_c.Add(tri[0]);
                        t_c.Add(tri[1]);
                        t_c.Add(tri[2]);
                        p_c.Add(points[tri[0]]);
                        p_c.Add(points[tri[1]]);
                        p_c.Add(points[tri[2]]);
                    }
                    else 
                    {
                        t_o.Add(tri[0]);
                        t_o.Add(tri[1]);
                        t_o.Add(tri[2]);
                        p_o.Add(points[tri[0]]);
                        p_o.Add(points[tri[1]]);
                        p_o.Add(points[tri[2]]);
                    }
                }
            }

            CondenseVerticesAndTriangles(p_c, t_c, out p_c, out t_c);
            CondenseVerticesAndTriangles(p_o, t_o, out p_o, out t_o);

            Continents.Points = p_c.ToArray();
            Continents.Triangles = t_c.ToArray();
            Oceans.Points = p_o.ToArray();
            Oceans.Triangles = t_o.ToArray();
        }

        private void CondenseVerticesAndTriangles(List<Point> p, List<int> t, out List<Point> points, out List<int> triangles)
        {
            // Get distinct members of v
            points = p.Distinct().ToList();
            for (int i = 0; i < points.Count; i++)
            {
                // For each vertex, if they match the current comparison vertex, change the triangle to the proper index
                for (int j = i; j < p.Count; j++)
                {
                    if (points[i].Pos == p[j].Pos) { t[j] = i; }
                }
            }

            triangles = t;
        }

        private void CreateMesh(GameObject parentObj)
        {
            GameObject obj = new GameObject("Mesh");
            obj.transform.parent = parentObj.transform;

            MeshFilter meshFilter = obj.AddComponent<MeshFilter>();
            meshFilter.sharedMesh = new Mesh();

            obj.AddComponent<MeshRenderer>().material = Materials.Map;

            Color[] colors = Colorize();
            Vector3[] vertices = Elevate();

            meshFilter.sharedMesh.vertices = vertices;
            meshFilter.sharedMesh.triangles = world.Sphere.meshFilters[0].mesh.triangles;
            meshFilter.sharedMesh.colors = colors;

            meshFilter.sharedMesh.RecalculateNormals();

            // For separating ocean and continents
            // CreateContinents(obj);
            // CreateOceans(obj);
        }

        private void CreateContinents(GameObject parentObj)
        {
            GameObject obj = new GameObject("Continents");
            obj.transform.parent = parentObj.transform;

            MeshFilter meshFilter = obj.AddComponent<MeshFilter>();
            meshFilter.sharedMesh = new Mesh();

            obj.AddComponent<MeshRenderer>().material = Materials.Map;

            Color[] colors = Colorize(Continents);
            Vector3[] vertices = Elevate(Continents);

            meshFilter.sharedMesh.vertices = vertices;
            meshFilter.sharedMesh.triangles = Continents.Triangles;
            meshFilter.sharedMesh.colors = colors;

            meshFilter.sharedMesh.RecalculateNormals();
        }

        private void CreateOceans(GameObject parentObj)
        {
            GameObject obj = new GameObject("Oceans");
            obj.transform.parent = parentObj.transform;

            MeshFilter meshFilter = obj.AddComponent<MeshFilter>();
            meshFilter.sharedMesh = new Mesh();

            obj.AddComponent<MeshRenderer>().material = Materials.Map;

            Color[] colors = Colorize(Oceans);
            Vector3[] vertices = Elevate(Oceans);

            meshFilter.sharedMesh.vertices = vertices;
            meshFilter.sharedMesh.triangles = Oceans.Triangles;
            meshFilter.sharedMesh.colors = colors;

            meshFilter.sharedMesh.RecalculateNormals();

        }
        private void CreateOceanShader(GameObject parentObj)
        {
            GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            obj.name = "OceanShader";
            obj.transform.parent = parentObj.transform;

            Object.Destroy(obj.GetComponent<SphereCollider>());

            obj.GetComponent<MeshRenderer>().material = Materials.Ocean;

            float scaleVal = 2f + 2f * 0.7f / 5f;
            obj.transform.localScale = new Vector3(scaleVal, scaleVal, scaleVal);
        }


        private Color[] Colorize()
        {
            Color[] colors = new Color[world.Sphere.meshFilters[0].mesh.vertexCount];
            for(int a = 0; a < world.Plates.Length; a++)
            {
                for(int b = 0; b < world.Plates[a].Points.Length; b++)
                {
                    Point p = world.Plates[a].Points[b];
                    colors[p.GlobalPosition] = world.Gradients.Terrain.Evaluate(p.Height.Space);
                }
            }
            return colors;
        }
        private Color[] Colorize(MeshData meshData)
        {
            Color[] colors = new Color[meshData.Points.Length];
            for(int a = 0; a < colors.Length; a++)
            {
                colors[a] = world.Gradients.Terrain.Evaluate(meshData.Points[a].Height.Space);
            }

            return colors;
        }
        
        private Vector3[] Elevate()
        {
            Vector3[] vertices = new Vector3[world.Sphere.meshFilters[0].mesh.vertexCount];
            for (int a = 0; a < world.Plates.Length; a++)
            {
                for (int b = 0; b < world.Plates[a].Points.Length; b++)
                {
                    Point p = world.Plates[a].Points[b];
                    vertices[p.GlobalPosition] = p.Pos + p.Pos * p.Height.Space / 5f;
                }
            }
            return vertices;
        }

        private Vector3[] Elevate(MeshData meshData)
        {
            Vector3[] vertices = new Vector3[meshData.Points.Length];
            for (int a = 0; a < meshData.Points.Length; a++)
            {
                Point._Height height = meshData.Points[a].Height;

                vertices[a] = meshData.Points[a].Pos + meshData.Points[a].Pos * height.Space / 5f;
            }

            return vertices;
        }
        
    }
}


