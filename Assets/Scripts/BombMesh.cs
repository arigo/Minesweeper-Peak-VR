using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class BombMesh : MonoBehaviour
{
    public int explodedMesh;

    static Mesh[] static_mesh = new Mesh[2];

    void Start()
    {
        if (static_mesh[explodedMesh] == null)
        {
            const int NSIDES = 9;
            const float SPIKE_END = 0.0125f / 0.0254f;
            const float SPIKE_WIDTH = 0.003f / 0.0254f;

            Quaternion[] orientations = new Quaternion[]
            {
                Quaternion.Euler(0, 90, 0),
                Quaternion.Euler(0, -45, 0),
                Quaternion.Euler(90, 0, 0),
                Quaternion.Euler(45, 0, 0),
                Quaternion.Euler(0, 0, -45),

                Quaternion.Euler(0, 0, 90),
                Quaternion.Euler(-45, 0, 0),
                Quaternion.Euler(0, 45, 0),
                Quaternion.Euler(0, 0, 45),
            };
            if (explodedMesh == 1)
                System.Array.Resize(ref orientations, 5);

            List<Vector3> vertices = new List<Vector3>();
            List<Vector2> uvs = new List<Vector2>();
            List<int> triangles = new List<int>();
            foreach (var q in orientations)
            {
                int b = vertices.Count;
                for (int i = 0; i < NSIDES; i++)
                {
                    float alpha = (i * (2 * Mathf.PI)) / NSIDES;
                    float sa = Mathf.Sin(alpha);
                    float ca = Mathf.Cos(alpha);

                    vertices.Add(q * new Vector3(0, 0, SPIKE_END));
                    vertices.Add(q * new Vector3(SPIKE_WIDTH * sa, SPIKE_WIDTH * ca, 0));
                    vertices.Add(q * new Vector3(0, 0, -SPIKE_END));

                    uvs.Add(new Vector2(0, 0));
                    uvs.Add(new Vector2(sa, ca));
                    uvs.Add(new Vector2(0, 0));
                }
                int b_prev = vertices.Count - 3;
                for (int i = 0; i < NSIDES; i++)
                {
                    triangles.Add(b);
                    triangles.Add(b + 1);
                    triangles.Add(b_prev + 1);
                    triangles.Add(b_prev + 1);
                    triangles.Add(b + 1);
                    triangles.Add(b + 2);
                    b_prev = b;
                    b += 3;
                }
            }

            var mesh = new Mesh();
            mesh.SetVertices(vertices);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(triangles, 0);
            mesh.Optimize();
            mesh.RecalculateBounds();
            mesh.RecalculateNormals();
            mesh.RecalculateTangents();
            mesh.UploadMeshData(true);
            static_mesh[explodedMesh] = mesh;
        }
        var filter = GetComponent<MeshFilter>();
        if (filter != null)
            filter.sharedMesh = static_mesh[explodedMesh];
        Destroy(this);
    }
}
