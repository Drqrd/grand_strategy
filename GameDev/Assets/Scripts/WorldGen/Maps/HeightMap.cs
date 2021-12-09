using UnityEngine;
using static WorldGeneration.HLZS;

namespace WorldGeneration.Maps
{
    public class HeightMap : Map
    {
        private Vector3[][] globalVertices;
        private int[][] globalTriangles;

        private float[][] map;

        public float[][] Map { get { return map; } }

        public HeightMap(World world) : base(world)
        {
            this.world = world;

            meshFilters = new MeshFilter[world.PlateCenters.Length];

            // Global verts, triangles and map
            globalVertices = new Vector3[meshFilters.Length][];
            globalTriangles = new int[meshFilters.Length][];
            map = new float[meshFilters.Length][];
        }

        public override void Build()
        {
            GameObject parentObj = new GameObject(World.MapDisplay.HeightMap.ToString());
            parentObj.transform.parent = world.transform;

            for (int i = 0; i < world.Plates.Length; i++)
            {
                GameObject obj = new GameObject("Plate " + i);
                obj.transform.parent = parentObj.transform;

                meshFilters[i] = obj.AddComponent<MeshFilter>();
                meshFilters[i].sharedMesh = world.Plates[i].SharedMesh;
                obj.AddComponent<MeshRenderer>().material = Resources.Load<Material>("Materials/WorldGen/Map");
            }

            CreateGlobalReferences();
        }

        private void CreateGlobalReferences()
        {
            for (int i = 0; i < meshFilters.Length; i++)
            {
                globalVertices[i] = new Vector3[meshFilters[i].sharedMesh.vertexCount];
                globalTriangles[i] = new int[meshFilters[i].sharedMesh.triangles.Length];
                globalVertices[i] = meshFilters[i].sharedMesh.vertices;
                globalTriangles[i] = meshFilters[i].sharedMesh.triangles;

                map[i] = new float[globalVertices[i].Length];
            }
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
}

