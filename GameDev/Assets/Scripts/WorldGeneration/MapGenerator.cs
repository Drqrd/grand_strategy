using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MapGenerator
{
    public abstract class NoiseMap
    {
        protected Transform parent;
        protected MeshFilter[] meshFilters;

        public NoiseMap(Transform parent, MeshFilter[] meshFilters)
        {
            this.parent = parent;
            this.meshFilters = meshFilters;
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
    public class TectonicPlateMap : NoiseMap
    {
        private TectonicPlate[] plates;

        public TectonicPlateMap(Transform parent, TectonicPlate[] plates, MeshFilter[] meshFilters = null ) : base(parent, meshFilters)
        {
            this.parent = parent;
            this.plates = plates;

            this.meshFilters = new MeshFilter[plates.Length];
        }

        public override void Build()
        {
            GameObject parentObj = new GameObject(World.MapDisplay.Plates.ToString());
            parentObj.transform.parent = parent;

            for(int i = 0; i < plates.Length; i++)
            {
                GameObject obj = new GameObject("Plate" + i);
                obj.transform.parent = parentObj.transform;

                meshFilters[i] = obj.AddComponent<MeshFilter>();
                meshFilters[i].sharedMesh = plates[i].SharedMesh;
                obj.AddComponent<MeshRenderer>().material = Resources.Load<Material>("Materials/Surface");
            }
        }
    }

    public class HeightMap : NoiseMap
    {
        private Vector3[][] globalVertices;
        private int[][] globalTriangles;

        private float[][] map;

        // Height parameters (For coloring the map)
        private float minHeight = HLZS.minHeight;
        private float startHeight = HLZS.averageHeight;
        private float maxHeight = HLZS.maxHeight;

        public HeightMap(Transform parent, MeshFilter[] meshFilters) : base(parent, meshFilters)
        {
            this.parent = parent;
            this.meshFilters = meshFilters;

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
            obj.transform.parent = parent;
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
    
    public class MoistureMap : NoiseMap
    {
        public MoistureMap(Transform parent, MeshFilter[] meshFilters) : base(parent, meshFilters)
        {
            this.parent = parent;
            this.meshFilters = meshFilters;
        }

        public override void Build()
        {
            GameObject obj = new GameObject(World.MapDisplay.MoistureMap.ToString());
            obj.transform.parent = parent;
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

    public class TemperatureMap : NoiseMap
    {
        public TemperatureMap(Transform parent, MeshFilter[] meshFilters) : base(parent, meshFilters)
        {
            this.parent = parent;
            this.meshFilters = meshFilters;
        }

        public override void Build()
        {
            GameObject obj = new GameObject(World.MapDisplay.TemperatureMap.ToString());
            obj.transform.parent = parent;
        }
    }
}

