using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;


namespace StoreClouding.SandBoxEngine.Terrain.Utils
{
    /// <summary>
    /// Simple Vector3 serializable with Protobuf-net.</summary>
    [ProtoContract]
    public struct ProtoVector3
    {

        [ProtoMember(1, IsRequired = true)]
        public float x;

        [ProtoMember(2, IsRequired = true)]
        public float y;

        [ProtoMember(3, IsRequired = true)]
        public float z;

        public ProtoVector3(float x, float y, float z)
        {
            this.x = x;
            this.y = y;
            this.z = z;
        }

        public ProtoVector3(Vector3 v)
        {
            this.x = v.x;
            this.y = v.y;
            this.z = v.z;
        }
    }
	
}
