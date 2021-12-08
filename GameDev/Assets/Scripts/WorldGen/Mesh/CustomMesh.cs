using UnityEngine;

// Abstract class for meshes
namespace WorldGeneration.Meshes
{
    public abstract class CustomMesh
    {
        public MeshFilter[] meshFilters { get; protected set; }
        public abstract void Build();
    }
}

