using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using StoreClouding.SandBoxEngine.Terrain.Utils;
namespace StoreClouding.SandBoxEngine.Communication.Terrain
{
    class TerrainMessage
    {
        public TerrainMessageType Type { get; private set; }

        public Vector3i ChunkPosition { get; private set; }

        public bool Deserialize(byte[] content)
        {
            try
            {
                Type = (TerrainMessageType)content[0];

                int x = BitConverter.ToInt32(content, 1);
                int y = BitConverter.ToInt32(content, 5);
                int z = BitConverter.ToInt32(content, 9);
                ChunkPosition = new Vector3i(x, y, z);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}
