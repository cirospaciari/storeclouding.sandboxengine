﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace StoreClouding.SandBoxEngine.Terrain.Utils
{
    public sealed class Vector3iComparer : IEqualityComparer<Vector3i>
    {
        public int GetHashCode(Vector3i v)
        {
            return v.x ^ v.y << 2 ^ v.z >> 2;
        }

        public bool Equals(Vector3i v1, Vector3i v2)
        {
            return v1.x == v2.x &&
                    v1.y == v2.y &&
                    v1.z == v2.z;
        }
    }
}