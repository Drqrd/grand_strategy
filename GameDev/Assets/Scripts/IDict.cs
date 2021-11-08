using System.Collections.Generic;

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

    public class BoundaryEdgeCompareOverride : IEqualityComparer<World.BoundaryEdge>
    {
        public bool Equals(World.BoundaryEdge a, World.BoundaryEdge b)
        {
            bool nrm = (a.Edge == b.Edge && a.InverseEdge == b.InverseEdge);
            bool inv = (a.Edge == b.InverseEdge && a.InverseEdge == b.Edge);
            if (nrm || inv) { return true; }
            else { return false; }
        }

        public int GetHashCode(World.BoundaryEdge edge) { return base.GetHashCode(); }
    }
}


