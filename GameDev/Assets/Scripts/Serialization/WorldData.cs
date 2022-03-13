using System;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class WorldData
{
    public SaveData saveData { get; set; }
    public DelaunayData delaunayData { get; set;}
    public MeshData meshData { get; set; }
    public Point[] points { get; set; }
    public Triangle[] triangles { get; set; }
    public Plate[] plates { get; set; }


    // For debug lines
    public class DelaunayData
    {
        public DelaunayData(Vector3[][] triangleEdges, Vector3[][] voronoiEdges, Vector3[] triangleCenters)
        {

            this.triangleEdges = triangleEdges;
            this.voronoiEdges = voronoiEdges;
            this.triangleCenters = triangleCenters;

            Dictionary<Vector3, int> map = new Dictionary<Vector3, int>();
            for(int a = 0; a < voronoiEdges.Length; a++)
            {
                for(int b = 0; b < voronoiEdges[a].Length; b++)
                {
                    if (map.ContainsKey(voronoiEdges[a][b])) { map[voronoiEdges[a][b]] += 1; }
                    else { map.Add(voronoiEdges[a][b], 1); }
                }
            }

            voronoiPoints = map.Keys.ToArray();
        }

        public Vector3[][] triangleEdges { get; private set; }
        public Vector3[][] voronoiEdges { get; private set; }
        public Vector3[] triangleCenters { get; private set; }
        public Vector3[] voronoiPoints { get; private set; }
        public Vector3[] debug { get; set; }
        public Vector3[][] finalCell { get; set; }
        public Vector3[] debugVoronoi { get; set; }
        public Vector3[][] debugNewVoronoiEdges { get; set; }
    }


    public class MeshData
    {
        public MeshData(Vector3[] v, int[] t, Vector3[] n)
        {
            vertices = v;
            triangles = t;
            normals = normals;
        }

        public Vector3[] vertices { get; private set; }
        public int[] triangles { get; private set; }
        public Vector3[] normals { get; private set; }
    }

    [Serializable]
    public class SaveData
    {
        public SaveData(Point[] pts)
        {
            
        }
    }

    [Serializable]
    public class Point
    {
        // Constructor
        public Vector3 vertex { get; private set; }
        public int globalIndex { get; private set; }

        // Setup
        public Point[] neighbors { get; set; }
        public Plate plate { get; set; }
        public int plateId { get; set; }

        // Map data
        public Height heightData { get; set; }
        public Moisture moistureData { get; set; }
        public Temperature temperatureData { get; set; }
        public Terrain terrainData { get; set; }

        public Point(Vector3 v, int gInd)
        {
            vertex = v;
            globalIndex = gInd;
        }

        // Heightmap
        public class Height
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
        public class Moisture
        {
            public float value { get; set; }
            public Moisture() { }
        }

        // Temperaturemap
        public class Temperature
        {
            public float value { get; set; }
            public Temperature() { }
        }

        // Terrainmap
        public class Terrain
        {
            public Vector3 vertex {get; set;}
            public Terrain() { }
        }


        // Override 
        public override bool Equals(object o)
        {
            Point other = o as Point;
            if (this.vertex == other.vertex) { return true; }
            return false;
        }
        public override int GetHashCode()
        {
            float hc = 1;

            hc += unchecked(hc * 314159 + vertex.x);
            hc -= unchecked(hc * 314159 + vertex.y);
            hc += unchecked(hc * 314159 + vertex.z);

            return (int)Mathf.Round(hc);
        }

        public static bool operator == (Point a, Point b)
        {
            return object.Equals(a, b);
        }
        public static bool operator !=(Point a, Point b)
        {
            return !object.Equals(a, b);
        }
    }

    public class Triangle
    {
        public Triangle(Point[] pts, int[] tris)
        {
            points = pts;
            triangles = tris;
            triangleCenter = IMath.Triangle.Centroid(pts[0].vertex, pts[1].vertex, pts[2].vertex);
            plate = null;
        }

        public Point[] points { get; private set; }
        public int[] triangles { get; private set; }
        public Vector3 triangleCenter { get; private set; }

        // Setup
        public Triangle[] neighbors { get; set; }

        // Plate
        public Plate plate { get; set; }
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

        public class Mesh
        {
            public Mesh(Vector3[] v, int[] t)
            {
                vertices = v;
                triangles = t;
            }

            public Vector3[] vertices { get; private set; }
            public int[] triangles { get; private set; }
        }

        public Plate(Vector3 c, float CvO, int id)
        {
            center = c;

            this.id = id;

            direction = GetRandomDirection();
            speed = GetRandomSpeed();
            plateType = UnityEngine.Random.Range(0f, 1f) >= CvO ? Type.Continental : Type.Oceanic;
            
            float onGradient = IMath.FloorFloat(UnityEngine.Random.Range(0f, 1f), 0.1f);
            color = plateType == Type.Continental ? new Color(0f, onGradient, 0f, 1f) : new Color(0f, 0f, onGradient, 1f);
        }

        public int id { get; private set; }
        public Point[] points { get; set; }
        public Triangle[] triangles { get; set; }
        public FaultLine[] faultLines { get; set; }
        public Vector3 center { get; set; }
        public Vector3 direction { get; private set; }
        public float speed { get; private set; }
        public Type plateType { get; private set; }
        public Color color { get; private set; }

        // Mesh
        public Mesh mesh { get; set; }

        private Vector3 GetRandomDirection()
        {
            Vector3 tangent = Vector3.Cross(center, Vector3.up);
            if (tangent.sqrMagnitude < float.Epsilon) { tangent = Vector3.Cross(center, Vector3.forward); }
            tangent.Normalize();

            Quaternion rotation = Quaternion.AngleAxis(UnityEngine.Random.Range(0f, 360f), center);
            return rotation * tangent;
        }

        private static float GetRandomSpeed()
        {
            return UnityEngine.Random.Range(MIN_SPEED, MAX_SPEED);
        }

        public Vector3[] GetVertices()
        {
            Vector3[] vertices = new Vector3[points.Length];
            for(int a = 0; a < points.Length; a++) { vertices[a] = points[a].vertex; }
            return vertices;
        }
    }

    public class Edge 
    {
        public Edge(Point a, Point b)
        {
            edge = new Point[] { a, b };
            inverse = new Point[] { b, a };
            edgeOf = new int[2];
        }
        public Point[] edge { get; private set; }
        public Point[] inverse { get; private set; }
        public int[] edgeOf { get; set; }

        public override bool Equals(object o)
        {
            Edge other = o as Edge;
            if (this.edge[0] == other.edge[0] && this.edge[1] == other.edge[1]) { return true; }
            if (this.edge[0] == other.inverse[0] && this.edge[1] == other.inverse[1]) { return true; }
            return false;
        }
        public override int GetHashCode()
        {
            float hc = this.edge.Length;

            foreach (Point val in this.edge)
            {
                hc += unchecked(hc * 314159 + val.vertex.x);
                hc -= unchecked(hc * 314159 + val.vertex.y);
                hc += unchecked(hc * 314159 + val.vertex.z);
            }

            return (int)Mathf.Round(hc);
        }

        public static bool operator ==(Edge v1, Edge v2)
        {
            return object.Equals(v1, v2);
        }

        public static bool operator !=(Edge v1, Edge v2)
        {
            return !object.Equals(v1, v2);
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

        public FaultLine(Edge[] e, Plate f1, Plate f2)
        {
            edges = e;

            faultOf = new Plate[] { f1, f2 };

            NullEdgePlates();
        }

        public Edge[] edges { get; private set; }
        public Plate[] faultOf { get; set; }
        public Type type { get; private set; }

        private void NullEdgePlates()
        {
            foreach(Edge e in edges)
            {
                foreach(Point p in e.edge)
                {
                    p.plate = null;
                }
            }
        }

        public void DetermineFaultLineType(World world)
        {
            Plate ind1 = faultOf[0];
            Plate ind2 = faultOf[1];
            Vector3 val1 = (ind1.direction - ind1.center).normalized * ind1.speed;
            Vector3 val2 = (ind2.direction - ind2.center).normalized * ind2.speed;

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