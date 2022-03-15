using System;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class WorldData
{
    public Save save { get; set; }
    public Debug debug { get; set; }
    public Mesh mesh { get; set; }
    public Cell[] cells { get; set; }
    public Plate[] plates { get; set; }


    // Debug Class
    public class Debug
    {
        public Debug() { }

        public Delaunay delaunay { get; set; }

        public class Delaunay
        {
            public Delaunay(Vector3[][] te, Vector3[][] ve, Vector3[] tc, Vector3[] ctc, Vector3[][] cve, Vector3[][] fc)
            {

                triangleEdges = te;
                voronoiEdges = ve;
                triangleCenters = tc;
                constructedTriangleCentroids = ctc;
                constructedVoronoiEdges = cve;
                finalCell = fc;

                Dictionary<Vector3, int> map = new Dictionary<Vector3, int>();
                for (int a = 0; a < voronoiEdges.Length; a++)
                {
                    for (int b = 0; b < voronoiEdges[a].Length; b++)
                    {
                        if (map.ContainsKey(voronoiEdges[a][b])) { map[voronoiEdges[a][b]] += 1; }
                        else { map.Add(voronoiEdges[a][b], 1); }
                    }
                }

                triangleCentroids = map.Keys.ToArray();
            }

            public Vector3[][] triangleEdges { get; private set; }
            public Vector3[][] voronoiEdges { get; private set; }
            public Vector3[] triangleCenters { get; private set; }
            public Vector3[] triangleCentroids { get; private set; }
            public Vector3[] constructedTriangleCentroids { get; private set; }
            public Vector3[][] constructedVoronoiEdges { get; private set; }
            public Vector3[][] finalCell { get; private set; }
        }
    }

    [Serializable]
    public class Save
    {
    }

    [Serializable]
    public class Cell
    {
        public Vector3 center { get; private set; }
        public Vector3[] points { get; private set; }
        public int globalIndex { get; private set; }

        // Setup
        public Cell[] neighbors { get; set; }
        public Plate plate { get; set; }
        public int plateId { get; set; }

        // Meshdata
        public Mesh mesh { get; private set; }

        // Map data
        public Height height { get; set; }
        public Moisture moisture { get; set; }
        public Temperature temperature { get; set; }
        public Terrain terrain { get; set; }

        public Cell(Vector3 v, Vector3[] pts, int gInd)
        {
            center = v;
            points = pts;
            globalIndex = gInd;
            plateId = -1;

            ConstructMesh();
        }

        // Heightmap
        public struct Height
        {
            public float surface { get; set; }
            public float space { get; set; }

            public Height(float defaultValue)
            {
                surface = defaultValue;
                space = defaultValue;
            }
        }

        // Moisturemap
        public struct Moisture
        {
            public float value { get; set; }
        }

        // Temperaturemap
        public struct Temperature
        {
            public float value { get; set; }
        }

        // Terrainmap
        public struct Terrain
        {
            public Vector3 vertex { get; set; }
        }

        private void ConstructMesh()
        {
            mesh = new Mesh();


            List<int> triangles = new List<int>();
            // 0 - 1 is edge, 0 - points.Length is edge
            for(int a = 1; a < points.Length - 1; a++)
            {
                int pt1 = 0;
                int pt2, pt3;
                if (IMath.Triangle.Clockwise3D(points[pt1], points[a], points[a + 1]))
                {
                    pt2 = a;
                    pt3 = a + 1;
                }
                else
                {
                    pt2 = a + 1;
                    pt3 = a;
                }

                triangles.Add(pt1);
                triangles.Add(pt2);
                triangles.Add(pt3);
            }

            mesh.vertices = points;
            mesh.triangles = triangles.ToArray();
            mesh.RecalculateNormals();
        }
    }

    public class Plate
    {
        // Constants
        private const float MIN_SPEED = 0.01f;
        private const float MAX_SPEED = 2.0f;
        public enum Type
        {
            Oceanic,
            Continental
        }

        public Plate(Cell c, float CvO, int id)
        {
            center = c;
            this.id = id;

            direction = GetRandomDirection();
            speed = GetRandomSpeed();

            plateType = UnityEngine.Random.Range(0f, 1f) >= CvO ? Type.Continental : Type.Oceanic;
            
            float onGradient = IMath.FloorFloat(UnityEngine.Random.Range(0f, 1f), 0.1f);
            color = plateType == Type.Continental ? new Color(0f, onGradient, 0f, 1f) :
                                                    new Color(0f, 0f, onGradient, 1f);
        }

        public int id { get; private set; }
        public Cell center { get; set; }
        public Cell[] cells { get; set; }
        public FaultLine[] faultLines { get; set; }
        public Vector3 direction { get; private set; }
        public float speed { get; private set; }
        public Type plateType { get; private set; }
        public Color color { get; private set; }

        // Mesh
        public Mesh mesh { get; private set; }

        private Vector3 GetRandomDirection()
        {
            Vector3 tangent = Vector3.Cross(center.center, Vector3.up);
            if (tangent.sqrMagnitude < float.Epsilon) { tangent = Vector3.Cross(center.center, Vector3.forward); }
            tangent.Normalize();

            Quaternion rotation = Quaternion.AngleAxis(UnityEngine.Random.Range(0f, 360f), center.center);
            return rotation * tangent;
        }

        private float GetRandomSpeed()
        {
            return UnityEngine.Random.Range(MIN_SPEED, MAX_SPEED);
        }
    }

    public class FaultLine
    {
        private static Color CONVERGENT_COLOR = Color.red;
        private static Color DIVERGENT_COLOR = Color.blue;
        private static Color TRANSFORM_COLOR = Color.green;

        public enum Type
        {
            Convergent,
            Divergent,
            Transform
        }

        public FaultLine(Cell[] e, Plate f1, Plate f2)
        {
            cells = e;

            faultOf = new Plate[] { f1, f2 };
        }

        public Cell[] cells { get; private set; }
        public Plate[] faultOf { get; set; }
        public Type type { get; private set; }

        public void DetermineFaultLineType(World world)
        {
            Plate ind1 = faultOf[0];
            Plate ind2 = faultOf[1];
            Vector3 val1 = (ind1.direction - ind1.center.center).normalized * ind1.speed;
            Vector3 val2 = (ind2.direction - ind2.center.center).normalized * ind2.speed;

            float val = Vector3.Dot(val1, val2);

            if (val > 0.1f) { type = Type.Convergent; }
            else if (val < -0.1f) { { type = Type.Divergent; } }
            else { type = Type.Transform; }
        }

        public static Color FColor(FaultLine fl)
        {
            switch (fl.type)
            {
                case Type.Convergent:
                    return CONVERGENT_COLOR;
                case Type.Divergent:
                    return DIVERGENT_COLOR;
                case Type.Transform:
                    return TRANSFORM_COLOR;
                default:
                    return Color.black;
            }
        }
    }
    
}