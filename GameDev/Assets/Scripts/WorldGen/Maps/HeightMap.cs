using UnityEngine;

namespace WorldGeneration.Maps
{
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
}

