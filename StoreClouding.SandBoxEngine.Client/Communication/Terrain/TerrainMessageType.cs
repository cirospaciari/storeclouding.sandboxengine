using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace StoreClouding.SandBoxEngine.Client.Communication.Terrain
{
    enum TerrainMessageType : byte
    {
        LoadBlockSet = 0, //pedido do cliente para atualizar/carregar o blockset
        TerrainStartPoint = 1, //pedido do cliente da area ao redor do player
        LoadChunkByPosition = 2, // pedido do cliente de um chunk em uma posição do mapa
        InvalidChunkPosition = 3, // resposta para cliente informando chunk null
        SucessReceived = 4, // resposta do cliente pedindo proximo chunk
        FailedReceived= 5, //Reenvia novamente a ultima resposta
        UpdateBlock = 6
    }
}
