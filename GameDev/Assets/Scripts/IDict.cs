using System.Collections.Generic;
using UnityEngine;

using static WorldData;

namespace IDict
{
    public class EdgeCompareOverride : IEqualityComparer<Edge>
    {
        public bool Equals(Edge a, Edge b)
        {
            if (a == null && b == null) { return true; }
            if (a == null || b == null) { return false; }

            bool val = true;
            for (int ind = 0; ind < a.edge.Length; ind++) { if (a.edge[ind].vertex != b.edge[ind].vertex) { val = false; } }
            return val;
        }

        public int GetHashCode(Edge e)
        {
            int hc = e.edge.Length;
            foreach (Point val in e.edge) 
            {
                int v = (int)Mathf.Floor(12 * val.vertex.x + 23 * val.vertex.y + 34 * val.vertex.z);
                hc = unchecked(hc * 314159 + v); 
            }
            return hc;
        }
    }
}


