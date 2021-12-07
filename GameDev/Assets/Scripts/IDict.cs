using System.Collections.Generic;
using UnityEngine;


namespace IDict
{
    public class IntArrCompareOverride : IEqualityComparer<int[]>
    {
        public bool Equals(int[] i1, int[] i2)
        {
            if (i1 == null && i2 == null) { return true; }
            if (i1 == null || i2 == null) { return false; }

            bool val = true;
            for (int ind = 0; ind < i1.Length; ind++) { if (i1[ind] != i2[ind]) { val = false; } }
            return val;
        }

        public int GetHashCode(int[] arr)
        {
            int hc = arr.Length;
            foreach (int val in arr) { hc = unchecked(hc * 314159 + val); }
            return hc;
        }
    }

    public class TectonicPlateEdgeCompareOverride : IEqualityComparer<TectonicPlate.Edge>
    {
        public bool Equals(TectonicPlate.Edge v1, TectonicPlate.Edge v2)
        {
            return (v1.edge == v2.edge || v1.edge == v2.invEdge);
        }

        public int GetHashCode(TectonicPlate.Edge e)
        {
            float hc = e.edge.Length;

            foreach(Vector3 val in e.edge)
            {
                hc = unchecked(hc * 314159 + val.x);
                hc = unchecked(hc * 314159 + val.y);
                hc = unchecked(hc * 314159 + val.z);
            }

            return (int)Mathf.Round(hc);
        }
    }
}


