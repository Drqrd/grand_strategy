using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace WorldGeneration.TectonicPlate.Objects
{
    public class Point
    {
        public Vector3 Pos { get; private set; }
        public Point[] Neighbors { get; private set; }

        public Point(Vector3 Pos)
        {
            this.Pos = Pos;
        }

        public void SetNearestNeighbors(Point[] points)
        {

        }
    }
}


