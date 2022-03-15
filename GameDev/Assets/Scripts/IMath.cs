using UnityEngine;

public static class IMath
{
    public static class Triangle
    {
        public static Vector3 Centroid(Vector3 a, Vector3 b, Vector3 c)
        {
            return new Vector3((a.x + b.x + c.x) / 3f, (a.y + b.y + c.y) / 3f, (a.z + b.z + c.z) / 3f);
        }
        public static Vector2 Centroid(Vector2 a ,Vector2 b, Vector2 c)
        {
            return new Vector2((a.x + b.x + c.x) / 3f, (a.y + b.y + c.y) / 3f);
        }

        public static bool Clockwise2D(Vector2 a, Vector2 b, Vector2 c)
        {
            return (b.x - a.x) * (c.y - a.y) - (c.x - a.x) * (b.y - a.y) > 0;
        }
        public static bool Clockwise3D(Vector3 a, Vector3 b, Vector3 c)
        {
            return Vector3.Dot(Vector3.Cross(b - a, c - a), a) > 0;
        }
    }

    public static float RightAngleDistance(Vector3 p, Vector3[] l)
    {
        if (l.Length != 2) { Debug.LogError("l must have length of 2."); return -1f; }
        return Mathf.Abs((l[1].x - l[0].x) * (l[0].y - p.y) - (l[0].x - p.x) * (l[1].y - l[0].y)) /
            Mathf.Sqrt(Mathf.Pow(l[1].x - l[0].x, 2f) + Mathf.Pow(l[1].y - l[0].y, 2f));
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
