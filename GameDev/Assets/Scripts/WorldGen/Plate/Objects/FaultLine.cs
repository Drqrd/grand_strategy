using UnityEngine;

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

        public Edge[] Edges { get; private set; }
        public FaultLineType Type { get; private set; }

        private static Color CONVERGENT_COLOR = Color.red;
        private static Color DIVERGENT_COLOR = Color.blue;
        private static Color TRANSFORM_COLOR = Color.green;


        public FaultLine(Edge[] edges)
        {
            Edges = edges;
        }

        public void DeterminePlateType(World world)
        {
            int ind1 = Edges[0].edgeOf[0];
            int ind2 = Edges[0].edgeOf[1];
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

