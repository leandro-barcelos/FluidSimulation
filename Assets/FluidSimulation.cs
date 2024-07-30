using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using Random = UnityEngine.Random;

public class FluidSimulation : MonoBehaviour
{
    [Header("Particle Settings")]
    [Range(100, 1000)] public int numParticles = 500;
    [Range(0.1f, 1f)] public float particleRadius = 0.5f;
    [Range(1, 50)] public int particleSizes = 12;
    public Material material;

    [Header("Container Settings")]
    public Vector2 boundsSize = new(16f, 10f);
    public Vector2 containerCenter = Vector2.zero;

    [Header("Grid Settings")]
    public bool drawGrid;
    [Range(1, 25)] public int subgridLength = 6;
    [Range(0.01f, 5f)] public float cellSize = 1f;

    // Particles
    private Vector2[] _positions;
    private Vector4[] _colors;
    private int _numParticles;

    // Mesh
    private Mesh _particleMesh;
    private Matrix4x4[] _particleMatrices;
    private MaterialPropertyBlock _block;

    // Grid

    private Hashtable _grid;

    // Utils
    private bool _enableGizmos;

    #region Event Functions

    private void Start()
    {
        InitializeParticles();
        InitializeGrid();
        _enableGizmos = true;
    }

    private void Update()
    {
        DrawContainer();

        if (numParticles != _numParticles) numParticles = _numParticles;

        _block.SetVectorArray("_Colors", _colors);
        Graphics.DrawMeshInstanced(_particleMesh, 0, material, _particleMatrices, numParticles, _block);
    }

    private void OnDrawGizmos()
    {
        if (_grid is not null && drawGrid)
        {
            foreach (DictionaryEntry entry in _grid)
            {
                var origin = (Vector2)entry.Key * subgridLength;
                DrawSubGrid(origin);
            }
        }
    }

    #endregion

    #region Particles
    private void InitializeParticles()
    {
        _particleMesh = PolyMesh(particleRadius, particleSizes);
        _particleMatrices = new Matrix4x4[numParticles];
        _block = new MaterialPropertyBlock();
        _numParticles = numParticles;

        _positions = new Vector2[_numParticles];

        Vector2 halfBoundsSize = boundsSize * 0.5f;

        _colors = new Vector4[numParticles];

        for (int i = 0; i < _numParticles; i++)
        {
            _positions[i] = new Vector2(
                Random.Range(containerCenter.x - halfBoundsSize.x, containerCenter.x + halfBoundsSize.x),
                Random.Range(containerCenter.y - halfBoundsSize.y, containerCenter.y + halfBoundsSize.y));
            _particleMatrices[i] = Matrix4x4.TRS(_positions[i], Quaternion.identity, Vector2.one);

            _colors[i] = Color.Lerp(Color.red, Color.blue, Random.value);
        }
    }

    public Mesh PolyMesh(float radius, int n)
    {
        Mesh mesh = new Mesh();

        //verticies
        List<Vector3> verticesList = new List<Vector3> { };
        float x;
        float y;
        for (int i = 0; i < n; i++)
        {
            x = radius * Mathf.Sin((2 * Mathf.PI * i) / n);
            y = radius * Mathf.Cos((2 * Mathf.PI * i) / n);
            verticesList.Add(new Vector3(x, y, 0f));
        }
        Vector3[] vertices = verticesList.ToArray();

        //triangles
        List<int> trianglesList = new List<int> { };
        for (int i = 0; i < (n - 2); i++)
        {
            trianglesList.Add(0);
            trianglesList.Add(i + 1);
            trianglesList.Add(i + 2);
        }
        int[] triangles = trianglesList.ToArray();

        //normals
        List<Vector3> normalsList = new List<Vector3> { };
        for (int i = 0; i < vertices.Length; i++)
        {
            normalsList.Add(-Vector3.forward);
        }
        Vector3[] normals = normalsList.ToArray();

        //uvs
        Vector2[] uvs = new Vector2[vertices.Length];
        for (int i = 0; i < uvs.Length; i++)
        {
            uvs[i] = new Vector2(vertices[i].x / (radius * 2) + 0.5f, vertices[i].y / (radius * 2) + 0.5f);
        }

        //initialise
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.normals = normals;
        mesh.uv = uvs;

        return mesh;
    }

    #endregion

    #region Grid

    private void InitializeGrid()
    {
        _grid = new Hashtable();
        var numHori = Mathf.Max(Mathf.Ceil(boundsSize.x / subgridLength), 1);
        var numVert = Mathf.Max(Mathf.Ceil(boundsSize.y / subgridLength), 1);

        for (int i = 0; i < numHori; i++)
        {
            for (int j = 0; j < numVert; j++)
            {
                AddToGrid(CreateSubgrid(), i, j);
                AddToGrid(CreateSubgrid(), -i, j);
                AddToGrid(CreateSubgrid(), i, -j);
                AddToGrid(CreateSubgrid(), -i, -j);
            }
        }
    }

    private struct GridCell
    {
        Vector2 position;
    }

    private GridCell[] CreateSubgrid()
    {
        return new GridCell[subgridLength];
    }

    private GridCell GetGridCell(GridCell[] subgrid, int i, int j)
    {
        return subgrid[(i * subgridLength) + j];
    }

    private void AddToGrid(GridCell[] subgrid, int x, int y)
    {
        var key = new Vector2(x, y);

        if (_grid.ContainsKey(key)) return;

        _grid.Add(key, subgrid);
    }

    #endregion

    #region Debug
    private void DrawContainer()
    {
        Vector2 halfBoundsSize = boundsSize * 0.5f;
        Vector2[] corners = new Vector2[4]
        {
            new Vector2(containerCenter.x - halfBoundsSize.x, containerCenter.y + halfBoundsSize.y),
            new Vector2(containerCenter.x + halfBoundsSize.x, containerCenter.y + halfBoundsSize.y),
            new Vector2(containerCenter.x - halfBoundsSize.x, containerCenter.y - halfBoundsSize.y),
            new Vector2(containerCenter.x + halfBoundsSize.x, containerCenter.y - halfBoundsSize.y)
        };

        // Draw top and bottom lines
        Debug.DrawLine(corners[0], corners[1], Color.green, 0f, true);
        Debug.DrawLine(corners[2], corners[3], Color.green, 0f, true);

        // Draw left and right lines
        Debug.DrawLine(corners[0], corners[2], Color.green, 0f, true);
        Debug.DrawLine(corners[1], corners[3], Color.green, 0f, true);
    }

    void DrawSubGrid(Vector2 origin)
    {
        Vector2 offset = new Vector2(subgridLength * cellSize / 2, subgridLength * cellSize / 2);

        // Draw vertical lines
        for (int x = 0; x <= subgridLength; x++)
        {
            Vector2 start = origin + new Vector2(x * cellSize, 0) - offset;
            Vector2 end = start + new Vector2(0, subgridLength * cellSize);
            Debug.DrawLine(start, end, Color.green);
        }

        // Draw horizontal lines
        for (int y = 0; y <= subgridLength; y++)
        {
            Vector2 start = origin + new Vector2(0, y * cellSize) - offset;
            Vector2 end = start + new Vector2(subgridLength * cellSize, 0);
            Debug.DrawLine(start, end, Color.green);
        }
    }

    #endregion
}
