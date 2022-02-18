using UnityEngine;

namespace WorldGeneration.Maps
{
    public abstract class Map
    {
        protected World world;
        protected _Materials Materials;
        protected class _Materials
        {
            public Material Map { get; private set; }
            public Material Surface { get; private set; }
            public Material Ocean { get; private set; }

            public _Materials(Material Map, Material Surface, Material Ocean)
            {
                this.Map = Map;
                this.Surface = Surface;
                this.Ocean = Ocean;
            }
        }

        public Map(World world)
        {
            this.world = world;
            this.Materials = new _Materials(
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
