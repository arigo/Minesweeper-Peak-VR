using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class Floor : MonoBehaviour
{
    public int halfResolution = 20;
    public float flatDistance = 2.3f;
    public float verticalDistance = 50f;
    public float randomZ = 0.1f;
    public float maximumRange = 10f;


    private void Start()
    {
        int nb_cells = halfResolution * 2;
        int nb_vertices = nb_cells + 1;

        Vector3[] vertices = new Vector3[nb_vertices * nb_vertices];
        Vector2[] uvs = new Vector2[nb_vertices * nb_vertices];
        int v_index = 0;

        for (int j = -halfResolution; j <= halfResolution; j++)
            for (int i = -halfResolution; i <= halfResolution; i++)
            {
                float distance = (new Vector2(i, j).magnitude - flatDistance) / verticalDistance;
                float z;
                if (distance <= 0)
                    z = 1;
                else
                    z = Mathf.Sqrt(1 - distance * distance) + Random.Range(-randomZ, randomZ);
                uvs[v_index] = new Vector2(i, j) * 0.0558f;
                vertices[v_index++] = new Vector3(i, z, j);
            }
        Debug.Assert(v_index == vertices.Length);

        List<int> triangles = new List<int>();
        for (int j = 0; j < nb_cells; j++)
            for (int i = 0; i < nb_cells; i++)
            {
                float dist2 = new Vector2(i - halfResolution + 0.5f, j - halfResolution + 0.5f).sqrMagnitude;
                if (dist2 > maximumRange * maximumRange)
                    continue;

                v_index = j * nb_vertices + i;
                triangles.Add(v_index);
                triangles.Add(v_index + nb_vertices);
                triangles.Add(v_index + 1);
                triangles.Add(v_index + 1);
                triangles.Add(v_index + nb_vertices);
                triangles.Add(v_index + nb_vertices + 1);
            }

        var mesh = new Mesh();
        mesh.vertices = vertices;
        mesh.uv = uvs;
        mesh.SetTriangles(triangles, 0);
        mesh.Optimize();
        mesh.RecalculateBounds();
        mesh.RecalculateNormals();
        mesh.RecalculateTangents();
        GetComponent<MeshFilter>().mesh = mesh;

        /* this quad is only there for the light probe */
        Destroy(transform.GetChild(0).gameObject);
    }
}
