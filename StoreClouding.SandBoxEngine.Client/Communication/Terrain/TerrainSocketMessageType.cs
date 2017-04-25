using StoreClouding.SandBoxEngine.Client.Terrain.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Map = StoreClouding.SandBoxEngine.Client.Terrain.Map;
using Data = StoreClouding.SandBoxEngine.Client.Terrain.Data;
namespace StoreClouding.SandBoxEngine.Client.Communication.Terrain
{
    class TerrainSocketMessageType : SocketMessageTypeBase
    {
        private bool RequestEnded = true;
        private bool IsTerrainStartPoint = false;
        //ultimo envio para dar timeout caso o servidor não responda
        private DateTime LastSend = DateTime.MinValue;
        private bool WaitingResponse = false;
        private Queue<TerrainMessageResponse> SendQueue = new Queue<TerrainMessageResponse>();
        public override int MessageTypeID
        {
            get { return (int)Common.SocketMessageTypes.Terrain; }
        }

        public override void OnMessageReceived(byte[] content)
        {
            var message = new TerrainMessage();
            //caso seja uma mensagem inválida avisa o servidor
            if (!message.Deserialize(content))
            {
                SendFailedMessage();
                return;
            }

            switch (message.Type)
            {

                case TerrainMessageType.InvalidChunkPosition:
                    SendSucessMessage();
                    RequestEnded = true;
                    break;
                case TerrainMessageType.LoadBlockSet:
                    LoadBlockSet(message.Content);
                    break;
                case TerrainMessageType.LoadChunkByPosition:
                    LoadChunk(message.Content);
                    break;
                case TerrainMessageType.TerrainStartPoint:
                    WaitingResponse = false;
                    SendSucessMessage();
                    RequestEnded = true;
                    break;
                case TerrainMessageType.UpdateBlock:
                    UpdateBlock(message.Content);
                    break;
                case TerrainMessageType.SucessReceived:
                    break;
                case TerrainMessageType.FailedReceived:
                    break;
                default:
                    break;
            }
        }

        public void RequestBlockSet()
        {
            IsTerrainStartPoint = false;
            RequestEnded = false;
            SendOrEnqueue(TerrainMessageType.LoadBlockSet, Vector3i.zero);
        }

        public void RequestChunk(Vector3i position)
        {
            IsTerrainStartPoint = false;
            RequestEnded = false;
            SendOrEnqueue(TerrainMessageType.LoadChunkByPosition, position);
        }

        public void RequestStartPoint()
        {
            RequestEnded = false;
            IsTerrainStartPoint = true;
            SendOrEnqueue(TerrainMessageType.TerrainStartPoint, Vector3i.zero);
        }

        private void UpdateBlock(byte[] data)
        {
            try
            {
                int x, y, z;
                x = BitConverter.ToInt32(data, 0);
                y = BitConverter.ToInt32(data, 4);
                z = BitConverter.ToInt32(data, 8);
                bool isDestroyed = data[12] == 1;


                var chunkPosition = new Vector3i(x, y, z);
                var chunk = GameApplication.Current.Terrain.ChunkByPosition(chunkPosition);
                Data.BlockData block = Data.BlockData.Deserialize(data, 13);
                if (isDestroyed)
                {
                    chunk.Blocks[block.Position.x][block.Position.y][block.Position.z] = null;
                }
                else
                {
                    block.ChunkMemoryID = chunk.MemoryID;
                    chunk.Blocks[block.Position.x][block.Position.y][block.Position.z] = block;
                }
                
                SendSucessMessage();
            }
            catch (Exception)
            {
                SendFailedMessage();
            }
        }

        private void LoadBlockSet(byte[] data)
        {
            try
            {
                WaitingResponse = false;
                GameApplication.Current.Terrain.BlockSet = Map.BlockSet.Deserialize(data);
                if (GameApplication.Current.Terrain.BlockSet == null)
                {
                    SendFailedMessage();
                    return;
                }
                SendSucessMessage();
                RequestEnded = true;
            }
            catch (Exception)
            {
                SendFailedMessage();
            }
        }

        private void LoadChunk(byte[] data)
        {
            try
            {
                WaitingResponse = false;
                bool isPart2 = false;
                Data.ChunkData chunk = null;
                if (!Data.ChunkData.TryDeserialize(data,
                                                   ref chunk,
                                                   (position) =>
                                                   {
                                                       isPart2 = true;
                                                       return GameApplication.Current.Terrain.ChunkByPosition(position, false);
                                                   })
                    || chunk == null)
                {

                    SendFailedMessage();
                    return;
                }

                if (chunk.MemoryID == -1 && GameApplication.Current.Terrain.AddChunk(chunk) == -1)
                {
                    SendFailedMessage();
                    return;
                }
                SendSucessMessage();
                if (isPart2 && !IsTerrainStartPoint)
                    RequestEnded = true;
            }
            catch (Exception)
            {
                SendFailedMessage();
            }
        }

        private void SendFailedMessage()
        {
            SendOrEnqueue(TerrainMessageType.FailedReceived, Vector3i.zero);
        }

        private void SendSucessMessage()
        {
            SendOrEnqueue(TerrainMessageType.SucessReceived, Vector3i.zero);
        }

        private void SendOrEnqueue(TerrainMessageType type, Vector3i position)
        {
            SendOrEnqueue(new TerrainMessageResponse(type, position));
        }

        private void SendOrEnqueue(TerrainMessageResponse response)
        {
            lock (SendQueue)
            {
                SendQueue.Enqueue(response);
            }

            //caso não esteja esperando nada atualiza fila enviando o ultimo adicionado
            if (!WaitingResponse)
                UpdateQueue();
        }

        private void UpdateQueue()
        {
            TerrainMessageResponse nextResponse = null;

            lock (SendQueue)
            {

                try
                {
                    //verifica proximo item da fila
                    nextResponse = SendQueue.Dequeue();
                }
                catch (Exception) { }
            }

            //nada na fila para enviar
            if (nextResponse == null)
            {
                //marca que não esta aguardando resposta
                WaitingResponse = false;
                LastSend = DateTime.MinValue;
                return;
            }

            //envia e aguarda resposta com dados de chunk
            Send(nextResponse.Serialize());
            if (nextResponse.Type == TerrainMessageType.SucessReceived)
            {
                //caso seja mensagem de recebido com sucesso faz a fila de envio andar
                System.Threading.Thread.Sleep(15);//aguarda 15ms para não gerar envios rapidamente seguidos
                WaitingResponse = false;
                LastSend = DateTime.MinValue;
                UpdateQueue();
                return;
            }
            //marca que esta aguardando resposta
            WaitingResponse = true;
            LastSend = DateTime.Now;
        }

        public override bool Update(out string error)
        {
            try
            {
                //verifica timeout da ultima operação
                if (WaitingResponse && DateTime.Now.Subtract(LastSend).Seconds > 5)
                    SendFailedMessage();

                error = null;
                return true;
            }
            catch (Exception)
            {
                error = "Faulty communication with the server";
                return false;
            }
        }

        public void WaitRequest()
        {
            while (!RequestEnded)
            {
                System.Threading.Thread.Sleep(15);
            }
        }
    }
}
