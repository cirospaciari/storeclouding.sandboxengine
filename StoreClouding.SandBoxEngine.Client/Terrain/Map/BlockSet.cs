using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace StoreClouding.SandBoxEngine.Client.Terrain.Map
{

    /// <summary>
    /// This is just a set of block types.</summary>
    public class BlockSet
    {

        public ConcurrentDictionary<int, Block> Blocks = new ConcurrentDictionary<int, Block>();

        public static BlockSet Deserialize(byte[] data)
        {
            var blockSet = new BlockSet();
            blockSet.Blocks = new ConcurrentDictionary<int, Block>();
            try
            {
                for (int i = 0; i < data.Length; i += 27)
                {
                    var block = new Block();
                    block.ID = data[i];
                    block.MaterialIndex = BitConverter.ToInt32(data,i+1);
                    block.Priority = BitConverter.ToInt32(data, i + 5);
                    block.IsDestructible = data[i + 9] == 1;
                    block.IsVegetationEnabled = data[i + 10] == 1;
                    float r, g, b, a;

                    a = BitConverter.ToSingle(data, i+11);
                    b = BitConverter.ToSingle(data, i+15);
                    g = BitConverter.ToSingle(data, i+19);
                    r = BitConverter.ToSingle(data, i+23);
                    block.VertexColor = new UnityEngine.Color(r, g, b, a);
                    blockSet.Blocks.TryAdd(block.ID, block);
                }
                return blockSet;
                
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}
