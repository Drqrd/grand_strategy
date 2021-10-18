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
    public class HeightMap : NoiseMap
    {
        public enum ContinentSize { Islands, Small, Medium, Large, Enormous, Pangea };
        public float[] distBetweenCenters = new float[] { .2f, .5f, .7f, .9f, 1.2f };
        public float[] continentFalloff = new float[] { 2f, 1f, .8f, .6f, .2f };

        public ContinentSize continentSize { get; set; }
        public Vector3[] continentCenters { get; private set; }
        private Vector3[][] globalVertices;

        // Height parameters (For coloring the map)
        private float minHeight = HLZS.minHeight;
        private float startHeight = HLZS.averageHeight;
        private float maxHeight = HLZS.maxHeight;



        public HeightMap(Transform parent, MeshFilter[] meshFilters) : base(parent, meshFilters)
        {
            this.parent = parent;
            this.meshFilters = meshFilters;

            // Global verts
            globalVertices = new Vector3[meshFilters.Length][];
            for (int i = 0; i < meshFilters.Length; i++)
            {
                globalVertices[i] = new Vector3[meshFilters[i].sharedMesh.vertexCount];
                globalVertices[i] = meshFilters[i].sharedMesh.vertices;
            }
        }

        public override void Build()
        {
            GameObject obj = new GameObject("Height Map");
            obj.transform.parent = parent;

            GenerateContinentCenters();
            GenerateContinents();
        }

        private void GenerateContinentCenters()
        {
            List<Vector3> centers = new List<Vector3>();

            centers.Add(Random.onUnitSphere);

            if (continentSize != ContinentSize.Pangea)
            {
                float minDist = distBetweenCenters[(int)continentSize];
                bool addToCenters;
                int MAX_TRIES = 999, tries = 0;

                while (tries < MAX_TRIES)
                {
                    // Get random point
                    addToCenters = true;
                    Vector3 c = Random.onUnitSphere;

                    // iterate through
                    for (int i = 0; i < centers.Count; i++)
                    {
                        // If distance is larger than allowed, add to tries, tell it to not execute last bit, and break
                        float dist = DistanceBetweenPoints(centers[i], c);

                        if (dist <= minDist)
                        {
                            tries += 1;
                            addToCenters = false;
                        }
                    }

                    if (addToCenters)
                    {
                        centers.Add(c);
                        tries = 0;
                    }
                }
            };

            continentCenters = centers.ToArray();
        }

        // Approach: From the continent center, steadily decrease the average height of the vertex until it hits ocean level.
        //           Variation through sampling
        // 
        // Pseudocode:
        // - for each vertex
        // -    for each continent center, find closest continent center to vertex
        // -    with distance from continent center, apply height falloff + sample
        private void GenerateContinents()
        {
            // Loop through each 
            for (int i = 0; i < globalVertices.Length; i++)
            {

            }
        }

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

        private float DistanceBetweenPoints(Vector3 a, Vector3 b)
        {
            return Mathf.Sqrt(Mathf.Pow(b.x - a.x, 2f) + Mathf.Pow(b.y - a.y, 2f) + Mathf.Pow(b.z - a.z, 2f));
        }

        private Vector3 TriangleCentroid(Vector3 a, Vector3 b, Vector3 c)
        {
            return new Vector3((a.x + b.x + c.x) / 3f, (a.y + b.y + c.y) / 3f, (a.z + b.z + c.z) / 3f);
        }
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
            GameObject obj = new GameObject("Moisture Map");
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
            GameObject obj = new GameObject("Temperature Map");
            obj.transform.parent = parent;
        }
    }
}

