using UnityEngine;


namespace WorldGeneration.TectonicPlate.Objects
{
    // Edge for boundary stuff
    public class Edge
    {
        // Indices of the vertices of each edge
        public int[] vertexIndices0 { get; set; }
        public int[] vertexIndices1 { get; set; }

        public Vector3[] edge { get; private set; }
        public Vector3[] invEdge { get; private set; }

        // Tectonic plates are stored in an array, edgeOf is an index array of that tectonic plate array
        // Describes the index of the plate the edge is a part of, should always be 2 long
        public int[] edgeOf { get; set; }

        public Edge(Vector3 v1, Vector3 v2, int[] vertexIndices, int i = -1)
        {
            edge = new Vector3[] { v1, v2 };
            invEdge = new Vector3[] { v2, v1 };

            vertexIndices0 = vertexIndices;

            // -1 placeholder value
            edgeOf = new int[2] { -1, -1 };
            edgeOf[0] = i;
        }

        /* --------------------------------------- */

        /* COMPARE OVERRIDES */

        public override bool Equals(object o)
        {
            Edge other = o as Edge;
            if (this.edge[0] == other.edge[0] && this.edge[1] == other.edge[1]) { return true; }
            if (this.edge[0] == other.invEdge[0] && this.edge[1] == other.invEdge[1]) { return true; }
            return false;
        }
        public override int GetHashCode()
        {
            float hc = this.edge.Length;

            foreach (Vector3 val in this.edge)
            {
                hc = unchecked(hc * 314159 + val.x);
                hc = unchecked(hc * 314159 + val.y);
                hc = unchecked(hc * 314159 + val.z);
            }

            return (int)Mathf.Round(hc);
        }
        public static bool operator ==(Edge v1, Edge v2)
        {
            if (v1.edge[0] == v2.edge[0] && v1.edge[1] == v2.edge[1]) { return true; }
            if (v1.edge[0] == v2.invEdge[0] && v1.edge[1] == v2.invEdge[1]) { return true; }
            return false;
        }

        public static bool operator !=(Edge v1, Edge v2)
        {
            if (v1.edge[0] == v2.edge[0] && v1.edge[1] == v2.edge[1]) { return false; }
            if (v1.edge[0] == v2.invEdge[0] && v1.edge[1] == v2.invEdge[1]) { return false; }
            return true;
        }
    }
}


