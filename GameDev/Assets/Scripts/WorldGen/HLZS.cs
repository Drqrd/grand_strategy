using UnityEngine;

/* Holdridge Life Zone System
* - https://en.wikipedia.org/wiki/Holdridge_life_zones
* 
*           /\
*     L -  /  \   - R
*         /    \
*        /______\ 
*           
* L = Latitudinal
* B = Altitudinal
* 
*/
namespace WorldGeneration
{
    public struct HLZS
    {
        public enum LifeZone
        {
            RainForest, WetForest, MoistForest, DryForest, VeryDryForest,
            RainTundra, WetTundra, MoistTundra, DryTundra, DryScrub, DesertScrub,
            Steppe, ThornSteppe, ThornWoodland, Desert
        }

        // In meters
        public static float MAX_HEIGHT = 19000f;
        public static float AVG_HEIGHT = 13800f;
        public static float SEA_LEVEL = 11000f;
        public static float AVG_DEPTH = 7300f;
        public static float MAX_DEPTH = 0f;


        private static float[] elevationTime = new float[] { 0.0f, 0.27f, 0.55f, 0.82f, 1.0f }; // 2750 m max
        private static float[] latitudeTime = new float[] { 0.0f, 0.12f, 0.25f, 0.5f, .73f, 1.0f }; // 90 deg max

        private static Color topRight = new Color(30f / 255f, 128f / 255f, 200f / 255f);
        private static Color topLeft = new Color(125f / 255f, 125f / 255f, 125f / 255f);
        private static Color botRight = new Color(0f / 255f, 180f / 255f, 80f / 255f);
        private static Color botLeft = new Color(255f / 255f, 255f / 255f, 100f / 255f);

        // Given a surface value, will evaluate the scale based on the continents normalized 0-1
        public static float EvaluateContinentalScale(float surfaceVal)
        {
            return (surfaceVal - SEA_LEVEL) / (MAX_HEIGHT - SEA_LEVEL);
        }

        // Same as surface but for the ocean
        public static float EvaluateOceanicScale(float oceanVal)
        {
            return (oceanVal) / (SEA_LEVEL);
        }
    }
}


