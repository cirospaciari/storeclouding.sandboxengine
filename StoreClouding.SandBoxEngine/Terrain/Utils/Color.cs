using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace StoreClouding.SandBoxEngine.Terrain.Utils
{
    public class Color
    {
        public float R { get; private set; }
        public float G { get; private set; }
        public float B { get; private set; }
        public float A { get; private set; }

        public Color(float r,float g, float b, float a)
        {
            this.R = r;
            this.G = g;
            this.B = b;
            this.A = a;
        }

    }
}
