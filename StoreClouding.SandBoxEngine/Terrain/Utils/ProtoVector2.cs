using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;


namespace StoreClouding.SandBoxEngine.Terrain.Utils
{
    /// <summary>
    /// Simple Vector2 serializable with Protobuf-net.</summary>
    [ProtoContract]
    public struct ProtoVector2
    {

        [ProtoMember(1, IsRequired = true)]
        public float x;

        [ProtoMember(2, IsRequired = true)]
        public float y;

        public ProtoVector2(float x, float y)
        {
            this.x = x;
            this.y = y;
        }

        public ProtoVector2(Vector2 v)
        {
            this.x = v.x;
            this.y = v.y;
        }
    }
	
}
