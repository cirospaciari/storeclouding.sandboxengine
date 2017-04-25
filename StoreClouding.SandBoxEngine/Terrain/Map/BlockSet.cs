using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;


namespace StoreClouding.SandBoxEngine.Terrain.Map
{
    /// <summary>
    /// This is just a set of block types.</summary>
    public class BlockSet
    {

        
        public ConcurrentDictionary<int, Block> Blocks = new ConcurrentDictionary<int, Block>();

        public byte[] Serialize()
        {
            List<byte> byteList = new List<byte>(7000);

            foreach (var block in this.Blocks)
            {
                if (block.Value == null)
                    continue;
                //27 bytes por block
                byteList.Add(Convert.ToByte(block.Value.ID));

                byteList.AddRange(BitConverter.GetBytes(block.Value.MaterialIndex));

                byteList.AddRange(BitConverter.GetBytes(block.Value.Priority));

                byteList.Add((byte)(block.Value.IsDestructible ? 1 : 0));
                byteList.Add((byte)(block.Value.IsVegetationEnabled ? 1 : 0));

                byteList.AddRange(BitConverter.GetBytes(block.Value.VertexColor.A));
                byteList.AddRange(BitConverter.GetBytes(block.Value.VertexColor.B));
                byteList.AddRange(BitConverter.GetBytes(block.Value.VertexColor.G));
                byteList.AddRange(BitConverter.GetBytes(block.Value.VertexColor.R));
            }

            return byteList.ToArray();
        }
    }

}
