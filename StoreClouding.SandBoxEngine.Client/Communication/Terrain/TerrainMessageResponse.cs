using StoreClouding.SandBoxEngine.Client.Terrain.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace StoreClouding.SandBoxEngine.Client.Communication.Terrain
{
    class TerrainMessageResponse
    {
        public TerrainMessageType Type { get; private set; }

        public Vector3i ChunkPosition { get; private set; }

        public TerrainMessageResponse(TerrainMessageType type,Vector3i position)
        {
            this.Type = type;
            this.ChunkPosition = position;
        }
        public byte[] Serialize()
        {
            try
            {
                List<byte> data = new List<byte>(13);

                data.Add((byte)Type);
                data.AddRange(BitConverter.GetBytes(ChunkPosition.x));
                data.AddRange(BitConverter.GetBytes(ChunkPosition.y));
                data.AddRange(BitConverter.GetBytes(ChunkPosition.z));

                
                return data.ToArray();
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}
