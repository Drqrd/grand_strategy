using UnityEngine;

namespace WorldGeneration.TectonicPlate.KDTree
{
    public class Node
    {
        public Vector3 Location { get; private set; }
        public Node Left { get; private set; }
        public Node Right { get; private set; }

        public Node(Vector3 loc, Node left = null, Node right = null)
        {
            Location = loc;
            Left = left;
            Right = right;
        }

        public bool IsLeaf { get { return Left == null && Right == null; } }
    }
}