using StoreClouding.SandBoxEngine.Client.Terrain.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace StoreClouding.SandBoxEngine.Client.Terrain.Data
{
    /// <summary>
    /// Contains all data of a chunk (ie. voxels). It also provides some methods to find chunk's neighbours.</summary>
    public class ChunkData
    {
        public BlockData[][][] Blocks { get; private set; }
        public Vector3i Position { get; private set; }
        public int MapID { get; private set; }
        private long memoryID = -1;
        public long MemoryID
        {
            get
            {
                return memoryID;
            }
            set
            {
                memoryID = value;
                //passa o chunkMemoryID para os blockDatas 
                for (int x = 0; x < Chunk.SIZE_X; x++)
                {
                    for (int y = 0; y < Chunk.SIZE_Y; y++)
                    {
                        for (int z = 0; z < Chunk.SIZE_Z; z++)
                        {
                            var blockData = Blocks[x][y][z];
                            if (blockData != null)
                                blockData.ChunkMemoryID = memoryID;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Instancia Chunk
        /// </summary>
        /// <param name="MapID">ID do mapa</param>
        /// <param name="DataBaseID">ID na base de dados (-1 caso não exista)</param>
        /// <param name="position">Posicao tridimencional</param>
        public ChunkData(int MapID, Vector3i position)
        {
            this.MapID = MapID;
            this.Position = position;

            Blocks = new BlockData[Chunk.SIZE_X][][];
            for (int x = 0; x < Chunk.SIZE_X; x++)
            {
                Blocks[x] = new BlockData[Chunk.SIZE_Y][];
                for (int y = 0; y < Chunk.SIZE_Y; y++)
                {
                    Blocks[x][y] = new BlockData[Chunk.SIZE_Z];
                }
            }

        }

        public ChunkData GetNeighbour(int x, int y, int z)
        {
            return GetNeighbour(new Vector3i(x, y, z));
        }
        public ChunkData GetNeighbour(Vector3i dir)
        {
            return GameApplication.Current.Terrain.ChunkByPosition(Position + dir);
        }
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

        public static bool TryDeserialize(byte[] data, ref ChunkData chunk, Func<Vector3i, ChunkData> getFromMemory)
        {
            try
            {
                int MapID = BitConverter.ToInt32(data, 0);
                int X = BitConverter.ToInt32(data, 4);
                int Y = BitConverter.ToInt32(data, 8);
                int Z = BitConverter.ToInt32(data, 12);
                bool isPart1 = data[16] == 0;
                if (isPart1) // caso seja a primeira parte cria um novo objeto
                    chunk = new ChunkData(MapID, new Vector3i(X, Y, Z));
                else
                {
                    chunk = getFromMemory(new Vector3i(X, Y, Z));
                    //caso seja a segunda parte e seja null o objeto de erro
                    //TCP é passado de forma ordenada significa que o servidor esta enviando errado ou que o client apagou da memoria o chunk
                    if (chunk == null)
                        return false;
                }

                for (int i = 17; i < data.Length; i += 9)
                {
                    var blockData = BlockData.Deserialize(data, i);
                    if (blockData == null)
                        return false;

                    chunk.Blocks[blockData.Position.x][blockData.Position.y][blockData.Position.z] = blockData;
                }
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

    }

}
