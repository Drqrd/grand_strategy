using UnityEngine;
using System.Collections.Generic;

using TectonicPlateObjects;

namespace MapGenerator
{
    public abstract class Map
    {
        protected World world;
        protected MeshFilter[] meshFilters;

        public Map(World world)
        {
            this.world = world;
            this.meshFilters = world.Sphere.meshFilters;
        }

        public abstract void Build();

        protected virtual float Sample(Vector3 v)
        {
            return Noise.Sum(Noise.methods[3][2], v, 1, 8, 2f, 0.5f).value;
        }
    }


    // Approach:
    // - For continents vs oceans, get random distribution of points a set distance from one another.
    // - Decrement height from the continent centers, which will be considered mountains
    public class TectonicPlateMap : Map
    {
        public LineRenderer[] Lines { get; private set; }

        public TectonicPlateMap(World world) : base(world)
        {
            this.world = world;

            meshFilters = new MeshFilter[world.PlateCenters.Length];
        }

        public override void Build()
        {
            GameObject parentObj = new GameObject(World.MapDisplay.Plates.ToString());
            parentObj.transform.parent = world.transform;

            // Boundary Stuff
            GameObject boundariesObj = new GameObject("Boundaries");
            boundariesObj.transform.parent = parentObj.transform;
            BuildBoundaries(boundariesObj.transform);

            for (int i = 0; i < world.Plates.Length; i++)
            {
                GameObject obj = new GameObject("Plate " + i);
                obj.transform.parent = parentObj.transform;

                meshFilters[i] = obj.AddComponent<MeshFilter>();
                meshFilters[i].sharedMesh = world.Plates[i].SharedMesh;
                obj.AddComponent<MeshRenderer>().material = Resources.Load<Material>("Materials/Surface");
            }
        }

        private void BuildBoundaries(Transform parent)
        {
            BuildLineObjects(parent);
            GenerateWeightedBoundaryColors();
        }

        private void BuildLineObjects(Transform parent)
        {
            List<LineRenderer> _lines = new List<LineRenderer>();
            for (int a = 0; a < world.PlateBoundaries.Edges.Length; a++)
            {
                GameObject lineObj = GameObject.Instantiate(Resources.Load<GameObject>("Prefabs/Line"));
                lineObj.name = "Line " + a;
                lineObj.transform.parent = parent;

                LineRenderer line = lineObj.GetComponent<LineRenderer>();

                Vector3[] vertices = new Vector3[100];

                // Get all relavent values
                Edge edge = world.PlateBoundaries.Edges[a];
                vertices[0] = edge.edge[0].normalized;
                vertices[99] = edge.edge[1].normalized;

                for(int b = 1; b < vertices.Length - 1; b++)
                {
                    vertices[b] = Vector3.Lerp(vertices[0], vertices[99], b / 100).normalized;
                }

                line.positionCount = 100;
                line.SetPositions(vertices);

                line.startColor = world.PlateBoundaries.DefaultColor;
                line.endColor = world.PlateBoundaries.DefaultColor;

                _lines.Add(line);
            }

            Lines = _lines.ToArray();
            _lines.Clear();
        }

        private void GenerateWeightedBoundaryColors()
        {

        }
    }

    public class HeightMap : Map
    {
        private Vector3[][] globalVertices;
        private int[][] globalTriangles;

        private float[][] map;

        // Height parameters (For coloring the map)
        private float minHeight = HLZS.minHeight;
        private float startHeight = HLZS.averageHeight;
        private float maxHeight = HLZS.maxHeight;

        public HeightMap(World world) : base(world)
        {
            this.world = world;
            // Global verts, triangles and map
            globalVertices = new Vector3[meshFilters.Length][];
            globalTriangles = new int[meshFilters.Length][];
            map = new float[globalVertices.Length][];
            for (int i = 0; i < meshFilters.Length; i++)
            {
                globalVertices[i] = new Vector3[meshFilters[i].sharedMesh.vertexCount];
                globalTriangles[i] = new int[meshFilters[i].sharedMesh.triangles.Length];
                globalVertices[i] = meshFilters[i].sharedMesh.vertices;
                globalTriangles[i] = meshFilters[i].sharedMesh.triangles;

                map[i] = new float[globalVertices[i].Length];
            }
        }

        public override void Build()
        {
            GameObject obj = new GameObject(World.MapDisplay.HeightMap.ToString());
            obj.transform.parent = world.transform;
        }

        

        // Approach: From the continent center, steadily decrease the average height of the vertex until it hits ocean level.
        //           Variation through sampling
        // 
        // Pseudocode:
        // - for each vertex
        // -    for each continent center, find closest continent center to vertex
        // -    with distance from continent center, apply height falloff + sample
        

        /*
        private void AddHeight()
        {
            for (int a = 0; a < meshFilters.Length; a++)
            {
                Vector3[] vertices = meshFilters[a].sharedMesh.vertices;

                for (int i = 0; i < vertices.Length; i++)
                {
                    vertices[i] *= ((Sample(vertices[i]) * .1f + 1f));
                }

                meshFilters[a].sharedMesh.vertices = vertices;
                meshFilters[a].sharedMesh.RecalculateNormals();
                meshFilters[a].sharedMesh.Optimize();
            }
        }
        */
    }
    
    public class MoistureMap : Map
    {
        public MoistureMap(World world) : base(world)
        {
            this.world = world;
        }

        public override void Build()
        {
            GameObject obj = new GameObject(World.MapDisplay.MoistureMap.ToString());
            obj.transform.parent = world.transform;
        }

        private void GenerateColors()
        {
            for (int i = 0; i < meshFilters.Length; i++)
            {
                Color[] colors = new Color[meshFilters[i].mesh.vertexCount];
                for (int j = 0; j < colors.Length; j += 3)
                {
                    colors[j] = new Color(Random.Range(0f, 1f), Random.Range(0f, 1f), Random.Range(0f, 1f));
                }

                meshFilters[i].mesh.colors = colors;
            }
        }
    }

    public class TemperatureMap : Map
    {
        public TemperatureMap(World world) : base(world)
        {
            this.world = world;
        }

        public override void Build()
        {
            GameObject obj = new GameObject(World.MapDisplay.TemperatureMap.ToString());
            obj.transform.parent = world.transform;
        }
    }
}

