using UnityEngine;
using WorldGeneration.TectonicPlate.Objects;

namespace WorldGeneration.TectonicPlate.KDTree
{
    public class Node
    {
        public int Axis { get; private set; }
        public Point Value { get; private set; }
        public Node Left { get; private set; }
        public Node Right { get; private set; }

        public Node( Point val, int axis = -1, Node left = null, Node right = null)
        {
            Axis = axis;
            Value = val;
            Left = left;
            Right = right;
        }

        public bool IsLeaf { get { return Left == null && Right == null; } }
    }
}