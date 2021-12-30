using UnityEngine;

public static class IMath
{
    public static float DistanceBetweenPoints(Vector3 a, Vector3 b)
    {
        return Mathf.Sqrt(Mathf.Pow(b.x - a.x, 2f) + Mathf.Pow(b.y - a.y, 2f) + Mathf.Pow(b.z - a.z, 2f));
    }

    public static Vector3 TriangleCentroid(Vector3 a, Vector3 b, Vector3 c)
    {
        return new Vector3((a.x + b.x + c.x) / 3f, (a.y + b.y + c.y) / 3f, (a.z + b.z + c.z) / 3f);
    }

    public static Vector3 RandomVector3()
    {
        return new Vector3(Random.Range(-1f, 1f), Random.Range(-1f, 1f), Random.Range(-1f, 1f)).normalized;
    }

    // Floors to the nearest to
    public static float FloorFloat(float f, float to)
    { 
        return Mathf.Floor(f / to) * to;
    }

    public static bool ColorApproximately(Color one, Color two, float threshold = 0.1f)
    {
        float r = Mathf.Pow(Mathf.Abs(one.r - two.r), 2f);
        float g = Mathf.Pow(Mathf.Abs(one.g - two.g), 2f);
        float b = Mathf.Pow(Mathf.Abs(one.b - two.b), 2f);
        if (Mathf.Sqrt(r+g+b) < threshold) { return true; }
        else { return false; }
    }
    
    public static float SumOf(float[] arr)
    {
        float total = 0;
        foreach(float val in arr) { total += val; }
        return total;
    }
}
