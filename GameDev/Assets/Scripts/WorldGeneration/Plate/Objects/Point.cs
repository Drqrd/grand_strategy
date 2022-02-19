using System.Linq;
using System.Collections.Generic;
using UnityEngine;


namespace WorldGeneration.TectonicPlate.Objects
{
    public class Point
    {
        public Vector3 Pos { get; private set; }
        public Point[] Neighbors { get; private set; }
        public _Height Height { get; set; }
        public _Temperature Temperature { get; set; }
        public _Moisture Moisture { get; set; }
        public int GlobalPosition { get; private set; }
        public int PlateId { get; private set; }

        public class _Height
        {
            public float Space { get; set; }
            public float Surface { get; set; }
        }

        public class _Temperature
        {
            public float Heat { get; set; }
        }

        public class _Moisture
        {
            public float Value { get; set; }
        }

        public Point(Vector3 Pos, int PlateId, int vertexPosition)
        {
            this.Pos = Pos;
            this.PlateId = PlateId;
            this.GlobalPosition = vertexPosition;

            // Height stuff
            Height = new _Height();
            Temperature = new _Temperature();
            Moisture = new _Moisture();
        }

        public void SetNearestNeighbors(Point[] points)
        {
            Neighbors = points;
        }
    }
}

