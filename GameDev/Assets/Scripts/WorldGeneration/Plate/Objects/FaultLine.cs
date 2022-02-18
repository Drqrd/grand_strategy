using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace WorldGeneration.TectonicPlate.Objects
{
    public class FaultLine
    {
        public enum FaultLineType
        {
            Convergent,
            Divergent,
            Transform
        }

        public int[] VertexIndices0 { get; private set; }
        public int[] VertexIndices1 { get; private set; }
        public int[] FaultOf { get; private set; }
        public Edge[] Edges { get; private set; }
        public FaultLineType Type { get; private set; }

        private static Color CONVERGENT_COLOR = Color.red;
        private static Color DIVERGENT_COLOR = Color.blue;
        private static Color TRANSFORM_COLOR = Color.green;


        public FaultLine(Edge[] edges)
        {
            Edges = edges;

            // Get vertex indices
            List<int> vertexIndices0 = new List<int>();
            List<int> vertexIndices1 = new List<int>();
            for(int a = 0; a < edges.Length; a++)
            {
                for (int b = 0; b < edges[a].vertexIndices0.Length; b++)
                {
                    vertexIndices0.Add(edges[a].vertexIndices0[b]);
                    vertexIndices1.Add(edges[a].vertexIndices1[b]);
                }
            }

            // Vertex Indices
            VertexIndices0 = vertexIndices0.ToArray();
            VertexIndices1 = vertexIndices1.ToArray();

            // FaultOf
            FaultOf = new int[2];
            FaultOf[0] = Edges[0].edgeOf[0];
            FaultOf[1] = Edges[0].edgeOf[1];
        }

        public void DetermineFaultLineType(World world)
        {
            int ind1 = FaultOf[0];
            int ind2 = FaultOf[1];
            Vector3 val1 = (world.Plates[ind1].Direction - world.Plates[ind1].Center).normalized  * world.Plates[ind1].Speed;
            Vector3 val2 = (world.Plates[ind2].Direction - world.Plates[ind2].Center).normalized * world.Plates[ind2].Speed;

            float val = Vector3.Dot(val1, val2);

            if (val > 0.1f) { Type = FaultLineType.Convergent; }
            else if (val < -0.1f) { { Type = FaultLineType.Divergent; } }
            else { Type = FaultLineType.Transform; }
        }

        public static Color FColor(FaultLine fl)
        {
            switch (fl.Type)
            {
                case FaultLineType.Convergent:
                    return CONVERGENT_COLOR;
                case FaultLineType.Divergent:
                    return DIVERGENT_COLOR;
                case FaultLineType.Transform:
                    return TRANSFORM_COLOR;
                default:
                    return Color.black;
            }
        }
    }
}

