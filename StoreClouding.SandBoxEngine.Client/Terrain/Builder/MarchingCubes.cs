using StoreClouding.SandBoxEngine.Client.Terrain.Data;
using StoreClouding.SandBoxEngine.Client.Terrain.Map;
using StoreClouding.SandBoxEngine.Client.Terrain.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace StoreClouding.SandBoxEngine.Client.Terrain.Builder
{
    internal class MarchingCubes
    {
        const float ISOLEVEL = 0f;

        private Vector3i[] gridPos = new Vector3i[8];
        private Vector3i[] gridCache = new Vector3i[8];
        private BlockData[] gridBlock = new BlockData[8];
        private int[] vertIndex = new int[12];
        private Color vColor = Color.clear;
        private int submesh = 0;
        private int priority = -1024;


        public void Reset()
        {
            for (int i = 0; i < 8; i++)
            {
                gridBlock[i] = null;
            }
        }

        private bool IsBlockInside(BlockData block)
        {
            return block != null && block.IsInside;
        }

        public void SetBlock(int i, BlockData block)
        {
            gridBlock[i] = block;
        }

        /*
           Given a grid cell and an calculate the triangular
           facets required to represent the isosurface through the cell.
           Return the number of triangular facets, the array "triangles"
           will be loaded up with the vertices at most 5 triangular facets.
            0 will be returned if the grid cell is either totally above
           of totally below the isolevel.
             4---------5
            /|        /|
           / |       / |
          7--0------6--1
          | /       | /
          |/        |/
          3---------2
		   
        */
        public void Polygonize(ChunkData chunk, Vector3i localPos, MeshData mesh, Vector3i[] gridPosBase, Vector3i[] gridCacheBase,
                                bool positiveBorderX, bool positiveBorderY, bool positiveBorderZ,
                                bool negativeBorderX, bool negativeBorderY, bool negativeBorderZ)
        {

            // Determine the index into the edge table which
            // tells us which vertices are inside of the surface
            int cubeindex = 0;
            if (IsBlockInside(gridBlock[0]))
                cubeindex |= 1;
            if (IsBlockInside(gridBlock[1]))
                cubeindex |= 2;
            if (IsBlockInside(gridBlock[2]))
                cubeindex |= 4;
            if (IsBlockInside(gridBlock[3]))
                cubeindex |= 8;
            if (IsBlockInside(gridBlock[4]))
                cubeindex |= 16;
            if (IsBlockInside(gridBlock[5]))
                cubeindex |= 32;
            if (IsBlockInside(gridBlock[6]))
                cubeindex |= 64;
            if (IsBlockInside(gridBlock[7]))
                cubeindex |= 128;

            // Cube is entirely in/out of the surface
            if (cubeindex == 0 || cubeindex == 255)
                return;

            // Liste des positions des blocks
            for (int i = 0; i < 8; i++)
            {
                gridPos[i] = gridPosBase[i] + localPos;
                gridCache[i] = gridCacheBase[i] + localPos;
            }

            // Determines submesh and vertex color, harmonizing it with neighbour
            submesh = 0;
            priority = -1024;
            ColorInterp(cubeindex,
                         positiveBorderX, positiveBorderY, positiveBorderZ,
                         negativeBorderX, negativeBorderY, negativeBorderZ);

            /* Find the vertices where the surface intersects the cube */
            ushort edge = MarchingCubesTables.edgeTable[cubeindex];
            int index = 0;
            int edgeVertexCacheIndex = 0;

            if ((edge & 1) != 0)
            {
                edgeVertexCacheIndex = gridCache[0].x + MeshData.CACHE_SIZE_X * (gridCache[0].y + MeshData.CACHE_SIZE_Y * gridCache[0].z);
                if (!mesh.edgeVertexCacheX.TryGetValue(edgeVertexCacheIndex, out index))
                {
                    index = mesh.vertices.Count;
                    mesh.vertices.Add(VertexInterp(gridPos[0], gridPos[1], gridBlock[0], gridBlock[1]));
                    mesh.colors.Add(vColor);
                    mesh.edgeVertexCacheX.Add(edgeVertexCacheIndex, index);
                }
                vertIndex[0] = index;
            }
            if ((edge & 2) != 0)
            {
                edgeVertexCacheIndex = gridCache[1].x + MeshData.CACHE_SIZE_X * (gridCache[1].y + MeshData.CACHE_SIZE_Y * gridCache[1].z);
                if (!mesh.edgeVertexCacheZ.TryGetValue(edgeVertexCacheIndex, out index))
                {
                    index = mesh.vertices.Count;
                    mesh.vertices.Add(VertexInterp(gridPos[1], gridPos[2], gridBlock[1], gridBlock[2]));
                    mesh.colors.Add(vColor);
                    mesh.edgeVertexCacheZ.Add(edgeVertexCacheIndex, index);
                }
                vertIndex[1] = index;
            }
            if ((edge & 4) != 0)
            {
                edgeVertexCacheIndex = gridCache[3].x + MeshData.CACHE_SIZE_X * (gridCache[3].y + MeshData.CACHE_SIZE_Y * gridCache[3].z);
                if (!mesh.edgeVertexCacheX.TryGetValue(edgeVertexCacheIndex, out index))
                {
                    index = mesh.vertices.Count;
                    mesh.vertices.Add(VertexInterp(gridPos[2], gridPos[3], gridBlock[2], gridBlock[3]));
                    mesh.colors.Add(vColor);
                    mesh.edgeVertexCacheX.Add(edgeVertexCacheIndex, index);
                }
                vertIndex[2] = index;
            }
            if ((edge & 8) != 0)
            {
                edgeVertexCacheIndex = gridCache[0].x + MeshData.CACHE_SIZE_X * (gridCache[0].y + MeshData.CACHE_SIZE_Y * gridCache[0].z);
                if (!mesh.edgeVertexCacheZ.TryGetValue(edgeVertexCacheIndex, out index))
                {
                    index = mesh.vertices.Count;
                    mesh.vertices.Add(VertexInterp(gridPos[3], gridPos[0], gridBlock[3], gridBlock[0]));
                    mesh.colors.Add(vColor);
                    mesh.edgeVertexCacheZ.Add(edgeVertexCacheIndex, index);
                }
                vertIndex[3] = index;
            }
            if ((edge & 16) != 0)
            {
                edgeVertexCacheIndex = gridCache[4].x + MeshData.CACHE_SIZE_X * (gridCache[4].y + MeshData.CACHE_SIZE_Y * gridCache[4].z);
                if (!mesh.edgeVertexCacheX.TryGetValue(edgeVertexCacheIndex, out index))
                {
                    index = mesh.vertices.Count;
                    mesh.vertices.Add(VertexInterp(gridPos[4], gridPos[5], gridBlock[4], gridBlock[5]));
                    mesh.colors.Add(vColor);
                    mesh.edgeVertexCacheX.Add(edgeVertexCacheIndex, index);
                }
                vertIndex[4] = index;
            }
            if ((edge & 32) != 0)
            {
                // No need cache test
                edgeVertexCacheIndex = gridCache[5].x + MeshData.CACHE_SIZE_X * (gridCache[5].y + MeshData.CACHE_SIZE_Y * gridCache[5].z);
                if (!mesh.edgeVertexCacheZ.TryGetValue(edgeVertexCacheIndex, out index))
                {
                    index = mesh.vertices.Count;
                    mesh.vertices.Add(VertexInterp(gridPos[5], gridPos[6], gridBlock[5], gridBlock[6]));
                    mesh.colors.Add(vColor);
                    mesh.edgeVertexCacheZ.Add(edgeVertexCacheIndex, index);
                }
                vertIndex[5] = index;
            }
            if ((edge & 64) != 0)
            {
                // No need cache test
                edgeVertexCacheIndex = gridCache[7].x + MeshData.CACHE_SIZE_X * (gridCache[7].y + MeshData.CACHE_SIZE_Y * gridCache[7].z);
                if (!mesh.edgeVertexCacheX.TryGetValue(edgeVertexCacheIndex, out index))
                {
                    index = mesh.vertices.Count;
                    mesh.vertices.Add(VertexInterp(gridPos[6], gridPos[7], gridBlock[6], gridBlock[7]));
                    mesh.colors.Add(vColor);
                    mesh.edgeVertexCacheX.Add(edgeVertexCacheIndex, index);
                }
                vertIndex[6] = index;
            }
            if ((edge & 128) != 0)
            {
                edgeVertexCacheIndex = gridCache[4].x + MeshData.CACHE_SIZE_X * (gridCache[4].y + MeshData.CACHE_SIZE_Y * gridCache[4].z);
                if (!mesh.edgeVertexCacheZ.TryGetValue(edgeVertexCacheIndex, out index))
                {
                    index = mesh.vertices.Count;
                    mesh.vertices.Add(VertexInterp(gridPos[7], gridPos[4], gridBlock[7], gridBlock[4]));
                    mesh.colors.Add(vColor);
                    mesh.edgeVertexCacheZ.Add(edgeVertexCacheIndex, index);
                }
                vertIndex[7] = index;
            }
            if ((edge & 256) != 0)
            {
                edgeVertexCacheIndex = gridCache[0].x + MeshData.CACHE_SIZE_X * (gridCache[0].y + MeshData.CACHE_SIZE_Y * gridCache[0].z);
                if (!mesh.edgeVertexCacheY.TryGetValue(edgeVertexCacheIndex, out index))
                {
                    index = mesh.vertices.Count;
                    mesh.vertices.Add(VertexInterp(gridPos[0], gridPos[4], gridBlock[0], gridBlock[4]));
                    mesh.colors.Add(vColor);
                    mesh.edgeVertexCacheY.Add(edgeVertexCacheIndex, index);
                }
                vertIndex[8] = index;
            }
            if ((edge & 512) != 0)
            {
                edgeVertexCacheIndex = gridCache[1].x + MeshData.CACHE_SIZE_X * (gridCache[1].y + MeshData.CACHE_SIZE_Y * gridCache[1].z);
                if (!mesh.edgeVertexCacheY.TryGetValue(edgeVertexCacheIndex, out index))
                {
                    index = mesh.vertices.Count;
                    mesh.vertices.Add(VertexInterp(gridPos[1], gridPos[5], gridBlock[1], gridBlock[5]));
                    mesh.colors.Add(vColor);
                    mesh.edgeVertexCacheY.Add(edgeVertexCacheIndex, index);
                }
                vertIndex[9] = index;
            }
            if ((edge & 1024) != 0)
            {
                // No need cache test
                edgeVertexCacheIndex = gridCache[2].x + MeshData.CACHE_SIZE_X * (gridCache[2].y + MeshData.CACHE_SIZE_Y * gridCache[2].z);
                if (!mesh.edgeVertexCacheY.TryGetValue(edgeVertexCacheIndex, out index))
                {
                    index = mesh.vertices.Count;
                    mesh.vertices.Add(VertexInterp(gridPos[2], gridPos[6], gridBlock[2], gridBlock[6]));
                    mesh.colors.Add(vColor);
                    mesh.edgeVertexCacheY.Add(edgeVertexCacheIndex, index);
                }
                vertIndex[10] = index;
            }
            if ((edge & 2048) != 0)
            {
                edgeVertexCacheIndex = gridCache[3].x + MeshData.CACHE_SIZE_X * (gridCache[3].y + MeshData.CACHE_SIZE_Y * gridCache[3].z);
                if (!mesh.edgeVertexCacheY.TryGetValue(edgeVertexCacheIndex, out index))
                {
                    index = mesh.vertices.Count;
                    mesh.vertices.Add(VertexInterp(gridPos[3], gridPos[7], gridBlock[3], gridBlock[7]));
                    mesh.colors.Add(vColor);
                    mesh.edgeVertexCacheY.Add(edgeVertexCacheIndex, index);
                }
                vertIndex[11] = index;
            }

            /* Create the triangle */
            int[] triSubtable = MarchingCubesTables.triTable[cubeindex];
            int triSubtableLength = triSubtable.Length;
            List<int> indices = mesh.GetIndices(submesh);
            for (int i = 0; i != triSubtableLength; i += 3)
            {
                indices.Add(vertIndex[triSubtable[i]]);
                indices.Add(vertIndex[triSubtable[i + 1]]);
                indices.Add(vertIndex[triSubtable[i + 2]]);
            }
        }


        //   Linearly interpolate the position where an isosurface cuts
        //   an edge between two vertices, each with their own scalar value
        private Vector3 VertexInterp(Vector3i p1i, Vector3i p2i, BlockData b1, BlockData b2)
        {
            Vector3 p1 = new Vector3(p1i.x * Chunk.SIZE_X_BLOCK, p1i.y * Chunk.SIZE_Y_BLOCK, p1i.z * Chunk.SIZE_Z_BLOCK);
            Vector3 p2 = new Vector3(p2i.x * Chunk.SIZE_X_BLOCK, p2i.y * Chunk.SIZE_Y_BLOCK, p2i.z * Chunk.SIZE_Z_BLOCK);

            float valp1 = b1 != null ? b1.Isovalue : 1f;
            float valp2 = b2 != null ? b2.Isovalue : 1f;

            if (Mathf.Approximately(ISOLEVEL, valp1))
                return p1;
            if (Mathf.Approximately(ISOLEVEL, valp2))
                return p2;
            if (Mathf.Approximately(valp1, valp2))
                return p1;

            //float mu = (ISOLEVEL - valp1) / (valp2 - valp1);
            float mu = valp1 / (valp1 - valp2);

            return new Vector3(p1.x + mu * (p2.x - p1.x), p1.y + mu * (p2.y - p1.y), p1.z + mu * (p2.z - p1.z));
        }

        // Find which color the vertex should have depending on blocks
        private void ColorInterp(int cubeindex,
            bool positiveBorderX, bool positiveBorderY, bool positiveBorderZ,
            bool negativeBorderX, bool negativeBorderY, bool negativeBorderZ)
        {
            for (int i = 0; i < 8; i++)
            {
                // check if block is inside
                if ((cubeindex & (1 << i)) != 0)
                {
                    Block block = gridBlock[i].Block;

                    if (block.Priority > priority)
                    {
                        priority = block.Priority;
                        submesh = block.MaterialIndex;
                        vColor = block.VertexColor;
                    }
                }
            }
        }


    }
}
