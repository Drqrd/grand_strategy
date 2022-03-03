using UnityEngine;

using static WorldData;

namespace WorldGeneration.Maps
{
    public abstract class Map
    {
        protected World world;
        protected SaveData saveData;
        protected Materials materials;
        protected class Materials
        {
            public Material map { get; private set; }
            public Material surface { get; private set; }
            public Material ocean { get; private set; }

            public Materials(Material map, Material surface, Material ocean)
            {
                this.map = map;
                this.surface = surface;
                this.ocean = ocean;
            }
        }

        public Map(World world)
        {
            this.world = world;
            this.saveData = world.worldData.saveData;

            this.materials = new Materials(
                Resources.Load<Material>("Materials/WorldGen/Map"),
                Resources.Load<Material>("Materials/WorldGen/Surface"),
                Resources.Load<Material>("Materials/WorldGen/Ocean")
                );
        }

        public abstract void Build();
        protected virtual float Sample(Vector3 v)
        {
            return Noise.Sum(Noise.methods[3][2], v, 1, 8, 2f, 0.5f).value + 0.5f;
        }
    }
}
