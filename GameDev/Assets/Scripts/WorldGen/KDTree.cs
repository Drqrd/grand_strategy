using System.Linq;
using System.Collections.Generic;
using UnityEngine;



using WorldGeneration.TectonicPlate.Objects;

// Spatial Partitioning Data Structure
namespace WorldGeneration.SPDS
{
    // KDTree for Nearest Neighbor Search
    public class KDTree
    {
        public class Node
        {
            public Point Value { get; private set; }
            public Node Left { get; private set; }
            public Node Right { get; private set; }
            public int Dimension { get; private set; }
            public Node(Point point, Node left, Node right, int dimension)
            {
                Value = point;
                Left = left;
                Right = right;
                Dimension = dimension;
            }

            public bool IsLeaf { get { return Left == null && Right == null; } }
        }

        public class Query
        {
            public Point QueryPoint { get; private set; }
            public SortedList<float, Point> Neighbors { get; private set; }
            private int neighborNum;
            public Query(Point queryPoint, int neighborNum)
            {
                QueryPoint = queryPoint;
                this.neighborNum = neighborNum;
                Neighbors = new SortedList<float, Point>();
            }

            public void Compare(Point compareTo)
            {
                Neighbors.Add(SquareDistance(QueryPoint.Pos, compareTo.Pos), compareTo);
                while (Neighbors.Count > neighborNum) { Neighbors.Remove(Neighbors.Count - 1); }
            }
        }

        private Point[] points;

        public Node Root { get; private set; }

        public KDTree(Point[] points)
        {
            this.points = points;

            Root = Construct(points.ToList());
        }

        public Node Construct(List<Point> points, int dimension = 0)
        {
            // Dimensions 0 - 2 (x, y, z) 
            dimension %= 3;

            // Convert to list
            List<Point> p = points.ToList();
            // Sort by dimension
            p = p.OrderBy(v => v.Pos[dimension]).ToList();

            int median = p.Count / 2 + (p.Count % 2) - 1;

            // Create branches
            if (p.Count == 0) { return null; }
            if (p.Count == 1) { return new Node(p[0], null, null, dimension); }
            else if (p.Count == 2) { return new Node(p[1], Construct( new List<Point> { p[0] }, dimension + 1), null, dimension); }
            else 
            {
                // Debug.Log("Median: " + median + ", Range To: " + p.Count / 2 + ", End Range: " + (p.Count / 2 - (1 - p.Count % 2)) + ", Range: " + p.Count);
                return new Node(p[median],
                Construct(p.GetRange(0, p.Count / 2), dimension + 1),
                Construct(p.GetRange(median + 1, p.Count / 2 - (1 - p.Count % 2)), dimension + 1), dimension);
            }
        }

        public Point[] Search(Point queryPoint, Node node, int neighborNumber)
        {
            Point currBest = Traverse(queryPoint, node);
            float bestGuess = SquareDistance(currBest.Pos, queryPoint.Pos);

        }

        public Point Traverse(Point queryPoint, Node node, int dimension = 0)
        {
            if (node.IsLeaf) { return node.Value; }

            dimension %= 3;

            bool left = node.Value.Pos[dimension] < queryPoint.Pos[dimension];
            if (left) { return Traverse(queryPoint, node.Left, dimension + 1); }
            else { return Traverse(queryPoint, node.Right, dimension + 1); }
        }

        public static float SquareDistance(Vector3 a, Vector3 b)
        {
            return Mathf.Pow(a.x - b.x, 2f) + Mathf.Pow(a.y - b.y, 2f) + Mathf.Pow(a.z - b.z, 2f);
        }
    }
}


