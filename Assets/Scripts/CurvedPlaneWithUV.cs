/*
MIT License

Copyright (c) 2016 Matt Favero

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
*/

using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
public class CurvedPlaneWithUV : MonoBehaviour
{
    private class MeshData
    {
        public Vector3[] Vertices { get; set; }
        public int[] Triangles { get; set; }
        public Vector2[] Uvs { get; set; }
    }

    [SerializeField]
    private float height = 1f;

    [SerializeField]
    private float radius = 2f;

    [SerializeField]
    [Range(1, 1024)]
    private int numSegments = 16;

    [SerializeField]
    [Range(0f, 360f)]
    private float curvatureDegrees = 60f;

    [SerializeField]
    private bool useArc = true;

    private MeshData plane;

    void Start()
    {
        Generate();
    }

    void OnValidate()
    {
        Generate();
    }

    [ContextMenu("Generate")]
    private void Generate()
    {
        GenerateScreen();
        UpdateMeshFilter();
    }

    private void UpdateMeshFilter()
    {
        var filter = GetComponent<MeshFilter>();

        var mesh = new Mesh
        {
            vertices = plane.Vertices,
            triangles = plane.Triangles,
            uv = plane.Uvs
        };
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        mesh.RecalculateTangents();

		#if UNITY_EDITOR
        UnityEditor.MeshUtility.Optimize(mesh);
		#endif

        filter.mesh = mesh;

        var col = GetComponent<MeshCollider>();
        if (col != null)
        {
            col.sharedMesh = mesh;
        }
    }

    private void GenerateScreen()
    {
        plane = new MeshData
        {
            Vertices = new Vector3[(numSegments + 2) * 2],
            Triangles = new int[numSegments * 6],
            Uvs = new Vector2[(numSegments + 2) * 2]
        };

        int i, j;
        for (i = j = 0; i < numSegments + 1; i++)
        {
            GenerateVertexPair(ref i, ref j);

            if (i < numSegments)
            {
                GenerateLeftTriangle(ref i, ref j);
                GenerateRightTriangle(ref i, ref j);
            }
        }
    }

    private void GenerateVertexPair(ref int i, ref int j)
    {
        float amt = ((float) i) / numSegments;
        float arcDegrees = curvatureDegrees * Mathf.Deg2Rad;
        float theta = -0.5f + amt;

        var x = useArc ? Mathf.Sin(theta * arcDegrees) * radius : (-0.5f * radius) + (amt * radius);
        var z = Mathf.Cos(theta * arcDegrees) * radius;

        plane.Vertices[i] = new Vector3(x, height / 2f, z);
        plane.Vertices[i + numSegments + 1] = new Vector3(x, -height / 2f, z);
        plane.Uvs[i] = new Vector2(amt, 1);
        plane.Uvs[i + numSegments + 1] = new Vector2(amt, 0);
    }

    private void GenerateLeftTriangle(ref int i, ref int j)
    {
        plane.Triangles[j++] = i;
        plane.Triangles[j++] = i + 1;
        plane.Triangles[j++] = i + numSegments + 1;
    }

    private void GenerateRightTriangle(ref int i, ref int j)
    {
        plane.Triangles[j++] = i + 1;
        plane.Triangles[j++] = i + numSegments + 2;
        plane.Triangles[j++] = i + numSegments + 1;
    }
}