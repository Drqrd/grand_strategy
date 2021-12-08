using UnityEngine;
using WorldGeneration.TectonicPlate.Objects;

namespace WorldGeneration.TectonicPlate.Objects
{
    public class Boundaries
    {
        // Boundary Properties
        private World world;

        public Edge[] Edges { get; private set; }
        public Color DefaultColor { get; private set; }
        public Color[] FaultColors { get; private set; }
        public Color[] WeightedFaultColors { get; private set; }

        // Constructors
        public Boundaries(World world, Edge[] Edges, int uniqueEdges)
        {
            this.world = world;
            this.Edges = Edges;
            this.DefaultColor = Color.red;
            this.FaultColors = new Color[Edges.Length];
            this.WeightedFaultColors = new Color[Edges.Length];

            GenerateColors(uniqueEdges);
        }

        private void GenerateColors(int uniqueEdges)
        {
            Color[] uniqueColors = new Color[uniqueEdges];

            for (int a = 0; a < uniqueEdges; a++)
            {
                // Generate a random color
                uniqueColors[a] = Random.ColorHSV();

                // Make sure the colors are different from previous colors
                for (int b = 0; b < a; b++)
                {
                    // If color is the same...
                    if (IMath.ColorApproximately(uniqueColors[a], uniqueColors[b]))
                    {
                        // Generate a new random color
                        uniqueColors[a] = Random.ColorHSV();
                        // Compare again
                        b = 0;
                    }
                }
            }

            // Assign the colors
            int ind = 0;
            int[] curr = Edges[0].edgeOf;
            FaultColors[0] = uniqueColors[ind];
            for (int a = 1; a < FaultColors.Length; a++)
            {
                if (Edges[a].edgeOf[0] != curr[0] || Edges[a].edgeOf[1] != curr[1])
                {
                    curr = Edges[a].edgeOf;
                    ind += 1;
                }
                FaultColors[a] = uniqueColors[ind];
            }
        }

        public void SetFaultColors(Color[] FaultColors)
        {
            if (FaultColors.Length == this.FaultColors.Length) { this.FaultColors = FaultColors; }
            else { Debug.LogError("INVALID SET."); }
        }

        public void SetWeightedFaultColors()
        {
            if (WeightedFaultColors != null)
            {
                for(int a = 0; a < Edges.Length; a++)
                {
                    WeightedFaultColors[a] = world.WeightedBoundaryGradient.Evaluate(Edges[a].strength);
                }
            }
            else { Debug.LogError("INVALID SET."); }
        }

        public void SetEdgeWeights(float[] EdgeWeights)
        {
            if (EdgeWeights.Length == this.Edges.Length)
            { 
                for(int a = 0; a < Edges.Length; a++)
                {
                    Edges[a].strength = EdgeWeights[a];
                }
            }
            else { Debug.LogError("INVALID SET."); }
        }
    }
}

