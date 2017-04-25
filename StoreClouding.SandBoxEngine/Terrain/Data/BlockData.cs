using StoreClouding.SandBoxEngine.Terrain.Map;
using StoreClouding.SandBoxEngine.Terrain.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;


namespace StoreClouding.SandBoxEngine.Terrain.Data
{
    /// <summary>
    /// This class contains all data of a block (ie. voxel).</summary>
    public class BlockData
    {
        public long ChunkMemoryID { get; private set; }
        private Block block;
        private float isovalue;
        private bool isInside;
        private bool isDestructible;
        private Vector3i position;
        private bool isPathBlocked;
        public bool IsDestroyed { get; set; }
        public long DataBaseID { get; set; }
        /// <summary>
        /// Create a new block.</summary>
        /// <param name="block"> Type of block of this block.</param>
        /// <param name="position"> Local position of the block in its chunk (in block's unit).</param>
        /// <param name="isovalue"> Isovalue of the block. Isovalue must be in range [-1;1]. 
        /// Negative isovalue means block is inside the terrain while positive isovalue means it is outside.</param>
        public BlockData(long ChunkMemoryID, long DataBaseID, Block block, Vector3i position, float isovalue, bool IsPathBlocked, bool IsDestroyed)
        {
            this.ChunkMemoryID = ChunkMemoryID;
            this.DataBaseID = DataBaseID;
            this.block = block;
            this.isovalue = isovalue;
            this.isInside = isovalue < 0.0f;
            this.isDestructible = block.IsDestructible;
            this.position = position;
            this.isPathBlocked = IsPathBlocked;
            this.IsDestroyed = IsDestroyed;
        }

        /// <summary>
        /// Isovalue of the block. Isovalue must be in range [-1;1]. 
        /// Negative isovalue means block is inside the terrain while positive isovalue means it is outside.</summary>
        internal float Isovalue
        {
            get
            {
                return isovalue;
            }
            set
            {
                isovalue = Mathf.Clamp(value, -1.0f, 1.0f);
                isInside = isovalue < 0.0f;
            }
        }

        /// <summary>
        /// Type of block of this block.</summary>
        internal Block Block
        {
            get
            {
                return block;
            }
            set
            {
                block = value;
                isDestructible = block.IsDestructible;
            }
        }

        /// <summary>
        /// Is path blocked on this block? Used by pathfinder.</summary>
        internal bool IsPathBlocked
        {
            get
            {
                return isPathBlocked;
            }
            set
            {
                isPathBlocked = value;
            }
        }

        /// <summary>
        /// Local position of the block in its chunk (in block's unit).</summary>
        internal Vector3i Position
        {
            get
            {
                return position;
            }
            set
            {
                position = value;
            }
        }

        /// <summary>
        /// Is this block inside or outside the terrain?</summary>
        internal bool IsInside
        {
            get
            {
                return isInside;
            }
        }

        /// <summary>
        /// Is this block destructible?</summary>
        internal bool IsDestructible
        {
            get
            {
                return isDestructible;
            }
        }

        /// <summary>
        /// Set isovalue of a block.</summary>
        /// <param name="blockData"> The block.</param>
        /// <param name="chunk"> Chunk of this block.</param>
        /// <param name="iso"> New isovalue of the block.</param>
        /// <param name="blockType"> Type of block.</param>
        /// <param name="isPathBlocked"> Should this block prevent pathfinder from passing here.</param>
        internal static void SetIsovalue(BlockData blockData, ChunkData chunk, float iso, Block blockType, bool isPathBlocked)
        {
            lock (blockData)
            {
                blockData.Block = blockType;
                blockData.Isovalue = iso;
                blockData.IsPathBlocked = isPathBlocked;
            }
        }

        /// <summary>
        /// Add isovalue to a block.</summary>
        /// <param name="blockData"> The block.</param>
        /// <param name="chunk"> Chunk of this block.</param>
        /// <param name="iso"> New isovalue of the block.</param>
        /// <param name="blockType"> Type of block.</param>
        /// <param name="isPathBlocked"> Should this block prevent pathfinder from passing here.</param>
        internal static void AddIsovalue(BlockData blockData, ChunkData chunk, float iso, Block blockType, bool isPathBlocked)
        {
            lock (blockData)
            {
                blockData.Block = blockType;
                blockData.Isovalue += iso;
                blockData.IsPathBlocked = isPathBlocked;
            }
        }

        public byte[] Serialize()
        {

            // 1 blockData = 9 bytes
            List<byte> byteList = new List<byte>(9);

            //1
            byteList.Add(Convert.ToByte(this.Block.ID));
            //4
            byteList.AddRange(BitConverter.GetBytes(this.Isovalue));
            //1
            byteList.Add(Convert.ToByte(this.Position.x));
            //1
            byteList.Add(Convert.ToByte(this.Position.y));
            //1
            byteList.Add(Convert.ToByte(this.Position.z));
            //1
            byteList.Add((byte)(this.IsPathBlocked ? 1 : 0));

            return byteList.ToArray();
        }
    }
	
}
