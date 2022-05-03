using System.Collections.Generic;
using System.Linq;
using System;
using UnityEngine;

using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using BurstCompile = Unity.Burst.BurstCompileAttribute;
using KNN;

using static WorldData;

namespace WorldGeneration.Maps
{
    // Approach:
    // - For continents vs oceans, get random distribution of points a set distance from one another.
    // - Decrement height from the continent centers, which will be considered mountains

    public class TectonicPlateMap : Map
    {
        public class FaultData
        {
            public FaultData() { }

            private static Color DEFAULT_COLOR = Color.yellow;
            public Color defaultColor { get { return DEFAULT_COLOR; } }
            public LineRenderer[] lines { get; set; }
            public Color[] colors { get; set; }
            public Color[] weightedColors { get; set; }

        }

        public FaultData faultData { get; private set; }

        private World.Parameters.Plates parameters;

        static readonly int centersId = Shader.PropertyToID("_Centers"),
                            cellsId = Shader.PropertyToID("_Cells"),
                            closestCenterId = Shader.PropertyToID("_ClosestCenter");

        public TectonicPlateMap(World world, Save save) : base(world)
        {
            this.world = world;
            this.save = save;

            parameters = world.parameters.plates;
        }

        public override void Build()
        {
            faultData = new FaultData();
            BuildTectonicPlates();
            // BuildGameObject();
        }

        private void BuildTectonicPlates()
        {
            Cell[] plateCenters = GeneratePlateCenters();
            world.worldData.plates = GeneratePlates(plateCenters);
        }

        private void BuildGameObject()
        {
            Mesh[] meshes = GetPlateMeshes();

            Plate[] plates = world.worldData.plates;

            GameObject parentObj = new GameObject(World.MapDisplay.TectonicPlateMap.ToString());
            parentObj.transform.parent = world.transform;

            GameObject platesObj = new GameObject("Plates");
            platesObj.transform.parent = parentObj.transform;

            CombineInstance[] combinePlateMeshes = new CombineInstance[plates.Length];
            for (int a = 0; a < plates.Length; a++)
            {
                Plate plate = plates[a];

                GameObject plateObj = new GameObject("Plate " + a);
                plateObj.transform.parent = platesObj.transform;

                CombineInstance[] combineCellMeshes = new CombineInstance[plate.cells.Length];
                for (int b = 0; b < plate.cells.Length; b++)
                {
                    GameObject cell = new GameObject("Cell" + b);
                    cell.transform.parent = plateObj.transform;

                    Color[] colors = new Color[plate.cells[b].mesh.vertices.Length];
                    for (int c = 0; c < colors.Length; c++) { colors[c] = plate.cells[b].color; }

                    MeshFilter meshFilter = cell.AddComponent<MeshFilter>();
                    meshFilter.sharedMesh = plate.cells[b].mesh;
                    meshFilter.sharedMesh.SetColors(colors);

                    combineCellMeshes[b].mesh = meshFilter.sharedMesh;
                    combineCellMeshes[b].transform = meshFilter.transform.localToWorldMatrix;

                    MeshRenderer meshRenderer = cell.AddComponent<MeshRenderer>();
                    meshRenderer.sharedMaterial = materials.map;

                    cell.SetActive(parameters.plateViewLevel == World.PlateViewLevel.Cell);
                }

                MeshFilter plateMeshFilter = plateObj.AddComponent<MeshFilter>();
                plateMeshFilter.sharedMesh = new Mesh();
                plateMeshFilter.sharedMesh.CombineMeshes(combineCellMeshes);
                plateMeshFilter.sharedMesh = IMath.Mesh.CollapseVertices(plateMeshFilter.sharedMesh);
                plateMeshFilter.sharedMesh.RecalculateNormals();

                MeshRenderer plateMeshRenderer = plateObj.AddComponent<MeshRenderer>();
                plateMeshRenderer.sharedMaterial = materials.map;

                combinePlateMeshes[a].mesh = plateMeshFilter.sharedMesh;
                combinePlateMeshes[a].transform = plateMeshFilter.transform.localToWorldMatrix;

                plateMeshRenderer.enabled = parameters.plateViewLevel == World.PlateViewLevel.Plate;
            }

            MeshFilter planetMeshFilter = parentObj.AddComponent<MeshFilter>();
            planetMeshFilter.sharedMesh = new Mesh();
            planetMeshFilter.sharedMesh.CombineMeshes(combinePlateMeshes);
            planetMeshFilter.sharedMesh = IMath.Mesh.CollapseVertices(planetMeshFilter.sharedMesh);
            planetMeshFilter.sharedMesh.RecalculateNormals();

            MeshRenderer planetMeshRenderer = parentObj.AddComponent<MeshRenderer>();
            planetMeshRenderer.sharedMaterial = materials.map;

            planetMeshRenderer.enabled = parameters.plateViewLevel == World.PlateViewLevel.Planet;
        }

        private Mesh[] GetPlateMeshes()
        {
            Mesh[] meshes = new Mesh[world.worldData.plates.Length];
            return meshes;
        }

    /*
    private void BuildBoundaries(Transform parent, int ind, out List<LineRenderer>[] linesList, out List<Color>[] color)
    {
        // List for lines
        FaultLine[] faultLines = world.worldData.plates[ind].faultLines;

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
            for (int b = 0; b < faultLines[a].edges.Length; b++)
            {
                Edge edge = faultLines[a].edges[b];
                linesList[a].Add(BuildLineRenderer(edge, faultLineObj.transform));
                color[a].Add(c);
            }
        }
    }

    private LineRenderer BuildLineRenderer(Vector3[] edge, Transform parent)
    {
        Vector3[] vertices = new Vector3[10];
        GameObject lineObj = GameObject.Instantiate(Resources.Load<GameObject>("Prefabs/Line"));
        lineObj.transform.parent = parent;
        LineRenderer line = lineObj.GetComponent<LineRenderer>();

        vertices[0] = edge[0].normalized;
        vertices[vertices.Length - 1] = edge[1].normalized;

        for (int b = 1; b < vertices.Length - 1; b++)
        {
            vertices[b] = Vector3.Lerp(vertices[0], vertices[vertices.Length - 1], (float)b / vertices.Length).normalized;
        }

        line.positionCount = vertices.Length;
        line.SetPositions(vertices);

        line.startColor = faultData.defaultColor;
        line.endColor = faultData.defaultColor;

        return line;
    }
    */

        // Empty for now
        public void Load()
        {
            GameObject parentObj = new GameObject(World.MapDisplay.TectonicPlateMap.ToString());
            parentObj.transform.parent = world.transform;

            GameObject platesObj = new GameObject("Plates");
            platesObj.transform.parent = parentObj.transform;

            GameObject faultLinesObj = new GameObject("Fault Lines");
            faultLinesObj.transform.parent = parentObj.transform;
        }

        /* ---  --- */
        // Generation of plate centers
        private Cell[] GeneratePlateCenters()
        {
            HashSet<Cell> centers = new HashSet<Cell>();
            while (centers.Count < parameters.plateNumber)
            {
                centers.Add(world.worldData.cells[UnityEngine.Random.Range(0, world.worldData.cells.Length - 1)]);
            }
            return centers.ToArray();
        }

        // Generates the actual plates
        private Plate[] GeneratePlates(Cell[] plateCenters)
        {
            World.Parameters.Plates parameters = world.parameters.plates;

            // Initialization
            Plate[] plates = new Plate[plateCenters.Length];

            // Build plates
            for (int i = 0; i < plateCenters.Length; i++)
            {
                plateCenters[i].plateId = i;
                plates[i] = new Plate(plateCenters[i], parameters.continentalVsOceanic, i);
            }

            RandomFloodFillCells(plates);
            GenerateFaultLines(plates);
            return plates;
        }

        private void RandomFloodFillCells(Plate[] plates)
        {
            /*
            // Compute Shader Approach
            ComputeShader computeShader = parameters.computeShader;
            ComputeBuffer centersBuffer = new ComputeBuffer(plates.Length, 3 * 4);
            ComputeBuffer cellsBuffer = new ComputeBuffer(world.worldData.cells.Length, 3 * 4);
            ComputeBuffer closestCenterBuffer = new ComputeBuffer(world.worldData.cells.Length, 4);

            float3[] plateCenters = new float3[plates.Length];
            for(int a =0; a < plateCenters.Length; a++) { plateCenters[a] = plates[a].center.center; }
            float3[] cellCenters = new float3[world.worldData.cells.Length];
            for(int a = 0; a < cellCenters.Length; a++) { cellCenters[a] = world.worldData.cells[a].center; }

            centersBuffer.SetData(plateCenters);
            cellsBuffer.SetData(cellCenters);

            computeShader.SetBuffer(0, centersId, centersBuffer);
            computeShader.SetBuffer(0, cellsId, cellsBuffer);
            computeShader.SetBuffer(0, closestCenterId, closestCenterBuffer);

            computeShader.Dispatch(0,Mathf.CeilToInt(world.worldData.cells.Length / 64f),1,1);

            uint[] closestCenter = new uint[world.worldData.cells.Length];
            closestCenterBuffer.GetData(closestCenter);

            for(int a = 0; a < closestCenter.Length; a++) { world.worldData.cells[a].plateId = (int)closestCenter[a]; }


            cellsBuffer.Release();
            centersBuffer.Release();
            closestCenterBuffer.Release();

        
            /*
            // Parallel Job Approach
            NativeArray<float3> centers = new NativeArray<float3>(plates.Length, Allocator.Persistent);
            for(int a = 0; a < plates.Length; a++) { centers[a] = plates[a].center.center; }

            NativeArray<float3> points = new NativeArray<float3>(world.worldData.cells.Length, Allocator.Persistent);
            for(int a = 0; a < world.worldData.cells.Length; a++) { points[a] = world.worldData.cells[a].center; }

            NativeArray<int> closestCenter = new NativeArray<int>(world.worldData.cells.Length, Allocator.Persistent);

            ParallelClosestPlateJob job = new ParallelClosestPlateJob()
            { 
                centers = centers,
                points = points,
                closestCenter = closestCenter 
            };

            JobHandle jobHandle = job.Schedule(world.worldData.cells.Length, 10);

            jobHandle.Complete();
            
            for(int a = 0; a < world.worldData.cells.Length; a++)
            {
                world.worldData.cells[a].plateId = closestCenter[a];
            }

            centers.Dispose();
            points.Dispose();
            closestCenter.Dispose();

            */

            // Flood Fill Approach
            Queue<Cell> queue = new Queue<Cell>();
            int[] plateIndices = new int[plates.Length];
            for (int a = 0; a < plates.Length; a++) { plateIndices[a] = a; }

            // Shuffles the indices
            for (int a = 0; a < plateIndices.Length; a++)
            {
                int rand = a + UnityEngine.Random.Range(0, plateIndices.Length - 1 - a);
                int temp = plateIndices[a];
                plateIndices[a] = plateIndices[rand];
                plateIndices[rand] = temp;
            }

            for(int a = 0; a < plateIndices.Length; a++) { queue.Enqueue(plates[plateIndices[a]].center); }

            int cnt = 0;
            while (queue.Count > 0 && cnt < world.parameters.resolution * 5)
            {
                Cell currentCell = queue.Dequeue();
                for (int b = 0; b < currentCell.neighbors.Length; b++)
                {
                    if (currentCell.neighbors[b].plateId == -1)
                    {
                        currentCell.neighbors[b].plateId = currentCell.plateId;
                        queue.Enqueue(currentCell.neighbors[b]);
                    }
                }
                cnt++;
            }

            // Assign cells
            List<Cell>[] cells = new List<Cell>[plates.Length];
            for (int a = 0; a < cells.Length; a++) { cells[a] = new List<Cell>(); }
            for (int a = 0; a < world.worldData.cells.Length; a++)
            {
                try { cells[world.worldData.cells[a].plateId].Add(world.worldData.cells[a]); }
                catch { UnityEngine.Debug.LogError(world.worldData.cells[a].plateId); }
            }
            for (int a = 0; a < cells.Length; a++)
            {
                plates[a].cells = cells[a].ToArray();
            }
        }

        private void GenerateFaultLines(Plate[] plates)
        {

            GetBorderCells(plates);

        }

        private void GetBorderCells(Plate[] plates)
        {
            HashSet<CellEdge>[] borderCellEdges = new HashSet<CellEdge>[plates.Length];
            for (int a = 0; a < plates.Length; a++)
            {
                borderCellEdges[a] = new HashSet<CellEdge>();
                for (int b = 0; b < plates[a].cells.Length; b++)
                {
                    Cell cellOne = plates[a].cells[b];
                    for (int c = 0; c < cellOne.neighbors.Length; c++)
                    {
                        Cell cellTwo = plates[a].cells[b].neighbors[c];
                        if (cellOne.plateId != cellTwo.plateId)
                        {
                            HashSet<Vector3> cellOneHash = new HashSet<Vector3>();
                            for (int d = 0; d < cellOne.points.Length; d++) { cellOneHash.Add(cellOne.points[d]); }

                            HashSet<Vector3> cellTwoHash = new HashSet<Vector3>();
                            for (int d = 0; d < cellTwo.points.Length; d++) { cellTwoHash.Add(cellTwo.points[d]); }

                            Vector3[] pqVerts = cellOneHash.Intersect(cellTwoHash).ToArray();
                            int[] pqs = new int[pqVerts.Length];
                            for (int d = 0; d < pqVerts.Length; d++)
                            {
                                pqs[d] = Array.IndexOf(cellOne.points, pqVerts[d]);
                            }

                            if (pqs.Length > 0)
                            {
                                borderCellEdges[a].Add(new CellEdge(pqs[pqs.Length - 1], pqs[0], b, a));
                                for (int d = 0; d < pqs.Length - 1; d++)
                                {
                                    borderCellEdges[a].Add(new CellEdge(pqs[d], pqs[d + 1], b, a));
                                }
                            }
                        }
                    }
                }
            }

            UnityEngine.Debug.Log(borderCellEdges.Length);

            // We have HashSet of all borderCellEdges for each plate (a)
            // See if for each plate (a), that hash set contains a plate edge for another plate
            // If it does, construct fault line and give to each plate


            List<CellEdge[]> plateEdges = new List<CellEdge[]>();
            int count = 0;
            for (int a = 0; a < borderCellEdges.Length; a++)
            {
                for (int b = a + 1; b < borderCellEdges.Length; b++)
                {
                    for(int c = 0; c < borderCellEdges[a].Count; c++)
                    {
                        for(int d = 0; d < borderCellEdges[b].Count; d++)
                        {
                            
                        }
                    }
                }
            }

            UnityEngine.Debug.Log(count);
        }

        [BurstCompile]
        struct ParallelClosestPlateJob : IJobParallelFor
        {
            [ReadOnly] public NativeArray<float3> centers;
            [ReadOnly] public NativeArray<float3> points;
            [WriteOnly] public NativeArray<int> closestCenter;

            public void Execute(int ind)
            {
                float minDistance = IMath.SquareDistance(centers[0], points[ind]);
                int minInd = 0;
                for (int a = 1; a < centers.Length; a++)
                {
                    float distance = IMath.SquareDistance(centers[a], points[ind]);
                    if (distance < minDistance)
                    {
                        minDistance = distance;
                        minInd = a;
                    }
                }

                closestCenter[ind] = minInd;
            }
        }
    }
}


