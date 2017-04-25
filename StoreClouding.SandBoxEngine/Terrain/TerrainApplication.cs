using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Map = StoreClouding.SandBoxEngine.Terrain.Map;
using Data = StoreClouding.SandBoxEngine.Terrain.Data;
using DAO = StoreClouding.SandBoxEngine.DAO;
using Utils = StoreClouding.SandBoxEngine.Terrain.Utils;
using System.Collections.Concurrent;
using System.Threading;
using StoreClouding.SandBoxEngine.Terrain.Utils;
using StoreClouding.SandBoxEngine.Terrain.Custom;
using System.Threading.Tasks;
using StoreClouding.SandBoxEngine.Communication;
using StoreClouding.SandBoxEngine.Communication.Terrain;

namespace StoreClouding.SandBoxEngine.Terrain
{
    public class TerrainApplication : IApplication
    {
        public Map.Map WorldMap { get; private set; }
        public Map.BlockSet BlockSet { get; private set; }
        private TerrainSocketMessageType TerrainCommunication = new TerrainSocketMessageType();

        public List<SocketMessageTypeBase> SocketMessageTypes { get; private set; }

        private long LastChunkMemoryID { get; set; }
        private bool AllInMemory { get; set; }
        private object LockChunkMemoryIDIncrement = new object();

        private ConcurrentDictionary<int, List<Data.ChunkData>> ChunksByMemoryID { get; set; }
        private ConcurrentDictionary<int, List<Data.ChunkData>> ChunksByDataBaseID { get; set; }
        private Queue<Data.ChunkData> ChunkInsertQueue { get; set; }
        private Queue<Data.BlockData> BlockInsertOrUpdateQueue { get; set; }

        private ConcurrentDictionary<int, List<Data.ChunkData>> PositionIndexedChunks { get; set; }

        public Utils.Vector3i MapLimits { get; internal set; }

        public TerrainApplication()
        {
            SocketMessageTypes = new List<SocketMessageTypeBase>();
            SocketMessageTypes.Add(TerrainCommunication);
        }
        public bool Start(out string error)
        {

            error = null;
            //Start instance and loading resources
            AllInMemory = true;// tudo em memoria
            BlockSet = new Map.BlockSet();
            LastChunkMemoryID = -1;
            WorldMap = new Map.Map();
            WorldMap.MapID = 1;
            WorldMap.MapCustomer = new WorldMapCustomer();
            WorldMap.MinSize = new Utils.Vector3i(6, 6, 6);
            //224 blocos altura max, 10x10 chunks de area
            WorldMap.MaxSize = new Utils.Vector3i(10, 224, 10);
            MapLimits = Utils.Vector3i.zero;

            Console.WriteLine("Loading BlockSet...");
            BlockSet = DAO.Terrain.Block.LoadBlockSet(out error);
            if (!String.IsNullOrWhiteSpace(error))
                return false;

            var first = BlockSet.Blocks.Values.FirstOrDefault();
            WorldMap.DefaultBlock = first;

            Console.WriteLine("Loading Chunks and Blocks...");
            long totalChunks = DAO.Terrain.ChunkData.DataBaseCount(out error);
            if (!String.IsNullOrWhiteSpace(error))
                return false;

            //define capacidade do cache em memoria
            int ChunkIndexCapacity;
            if (totalChunks > int.MaxValue)
                ChunkIndexCapacity = int.MaxValue;
            else
                ChunkIndexCapacity = (int)totalChunks;

            ChunksByMemoryID = new ConcurrentDictionary<int, List<Data.ChunkData>>(8, ChunkIndexCapacity);
            ChunksByDataBaseID = new ConcurrentDictionary<int, List<Data.ChunkData>>(16, ChunkIndexCapacity);
            PositionIndexedChunks = new ConcurrentDictionary<int, List<Data.ChunkData>>(8, ChunkIndexCapacity);

            //define capacidade da fila de insert e update de terrenos
            ChunkInsertQueue = new Queue<Data.ChunkData>(10000);
            BlockInsertOrUpdateQueue = new Queue<Data.BlockData>(100000);

            if (!DAO.Terrain.ChunkData.LoadAllToApplication(out error))
                return false;

            Console.WriteLine("Terrain loaded!");

            return true;
        }

        public void InsertOrUpdateBlock(Data.BlockData block)
        {
            lock (BlockInsertOrUpdateQueue)
            {
                BlockInsertOrUpdateQueue.Enqueue(block);
                TerrainCommunication.SendBlockUpdate(block);
            }
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

            int maxX = MapLimits.x;
            int maxZ = MapLimits.z;
            int maxY = MapLimits.y;

            if (MapLimits.x < chunk.Position.x)
                maxX = chunk.Position.x;

            if (MapLimits.y < chunk.Position.y)
                maxY = chunk.Position.y;

            if (MapLimits.z < chunk.Position.z)
                maxZ = chunk.Position.z;

            MapLimits = new Vector3i(maxX, maxY, maxZ);

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
            


            if (chunk.DataBaseID <= -1)
                lock (ChunkInsertQueue)
                {
                    ChunkInsertQueue.Enqueue(chunk);
                }
            else{
                //adiciona no index de base de dados caso ja exista o ID do banco
                var dataBaseHashKey = chunk.DataBaseID.GetHashCode();
                var dataBaseChunks = this.ChunksByDataBaseID.GetOrAdd(dataBaseHashKey,new List<Data.ChunkData>());
                lock (dataBaseChunks)
                {
                    dataBaseChunks.Add(chunk);
                }
                
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
        /// Busca na aplicação o chunk pelo index de banco
        /// </summary>
        /// <param name="dataBaseID">DataBaseID do chunk</param>
        /// <returns>Retorna chunk encontrado ou null caso não encontre</returns>
        public Data.ChunkData ChunkByDataBaseID(long dataBaseID)
        {
            List<Data.ChunkData> result;
            var hashKey = dataBaseID.GetHashCode();
            if (ChunksByDataBaseID.TryGetValue(hashKey, out result) && result != null)
            {

                lock (result)
                {
                    foreach (var item in result)
                    {
                        if (item.DataBaseID == dataBaseID)
                            return item;
                    }
                }
            }
            if (AllInMemory)
                return null;

            string error;
            //caso não encontre verifica se existe no banco fora do cache
            var databaseChunk = DAO.Terrain.ChunkData.Load(dataBaseID, out error);
           
            if (!string.IsNullOrWhiteSpace(error))
                throw new Exception(error);

            if (databaseChunk == null)
                return null;

            return databaseChunk;
        }

        /// <summary>
        /// Busca na aplicação o chunk indexado pela posição
        /// </summary>
        /// <param name="position">X,Y,Z do chunk</param>
        /// <returns>Caso exista retorna chunk encontrado caso contrário null</returns>
        public Data.ChunkData ChunkByPosition(Utils.Vector3i position)
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
            if (AllInMemory)
                return null;

            //caso não encontre busca no banco
            string error;
            //caso não encontre verifica se existe no banco fora do cache
            var databaseChunk = DAO.Terrain.ChunkData.Load(position, out error);

            if (!string.IsNullOrWhiteSpace(error))
                throw new Exception(error);

            if (databaseChunk == null)
                return null;

            return databaseChunk;
        }

        public bool Stop(out string error)
        {
            error = null;
            var stopThread = new System.Threading.Thread(() =>
            {
                string threadError = null;

                while (ChunkInsertQueue.Count > 0 || BlockInsertOrUpdateQueue.Count > 0)
                {
                    Update(out threadError);
                }
            });
            stopThread.Start();
            stopThread.Join();
            return true;
        }
        public bool UpdateBlocks(out string error)
        {
            
                
            error = null;

            int pendingBlocks;
            lock (BlockInsertOrUpdateQueue)
                pendingBlocks = BlockInsertOrUpdateQueue.Count;

            for (int i = 0; i < pendingBlocks; i++)
            {
                if (!DequeueBlock(out error))
                    return false;
            }
            return true;
        }
        public bool UpdateChunks(out string error)
        {
            error = null;
            int pendingChunks;
            lock (ChunkInsertQueue)
                pendingChunks = ChunkInsertQueue.Count;

            for (int i = 0; i < pendingChunks; i++)
                if (!DequeueChunk(out error))
                    return false;

            return true;
        }
        public bool Update(out string error)
        {

            error = null;
            if (!UpdateChunks(out error))
                return false;

            if(!UpdateBlocks(out error))
                return false;

            return true;
        }

        private bool DequeueChunk(out string error)
        {
            error = null;
            try
            {
                Data.ChunkData chunk = null;
                lock (ChunkInsertQueue)
                {
                    try
                    {
                        chunk = ChunkInsertQueue.Dequeue();
                    }
                    //empty queue
                    catch (InvalidOperationException) { }
                }
                if (chunk != null)
                {
                    lock (chunk)
                    {
                        if (!DAO.Terrain.ChunkData.Insert(chunk) && chunk.DataBaseID <= -1)
                        {
                            lock (ChunkInsertQueue)
                            {
                                //try again later
                                ChunkInsertQueue.Enqueue(chunk);
                            }
                        }
                        else
                        {
                            //atualiza index de base de dados adicionando o novo id
                            var dataBaseHashKey = chunk.DataBaseID.GetHashCode();
                            var dataBaseChunks = this.ChunksByDataBaseID.GetOrAdd(dataBaseHashKey, new List<Data.ChunkData>());
                            lock (dataBaseChunks)
                            {
                                dataBaseChunks.Add(chunk);
                            }
                        }
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                error = ex.ToString();
                return false;
            }
        }

        private bool DequeueBlock(out string error)
        {
            error = null;
            try
            {
                Data.BlockData block = null;
                lock (BlockInsertOrUpdateQueue)
                {
                    try
                    {
                        block = BlockInsertOrUpdateQueue.Dequeue();
                    }
                    //empty queue
                    catch (InvalidOperationException) { }
                }
                if (block != null)
                {
                    lock (block)
                    {
                        bool enqueueAgain;
                        Data.ChunkData chunk = ChunkByMemoryID(block.ChunkMemoryID);
                        bool chunkSaved;
                        lock (chunk)
                        {
                            chunkSaved = chunk.DataBaseID > -1;
                        }
                        //verifica se o chunk foi salvo no banco
                        if (chunkSaved)
                        {

                            if (block.DataBaseID <= -1)
                            {
                                enqueueAgain = (!DAO.Terrain.BlockData.Insert(block) && block.DataBaseID <= -1);
                            }
                            else
                            {
                                enqueueAgain = (!DAO.Terrain.BlockData.Update(block));
                            }
                        }
                        else// aguarda na fila ate que seu chunk esteja salvo no banco de dados
                            enqueueAgain = true;

                        if (enqueueAgain)
                        {
                            lock (BlockInsertOrUpdateQueue)
                            {
                                //try again later
                                BlockInsertOrUpdateQueue.Enqueue(block);
                            }
                        }
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                error = ex.ToString();
                return false;
            }
        }
    }
}
