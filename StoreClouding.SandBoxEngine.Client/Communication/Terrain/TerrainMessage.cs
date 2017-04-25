using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace StoreClouding.SandBoxEngine.Client.Communication.Terrain
{
    class TerrainMessage
    {
        public TerrainMessageType Type { get; private set; }
        public byte[] Content { get; private set; }

        public bool Deserialize(byte[] content)
        {
            try
            {
                Type = (TerrainMessageType)content[0];

                Content = content.Skip(1).ToArray();
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}
