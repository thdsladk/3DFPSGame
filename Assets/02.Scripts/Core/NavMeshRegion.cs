using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

// 역할 : 구워 둔 NavMesh 에서 서로 오고 갈 수 있게 이어진 가장 넓은 영역을 찾아 두고, 그 안에서 무작위 위치를 뽑아 준다.
// NavMesh 삼각형들을 꼭짓점을 공유하는 것끼리 묶으면(유니온 파인드) 걸어서 이어진 영역들이 나온다. 그중 넓이가 가장 큰 영역만 쓴다.
// (지붕 위나 막힌 구석처럼 따로 떨어진 작은 영역은 쓰지 않는다. 한쪽으로만 내려가는 링크는 오고 갈 수 없으므로 잇지 않는다)
public class NavMeshRegion
{
    private const float WeldPrecision = 0.01f; // 이 거리 안의 꼭짓점은 같은 점으로 본다 (타일 경계에서 겹친 꼭짓점을 잇기 위해)
    private const int MaxSampleAttempts = 10; // 뽑은 점이 거리 범위를 벗어나면 다시 뽑는 횟수

    private readonly Vector3[] _vertices;
    private readonly List<int> _triangles = new List<int>(); // 가장 넓은 영역의 삼각형 꼭짓점 번호 (3개씩)
    private readonly List<int> _candidates = new List<int>(); // 이번에 뽑을 수 있는 삼각형 (매번 새로 만들지 않도록 재사용)
    private readonly List<float> _cumulativeAreas = new List<float>(); // 넓은 삼각형일수록 잘 뽑히도록 넓이를 누적해 둔다 (재사용)

    public float Area { get; private set; } // 가장 넓은 영역의 넓이 (㎡)
    public int TriangleCount => _triangles.Count / 3;

    public NavMeshRegion()
    {
        NavMeshTriangulation triangulation = NavMesh.CalculateTriangulation();
        _vertices = triangulation.vertices;
        int[] indices = triangulation.indices;

        // 1. 같은 자리에 겹친 꼭짓점을 하나로 합친다.
        int[] welded = new int[_vertices.Length];
        var vertexByPosition = new Dictionary<Vector3Int, int>();
        for (int i = 0; i < _vertices.Length; i++)
        {
            Vector3Int key = Vector3Int.RoundToInt(_vertices[i] / WeldPrecision);
            if (vertexByPosition.TryGetValue(key, out int first) == false)
            {
                first = i;
                vertexByPosition.Add(key, i);
            }

            welded[i] = first;
        }

        // 2. 삼각형의 세 꼭짓점을 같은 묶음으로 합친다. 꼭짓점을 함께 쓰는 삼각형끼리는 같은 묶음이 된다.
        int[] parents = new int[_vertices.Length];
        for (int i = 0; i < parents.Length; i++)
        {
            parents[i] = i;
        }

        for (int t = 0; t < indices.Length; t += 3)
        {
            Union(parents, welded[indices[t]], welded[indices[t + 1]]);
            Union(parents, welded[indices[t + 1]], welded[indices[t + 2]]);
        }

        // 3. 묶음마다 넓이를 더해 가장 넓은 묶음을 고른다.
        var areaByRoot = new Dictionary<int, float>();
        int bestRoot = -1;
        float bestArea = 0f;
        for (int t = 0; t < indices.Length; t += 3)
        {
            int root = Find(parents, welded[indices[t]]);
            areaByRoot.TryGetValue(root, out float area);
            area += GetTriangleArea(indices[t], indices[t + 1], indices[t + 2]);
            areaByRoot[root] = area;

            if (area > bestArea)
            {
                bestArea = area;
                bestRoot = root;
            }
        }

        // 4. 가장 넓은 묶음의 삼각형만 남긴다.
        for (int t = 0; t < indices.Length; t += 3)
        {
            if (Find(parents, welded[indices[t]]) != bestRoot) continue;

            _triangles.Add(indices[t]);
            _triangles.Add(indices[t + 1]);
            _triangles.Add(indices[t + 2]);
        }

        Area = bestArea;
    }

    // 중심에서 minDistance ~ maxDistance 거리 안의 무작위 위치를 가장 넓은 영역 안에서 뽑는다. 뽑을 곳이 없으면 false.
    public bool TryGetRandomPoint(Vector3 center, float minDistance, float maxDistance, out Vector3 point)
    {
        point = center;

        // 1. 무게중심이 거리 범위 안에 있는 삼각형을 모으고, 넓이를 누적해 둔다.
        _candidates.Clear();
        _cumulativeAreas.Clear();
        float totalArea = 0f;
        for (int t = 0; t < _triangles.Count; t += 3)
        {
            Vector3 centroid = (_vertices[_triangles[t]] + _vertices[_triangles[t + 1]] + _vertices[_triangles[t + 2]]) / 3f;
            float distance = Vector3.Distance(centroid, center);
            if (distance < minDistance || distance > maxDistance) continue;

            totalArea += GetTriangleArea(_triangles[t], _triangles[t + 1], _triangles[t + 2]);
            _candidates.Add(t);
            _cumulativeAreas.Add(totalArea);
        }

        if (_candidates.Count == 0) return false;

        // 2. 넓이에 비례해 삼각형을 고르고 그 안의 무작위 점을 뽑는다. 큰 삼각형은 일부가 범위를 벗어나므로 벗어나면 다시 뽑는다.
        for (int attempt = 0; attempt < MaxSampleAttempts; attempt++)
        {
            int index = _cumulativeAreas.BinarySearch(Random.value * totalArea);
            if (index < 0) index = Mathf.Min(~index, _candidates.Count - 1);

            int t = _candidates[index];
            Vector3 candidate = GetRandomPointInTriangle(_triangles[t], _triangles[t + 1], _triangles[t + 2]);
            float distance = Vector3.Distance(candidate, center);
            if (distance < minDistance || distance > maxDistance) continue;

            point = candidate;
            return true;
        }

        return false;
    }

    private Vector3 GetRandomPointInTriangle(int a, int b, int c)
    {
        // 평행사변형 안의 점을 뽑고, 삼각형 밖(대각선 너머)이면 안으로 뒤집는다.
        float u = Random.value;
        float v = Random.value;
        if (u + v > 1f)
        {
            u = 1f - u;
            v = 1f - v;
        }

        return _vertices[a] + (_vertices[b] - _vertices[a]) * u + (_vertices[c] - _vertices[a]) * v;
    }

    private float GetTriangleArea(int a, int b, int c)
    {
        return Vector3.Cross(_vertices[b] - _vertices[a], _vertices[c] - _vertices[a]).magnitude * 0.5f;
    }

    // 묶음의 대표 번호를 찾는다. 지나온 번호는 대표를 바로 가리키게 해서 다음 찾기를 빠르게 한다.
    private static int Find(int[] parents, int x)
    {
        int root = x;
        while (parents[root] != root)
        {
            root = parents[root];
        }

        while (parents[x] != root)
        {
            int next = parents[x];
            parents[x] = root;
            x = next;
        }

        return root;
    }

    private static void Union(int[] parents, int a, int b)
    {
        int rootA = Find(parents, a);
        int rootB = Find(parents, b);
        if (rootA != rootB) parents[rootB] = rootA;
    }
}
