using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using WorldGeneration.TectonicPlate.Objects;


namespace WorldGeneration.TectonicPlate.KDTree
{
    public static class KD
    {
        // Number of points to have in each basket
        private const int THRESHOLD = 2;


        // Finds the nearest (up to 4) neighbors of each point in an array of points, and sets them
        public static Node BuildTree(List<Point> points, int depth = 0)
        {
            Debug.Log("Depth: " + depth + ", PointNum: " + points.Count);

            if (points.Count <= THRESHOLD) { return null; }

            // Number of dimensions
            int k = 3;
            int axis = depth % k;

            points = Sort(points, axis);

            int median = Mathf.FloorToInt((float)points.Count / 2f);

            Debug.Log(median);

            return new Node( //?????? ,                                                            // loc
                            BuildTree(points.GetRange(0, median), depth + 1),                          // left
                            BuildTree(points.GetRange(median + 1, points.Count - 1), depth + 1));      // right
        }

        private static List<Point> Sort(List<Point> p, int c)
        {
            switch (c)
            {
                case 0:
                    p.OrderBy(v => v.Pos.x);
                    return p;
                case 1:
                    p.OrderBy(v => v.Pos.y);
                    return p;
                case 2:
                    p.OrderBy(v => v.Pos.z);
                    return p;
                default:
                    Debug.LogError("ERROR IN KDTREE SORTING --- AXIS VALUE: " + c);
                    return p;
            }
        }
    }
}

