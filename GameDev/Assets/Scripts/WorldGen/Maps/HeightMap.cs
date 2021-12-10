using UnityEngine;
using static WorldGeneration.HLZS;

namespace WorldGeneration.Maps
{
    // All values for height are stored as surface and converted to space to avoid rounding errors.
    public class HeightMap : Map
    {
        private MeshFilter[] meshFilters;

        private Vector3[][] globalVertices;

        private float[][] surfaceMap;
        private float[][] spaceMap;

        public float[][] SurfaceMap { get { return surfaceMap; } }
        public float[][] SpaceMap { get { return spaceMap; } }

        public HeightMap(World world) : base(world)
        {
            this.world = world;

            meshFilters = new MeshFilter[world.PlateCenters.Length];

            globalVertices = new Vector3[meshFilters.Length][];
            surfaceMap = new float[meshFilters.Length][];
            spaceMap = new float[meshFilters.Length][];
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
            CalculateHeight();
        }

        private void CreateGlobalReferences()
        {
            for (int i = 0; i < meshFilters.Length; i++)
            {
                globalVertices[i] = new Vector3[meshFilters[i].sharedMesh.vertexCount];
                globalVertices[i] = meshFilters[i].sharedMesh.vertices;

                surfaceMap[i] = new float[globalVertices[i].Length];
                spaceMap[i] = new float[globalVertices[i].Length];
            }
        }

        private void CalculateHeight()
        {
            for (int a = 0; a < meshFilters.Length; a++)
            {
                surfaceMap[a] = new float[globalVertices[a].Length];

                for (int i = 0; i < globalVertices[a].Length; i++)
                {
                    surfaceMap[a][i] = Mathf.Clamp(Sample(globalVertices[a][i]), -1f, 1f);
                }
            }

            float min = surfaceMap[0][0], max = surfaceMap[0][0];
            for (int a = 0; a < surfaceMap.Length; a++)
            {
                for (int b = 0; b < surfaceMap[a].Length; b++)
                {
                    max = surfaceMap[a][b] > max ? surfaceMap[a][b] : max;
                    min = surfaceMap[a][b] < min ? surfaceMap[a][b] : min;
                }
            }

            for (int a = 0; a < surfaceMap.Length; a++)
            {
                for (int b = 0; b < surfaceMap[a].Length; b++)
                {

                }
            }

        }

        public static float ScaleSurfaceToSpace(float input)
        {
            if (input < 0f)
            {
                Debug.LogError("ERROR INPUT MUST BE NEGATIVE.");
                return -1;
            }
            else { return input / MAX_HEIGHT; }
        }

        public static float ScaleSpaceToSurface(float input)
        {
            if (input < 0f || input > 8000f)
            {
                Debug.LogError("ERROR INPUT MUST BE < 0 or > 8000.");
                return -1;
            }
            else { return input * MAX_HEIGHT; }
        }
    }
}

