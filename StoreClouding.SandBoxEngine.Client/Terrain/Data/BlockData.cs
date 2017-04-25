﻿using StoreClouding.SandBoxEngine.Client.Terrain.Map;
using StoreClouding.SandBoxEngine.Client.Terrain.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace StoreClouding.SandBoxEngine.Client.Terrain.Data
{
     /// <summary>
    /// This class contains all data of a block (ie. voxel).</summary>
    public class BlockData
    {
        public long ChunkMemoryID { get; set; }
        private Block block;
        private float isovalue;
        private bool isInside;
        private bool isDestructible;
        private Vector3i position;
        private bool isPathBlocked;
        public bool IsDestroyed { get; set; }

        /// <summary>
        /// Create a new block.</summary>
        /// <param name="block"> Type of block of this block.</param>
        /// <param name="position"> Local position of the block in its chunk (in block's unit).</param>
        /// <param name="isovalue"> Isovalue of the block. Isovalue must be in range [-1;1]. 
        /// Negative isovalue means block is inside the terrain while positive isovalue means it is outside.</param>
        public BlockData(Block block, Vector3i position, float isovalue, bool IsPathBlocked, bool IsDestroyed)
        {
            //this.ChunkMemoryID = ChunkMemoryID;
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

        public static BlockData Deserialize(byte[] data, int startIndex)
        {
            int blockID = data[startIndex];
            Block block = GameApplication.Current.Terrain.BlockSet.Blocks[blockID];
            float isoValue = BitConverter.ToSingle(data, startIndex + 1);
            int x, y, z;
            x = data[5];
            y = data[6];
            z = data[7];
            var position = new Vector3i(x, y, z);
            var isPathBlocked = data[8] == 1;
            return new BlockData(block, position, isoValue, isPathBlocked, false);
        }
    }
}
