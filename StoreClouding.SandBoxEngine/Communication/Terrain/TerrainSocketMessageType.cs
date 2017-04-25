using StoreClouding.SandBoxEngine.Terrain.Data;
using StoreClouding.SandBoxEngine.Terrain.Utils;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace StoreClouding.SandBoxEngine.Communication.Terrain
{
    class TerrainSocketMessageType : SocketMessageTypeBase
    {

        private ConcurrentDictionary<int, bool> WaitingResponse = new ConcurrentDictionary<int, bool>();
        private ConcurrentDictionary<int, Queue<TerrainMessageResponse>> SendQueue = new ConcurrentDictionary<int, Queue<TerrainMessageResponse>>();

        //Chunks enviados para um determinado usuário durando a sessão dele será usado para atualizar os chunks enviados
        //e tambem evitar envio duplicado
        private ConcurrentDictionary<int, ConcurrentDictionary<int, List<ChunkData>>> SendedChunks = new ConcurrentDictionary<int, ConcurrentDictionary<int, List<ChunkData>>>(8, 5000);
        
        public override int MessageTypeID
        {
            get { return (int)Common.SocketMessageTypes.Terrain; }
        }

        public override void OnMessageReceived(int connectionID, byte[] content)
        {
            var message = new TerrainMessage();
            //caso seja uma mensagem inválida desconecta o cliente
            if (!message.Deserialize(content))
            {
                Kick(connectionID);
                return;
            }

            switch (message.Type)
            {
                case TerrainMessageType.LoadBlockSet:
                    SendBlockSet(connectionID);
                    break;
                case TerrainMessageType.TerrainStartPoint:
                    SendTerrainStartPoint(connectionID);
                    break;
                case TerrainMessageType.LoadChunkByPosition:
                    SendChunk(connectionID, message.ChunkPosition);
                    break;
                case TerrainMessageType.SucessReceived:
                    UpdateQueue(connectionID, true);
                    break;
                case TerrainMessageType.FailedReceived:
                    UpdateQueue(connectionID, false);
                    break;
                default:
                    break;
            }
        }

        public override bool Update(out string error)
        {
            //não implementado talvez em um futuro ou em outra action
            error = null;
            return true;
        }

        public void SendBlockUpdate(BlockData block)
        {
            lock (block)
            {
                List<byte> data = new List<byte>();
                var chunk = GameApplication.Current.Terrain.ChunkByMemoryID(block.ChunkMemoryID);
                data.AddRange(BitConverter.GetBytes(chunk.Position.x));
                data.AddRange(BitConverter.GetBytes(chunk.Position.y));
                data.AddRange(BitConverter.GetBytes(chunk.Position.z));

                data.Add((byte)(block.IsDestroyed ? 1 : 0));

                data.AddRange(block.Serialize());

                //envia update para todos os usuário que tiverem aquele chunk carregado
                foreach (var connectionID in SendedChunks.Keys)
                {
                    if (GetFromSendedChunkList(connectionID, chunk.MemoryID) != null)
                        SendOrEnqueue(connectionID, TerrainMessageType.UpdateBlock, data.ToArray());
                }
            }
        }

        private ChunkData GetFromSendedChunkList(int connectionID, long chunkMemoryID)
        {
            var hashKey = chunkMemoryID.GetHashCode();
            var connectionChunks = SendedChunks.GetOrAdd(connectionID, new ConcurrentDictionary<int, List<ChunkData>>(8, 8000));

            var chunkList = connectionChunks.GetOrAdd(hashKey, new List<ChunkData>(1));
            lock (chunkList)
            {
                foreach (var chunk in chunkList)
                {
                    if (chunk.MemoryID == chunkMemoryID)
                        return chunk;
                }
            }
            return null;
        }

        private void AddToSendedChunkList(int connectionID, ChunkData chunk)
        {
            if (chunk == null)
                return;
            var connectionChunks = SendedChunks.GetOrAdd(connectionID, new ConcurrentDictionary<int, List<ChunkData>>(8, 8000));
            var hashKey = chunk.MemoryID.GetHashCode();
            var chunkList = connectionChunks.GetOrAdd(hashKey, new List<ChunkData>(1));
            lock (chunkList)
            {
                chunkList.Add(chunk);
            }
        }

        private void SendTerrainStartPoint(int connectionID)
        {
            //aqui ficará a verificação de qual usuário é a sessão por agora para teste player = 1 
            //int player = 1;
            int ChunkID = 1;//chunk em que o player se encontra
            var chunk = GameApplication.Current.Terrain.ChunkByDataBaseID(ChunkID);
            var part01 = chunk.Serialize(true);
            var part02 = chunk.Serialize(false);
            SendOrEnqueue(connectionID, TerrainMessageType.LoadChunkByPosition, part01);
            SendOrEnqueue(connectionID, TerrainMessageType.LoadChunkByPosition, part02);
            AddToSendedChunkList(connectionID, chunk);

            //envia vizinhos de 2 niveis (campo de visão do usuário)
            SendNeightbours(connectionID, chunk, 1);

            SendOrEnqueue(connectionID, TerrainMessageType.TerrainStartPoint, Common.SocketDefaultMessages.TerrainStartPointEnded);
        }

        private void SendNeightbours(int connectionID, SandBoxEngine.Terrain.Data.ChunkData chunk, int repercusionLevel = 0)
        {
            //encontra chunks vizinhos do chunk em que o jogador se encontra
            var neighbours = chunk.FindNeighbours();
            //envia chunks vizinhos 
            foreach (var neighbour in neighbours)
            {
                //verifica se existe ou se ja foi enviado
                if (neighbour == null || GetFromSendedChunkList(connectionID, neighbour.MemoryID) != null)
                    continue;
                //serializa vizinho
                var neighbourPart01 = neighbour.Serialize(true);
                var neighbourPart02 = neighbour.Serialize(false);
                //adiciona a lista de envio
                SendOrEnqueue(connectionID, TerrainMessageType.LoadChunkByPosition, neighbourPart01);
                SendOrEnqueue(connectionID, TerrainMessageType.LoadChunkByPosition, neighbourPart02);
                //marca como enviado
                AddToSendedChunkList(connectionID, neighbour);
                //verifica se ainda é para enviar os vizinhos dos vizinhos
                if (repercusionLevel > 0)
                    SendNeightbours(connectionID, chunk, repercusionLevel - 1);
            }
        }

        private void SendBlockSet(int connectionID)
        {
            byte[] content = GameApplication.Current.Terrain.BlockSet.Serialize();

            SendOrEnqueue(connectionID, TerrainMessageType.LoadBlockSet, content);
        }

        private void SendChunk(int connectionID, Vector3i position)
        {

            var chunk = GameApplication.Current.Terrain.ChunkByPosition(position);
            if (chunk == null)
            {
                SendOrEnqueue(connectionID, TerrainMessageType.InvalidChunkPosition, position.Serialize());
                return;
            }

            var part01 = chunk.Serialize(true);
            var part02 = chunk.Serialize(false);
            SendOrEnqueue(connectionID, TerrainMessageType.LoadChunkByPosition, part01);
            SendOrEnqueue(connectionID, TerrainMessageType.LoadChunkByPosition, part02);
        }

        private void UpdateQueue(int connectionID, bool forwardQueue)
        {

            var connectionResponses = SendQueue.GetOrAdd(connectionID, new Queue<TerrainMessageResponse>());
            TerrainMessageResponse nextResponse = null;

            lock (connectionResponses)
            {
                if (forwardQueue)//passa para o proximo item elimando o mais antigo
                    connectionResponses.Dequeue();

                try
                {
                    //verifica proximo item da fila
                    nextResponse = connectionResponses.Peek();
                }
                catch (Exception) { }
            }

            //nada na fila para enviar
            if (nextResponse == null)
            {
                //marca que não esta aguardando resposta
                WaitingResponse.AddOrUpdate(connectionID, false, (id, actual) =>
                {
                    return false;
                });
                return;
            }
            //envia e aguarda resposta de chunk/block carregado
            Send(nextResponse.ConnectionID, nextResponse.Serialize());
            //marca que esta aguardando resposta
            WaitingResponse.AddOrUpdate(connectionID, true, (id, actual) =>
            {
                return true;
            });
        }

        private void SendOrEnqueue(int connectionID, TerrainMessageType type, byte[] content)
        {
            SendOrEnqueue(new TerrainMessageResponse(connectionID, type, content));
        }

        private void SendOrEnqueue(TerrainMessageResponse response)
        {
            var connectionResponses = SendQueue.GetOrAdd(response.ConnectionID, new Queue<TerrainMessageResponse>());
            lock (connectionResponses)
            {
                connectionResponses.Enqueue(response);
            }
            //caso não esteja esperando nada atualiza fila enviando o ultimo adicionado
            bool waitingResponse = WaitingResponse.GetOrAdd(response.ConnectionID, false);
            if (!waitingResponse)
                UpdateQueue(response.ConnectionID, false);
        }

    }
}
