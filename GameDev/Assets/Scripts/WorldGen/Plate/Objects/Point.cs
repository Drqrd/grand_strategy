using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace WorldGeneration.TectonicPlate.Objects
{
    public class Point
    {
        public Vector3 Pos { get; private set; }
        public Point[] Neighbors { get; private set; }
        public _Height Height { get; set; }
        public int GlobalPosition { get; private set; }

        public class _Height
        {
            public float Space { get; set; }
            public float Surface { get; set; }
            public float NeighborRefValue { get; set; }
        }

        public Point(Vector3 Pos, int vertexPosition)
        {
            this.Pos = Pos;

            this.GlobalPosition = vertexPosition;

            // Height stuff
            Height = new _Height();
        }

        public void SetNearestNeighbors(Point[] points)
        {
            Neighbors = points;
        }
    }
}


