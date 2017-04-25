using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;


namespace StoreClouding.SandBoxEngine.Client.Terrain.Utils
{
    /// <summary>
    /// A 3D vector with integer values.</summary>
    [ProtoContract]
    public struct Vector3i
    {

        [ProtoMember(1, IsRequired = true)]
        public int x;
        [ProtoMember(2, IsRequired = true)]
        public int y;
        [ProtoMember(3, IsRequired = true)]
        public int z;
        public static readonly Vector3i zero = new Vector3i(0, 0, 0);
        public static readonly Vector3i one = new Vector3i(1, 1, 1);
        public static readonly Vector3i forward = new Vector3i(0, 0, 1);
        public static readonly Vector3i back = new Vector3i(0, 0, -1);
        public static readonly Vector3i up = new Vector3i(0, 1, 0);
        public static readonly Vector3i down = new Vector3i(0, -1, 0);
        public static readonly Vector3i left = new Vector3i(-1, 0, 0);
        public static readonly Vector3i right = new Vector3i(1, 0, 0);
        public static readonly Vector3i forward_right = new Vector3i(1, 0, 1);
        public static readonly Vector3i forward_left = new Vector3i(-1, 0, 1);
        public static readonly Vector3i forward_up = new Vector3i(0, 1, 1);
        public static readonly Vector3i forward_down = new Vector3i(0, -1, 1);
        public static readonly Vector3i back_right = new Vector3i(1, 0, -1);
        public static readonly Vector3i back_left = new Vector3i(-1, 0, -1);
        public static readonly Vector3i back_up = new Vector3i(0, 1, -1);
        public static readonly Vector3i back_down = new Vector3i(0, -1, -1);
        public static readonly Vector3i up_right = new Vector3i(1, 1, 0);
        public static readonly Vector3i up_left = new Vector3i(-1, 1, 0);
        public static readonly Vector3i down_right = new Vector3i(1, -1, 0);
        public static readonly Vector3i down_left = new Vector3i(-1, -1, 0);
        public static readonly Vector3i forward_right_up = new Vector3i(1, 1, 1);
        public static readonly Vector3i forward_right_down = new Vector3i(1, -1, 1);
        public static readonly Vector3i forward_left_up = new Vector3i(-1, 1, 1);
        public static readonly Vector3i forward_left_down = new Vector3i(-1, -1, 1);
        public static readonly Vector3i back_right_up = new Vector3i(1, 1, -1);
        public static readonly Vector3i back_right_down = new Vector3i(1, -1, -1);
        public static readonly Vector3i back_left_up = new Vector3i(-1, 1, -1);
        public static readonly Vector3i back_left_down = new Vector3i(-1, -1, -1);
        public static readonly Vector3i[] directions = new Vector3i[] {
			left, right,
			back, forward,
			down, up,
		};
        public static readonly Vector3i[] allDirections = new Vector3i[] {
			left, 
			right,
			back, 
			forward,
			down, 
			up,
			forward_right,
			forward_left,
			forward_up,
			forward_down,
			back_right,
			back_left,
			back_up,
			back_down,
			up_right,
			up_left,
			down_right,
			down_left,
			forward_right_up,
			forward_right_down,
			forward_left_up,
			forward_left_down,
			back_right_up,
			back_right_down,
			back_left_up,
			back_left_down,
		};

        public static int IndexOfDirection(Vector3i direction)
        {
            return System.Array.IndexOf(allDirections, direction);
        }

        public static bool AreNeighbours(Vector3i a, Vector3i b)
        {
            return (a.x == b.x || a.x == b.x + 1 || a.x == b.x - 1) &&
                    (a.y == b.y || a.y == b.y + 1 || a.y == b.y - 1) &&
                    (a.z == b.z || a.z == b.z + 1 || a.z == b.z - 1);
        }

        public static Vector3i GetNeighbourDirection(Vector3i a, Vector3i b)
        {
            return b - a;
        }

        public Vector3i(int x, int y, int z)
        {
            this.x = x;
            this.y = y;
            this.z = z;
        }

        public Vector3i(Vector3 v)
        {
            this.x = (int)v.x;
            this.y = (int)v.y;
            this.z = (int)v.z;
        }

        public static int DistanceSquared(Vector3i a, Vector3i b)
        {
            int dx = b.x - a.x;
            int dy = b.y - a.y;
            int dz = b.z - a.z;
            return dx * dx + dy * dy + dz * dz;
        }

        public int DistanceSquared(Vector3i v)
        {
            return DistanceSquared(this, v);
        }

        public static int FlatDistanceSquared(Vector3i a, Vector3i b)
        {
            int dx = b.x - a.x;
            int dz = b.z - a.z;
            return dx * dx + dz * dz;
        }

        public int FlatDistanceSquared(Vector3i v)
        {
            return FlatDistanceSquared(this, v);
        }

        public static float Distance(Vector3i a, Vector3i b)
        {
            int dx = b.x - a.x;
            int dy = b.y - a.y;
            int dz = b.z - a.z;
            return Convert.ToSingle(Math.Sqrt(dx * dx + dy * dy + dz * dz));
        }

        public float Distance(Vector3i v)
        {
            return Distance(this, v);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 23 + z;
                hash = hash * 23 + x;
                hash = hash * 23 + y;
                return hash;
            }
            //return x ^ y << 2 ^ z >> 2;
        }

        public override bool Equals(object other)
        {
            if (!(other is Vector3i))
                return false;
            Vector3i vector = (Vector3i)other;
            return x == vector.x &&
                   y == vector.y &&
                   z == vector.z;
        }

        public bool Equals(Vector3i vector)
        {
            return x == vector.x &&
                    y == vector.y &&
                    z == vector.z;
        }

        public override string ToString()
        {
            return "Vector3i(" + x + " " + y + " " + z + ")";
        }

        public static bool operator ==(Vector3i a, Vector3i b)
        {
            return a.x == b.x &&
                   a.y == b.y &&
                   a.z == b.z;
        }

        public static bool operator !=(Vector3i a, Vector3i b)
        {
            return a.x != b.x ||
                   a.y != b.y ||
                   a.z != b.z;
        }

        public static Vector3i operator -(Vector3i a, Vector3i b)
        {
            return new Vector3i(a.x - b.x, a.y - b.y, a.z - b.z);
        }

        public static Vector3i operator -(Vector3i a)
        {
            return new Vector3i(-a.x, -a.y, -a.z);
        }

        public static Vector3i operator +(Vector3i a, Vector3i b)
        {
            return new Vector3i(a.x + b.x, a.y + b.y, a.z + b.z);
        }

        public static Vector3i operator *(Vector3i b, int a)
        {
            return new Vector3i(a * b.x, a * b.y, a * b.z);
        }

        public static Vector3 operator *(Vector3i b, float a)
        {
            return new Vector3(a * b.x, a * b.y, a * b.z);
        }

        public static implicit operator Vector3(Vector3i v)
        {
            return new Vector3(v.x, v.y, v.z);
        }

        public static Vector3i operator *(int a, Vector3i b)
        {
            return new Vector3i(a * b.x, a * b.y, a * b.z);
        }

        public byte[] Serialize()
        {
            List<byte> bytes = new List<byte>(12);
            bytes.AddRange( BitConverter.GetBytes(x));
            bytes.AddRange(BitConverter.GetBytes(y));
            bytes.AddRange(BitConverter.GetBytes(z));
            return bytes.ToArray();
        }
    }

}
