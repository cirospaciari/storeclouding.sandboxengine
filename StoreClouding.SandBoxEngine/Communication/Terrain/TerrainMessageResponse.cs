using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace StoreClouding.SandBoxEngine.Communication.Terrain
{
    class TerrainMessageResponse
    {
        public int ConnectionID { get; private set; }
        public TerrainMessageType Type { get; private set; }
        public byte[] Content { get; private set; }
        public TerrainMessageResponse(int connectionID,TerrainMessageType type, byte[] content)
        {
            this.Type = type;
            this.Content = content;
            this.ConnectionID = connectionID;
        }

        public byte[] Serialize()
        {
            return TerrainMessageResponse.Serialize(this.Type, this.Content);
        }

        public static byte[] Serialize(TerrainMessageType type, byte[] content)
        {
            List<byte> message = new List<byte>(content.Length + 1);
            message.Add((byte)type);
            message.AddRange(content);
            return message.ToArray();
        }
    }
}
