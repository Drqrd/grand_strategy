using System.Collections;
using System.Collections.Generic;
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
public class HLZS
{
    public enum LifeZone { RainForest, WetForest, MoistForest, DryForest, VeryDryForest,
        RainTundra, WetTundra, MoistTundra, DryTundra, DryScrub, DesertScrub,
        Steppe, ThornSteppe, ThornWoodland, Desert }

    private float[] elevationTime = new float[] { 0.0f, 0.27f, 0.55f, 0.82f, 1.0f }; // 2750 m max
    private float[] latitudeTime = new float[] { 0.0f, 0.12f, 0.25f, 0.5f, .73f, 1.0f }; // 90 deg max

    Color topRight = new Color(30f / 255f, 128f / 255f, 200f / 255f);
    Color topLeft = new Color(125f / 255f, 125f / 255f, 125f / 255f);
    Color botRight = new Color(0f / 255f, 180f / 255f, 80f / 255f);
    Color botLeft = new Color(255f / 255f, 255f / 255f, 100f / 255f);

    private void Start()
    {
    }
}
