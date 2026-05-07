using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Reign.SRP
{
    public static class BlitMesh
    {
        public static Mesh mesh { get; private set; }
        public static Mesh meshFlipped { get; private set; }

        public static void InitCheck()
        {
            if (mesh != null && meshFlipped != null) return;

            const int size = 1;
            var verts = new Vector3[]
            {
                new Vector3(-size, -size, 0),
                new Vector3(-size, size, 0),
                new Vector3(size, size, 0),
                new Vector3(size, -size, 0)
            };

            var uvs = new Vector2[]
            {
                new Vector2(0, 0),
                new Vector2(0, 1),
                new Vector2(1, 1),
                new Vector2(1, 0)
            };

            var indices = new int[]
            {
                0, 1, 2,
                0, 2, 3
            };

            mesh = new Mesh();
            mesh.vertices = verts;
            mesh.uv = uvs;
            mesh.SetIndices(indices, MeshTopology.Triangles, 0);
            mesh.Optimize();

            // flipped version
            for (int i = 0; i != uvs.Length; ++i)
            {
                uvs[i].y = 1 - uvs[i].y;
            }

            meshFlipped = new Mesh();
            meshFlipped.vertices = verts;
            meshFlipped.uv = uvs;
            meshFlipped.SetIndices(indices, MeshTopology.Triangles, 0);
            meshFlipped.Optimize();

            // 90deg version
            uvs = new Vector2[]
            {
                new Vector2(0, 1),
                new Vector2(1, 1),
                new Vector2(1, 0),
                new Vector2(0, 0)
            };
        }
    }
}
