using UnityEngine;

namespace WorldGeneration.Meshes
{
    public class CubeSphere : CustomMesh
    {
        private Vector3[] directions = new Vector3[6]
        {
            Vector3.up, Vector3.down, Vector3.left, Vector3.forward, Vector3.right, Vector3.back
        };

        private Transform parent;
        private World.Parameters.CustomMesh parameters;

        public CubeSphere(Transform parent, int resolution, float jitter) : base(resolution, jitter)
        {
            this.parent = parent;

            this.resolution = resolution;
            this.jitter = jitter;

            parameters = parent.GetComponent<World>().parameters.customMesh;
        }

        // Each face will have resolution * resolution vertices
        // Each face will be subdivided based on chunk number, and any leftover vertices are added to end chunk
        public override void Build()
        {
            GameObject parentObj = new GameObject("Mesh");
            parentObj.transform.parent = parent;
            
            // Get the 6 faces
            MeshFilter[] meshFilters = new MeshFilter[6];
            for(int a = 0; a < directions.Length; a++)
            {
                GameObject faceObj = new GameObject("Face");
                faceObj.transform.parent = parentObj.transform;

                if (parameters.chunks == 1)
                {
                    faceObj.AddComponent<MeshRenderer>().sharedMaterial = Resources.Load<Material>("Materials/WorldGen/Map");

                    meshFilters[a] = faceObj.AddComponent<MeshFilter>();

                    MeshData meshData = ConstructMesh(directions[a]);

                    meshFilters[a].sharedMesh = new Mesh();
                    meshFilters[a].sharedMesh.vertices = meshData.vertices;
                    meshFilters[a].sharedMesh.triangles = meshData.triangles;
                    meshFilters[a].sharedMesh.RecalculateNormals();
                }
                else
                {
                    for(int b = 0; b < parameters.chunks; b++)
                    {
                        for (int c = 0; c < parameters.chunks; c++)
                        {
                            GameObject chunkObj = new GameObject("Chunk " + (c + b * parameters.chunks));
                            chunkObj.transform.parent = faceObj.transform;

                            chunkObj.AddComponent<MeshRenderer>().sharedMaterial = Resources.Load<Material>("Materials/WorldGen/Map");

                            MeshFilter meshFilter = chunkObj.AddComponent<MeshFilter>();
                            
                            MeshData meshData = ConstructChunk(directions[a], b, c);

                            meshFilter.sharedMesh = new Mesh();
                            meshFilter.sharedMesh.vertices = meshData.vertices;
                            meshFilter.sharedMesh.triangles = meshData.triangles;
                            meshFilter.sharedMesh.RecalculateNormals();
                        }
                    }
                }
            }
        }

        // Easy copy pasta (but sourced so ok /s)
        // https://youtu.be/sLqXFF8mlEU?t=52
        // Plus a bit more for normal calculation (to avoid seams in mesh)
        private MeshData ConstructMesh(Vector3 localUp)
        {
            Vector3 axisA = new Vector3(localUp.y, localUp.z, localUp.x);
            Vector3 axisB = Vector3.Cross(localUp, axisA);

            Vector3[] vertices = new Vector3[resolution * resolution];
            int[] triangles = new int[(resolution - 1) * (resolution - 1) * 6];
            Vector3[] normals = new Vector3[resolution * resolution];

            int triIndex = 0;

            for (int a = 0; a < resolution; a++)
            {
                for (int b = 0; b < resolution; b++)
                {
                    int vertexIndex = b + a * resolution;
                    Vector2 t = new Vector2(b, a) / (resolution - 1f);
                    Vector3 point = localUp + axisA * (2 * t.x - 1) + axisB * (2 * t.y - 1);
                    vertices[vertexIndex] = CubeToSphere(point);

                    if (b != resolution - 1 && a != resolution - 1)
                    {
                        triangles[triIndex + 0] = vertexIndex;
                        triangles[triIndex + 1] = vertexIndex + resolution + 1;
                        triangles[triIndex + 2] = vertexIndex + resolution;

                        triangles[triIndex + 3] = vertexIndex;
                        triangles[triIndex + 4] = vertexIndex + 1;
                        triangles[triIndex + 5] = vertexIndex + resolution + 1;
                        triIndex += 6;
                    }
                }
            }

            return new MeshData(vertices, triangles, normals);
        }

        private MeshData ConstructChunk(Vector3 localUp, int up, int right)
        {
            Vector3 axisA = new Vector3(localUp.y, localUp.z, localUp.x);
            Vector3 axisB = Vector3.Cross(localUp, axisA);

            Vector3[] vertices = new Vector3[resolution * resolution];
            int[] triangles = new int[(resolution - 1) * (resolution - 1) * 6];
            Vector3[] normals = new Vector3[resolution * resolution];

            int triIndex = 0;

            float r = Mathf.Lerp(-1f, 1f, (float)right / (parameters.chunks - 1));
            float u = Mathf.Lerp(-1f, 1f, (float)up / (parameters.chunks - 1));
            Vector2 fractionOfFace = new Vector2(r,u);
            Debug.Log(fractionOfFace);

            for (int a = 0; a < resolution; a++)
            {
                for (int b = 0; b < resolution; b++)
                {
                    int vertexIndex = b + a * resolution;
                    Vector2 t = new Vector2(b, a) / (resolution - 1f);
                    Vector3 point = localUp + (axisA * (2 * t.x + (parameters.chunks - 1f) * fractionOfFace.x - 1) / parameters.chunks) + (axisB * (2 * t.y + (parameters.chunks - 1f) * fractionOfFace.y - 1) / parameters.chunks);
                    vertices[vertexIndex] = CubeToSphere(point);

                    if (b != resolution - 1 && a != resolution - 1)
                    {
                        triangles[triIndex + 0] = vertexIndex;
                        triangles[triIndex + 1] = vertexIndex + resolution + 1;
                        triangles[triIndex + 2] = vertexIndex + resolution;

                        triangles[triIndex + 3] = vertexIndex;
                        triangles[triIndex + 4] = vertexIndex + 1;
                        triangles[triIndex + 5] = vertexIndex + resolution + 1;
                        triIndex += 6;
                    }
                }
            }

            return new MeshData(vertices, triangles, normals);
        }

        private Vector3 CubeToSphere(Vector3 v)
        {
            float x2 = v.x * v.x;
            float y2 = v.y * v.y;
            float z2 = v.z * v.z;
            float x = v.x * Mathf.Sqrt(1 - (y2 + z2) / 2 + (y2 * z2) / 3);
            float y = v.y * Mathf.Sqrt(1 - (x2 + z2) / 2 + (x2 * z2) / 3);
            float z = v.z * Mathf.Sqrt(1 - (y2 + x2) / 2 + (y2 * x2) / 3);

            return new Vector3(x, y, z);
        }
    }
}

