using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace StoreClouding.SandBoxEngine.Terrain.Utils
{
    /// <summary>
    /// Gives some methods to work with Vector3.</summary>
    public static class Vector3Utils
    {
        public static float DistanceSquared(Vector3 a, Vector3 b)
        {
            float dx = b.x - a.x;
            float dy = b.y - a.y;
            float dz = b.z - a.z;
            return dx * dx + dy * dy + dz * dz;
        }
    }
}
