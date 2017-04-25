using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;


namespace StoreClouding.SandBoxEngine.Terrain.Utils
{
    
    public struct Vector3
    {

    
        public float x;

    
        public float y;

    
        public float z;

        public Vector3(float x, float y)
        {
            this.x = x;
            this.y = y;
            this.z = 0;
        }
        public Vector3(float x, float y, float z)
        {
            this.x = x;
            this.y = y;
            this.z = z;
        }

        public Vector3(Vector3 v)
        {
            this.x = v.x;
            this.y = v.y;
            this.z = v.z;
        }
    }
	
}
