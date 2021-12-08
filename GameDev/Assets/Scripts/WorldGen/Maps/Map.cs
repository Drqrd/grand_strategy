using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace WorldGeneration.Maps
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
}