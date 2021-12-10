using System.Collections.Generic;
using UnityEngine;
using WorldGeneration.TectonicPlate.Objects;


namespace WorldGeneration.Maps
{
    // Approach:
    // - For continents vs oceans, get random distribution of points a set distance from one another.
    // - Decrement height from the continent centers, which will be considered mountains
    public class TectonicPlateMap : Map
    {
        private static Color DEFAULT_COLOR = Color.yellow;
        private MeshFilter[] meshFilters;

        public LineRenderer[] Lines { get; private set; }
        public static Color DefaultColor { get { return DEFAULT_COLOR; } }
        public Color[] FaultLineColors { get; private set; }
        public Color[] WeightedLineColors { get; private set; }
        
        public TectonicPlateMap(World world) : base(world)
        {
            this.world = world;

            meshFilters = new MeshFilter[world.PlateCenters.Length];
        }

        public override void Build()
        {
            GameObject parentObj = new GameObject(World.MapDisplay.TectonicPlateMap.ToString());
            parentObj.transform.parent = world.transform;

            GameObject platesObj = new GameObject("Plates");
            platesObj.transform.parent = parentObj.transform;

            GameObject faultLinesObj = new GameObject("Fault Lines");
            faultLinesObj.transform.parent = parentObj.transform;

            List<LineRenderer> lines = new List<LineRenderer>();
            List<Color> colors = new List<Color>();
            List<Color> weightedColors = new List<Color>();


            for (int i = 0; i < world.Plates.Length; i++)
            {
                GameObject obj = new GameObject("Plate " + i);
                obj.transform.parent = platesObj.transform;

                meshFilters[i] = obj.AddComponent<MeshFilter>();
                meshFilters[i].sharedMesh = world.Plates[i].SharedMesh;
                obj.AddComponent<MeshRenderer>().material = Resources.Load<Material>("Materials/WorldGen/Surface");

                List<LineRenderer>[] linesList;
                List<Color>[] color;

                // Build fault lines of plate
                BuildBoundaries(faultLinesObj.transform, i, out linesList, out color);

                // Collapse line and colors
                for (int a  = 0; a < linesList.Length; a++)
                {
                    for (int b = 0; b < linesList[a].Count; b++)
                    {
                        lines.Add(linesList[a][b]);
                        colors.Add(color[a][b]);
                    }
                }

                // Get weighted color
                foreach (FaultLine faultLine in world.Plates[i].FaultLines)
                {
                    Color wc = FaultLine.FColor(faultLine);
                    for (int b = 0; b < faultLine.Edges.Length; b++)
                    {
                        weightedColors.Add(wc);
                    }
                }
            }

            Lines = lines.ToArray();
            FaultLineColors = colors.ToArray();
            WeightedLineColors = weightedColors.ToArray();
        }

        private void BuildBoundaries(Transform parent, int ind, out List<LineRenderer>[] linesList, out List<Color>[] color)
        {
            // List for lines
            FaultLine[] faultLines = world.Plates[ind].FaultLines;

            linesList = new List<LineRenderer>[faultLines.Length];
            color = new List<Color>[faultLines.Length];

            // Iterate through FaultLines
            for (int a = 0; a < faultLines.Length; a++)
            {
                GameObject faultLineObj = new GameObject("Fault Line");
                faultLineObj.transform.parent = parent;

                linesList[a] = new List<LineRenderer>();
                color[a] = new List<Color>();

                Color c = Random.ColorHSV();

                // Get all relavent values
                for (int b = 0; b < faultLines[a].Edges.Length; b++)
                {
                    Edge edge = faultLines[a].Edges[b];
                    linesList[a].Add(BuildLineRenderer(edge, faultLineObj.transform));
                    color[a].Add(c);
                }
            }
        }

        private LineRenderer BuildLineRenderer(Edge edge, Transform parent)
        {
            Vector3[] vertices = new Vector3[10];
            GameObject lineObj = GameObject.Instantiate(Resources.Load<GameObject>("Prefabs/Line"));
            lineObj.transform.parent = parent;
            LineRenderer line = lineObj.GetComponent<LineRenderer>();

            vertices[0] = edge.edge[0].normalized;
            vertices[vertices.Length - 1] = edge.edge[1].normalized;

            for (int b = 1; b < vertices.Length - 1; b++)
            {
                vertices[b] = Vector3.Lerp(vertices[0], vertices[vertices.Length - 1], (float)b / vertices.Length).normalized;
            }

            line.positionCount = vertices.Length;
            line.SetPositions(vertices);

            line.startColor = DEFAULT_COLOR;
            line.endColor = DEFAULT_COLOR;

            return line;
        }
    }
}

