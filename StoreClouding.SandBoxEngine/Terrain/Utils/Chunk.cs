//#define TERRAVOL_RTP3

using StoreClouding.SandBoxEngine.Terrain.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;


namespace StoreClouding.SandBoxEngine.Terrain.Utils
{
    public class Chunk 
    {

        internal const int SIZE_X_BITS = 3;
        internal const int SIZE_Y_BITS = 3;
        internal const int SIZE_Z_BITS = 3;
        // Number of blocks in a chunk
        public const int SIZE_X = 1 << SIZE_X_BITS;
        public const int SIZE_Y = 1 << SIZE_Y_BITS;
        public const int SIZE_Z = 1 << SIZE_Z_BITS;
        // Size of each block
        public static int SIZE_X_BLOCK = 3;
        public static int SIZE_Y_BLOCK = 3;
        public static int SIZE_Z_BLOCK = 3;
        public static float SIZE_AVERAGE_BLOCK = 3;
        // Total size (in Unity's units) of a chunk
        public static int SIZE_X_TOTAL = SIZE_X * SIZE_X_BLOCK;
        public static int SIZE_Y_TOTAL = SIZE_Y * SIZE_Y_BLOCK;
        public static int SIZE_Z_TOTAL = SIZE_Z * SIZE_Z_BLOCK;
        // Size of a block
        public static Vector3i SIZE_BLOCK = new Vector3i(SIZE_X_BLOCK, SIZE_Y_BLOCK, SIZE_Z_BLOCK);

        /*public static Bounds ChunkBounds = new Bounds(
            new Vector3(SIZE_X_TOTAL / 2, SIZE_Y_TOTAL / 2, SIZE_Z_TOTAL / 2),
            new Vector3(SIZE_X_TOTAL, SIZE_Y_TOTAL, SIZE_Z_TOTAL));*/


        /// <summary>
        /// Convert block position to chunk position.
        /// </summary>
        internal static Vector3i ToChunkPosition(int pointX, int pointY, int pointZ)
        {
            int chunkX = pointX >> SIZE_X_BITS;
            int chunkY = pointY >> SIZE_Y_BITS;
            int chunkZ = pointZ >> SIZE_Z_BITS;
            return new Vector3i(chunkX, chunkY, chunkZ);
        }

        /// <summary>
        /// Convert block position to chunk position.
        /// </summary>
        internal static Vector3i ToChunkPosition(Vector3i p)
        {
            int chunkX = p.x >> SIZE_X_BITS;
            int chunkY = p.y >> SIZE_Y_BITS;
            int chunkZ = p.z >> SIZE_Z_BITS;
            return new Vector3i(chunkX, chunkY, chunkZ);
        }

        /// <summary>
        /// Convert absolute block position to local block position (ie. position of block in its chunk).
        /// </summary>
        internal static Vector3i ToLocalPosition(int pointX, int pointY, int pointZ)
        {
            int localX = pointX & (SIZE_X - 1);
            int localY = pointY & (SIZE_Y - 1);
            int localZ = pointZ & (SIZE_Z - 1);
            return new Vector3i(localX, localY, localZ);
        }

        /// <summary>
        /// Convert absolute block position to local block position (ie. position of block in its chunk).
        /// </summary>
        internal static Vector3i ToLocalPosition(Vector3i p)
        {
            int localX = p.x & (SIZE_X - 1);
            int localY = p.y & (SIZE_Y - 1);
            int localZ = p.z & (SIZE_Z - 1);
            return new Vector3i(localX, localY, localZ);
        }

        /// <summary>
        /// Convert chunk + block position to world position in world units.
        /// </summary>
        internal static Vector3i ToWorldPosition(Vector3i chunkPosition, Vector3i localPosition)
        {
            int worldX = chunkPosition.x * SIZE_X_TOTAL + localPosition.x * SIZE_X_BLOCK;
            int worldY = chunkPosition.y * SIZE_Y_TOTAL + localPosition.y * SIZE_Y_BLOCK;
            int worldZ = chunkPosition.z * SIZE_Z_TOTAL + localPosition.z * SIZE_Z_BLOCK;
            return new Vector3i(worldX, worldY, worldZ);
        }

        /// <summary>
        /// Convert absolute block position to world position in world units.
        /// </summary>
        internal static Vector3 ToWorldPosition(Vector3i absoluteBlockPosition)
        {
            int worldX = absoluteBlockPosition.x * SIZE_X_BLOCK;
            int worldY = absoluteBlockPosition.y * SIZE_Y_BLOCK;
            int worldZ = absoluteBlockPosition.z * SIZE_Z_BLOCK;
            return new Vector3(worldX, worldY, worldZ);
        }

        /// <summary>
        /// Convert chunk + local position to world position in world units.
        /// </summary>
        internal static Vector3 ToWorldPosition(Vector3i chunkPosition, Vector3 localPosition)
        {
            float worldX = chunkPosition.x * SIZE_X_TOTAL + localPosition.x;
            float worldY = chunkPosition.y * SIZE_Y_TOTAL + localPosition.y;
            float worldZ = chunkPosition.z * SIZE_Z_TOTAL + localPosition.z;
            return new Vector3(worldX, worldY, worldZ);
        }

        /// <summary>
        /// Convert world position (in world units) to absolute block position by rounding value.
        /// </summary>
        public static Vector3i ToTerraVolPosition(Vector3 position)
        {
            int posX = Mathf.RoundToInt(position.x / Chunk.SIZE_X_BLOCK);
            int posY = Mathf.RoundToInt(position.y / Chunk.SIZE_Y_BLOCK);
            int posZ = Mathf.RoundToInt(position.z / Chunk.SIZE_Z_BLOCK);
            return new Vector3i(posX, posY, posZ);
        }

        /// <summary>
        /// Convert world position (in world units) to absolute block position.
        /// </summary>
        public static Vector3i ToTerraVolPositionFloor(Vector3 position)
        {
            int posX = Mathf.FloorToInt(position.x / Chunk.SIZE_X_BLOCK);
            int posY = Mathf.FloorToInt(position.y / Chunk.SIZE_Y_BLOCK);
            int posZ = Mathf.FloorToInt(position.z / Chunk.SIZE_Z_BLOCK);
            return new Vector3i(posX, posY, posZ);
        }


        internal static void SetBlockSize(int x, int y, int z)
        {
            SIZE_X_BLOCK = x;
            SIZE_Y_BLOCK = y;
            SIZE_Z_BLOCK = z;
            SIZE_AVERAGE_BLOCK = (float)(SIZE_X_BLOCK + SIZE_Y_BLOCK + SIZE_Z_BLOCK) / 3f;
            SIZE_X_TOTAL = SIZE_X * SIZE_X_BLOCK;
            SIZE_Y_TOTAL = SIZE_Y * SIZE_Y_BLOCK;
            SIZE_Z_TOTAL = SIZE_Z * SIZE_Z_BLOCK;
           /* ChunkBounds = new Bounds(
                new Vector3(SIZE_X_TOTAL / 2, SIZE_Y_TOTAL / 2, SIZE_Z_TOTAL / 2),
                new Vector3(SIZE_X_TOTAL, SIZE_Y_TOTAL, SIZE_Z_TOTAL));*/
        }
    }
}
