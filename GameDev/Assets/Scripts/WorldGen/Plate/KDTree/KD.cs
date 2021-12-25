using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using WorldGeneration.TectonicPlate.Objects;

/*
 * Sources:
 * https://gopalcdas.com/2017/05/24/construction-of-k-d-tree-and-using-it-for-nearest-neighbour-search/
 * http://andrewd.ces.clemson.edu/courses/cpsc805/references/nearest_search.pdf
 * 
 */


namespace WorldGeneration.TectonicPlate.KDTree
{
    public static class KD
    {
        // Number of points to have in each basket
        private const int THRESHOLD = 1;


        // Finds the nearest (up to 4) neighbors of each point in an array of points, and sets them
        public static Node BuildTree(List<Point> points, int depth = 0)
        {
            if (points.Count > THRESHOLD)
            {
                Debug.Log("Depth: " + depth + ", PointNum: " + points.Count);

                // Number of dimensions
                int k = 3;
                int axis = depth % k;

                points.OrderBy(v => v.Pos[axis]);

                int divisor = points.Count / 2;

                return new Node(points[divisor],                                                            // value
                                axis,                                                                       // axis 
                                BuildTree(points.GetRange(0, divisor), depth + 1),                          // left
                                BuildTree(points.GetRange(divisor, points.Count - divisor), depth + 1));    // right
            }
            // Return an actual node value if it is a leaf node
            else
            {
                return new Node(points[0]);
            }
        }
        
        // K nearest neighbor search
        public static Point KNNS(Point point, Node node, Point refPoint, float distance, int k)
        {
            SortedList<float, Point> sPointList = new SortedList<float, Point>();
            if (node == null)
            {
                return null;
            }
            if (node.IsLeaf)
            {
                if (node.Pos[])
                sPointList.Add(distance, point);
            }
            else
            {

            }

            return point;
        }
    }
}

