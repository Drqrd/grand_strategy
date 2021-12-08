using UnityEngine;

namespace WorldGeneration.Maps
{
    public class MoistureMap : Map
    {
        public MoistureMap(World world) : base(world)
        {
            this.world = world;
        }

        public override void Build()
        {
            GameObject obj = new GameObject(World.MapDisplay.MoistureMap.ToString());
            obj.transform.parent = world.transform;
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
}

