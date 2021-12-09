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
        public LineRenderer[] Lines { get; private set; }

        public TectonicPlateMap(World world) : base(world)
        {
            this.world = world;

            meshFilters = new MeshFilter[world.PlateCenters.Length];
        }

        public override void Build()
        {
            GameObject parentObj = new GameObject(World.MapDisplay.Plates.ToString());
            parentObj.transform.parent = world.transform;

            // Boundary Stuff
            GameObject boundariesObj = new GameObject("Boundaries");
            boundariesObj.transform.parent = parentObj.transform;
            BuildBoundaries(boundariesObj.transform);

            for (int i = 0; i < world.Plates.Length; i++)
            {
                GameObject obj = new GameObject("Plate " + i);
                obj.transform.parent = parentObj.transform;

                meshFilters[i] = obj.AddComponent<MeshFilter>();
                meshFilters[i].sharedMesh = world.Plates[i].SharedMesh;
                obj.AddComponent<MeshRenderer>().material = Resources.Load<Material>("Materials/WorldGen/Surface");
            }
        }

        private void BuildBoundaries(Transform parent)
        {
            List<LineRenderer> _lines = new List<LineRenderer>();
            for (int a = 0; a < world.PlateBoundaries.Edges.Length; a++)
            {
                GameObject lineObj = GameObject.Instantiate(Resources.Load<GameObject>("Prefabs/Line"));
                lineObj.name = "Line " + a;
                lineObj.transform.parent = parent;

                LineRenderer line = lineObj.GetComponent<LineRenderer>();

                Vector3[] vertices = new Vector3[10];

                // Get all relavent values
                Edge edge = world.PlateBoundaries.Edges[a];
                vertices[0] = edge.edge[0].normalized;
                vertices[vertices.Length - 1] = edge.edge[1].normalized;

                for (int b = 1; b < vertices.Length - 1; b++)
                {
                    vertices[b] = Vector3.Lerp(vertices[0], vertices[vertices.Length - 1], (float)b / vertices.Length).normalized;
                }

                // Assign center
                edge.center = vertices[vertices.Length / 2];
                edge.CalculateDirection();

                line.positionCount = vertices.Length;
                line.SetPositions(vertices);

                line.startColor = world.PlateBoundaries.DefaultColor;
                line.endColor = world.PlateBoundaries.DefaultColor;

                _lines.Add(line);
            }

            Lines = _lines.ToArray();
            _lines.Clear();
        }
    }
}

