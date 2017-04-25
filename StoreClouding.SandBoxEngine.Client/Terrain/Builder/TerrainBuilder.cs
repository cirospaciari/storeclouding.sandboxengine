using StoreClouding.SandBoxEngine.Client.Terrain.Data;
using StoreClouding.SandBoxEngine.Client.Terrain.Utils;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace StoreClouding.SandBoxEngine.Client.Terrain.Builder
{
    class TerrainBuilder
    {

        private ConcurrentDictionary<int, List<BuildData>> ChunksBuildData = new ConcurrentDictionary<int, List<BuildData>>(8, 8000);

        private MarchingCubes marchingCubes;
        public static int subMeshCount;

        private BlockData[] lineX0Y0;
        private BlockData[] lineX0Y1;

        private BlockData[] lineX1Y0;
        private BlockData[] lineX1Y1;

        private BlockData[][] lineX0ZP;
        private BlockData[][] lineX1ZP;

        private ChunkData neighbourXP;
        private ChunkData neighbourYP;
        private ChunkData neighbourZP;
        private ChunkData neighbourXPZP;
        private ChunkData neighbourXPYP;
        private ChunkData neighbourYPZP;
        private ChunkData neighbourXPYPZP;

        private Vector3i[] gridPos = null;
        public TerrainBuilder()
        {
            marchingCubes = new MarchingCubes();
            subMeshCount = Builder.Components.Map.Current.Materials.Length;
        }


        /// <summary>
        /// Perform UV mapping on the mesh.</summary>
        /// <param name="mesh"> The mesh on which UV mapping will be done.</param>
        /// <param name="chunk"> The chunk of the mesh.</param>
        private void ComputeUVs(MeshData mesh, ChunkData chunk)
        {
            Vector3 offset = (Vector3)Utils.Chunk.ToWorldPosition(chunk.Position, Vector3i.zero);
            // UV
            foreach (Vector3 ver in mesh.vertices)
            {
                mesh.uv.Add(new Vector2(ver.x + offset.x, ver.z + offset.z));
            }
        }

        private void ComputeColorsAfterBuild(MeshData mesh, ChunkData chunk, bool waterLevel, bool underwater)
        {
            Color topColor = new Color(1f, 0, 0, 0);
            Color cliffsColor = new Color(0, 0, 0, 1f);
            int vCount = mesh.vertices.Count;

            for (int i = 0; i < vCount; i++)
            {
                if (mesh.colors[i] == topColor)
                {
                    if (mesh.normals[i].y < 0.5f)
                    {
                        mesh.colors[i] = cliffsColor;
                    }
                }
            }
        }

        /// <summary>
        /// Compute normals of the mesh.</summary>
        /// <param name="mesh"> The mesh on which normals will be computed.</param>
        /// <param name="chunk"> The chunk of the mesh.</param>
        private void ComputeNormals(MeshData mesh, ChunkData chunk)
        {
            Vector3[] normals = new Vector3[mesh.vertices.Count];

            for (int i = 0; i < normals.Length; i++)
            {
                normals[i] = Vector3.zero;
            }

            for (int sub = 0; sub < mesh.GetSubMeshCount(); sub++)
            {
                List<int> indices = mesh.GetIndices(sub);
                for (int i = 0; i < indices.Count; i += 3)
                {
                    int i1 = indices[i + 0];
                    int i2 = indices[i + 1];
                    int i3 = indices[i + 2];

                    // get the three vertices that make the faces
                    Vector3 p1 = mesh.vertices[i1];
                    Vector3 p2 = mesh.vertices[i2];
                    Vector3 p3 = mesh.vertices[i3];

                    Vector3 v1 = p2 - p1;
                    Vector3 v2 = p3 - p1;
                    Vector3 normal = Vector3.Cross(v1, v2);
                    //normal.Normalize();
                    normals[i1] += normal;
                    normals[i2] += normal;
                    normals[i3] += normal;
                }
            }

            mesh.normals.Clear();
            mesh.normals.Capacity = normals.Length + 16;
            for (int i = 0; i < normals.Length; i++)
            {
                Vector3 normal = normals[i];
                normal.Normalize();
                mesh.normals.Add(normal);
            }
        }

        /// <summary>
        /// Compute tangents of the mesh.</summary>
        /// <param name="mesh"> The mesh on which tangents will be computed.</param>
        private void SolveTangents(MeshData mesh)
        {
            int vertexCount = mesh.vertices.Count;

            Vector3[] tan1 = new Vector3[vertexCount];
            Vector3[] tan2 = new Vector3[vertexCount];

            for (int sub = 0; sub < mesh.GetSubMeshCount(); sub++)
            {
                List<int> indices = mesh.GetIndices(sub);
                for (int a = 0; a < indices.Count; a += 3)
                {
                    int i1 = indices[a + 0];
                    int i2 = indices[a + 1];
                    int i3 = indices[a + 2];

                    Vector3 v1 = mesh.vertices[i1];
                    Vector3 v2 = mesh.vertices[i2];
                    Vector3 v3 = mesh.vertices[i3];

                    Vector2 w1 = mesh.uv[i1];
                    Vector2 w2 = mesh.uv[i2];
                    Vector2 w3 = mesh.uv[i3];

                    float x1 = v2.x - v1.x;
                    float x2 = v3.x - v1.x;
                    float y1 = v2.y - v1.y;
                    float y2 = v3.y - v1.y;
                    float z1 = v2.z - v1.z;
                    float z2 = v3.z - v1.z;

                    float s1 = w2.x - w1.x;
                    float s2 = w3.x - w1.x;
                    float t1 = w2.y - w1.y;
                    float t2 = w3.y - w1.y;

                    float div = s1 * t2 - s2 * t1;
                    float r = Mathf.Approximately(div, 0f) ? 0f : 1f / div;

                    Vector3 sdir = new Vector3((t2 * x1 - t1 * x2) * r, (t2 * y1 - t1 * y2) * r, (t2 * z1 - t1 * z2) * r);
                    Vector3 tdir = new Vector3((s1 * x2 - s2 * x1) * r, (s1 * y2 - s2 * y1) * r, (s1 * z2 - s2 * z1) * r);

                    tan1[i1] += sdir;
                    tan1[i2] += sdir;
                    tan1[i3] += sdir;

                    tan2[i1] += tdir;
                    tan2[i2] += tdir;
                    tan2[i3] += tdir;
                }
            }


            for (int a = 0; a < vertexCount; ++a)
            {
                Vector3 n = mesh.normals[a];
                Vector3 t = tan1[a];

                Vector3 tmp = (t - n * Vector3.Dot(n, t)).normalized;
                float w = (Vector3.Dot(Vector3.Cross(n, t), tan2[a]) < 0.0f) ? -1.0f : 1.0f;
                mesh.tangents.Add(new Vector4(tmp.x, tmp.y, tmp.z, w));
            }
        }

        /// <summary>
        /// Harmonize twin vertices.</summary>
        private void HarmonizeTwin(int vi, int nvi, MeshData mesh, MeshData nMesh, Vector3 dir)
        {
            try
            {
                // Harmonize normals
                mesh.normals[vi] = nMesh.normals[nvi];
                // Harmonize colors
                //mesh.colors[vi] = nMesh.colors[nvi];
                // Harmonize tangents
                mesh.tangents[vi] = nMesh.tangents[nvi];

            }
            catch (System.Exception e)
            {
                Debug.LogException(e);
                Debug.Log("HarmonizeTwin: vi=" + vi + " nvi=" + nvi + " mesh.vertices.Count=" + mesh.vertices.Count + " nMesh.vertices.Count=" + nMesh.vertices.Count);
                Debug.Log("HarmonizeTwin: vi=" + vi + " nvi=" + nvi + " mesh.normals.Count=" + mesh.normals.Count + " nMesh.normals.Count=" + nMesh.normals.Count);
                Debug.Log("HarmonizeTwin: vi=" + vi + " nvi=" + nvi + " mesh.colors.Count=" + mesh.colors.Count + " nMesh.colors.Count=" + nMesh.colors.Count);
            }
        }

        /// <summary>
        /// Harmonize all twin vertices of the mesh.</summary>
        /// <param name="mesh"> The mesh to harmonize with its neighbours.</param>
        /// <param name="chunk"> The chunk of the mesh.</param>
        /// <param name="threaded"> Is it called from a thread?</param>
        private void HarmonizeTwinVertices(MeshData mesh, ChunkData chunk, bool threaded)
        {
            // Iterate over neighbours
            foreach (Vector3i dir in Vector3i.allDirections)
            {
                ChunkData neighbour = chunk.GetNeighbour(dir);
                if (neighbour == null)
                    continue;
                BuildData neighbourBuildData = this.GetBuildData(neighbour.MemoryID);
                // Wait...
                while (threaded && neighbourBuildData.BuildStarted && !neighbourBuildData.IsHarmonizable)
                {
                    System.Threading.Thread.Sleep(21);
                }

                if (neighbourBuildData.IsHarmonizable)
                {
                    MeshData nMesh = neighbourBuildData.TempMeshData;
                    if (nMesh == null || nMesh.vertices.Count == 0)
                    {
                        continue;
                    }

                    int nx = 0;
                    int startX = 0;
                    int endX = Chunk.SIZE_X;
                    if (dir.x == -1)
                    {
                        nx = endX;
                        endX = startX;
                    }
                    else if (dir.x == 1)
                    {
                        nx = -endX;
                        startX = endX;
                    }
                    int ny = 0;
                    int startY = 0;
                    int endY = Chunk.SIZE_Y;
                    if (dir.y == -1)
                    {
                        ny = endY;
                        endY = startY;
                    }
                    else if (dir.y == 1)
                    {
                        ny = -endY;
                        startY = endY;
                    }
                    int nz = 0;
                    int startZ = 0;
                    int endZ = Chunk.SIZE_Z;
                    if (dir.z == -1)
                    {
                        nz = endZ;
                        endZ = startZ;
                    }
                    else if (dir.z == 1)
                    {
                        nz = -endZ;
                        startZ = endZ;
                    }
                    Vector3i direction = dir;
                    direction.x *= Chunk.SIZE_X_TOTAL;
                    direction.y *= Chunk.SIZE_Y_TOTAL;
                    direction.z *= Chunk.SIZE_Z_TOTAL;
                    Vector3 directionf = (Vector3)direction;
                    int vi, nvi;

                    for (int x = startX; x <= endX; x++)
                    {
                        for (int y = startY; y <= endY; y++)
                        {
                            for (int z = startZ; z <= endZ; z++)
                            {
                                int cacheIndex = x + MeshData.CACHE_SIZE_X * (y + MeshData.CACHE_SIZE_Y * z);
                                int nCacheIndex = (x + nx) + MeshData.CACHE_SIZE_X * ((y + ny) + MeshData.CACHE_SIZE_Y * (z + nz));

                                // CACHE X
                                if (mesh.edgeVertexCacheX.TryGetValue(cacheIndex, out vi))
                                {
                                    if (nMesh.edgeVertexCacheX.TryGetValue(nCacheIndex, out nvi))
                                    {
                                        HarmonizeTwin(vi, nvi, mesh, nMesh, directionf);
                                    }
                                }
                                // CACHE Y
                                if (mesh.edgeVertexCacheY.TryGetValue(cacheIndex, out vi))
                                {
                                    if (nMesh.edgeVertexCacheY.TryGetValue(nCacheIndex, out nvi))
                                    {
                                        HarmonizeTwin(vi, nvi, mesh, nMesh, directionf);
                                    }
                                }
                                // CACHE Z
                                if (mesh.edgeVertexCacheZ.TryGetValue(cacheIndex, out vi))
                                {
                                    if (nMesh.edgeVertexCacheZ.TryGetValue(nCacheIndex, out nvi))
                                    {
                                        HarmonizeTwin(vi, nvi, mesh, nMesh, directionf);
                                    }
                                }

                            }
                        }
                    }
                }

            }
        }



        private void Build(ChunkData chunk, MeshData meshData)
        {

            // Get neighbours
            neighbourXP = chunk.GetNeighbour(1, 0, 0);
            neighbourYP = chunk.GetNeighbour(0, 1, 0);
            neighbourZP = chunk.GetNeighbour(0, 0, 1);
            neighbourXPZP = chunk.GetNeighbour(1, 0, 1);
            neighbourYPZP = chunk.GetNeighbour(0, 1, 1);
            neighbourXPYP = chunk.GetNeighbour(1, 1, 0);
            neighbourXPYPZP = chunk.GetNeighbour(1, 1, 1);

            gridPos = MarchingCubesTables.gridPos;


            // Iterate through X XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
            for (int x = 0; x < Chunk.SIZE_X; x++)
            {
                bool positiveBorderX = x == Chunk.SIZE_X - 1;
                bool negativeBorderX = x == 0;

                // Get X plans (x and x+1) handling neighbours
                BlockData[][] planX0 = chunk.Blocks[x];
                BlockData[][] planX1 = null;
                lineX0ZP = null;
                lineX1ZP = null;

                // lineX0ZP & lineXHZP
                if (neighbourZP != null)
                {

                    lineX0ZP = neighbourZP.Blocks[x];//neighbourZP.GetBlocksXLineInPlanZ0(x);
                }

                // planX1 & lineX1ZP
                if (!positiveBorderX)
                {
                    planX1 = chunk.Blocks[x + 1];
                    if (neighbourZP != null)
                        lineX1ZP = neighbourZP.Blocks[x];
                }
                else
                {
                    if (neighbourXP != null)
                        planX1 = neighbourXP.Blocks[0];
                    if (neighbourXPZP != null)
                        lineX1ZP = neighbourXPZP.Blocks[0];
                }


                // Iterate through Y YYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYY
                for (int y = 0; y < Chunk.SIZE_Y; y++)
                {
                    bool positiveBorderY = y == Chunk.SIZE_Y - 1;
                    bool negativeBorderY = y == 0;

                    // Get Y lines (x/y, x/y+1, x+1/y and x+1/y+1) handling neighbours
                    lineX0Y0 = null;
                    lineX0Y1 = null;
                    lineX1Y0 = null;
                    lineX1Y1 = null;

                    if (planX0 != null)
                        lineX0Y0 = planX0[y];
                    if (planX1 != null)
                        lineX1Y0 = planX1[y];

                    // X0					
                    if (!positiveBorderY && planX0 != null)
                    {
                        lineX0Y1 = planX0[y + 1];
                    }
                    else
                    {
                        if (neighbourYP != null)
                            lineX0Y1 = neighbourYP.Blocks[x][0];
                    }

                    if (!positiveBorderY && planX1 != null)
                    {
                        lineX1Y1 = planX1[y + 1];
                    }
                    else
                    {
                        if (positiveBorderX)
                        {
                            if (neighbourXPYP != null)
                                lineX1Y1 = neighbourXPYP.Blocks[0][0];
                        }
                        else
                        {
                            if (neighbourYP != null)
                                lineX1Y1 = neighbourYP.Blocks[x + 1][0];
                        }
                    }


                    // Iterate through Z ZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZ
                    for (int z = 0; z < Chunk.SIZE_Z; z++)
                    {
                        bool positiveBorderZ = z == Chunk.SIZE_Z - 1;
                        bool negativeBorderZ = z == 0;

                        // Get ready for new voxel
                        marchingCubes.Reset();
                        Vector3i localPos = new Vector3i(x, y, z);

                        // Polygonize
                        FastSetBlocksRegularCell(positiveBorderX, positiveBorderY, positiveBorderZ, x, y, z);
                        marchingCubes.Polygonize(chunk, localPos, meshData, gridPos, gridPos,
                                                    positiveBorderX, positiveBorderY, positiveBorderZ,
                                                    negativeBorderX, negativeBorderY, negativeBorderZ);

                    } // Z
                } // Y
            } // X

        }

        // Assign Marching Cubes blocks for polygonization of cell
        private void FastSetBlocksRegularCell(
                            bool positiveBorderX,
                            bool positiveBorderY,
                            bool positiveBorderZ,
                            int x, int y, int z)
        {
            marchingCubes.SetBlock(0, lineX0Y0 != null ? lineX0Y0[z] : null);
            marchingCubes.SetBlock(1, lineX1Y0 != null ? lineX1Y0[z] : null);
            marchingCubes.SetBlock(4, lineX0Y1 != null ? lineX0Y1[z] : null);
            marchingCubes.SetBlock(5, lineX1Y1 != null ? lineX1Y1[z] : null);
            if (!positiveBorderZ)
            {
                marchingCubes.SetBlock(3, lineX0Y0 != null ? lineX0Y0[z + 1] : null);
                marchingCubes.SetBlock(2, lineX1Y0 != null ? lineX1Y0[z + 1] : null);
                marchingCubes.SetBlock(7, lineX0Y1 != null ? lineX0Y1[z + 1] : null);
                marchingCubes.SetBlock(6, lineX1Y1 != null ? lineX1Y1[z + 1] : null);
            }
            else
            {

                marchingCubes.SetBlock(2, lineX1ZP != null ? lineX1ZP[y][0] : null);
                marchingCubes.SetBlock(3, lineX0ZP != null ? lineX0ZP[y][0] : null);

                if (!positiveBorderY)
                {
                    marchingCubes.SetBlock(6, lineX1ZP != null ? lineX1ZP[y + 1][0] : null);
                    marchingCubes.SetBlock(7, lineX0ZP != null ? lineX0ZP[y + 1][0] : null);
                }
                else
                {
                    if (neighbourYPZP != null)
                        marchingCubes.SetBlock(7, neighbourYPZP.Blocks[x][0][0]);

                    if (!positiveBorderX)
                    {
                        if (neighbourYPZP != null)
                            marchingCubes.SetBlock(6, neighbourYPZP.Blocks[x+1][0][0]);
                    }
                    else
                    {
                        if (neighbourXPYPZP != null)
                            marchingCubes.SetBlock(6, neighbourXPYPZP.Blocks[0][0][0]);
                    }
                }
            }
        }


        public void BuildChunkImediate(ChunkData chunkData)
        {
            var buildData = GetBuildData(chunkData.MemoryID) ?? new BuildData(chunkData.MemoryID);
            buildData.IsHarmonizable = false;
            buildData.BuildStarted = true;
            AddOrUpdateBuilData(buildData);


            MeshData meshData = new MeshData(subMeshCount);
            Build(chunkData, meshData);


            if (meshData.vertices.Count != 0)
            {
                ComputeNormals(meshData, chunkData);
                ComputeUVs(meshData, chunkData);
                SolveTangents(meshData);
                //if (chunkData.useRTP3)
                //  ComputeColorsAfterBuild(meshData, chunkData, chunkData.Position.y == 0, chunkData.Position.y < 0);

                buildData.TempMeshData = meshData;
                buildData.IsHarmonizable = true;

                HarmonizeTwinVertices(meshData, chunkData, false);
            }

            // Precompute arrays from lists and post-build
            meshData.PrepareArrays();
            buildData.TempMeshData = meshData;
        }

        public bool PostBuild(ChunkData chunkData)
        {
            var buildData = GetBuildData(chunkData.MemoryID) ?? new BuildData(chunkData.MemoryID);
            buildData.BuildStarted = false;
            AddOrUpdateBuilData(buildData);
            if (buildData.Built)
                return false;

            if (chunkData == null || buildData.MeshData == null)
                return false;

            buildData.Component.transform.localPosition = new Vector3(chunkData.Position.x * Chunk.SIZE_X_TOTAL, chunkData.Position.y * Chunk.SIZE_Y_TOTAL, chunkData.Position.z * Chunk.SIZE_Z_TOTAL);

            if (buildData.MeshData.vertices.Count != 0)
            {
                // Update mesh filter and mesh collider
                buildData.Component.MeshCollider.sharedMesh = null;
                Mesh filterMesh = buildData.MeshData.ToMesh(buildData.Component.Filter.sharedMesh);
                if (filterMesh != null)
                    filterMesh.bounds = Components.Chunk.ChunkBounds;

                buildData.Component.Filter.sharedMesh = filterMesh;
                buildData.Component.MeshCollider.sharedMesh = buildData.Component.Filter.sharedMesh;

                if (createVegetation)
                {
                    // Clear existing vegetation
                    ClearVegetation();

                    // Ask the grass generator to generate grass
                    GenerateGrass();
                }

                buildData.Component.MeshRenderer.enabled = true;
                buildData.Component.MeshCollider.enabled = true;
            }


            buildData.Built = true;
            return true;
        }

        public void AddOrUpdateBuilData(BuildData newBuildData)
        {
            var hashKey = newBuildData.ChunkMemoryID.GetHashCode();
            var buildDataList = ChunksBuildData.GetOrAdd(hashKey, new List<BuildData>(1));
            lock (buildDataList)
            {
                for (int i = 0; i < buildDataList.Count; i++)
                {
                    var buildData = buildDataList[i];
                    //caso encontre atualiza
                    if (buildData.ChunkMemoryID == newBuildData.ChunkMemoryID)
                    {
                        buildDataList[i] = newBuildData;
                        return;
                    }
                }
                //caso não encontre adiciona
                buildDataList.Add(newBuildData);
            }
        }

        public BuildData GetBuildData(long chunkMemoryID)
        {
            var hashKey = chunkMemoryID.GetHashCode();
            var buildDataList = ChunksBuildData.GetOrAdd(hashKey, new List<BuildData>(1));
            lock (buildDataList)
            {
                foreach (var buildData in buildDataList)
                {
                    if (buildData.ChunkMemoryID == chunkMemoryID)
                        return buildData;
                }
            }
            return null;
        }
    }
}
