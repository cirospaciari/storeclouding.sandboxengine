using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Utils = StoreClouding.SandBoxEngine.Terrain.Utils;
using Map = StoreClouding.SandBoxEngine.Terrain.Map;

namespace StoreClouding.SandBoxEngine.Terrain.Custom
{
    public interface IMapCustomer
    {
        Map.Block OnBlockGenerate(Map.Map map, Utils.Vector3i position, int deep);
    }
}
