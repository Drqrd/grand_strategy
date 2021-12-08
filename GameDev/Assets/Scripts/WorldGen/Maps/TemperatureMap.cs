using UnityEngine;

namespace WorldGeneration.Maps
{
    public class TemperatureMap : Map
    {
        public TemperatureMap(World world) : base(world)
        {
            this.world = world;
        }

        public override void Build()
        {
            GameObject obj = new GameObject(World.MapDisplay.TemperatureMap.ToString());
            obj.transform.parent = world.transform;
        }
    }
}

