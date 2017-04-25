using StoreClouding.SandBoxEngine.Terrain.Map;

using System.Collections.Generic;
using System.Linq;
using System.Text;
using Utils = StoreClouding.SandBoxEngine.Terrain.Utils;
using ProtoBuf;
using StoreClouding.SandBoxEngine.Terrain.Utils;
using StoreClouding.SandBoxEngine.Terrain.Data;
using UnityEngine;

namespace StoreClouding.SandBoxEngine.Terrain.Generator
{
    public class TerrainGenerator
    {
        public static readonly int GROUND_LEVEL = Utils.Chunk.SIZE_Y * 1;

        public static readonly int WATER_LEVEL = 0;

        public static readonly int DISTANCE_BORDER_REDUCE = 2 * Utils.Chunk.SIZE_X;

        private PerlinNoise2D noise1;

        private PerlinNoise2D noise2;

        private PerlinNoise3D noise3d;

        private float roughness;
        // Seed for perlin noise 2D
        [ProtoMember(3, IsRequired = true)]
        private ProtoVector2[] noise2dRandomValues;

        // Seed for perlin noise 3D
        [ProtoMember(4, IsRequired = true)]
        private ProtoVector2[] noise3dRandomValues;

        public float granularity = 30f;

        public bool randomize = true;

        public float HeightCoefOfCliffs = 40f;

        public float HeightCoefOfHills = 70f;

        public float MaxHeightOfCliffsAboveHills = 80f;

        public float MaxHeightOfHills = 220f;

        public float MinHeightOfGround = 0f;

        public bool useHeightmap;

        // Random
        System.Random random = new System.Random();

        TerrainApplication application;
        public TerrainGenerator(TerrainApplication app)
        {
            MaxHeightOfHills = app.WorldMap.MaxSize.y;

            roughness = Mathf.Clamp(roughness, 0f, 100f) / 100f;
            noise2dRandomValues = new ProtoVector2[2];
            noise3dRandomValues = new ProtoVector2[256];
            noise1 = new PerlinNoise2D(1 / (5f * granularity), randomize, noise2dRandomValues[0]).SetOctaves(5);
            noise2 = new PerlinNoise2D(1 / (5f * granularity), randomize, noise2dRandomValues[1]).SetOctaves(2);
            noise3d = new PerlinNoise3D(1 / granularity, randomize, noise3dRandomValues);
            noise2dRandomValues[0] = noise1.Offset;
            noise2dRandomValues[1] = noise2.Offset;
            noise3dRandomValues = noise3d.RandVals;

            application = app;
        }
        public void GenerateWorldMap()
        {
            MaxHeightOfHills = application.WorldMap.MaxSize.y;

            for (int x = 0; x < application.WorldMap.MaxSize.x; x++)
            {
                for (int z = 0; z < application.WorldMap.MaxSize.z; z++)
                {
                    GenerateWorldMapColumn(x, z);
                }
            }
        }

        private void GenerateWorldMapColumn(int cx, int cz)
        {
            float ground = WATER_LEVEL - 2;
            float underground = application.WorldMap.MinSize.y * Utils.Chunk.SIZE_Y;

            for (int z = 0; z < Utils.Chunk.SIZE_Z; z++)
            {
                for (int x = 0; x < Utils.Chunk.SIZE_X; x++)
                {
                    int worldX = cx * Utils.Chunk.SIZE_X + x;
                    int worldZ = cz * Utils.Chunk.SIZE_Z + z;

                    float h1 = noise1.Noise(worldX, worldZ) * HeightCoefOfHills;
                    h1 = Mathf.Clamp(Mathf.Abs(h1), MinHeightOfGround, MaxHeightOfHills);

                    float h2 = noise2.Noise(worldX, worldZ) * HeightCoefOfCliffs;
                    h2 = Mathf.Clamp(h2, 0f, MaxHeightOfCliffsAboveHills);
                    h2 += h1;

                    int deep = 0;
                    int worldY = application.MapLimits.y * Utils.Chunk.SIZE_Y;
                    int worldYStart = (int)h2 + Utils.Chunk.SIZE_Y * 4;

                    // Reset top blocks
                    if (worldY > worldYStart)
                    {
                        for (; worldY > worldYStart; worldY--)
                        {
                            NullifyWorldMapBlock(worldX, worldY, worldZ);
                        }
                    }
                    else
                    {
                        worldY = worldYStart;
                    }

                    // Collines
                    for (; worldY > h1; worldY--)
                    {
                        float isovalue = noise3d.Noise(worldX, worldY, worldZ);
                        if (worldY > h2)
                        {
                            isovalue += Mathf.Pow(worldY - h2, 1.2f) * 0.05f;
                        }

                        if (roughness != 0 && Rand(-1f, 1f) < 0)
                        {
                            isovalue += Rand(Mathf.Clamp(Mathf.Sin(x + z), -roughness, 0f), roughness / 4f);
                        }

                        if (isovalue < 0f)
                            deep++;
                        else
                            deep = 0;

                        if (isovalue < 1f)
                        {
                            isovalue = Mathf.Clamp(isovalue, -1.0f, 1.0f);
                            GenerateWorldMapBlock(worldX, worldY, worldZ, deep, isovalue);
                        }
                        else
                        {
                            NullifyWorldMapBlock(worldX, worldY, worldZ);
                        }
                    }

                    // Sol
                    for (; worldY >= ground; worldY--)
                    {
                        float isovalue = noise3d.Noise(worldX, worldY, worldZ);

                        isovalue -= Mathf.Pow(h1 - worldY, 1.5f) * 0.01f;

                        if (roughness != 0 && Rand(-1f, 1f) < 0)
                        {
                            isovalue += Rand(Mathf.Clamp(Mathf.Sin(x + z), -roughness, 0f), roughness / 4f);
                        }

                        if (isovalue < 0f)
                            deep++;
                        else
                            deep = 0;

                        if (isovalue < 1f)
                        {
                            isovalue = Mathf.Clamp(isovalue, -1.0f, 1.0f);
                            GenerateWorldMapBlock(worldX, worldY, worldZ, deep, isovalue);
                        }
                        else
                        {
                            NullifyWorldMapBlock(worldX, worldY, worldZ);
                        }
                    }

                    // Sous-sol
                    for (; worldY >= underground; worldY--)
                    {
                        deep++;
                        GenerateWorldMapBlock(worldX, worldY, worldZ, deep, -1.0f);
                    }
                }
            }
        }
        /// <summary>
        /// Nullify voxel data at given position.</summary>
        /// <param name="worldX"> X coordinate of absolute position of the block in block's unit.</param>
        /// <param name="worldY"> Y coordinate of absolute position of the block in block's unit.</param>
        /// <param name="worldZ"> Z coordinate of absolute position of the block in block's unit.</param>
        private void NullifyWorldMapBlock(int worldX, int worldY, int worldZ)
        {
            ChunkData chunk = application.ChunkByPosition(Utils.Chunk.ToChunkPosition(worldX, worldY, worldZ));
            if (chunk != null)
            {
                var p = Utils.Chunk.ToLocalPosition(worldX, worldY, worldZ);
                BlockData block = chunk.Blocks[p.x][p.y][p.z];
                if (block != null && !block.IsDestroyed)
                {
                    block.IsDestroyed = true;
                    application.InsertOrUpdateBlock(block);
                }

            }
        }
        /// <summary>
        /// Get type of block depending on its position and deepness.</summary>
        /// <param name="worldX"> X coordinate of absolute position of the block in block's unit.</param>
        /// <param name="worldY"> Y coordinate of absolute position of the block in block's unit.</param>
        /// <param name="worldZ"> Z coordinate of absolute position of the block in block's unit.</param>
        /// <param name="deep"> Deepness of the block (how deep in the ground it is). Can be useful to determine which type of block to use.</param>
        /// <returns> Type of block.</returns>
        private Block GetWorldMapBlock(int worldX, int worldY, int worldZ, int deep)
        {
            if (application.WorldMap.MapCustomer != null)
            {
                Block block = application.WorldMap.MapCustomer.OnBlockGenerate(application.WorldMap,new Vector3i(worldX, worldY, worldZ), deep);
                if (block != null)
                    return block;
            }
            return application.WorldMap.DefaultBlock;
        }

        /// <summary>
        /// Set voxel data at given position.</summary>
        /// <param name="worldX"> X coordinate of absolute position of the block in block's unit.</param>
        /// <param name="worldY"> Y coordinate of absolute position of the block in block's unit.</param>
        /// <param name="worldZ"> Z coordinate of absolute position of the block in block's unit.</param>
        /// <param name="deep"> Deepness of the block (how deep in the ground it is). Can be useful to determine which type of block to use.</param>
        /// <param name="isovalue"> Isovalue of the block.</param>
        private void GenerateWorldMapBlock(int worldX, int worldY, int worldZ, int deep, float isovalue)
        {
            Block block = GetWorldMapBlock(worldX, worldY, worldZ, deep);
            if (block != null)
            {
                //busca ou cria chunk caso não exista
                var chunkPosition = Utils.Chunk.ToChunkPosition(worldX, worldY, worldZ);
                ChunkData chunk = application.ChunkByPosition(chunkPosition);
                if (chunk == null)
                {
                    chunk = new ChunkData(application.WorldMap.MapID, -1, chunkPosition);
                    application.AddChunk(chunk);
                }

                //busca e atualiza bloco caso exista ou cria bloco caso não exista
                var p = Utils.Chunk.ToLocalPosition(worldX, worldY, worldZ);
                BlockData blockData = chunk.Blocks[p.x][p.y][p.z];
                if (blockData != null)
                {
                    blockData.Isovalue = isovalue;
                    blockData.Block = block;
                    blockData.IsDestroyed = false;
                }
                else
                {
                    blockData = new BlockData(chunk.MemoryID, -1, block, p, isovalue, false, false);
                }
                application.InsertOrUpdateBlock(blockData);
            }
        }

        private float Rand(float a, float b)
        {
            float r;
            lock (random)
                r = (float)random.NextDouble();
            return r * (b - a) - b;
        }
    }
}
