using System.Collections.Generic;
using UnityEngine;

// Approach:
// - For continents vs oceans, get random distribution of points a set distance from one another.
// - Decrement height from the continent centers, which will be considered mountains
public class HeightMap
{
    public enum ContinentSize { Islands, Small, Medium, Large, Enormous, Pangea };
    public float[] distBetweenCenters = new float[]{ .2f, .5f, .7f, .9f, 1.2f };
    public ContinentSize continentSize { get; set; }

    public Vector3[] continentCenters { get; private set; }


    private MeshFilter[] meshFilters;
    public HeightMap(MeshFilter[] meshFilters)
    {
        this.meshFilters = meshFilters;
    }

    public void Build()
    {
        GetContinentCenters();
        AddHeight();
    }

    private void GetContinentCenters()
    {
        List<Vector3> centers = new List<Vector3>();

        centers.Add(Random.onUnitSphere);

        if (continentSize != ContinentSize.Pangea)
        {
            float minDist = distBetweenCenters[(int)continentSize];
            bool addToCenters;
            int MAX_TRIES = 999, tries = 0;

            while (tries < MAX_TRIES)
            {
                // Get random point
                addToCenters = true;
                Vector3 c = Random.onUnitSphere;
                
                // iterate through
                for(int i = 0; i < centers.Count; i++)
                {
                    // If distance is larger than allowed, add to tries, tell it to not execute last bit, and break
                    float dist = DistanceBetweenPoints(centers[i], c);
                    
                    if (dist <= minDist) 
                    { 
                        tries += 1;
                        addToCenters = false;
                    }
                }

                if (addToCenters) 
                { 
                    centers.Add(c); 
                    tries = 0; 
                }
            }
        };

        continentCenters = centers.ToArray();
    }

    private void AddHeight()
    {
        for (int a = 0; a < meshFilters.Length; a++)
        {
            Vector3[] vertices = meshFilters[a].sharedMesh.vertices;

            for (int i = 0; i < vertices.Length; i++)
            {
                vertices[i] *= ((Sample(vertices[i]) * .1f + 1f));
            }

            meshFilters[a].sharedMesh.vertices = vertices;
            meshFilters[a].sharedMesh.RecalculateNormals();
            meshFilters[a].sharedMesh.Optimize();
        }
    }

    private float DistanceBetweenPoints(Vector3 a, Vector3 b)
    {
        return Mathf.Sqrt(Mathf.Pow(b.x - a.x, 2f) + Mathf.Pow(b.y - a.y, 2f) + Mathf.Pow(b.z - a.z, 2f));
    }

    private Vector3 TriangleCentroid(Vector3 a, Vector3 b, Vector3 c)
    {
        return new Vector3((a.x + b.x + c.x) / 3f, (a.y + b.y + c.y) / 3f, (a.z + b.z + c.z) / 3f);
    }

    private float Sample(Vector3 v)
    {
        return Noise.Sum(Noise.methods[3][2], v, 1, 8,2f,0.5f).value;
    }
}
