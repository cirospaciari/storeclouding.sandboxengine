using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace StoreClouding.SandBoxEngine.Terrain.Utils
{
    public sealed class Vector2iComparer : IEqualityComparer<Vector2i>
    {
        public int GetHashCode(Vector2i v)
        {
            return (1789 + v.x) * 1789 + v.y;
        }

        public bool Equals(Vector2i v1, Vector2i v2)
        {
            return v1.x == v2.x &&
                    v1.y == v2.y;
        }
    }
}
