using StoreClouding.SandBoxEngine.Terrain.Custom;
using StoreClouding.SandBoxEngine.Terrain.Data;
using StoreClouding.SandBoxEngine.Terrain.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Utils = StoreClouding.SandBoxEngine.Terrain.Utils;

namespace StoreClouding.SandBoxEngine.Terrain.Map
{
    public class Map
    {
        public int MapID { get; set; }
        public Block DefaultBlock { get; set; }
        public Utils.Vector3i MinSize { get; set; }
        public Utils.Vector3i MaxSize { get; set; }
        public IMapCustomer MapCustomer { get; set; }
        public StoreClouding.SandBoxEngine.Terrain.Map.BlockSet BlockSet { get; private set; }
    }
}
