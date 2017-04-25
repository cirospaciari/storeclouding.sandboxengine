using StoreClouding.SandBoxEngine.Client.Communication;
using StoreClouding.SandBoxEngine.Client.Communication.Terrain;
using StoreClouding.SandBoxEngine.Client.Terrain.Builder;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace StoreClouding.SandBoxEngine.Client.Terrain
{
    public class TerrainApplication : IApplication
    {
        public TerrainBuilder Builder { get; private set; }
        public Map.Map WorldMap { get; private set; }
        public Map.BlockSet BlockSet { get; set; }
        private TerrainSocketMessageType TerrainCommunication = new TerrainSocketMessageType();
        
        public List<SocketMessageTypeBase> SocketMessageTypes {get; private set;}

        private long LastChunkMemoryID { get; set; }
        private object LockChunkMemoryIDIncrement = new object();

        private ConcurrentDictionary<int, List<Data.ChunkData>> ChunksByMemoryID { get; set; }
        private ConcurrentDictionary<int, List<Data.ChunkData>> PositionIndexedChunks { get; set; }

        public TerrainApplication()
        {
            SocketMessageTypes = new List<SocketMessageTypeBase>();
            SocketMessageTypes.Add(TerrainCommunication);
            Builder = new TerrainBuilder();
        }

        public bool Start(out string error)
        {
            
            error = null;
            LastChunkMemoryID = -1;
           
            WorldMap = new Map.Map();
            WorldMap.MapID = 1;
            
            ChunksByMemoryID = new ConcurrentDictionary<int, List<Data.ChunkData>>(8,100000);
            PositionIndexedChunks = new ConcurrentDictionary<int, List<Data.ChunkData>>(8, 100000);

            TerrainCommunication.RequestBlockSet();
            TerrainCommunication.WaitRequest();

            TerrainCommunication.RequestStartPoint();
            TerrainCommunication.WaitRequest();

            return true;
        }

        /// <summary>
        /// Adiciona novo chunk no cache de memoria
        /// </summary>
        /// <param name="chunk">Chunk a ser adiciona no cache de memoria</param>
        /// <returns>Retorna ID na memoria gerado para o chunk ou -1 caso não consiga adicionar o id de memoria do chunk é atualizado no objeto caso tenha sucesso</returns>
        public long AddChunk(Data.ChunkData chunk)
        {
            if (chunk == null)
                return -1;

            int positionHashKey = chunk.Position.GetHashCode();
            //busca ou cria index para o hash
            var positionIndexedChunks = this.PositionIndexedChunks.GetOrAdd(positionHashKey, new List<Data.ChunkData>());
            long MemoryID = -1;

            //incrementa ID
            lock (LockChunkMemoryIDIncrement)
            {
                LastChunkMemoryID++;
                MemoryID = LastChunkMemoryID;
            }

            var hashKey = MemoryID.GetHashCode();
            //Tenta adicionar na memoria e no hashIndex
            var chunks = this.ChunksByMemoryID.GetOrAdd(hashKey, new List<Data.ChunkData>());
            lock (chunks)
            {
                chunks.Add(chunk);
            }

            chunk.MemoryID = MemoryID;
            lock (positionIndexedChunks)
            {
                //adiciona no index
                positionIndexedChunks.Add(chunk);
            }

            return MemoryID;
        }
        /// <summary>
        /// Busca na aplicação o chunk pelo index de memoria
        /// </summary>
        /// <param name="memoryID">MemoryID do chunk</param>
        /// <returns>Retorna chunk encontrado ou null caso não encontre</returns>
        public Data.ChunkData ChunkByMemoryID(long memoryID)
        {
            List<Data.ChunkData> result;
            var hashKey = memoryID.GetHashCode();
            if (!ChunksByMemoryID.TryGetValue(hashKey, out result))
                return null;
            if (result == null)
                return null;

            lock (result)
            {
                foreach (var item in result)
                {
                    if (item.MemoryID == memoryID)
                        return item;
                }
            }
            return null;
        }

        /// <summary>
        /// Busca na aplicação o chunk indexado pela posição
        /// </summary>
        /// <param name="position">X,Y,Z do chunk</param>
        /// <returns>Caso exista retorna chunk encontrado caso contrário null</returns>
        public Data.ChunkData ChunkByPosition(Utils.Vector3i position, bool requestServerIfNotExists = true)
        {
            List<Data.ChunkData> chunks;
            int hashKey = position.GetHashCode();
            if (this.PositionIndexedChunks.TryGetValue(hashKey, out chunks) && chunks != null)
            {

                lock (chunks)
                {
                    foreach (var chunk in chunks)
                    {
                        if (chunk.Position.Equals(position))
                        {
                            return chunk;
                        }
                    }
                }
            }
            if (!requestServerIfNotExists)
                return null;

            TerrainCommunication.RequestChunk(position);
            TerrainCommunication.WaitRequest();
            //busca da memoria e não tenta pedir novamente caso não encontre é porque não existe
            return ChunkByPosition(position,false);
        }

        public bool Update(out string error)
        {
            error = null;
            return true;
        }

        public bool Stop(out string error)
        {
            error = null;
            return true;
        }
    }
}
