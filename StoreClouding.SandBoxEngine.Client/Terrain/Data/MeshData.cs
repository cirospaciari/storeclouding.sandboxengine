using StoreClouding.SandBoxEngine.Client.Terrain.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace StoreClouding.SandBoxEngine.Client.Terrain.Data
{
    public class MeshData
    {

        public static readonly int CACHE_SIZE_X = Chunk.SIZE_X + 1;
        public static readonly int CACHE_SIZE_Y = Chunk.SIZE_Y + 1;
        public static readonly int CACHE_SIZE_Z = Chunk.SIZE_Z + 1;

        public List<Vector3> vertices = new List<Vector3>();
        public readonly List<Vector2> uv = new List<Vector2>();
        public List<Vector3> normals = new List<Vector3>();
        public readonly List<Vector4> tangents = new List<Vector4>();
        public readonly List<Color> colors = new List<Color>();
        public readonly List<int>[] indices;
        public readonly Dictionary<int, int> edgeVertexCacheX = new Dictionary<int, int>();
        public readonly Dictionary<int, int> edgeVertexCacheY = new Dictionary<int, int>();
        public readonly Dictionary<int, int> edgeVertexCacheZ = new Dictionary<int, int>();

        // Allow to precompute list.ToArray in threads
        public Vector3[] verticesArray = null;
        public Vector3[] normalsArray = null;
        public Vector2[] uvArray = null;
        public Vector4[] tangentsArray = null;
        public Color[] colorsArray = null;
        public int[][] indicesArray = null;

        public MeshData(int subMeshCount)
        {
            indices = new List<int>[subMeshCount];
            for (int i = 0; i < subMeshCount; i++)
            {
                indices[i] = new List<int>();
            }
        }

        public int GetSubMeshCount()
        {
            return indices.Length;
        }

        public List<int> GetIndices(int index)
        {
            return indices[index];
        }

        public void SetIndices(int index, List<int> indicesSub)
        {
            indices[index] = indicesSub;
        }

        public void Clear()
        {
            vertices.Clear();
            uv.Clear();
            normals.Clear();
            colors.Clear();
            tangents.Clear();
            foreach (List<int> list in indices)
            {
                list.Clear();
            }
            edgeVertexCacheX.Clear();
            edgeVertexCacheY.Clear();
            edgeVertexCacheZ.Clear();
            verticesArray = null;
            colorsArray = null;
            normalsArray = null;
            uvArray = null;
            tangentsArray = null;
            indicesArray = null;
        }

        public void PrepareArrays()
        {
            verticesArray = vertices.ToArray();
            colorsArray = colors.ToArray();
            normalsArray = normals.ToArray();
            uvArray = uv.ToArray();
            tangentsArray = tangents.ToArray();
            indicesArray = new int[indices.Length][];
            for (int i = 0; i < indices.Length; i++)
            {
                indicesArray[i] = indices[i].ToArray();
            }
        }

        public Mesh ToMesh(Mesh mesh)
        {
            if (vertices.Count == 0)
            {
                if (mesh != null)
                {
                    if (!Application.isEditor)
                        GameObject.Destroy(mesh);
                    else
                        GameObject.DestroyImmediate(mesh);
                }
                return null;
            }

            if (mesh == null)
                mesh = new Mesh();

            if (verticesArray == null)
            {
                Debug.LogError("Please call meshData.PrepareArrays() before meshData.ToMesh()");
                return null;
            }

            mesh.Clear();
            mesh.vertices = verticesArray;
            mesh.colors = colorsArray;
            mesh.normals = normalsArray;
            mesh.uv = uvArray;
            mesh.tangents = tangentsArray;

            mesh.subMeshCount = indicesArray.Length;
            for (int i = 0; i < indicesArray.Length; i++)
            {
                //TODO: CORREÇÃO FEITA DE ACORDO COM COMENTARIO FAVOR REVISAR
                if (indicesArray[i].Length > 0)
                    mesh.SetTriangles(indicesArray[i], i);
                else
                    mesh.SetTriangles(new int[3] { 0, 0, 0 }, i);//INSERIDO PARA EVITAR CRASH ANALISAR
            }

            return mesh;
        }

    }
	
}
