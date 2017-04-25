using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace StoreClouding.SandBoxEngine.Terrain.Custom
{
    public class WorldMapCustomer : IMapCustomer
    {
        public Map.Block OnBlockGenerate(Map.Map map, Utils.Vector3i position, int deep)
        {
            Map.Block sandBlock;
            if (GameApplication.Current.Terrain.BlockSet.Blocks.TryGetValue(2, out sandBlock) && sandBlock != null && position.y < -2)
                return sandBlock;
            return map.DefaultBlock;
        }
    }
}
