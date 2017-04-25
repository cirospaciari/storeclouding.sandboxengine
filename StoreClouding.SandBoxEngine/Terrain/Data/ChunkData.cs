using StoreClouding.SandBoxEngine.Terrain.Map;
using StoreClouding.SandBoxEngine.Terrain.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace StoreClouding.SandBoxEngine.Terrain.Data
{
    /// <summary>
    /// Contains all data of a chunk (ie. voxels). It also provides some methods to find chunk's neighbours.</summary>
    public class ChunkData
    {
        public BlockData[][][] Blocks {get;private set;}
        public Vector3i Position {get; private set;}
        //public ChunkData[] Neighbours {get; private set;}
        public int MapID { get; private set; }
        public long DataBaseID { get; set; }
        public long MemoryID { get; set; }

        /// <summary>
        /// Instancia Chunk
        /// </summary>
        /// <param name="MapID">ID do mapa</param>
        /// <param name="DataBaseID">ID na base de dados (-1 caso não exista)</param>
        /// <param name="position">Posicao tridimencional</param>
        public ChunkData(int MapID,long DataBaseID, Vector3i position)
        {
            this.DataBaseID = DataBaseID;
            this.MapID = MapID;
            this.Position = position;
            //this.Neighbours = new ChunkData[3 * 3 * 3]; 

            Blocks = new BlockData[Chunk.SIZE_X][][];
            //blocksPlanZ0 = new BlockData[Chunk.SIZE_X][];
            //blocksLineZ0Y0 = new BlockData[Chunk.SIZE_X];
            for (int x = 0; x < Chunk.SIZE_X; x++)
            {
                Blocks[x] = new BlockData[Chunk.SIZE_Y][];
                //blocksPlanZ0[x] = new BlockData[Chunk.SIZE_Y];
                for (int y = 0; y < Chunk.SIZE_Y; y++)
                {
                    Blocks[x][y] = new BlockData[Chunk.SIZE_Z];
                }
            }

        }

        public byte[] Serialize(bool isPart1 = true)
        {
            List<byte> byteList = new List<byte>();

            //4
            byteList.AddRange(BitConverter.GetBytes(this.MapID));
            //4
            byteList.AddRange(BitConverter.GetBytes(this.Position.x));
            //4
            byteList.AddRange(BitConverter.GetBytes(this.Position.y));
            //4
            byteList.AddRange(BitConverter.GetBytes(this.Position.z));
            //1 (0 = Part1, 1 = Part2)
            byteList.Add((byte)(isPart1 ? 0 : 1));

            int start = isPart1 ? 0 : 4;
            int limit = start + 4;
            for (int x = start; x < limit; x++)
                for (int y = start; y < limit; y++)
                    for (int z = start; z < limit; z++)
                    {
                        var blockData = this.Blocks[x][y][z];
                        if(blockData == null || blockData.IsDestroyed)
                            continue;
                        byteList.AddRange(blockData.Serialize());
                    }

            return byteList.ToArray();
        }
        /// Find all neighbours of this chunk and store them in an array.</summary>
        public ChunkData[] FindNeighbours()
        {
            ChunkData[] neighbours = new ChunkData[3 * 3 * 3];
            // Find neighbours
            foreach (Vector3i dir in Vector3i.allDirections)
            {
                ChunkData chunk = GameApplication.Current.Terrain.ChunkByPosition(Position + dir);
                neighbours[(dir.x + 1) + (dir.y + 1) * 3 + (dir.z + 1) * 9] = chunk;
            }
            return neighbours;
        }
        /*/// <summary>
        

        /// <summary>
        /// Clear neighbours array.</summary>
        public void ClearNeighbours()
        {
            // Find neighbours
            for (int x = -1; x <= 1; x++)
            {
                for (int y = -1; y <= 1; y++)
                {
                    for (int z = -1; z <= 1; z++)
                    {
                        ChunkData neighbour = GetNeighbour(x, y, z);
                        if (neighbour != null)
                        {
                            neighbour.UnsetNeighbour(-x, -y, -z);
                            this.UnsetNeighbour(x, y, z);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Remove a neighbour from the chunk at the given direction.</summary>
        public void UnsetNeighbour(int x, int y, int z)
        {
            neighbours[(x + 1) + (y + 1) * 3 + (z + 1) * 9] = null;
        }

        /// <summary>
        /// Returns the neighbour of this chunk at the given direction.</summary>
        public ChunkData GetNeighbour(int x, int y, int z)
        {
            return neighbours[(x + 1) + (y + 1) * 3 + (z + 1) * 9];
        }

        /// <summary>
        /// Returns the neighbour of this chunk at the given direction.</summary>
        public ChunkData GetNeighbour(Vector3i dir)
        {
            return neighbours[(dir.x + 1) + (dir.y + 1) * 3 + (dir.z + 1) * 9];
        }

        /// <summary>
        /// Returns the neighbour of this chunk at the given direction. Create it if it doesn't exist.</summary>
        public ChunkData GetNeighbourInstance(Vector3i dir)
        {
            ChunkData n = neighbours[(dir.x + 1) + (dir.y + 1) * 3 + (dir.z + 1) * 9];
            if (n == null)
            {
                n = map.GetChunkDataInstance(this.position + dir);
            }
            return n;
        }

        public override bool Equals(object other)
        {
            if (!(other is ChunkData))
                return false;
            ChunkData c = (ChunkData)other;
            return id == c.id;
        }

        public override int GetHashCode()
        {
            return id;
        }

        /// <summary>
        /// Get the chunk component. Create it if needed.</summary>
        public Chunk GetChunkInstance()
        {
            if (chunk == null)
                chunk = Chunk.CreateChunk(position, map, this);
            return chunk;
        }

        /// <summary>
        /// Replace block at the given position.</summary>
        /// <param name="block"> The block to put at the given position.</param>
        /// <param name="pos"> Local position of the block in block's unit.</param>
        public void SetBlock(float isovalue, Block block, Vector3i pos)
        {
            this.SetBlock(isovalue, block, pos.x, pos.y, pos.z);
        }

        /// <summary>
        /// Replace block at the given position.</summary>
        /// <param name="block"> The block to put at the given position.</param>
        /// <param name="x"> X coordinate of local position of the block in block's unit.</param>
        /// <param name="y"> Y coordinate of local position of the block in block's unit.</param>
        /// <param name="z"> Z coordinate of local position of the block in block's unit.</param>
        public void SetBlock(float isovalue, Block block, int x, int y, int z)
        {
            BlockData blockData = blocks[x][y][z];
            if (blockData != null)
            {
                blockData.Isovalue = isovalue;
                blockData.Block = block;
            }
            else
            {
                //blockData = BlockPool.Get(block, new Vector3i(x, y, z), isovalue);
                blocks[x][y][z] = blockData;
            }
        }

        /// <summary>
        /// Nullify block at the given position.</summary>
        /// <param name="x"> X coordinate of local position of the block in block's unit.</param>
        /// <param name="y"> Y coordinate of local position of the block in block's unit.</param>
        /// <param name="z"> Z coordinate of local position of the block in block's unit.</param>
        public void NullifyBlock(Vector3i p)
        {
            BlockData block = blocks[p.x][p.y][p.z];
            if (block != null)
            {
                //BlockPool.Free(block);
                blocks[p.x][p.y][p.z] = null;
            }
        }

        /// <summary>
        /// Clears all blocks of this chunk.</summary>
        public void ClearBlocks()
        {
            for (int x = 0; x < Chunk.SIZE_X; ++x)
            {
                for (int y = 0; y < Chunk.SIZE_Y; ++y)
                {
                    for (int z = 0; z < Chunk.SIZE_Z; ++z)
                    {
                        NullifyBlock(new Vector3i(x, y, z));
                    }
                }
            }
        }

        /// <summary>
        /// Returns block at the given position.</summary>
        /// <param name="pos"> Local position of the block in block's unit.</param>
        public BlockData GetBlock(Vector3i pos)
        {
            return blocks[pos.x][pos.y][pos.z];
        }

        /// <summary>
        /// Returns block at the given position.</summary>
        /// <param name="x"> X coordinate of local position of the block in block's unit.</param>
        /// <param name="y"> Y coordinate of local position of the block in block's unit.</param>
        /// <param name="z"> Z coordinate of local position of the block in block's unit.</param>
        public BlockData GetBlock(int x, int y, int z)
        {
            return blocks[x][y][z];
        }

        public BlockData[][] GetBlocksXPlan(int x)
        {
            return blocks[x];
        }
        public BlockData[] GetBlocksXYLine(int x, int y)
        {
            return blocks[x][y];
        }

        /// <summary>
        /// Returns block at the given position, safely handling blocks which are actually in a neighbour of this chunk.</summary>
        /// <param name="localPosition"> Local position of the block in block's unit. Can be lower than 0 or greater than Chunk.SIZE_X/Y/Z</param>
        internal BlockData GetBlockSafeNeighbours(Vector3i localPosition)
        {
            ChunkData chunk = this;
            Vector3i dir = Vector3i.zero;
            if (localPosition.x < 0) dir.x = -1;
            else if (localPosition.x >= Chunk.SIZE_X) dir.x = 1;
            if (localPosition.y < 0) dir.y = -1;
            else if (localPosition.y >= Chunk.SIZE_Y) dir.y = 1;
            if (localPosition.z < 0) dir.z = -1;
            else if (localPosition.z >= Chunk.SIZE_Z) dir.z = 1;
            if (dir != Vector3i.zero)
            {
                chunk = GetNeighbour(dir);
                localPosition = Chunk.ToLocalPosition(localPosition);
            }
            if (chunk != null)
            {
                return chunk.GetBlock(localPosition);
            }
            return null;
        }

        /// <summary>
        /// A* Checks if a position is free or marked (and legal). This method can search in neighbours if needed.</summary>
        /// <param name="localPosition"> Local position of the block in block's unit. Can be lower than 0 or greater than Chunk.SIZE_X/Y/Z</param>
        /// <param name="aboveGroundOnly"> Should the path be above the ground or can it be in the air?</param>
        internal bool PositionIsFree(Vector3i localPosition, bool aboveGroundOnly)
        {
            BlockData block = GetBlockSafeNeighbours(localPosition);

            if (block != null)
            {
                if (!aboveGroundOnly)
                {

                    return block != null && !block.IsInside && !block.IsPathBlocked;

                }
                else if (block != null && !block.IsInside && !block.IsPathBlocked)
                {

                    for (int y = -1; y >= -1; y--)
                    {
                        Vector3i localPositionb = localPosition;
                        localPositionb.y += y;
                        BlockData blockb = GetBlockSafeNeighbours(localPositionb);
                        if (blockb != null && blockb.IsInside)
                            return true;
                    }

                }
            }
            return false;
        }*/
    }
	
}
