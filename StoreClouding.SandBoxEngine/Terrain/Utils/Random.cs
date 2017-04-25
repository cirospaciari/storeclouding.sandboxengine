using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace StoreClouding.SandBoxEngine.Terrain.Utils
{
    class Random
    {
        private static System.Random random = new System.Random();
        public static float value
        {
            get
            {
                double result;
                lock (random)
                {
                    result = random.NextDouble();
                }
                return Convert.ToSingle(result);
            }
        }
        public static float Range(float min, float max)
        {
            double result;
            lock (random)
            {
                result = min + (random.NextDouble() * (max - min));
            }
            return Convert.ToSingle(result);
        }
    }
}
