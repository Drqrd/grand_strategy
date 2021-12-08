using UnityEngine;


namespace WorldGeneration.TectonicPlate.Objects
{
    // Edge for boundary stuff
    public class Edge
    {
        public Vector3[] edge { get; private set; }
        public Vector3[] invEdge { get; private set; }

        // Tectonic plates are stored in an array, edgeOf is an index array of that tectonic plate array
        // Describes the index of the plate the edge is a part of, should always be 2 long
        public int[] edgeOf { get; set; }
        public Vector3 center { get; set; }
        public Vector3 direction { get; private set; }
        public int directionSign { get; private set; }
        public float strength { get; set; }

        public Edge(Vector3 v1, Vector3 v2, int i = -1)
        {
            edge = new Vector3[] { v1, v2 };
            invEdge = new Vector3[] { v2, v1 };

            // -1 placeholder value
            edgeOf = new int[2] { -1, -1 };
            edgeOf[0] = i;

            center = Vector3.zero;
            strength = -2f;
        }


        // Calculates direction
        public void CalculateDirection()
        {
            if (center != Vector3.zero && strength != -2f)
            {
                Vector3 tangent = Vector3.Cross(center, edge[0] - edge[1]);
                if (tangent.sqrMagnitude < float.Epsilon) { tangent = Vector3.Cross(center, Vector3.forward); }
                tangent.Normalize();

                Quaternion rotation = Quaternion.AngleAxis(180f, center);

                Vector3 rotdDir = rotation * tangent;

                float t = Mathf.Abs(strength) * 0.1f > 0.006f ? Mathf.Abs(strength) * 0.1f : 0.006f;
                if (directionSign != 0) { direction = Vector3.Lerp(center, (rotdDir * directionSign) + center, t); }
                else { direction = center; }
            }
            else
            {
                Debug.LogError("INVALID DIRECTION CALCULATION.");
            }
        }

        public void SetDirectionSign(int i)
        {
            if (i != -1 && i != 1 && i != 0) { Debug.LogError("INVALID SET."); }
            else { directionSign = i; }
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


