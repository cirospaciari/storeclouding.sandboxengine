using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;


namespace StoreClouding.SandBoxEngine.Terrain.Utils
{
    /// <summary>
    /// A 2D vector with integer values.</summary>
    [ProtoContract]
    public struct Vector2i
    {
        [ProtoMember(1, IsRequired = true)]
        public int x;
        [ProtoMember(2, IsRequired = true)]
        public int y;
        public static readonly Vector2i zero = new Vector2i(0, 0);
        public static readonly Vector2i one = new Vector2i(1, 1);

        public Vector2i(int x, int y)
        {
            this.x = x;
            this.y = y;
        }

        public static int DistanceSquared(Vector2i a, Vector2i b)
        {
            int dx = b.x - a.x;
            int dy = b.y - a.y;
            return dx * dx + dy * dy;
        }

        public int DistanceSquared(Vector2i v)
        {
            return DistanceSquared(this, v);
        }

        public override int GetHashCode()
        {
            return (1789 + x) * 1789 + y;
        }

        private int ShiftAndWrap(int value, int positions)
        {
            positions = positions & 0x1F;

            // Save the existing bit pattern, but interpret it as an unsigned integer.
            uint number = BitConverter.ToUInt32(BitConverter.GetBytes(value), 0);
            // Preserve the bits to be discarded.
            uint wrapped = number >> (32 - positions);
            // Shift and wrap the discarded bits.
            return BitConverter.ToInt32(BitConverter.GetBytes((number << positions) | wrapped), 0);
        }

        public override bool Equals(object other)
        {
            if (!(other is Vector2i))
                return false;
            Vector2i vector = (Vector2i)other;
            return x == vector.x &&
                   y == vector.y;
        }

        public bool Equals(Vector2i vector)
        {
            return x == vector.x &&
                    y == vector.y;
        }

        public override string ToString()
        {
            return "Vector2i(" + x + " " + y + ")";
        }

        public static bool operator ==(Vector2i a, Vector2i b)
        {
            return a.x == b.x &&
                   a.y == b.y;
        }

        public static bool operator !=(Vector2i a, Vector2i b)
        {
            return a.x != b.x ||
                   a.y != b.y;
        }

        public static Vector2i operator -(Vector2i a, Vector2i b)
        {
            return new Vector2i(a.x - b.x, a.y - b.y);
        }

        public static Vector2i operator -(Vector2i a)
        {
            return new Vector2i(-a.x, -a.y);
        }

        public static Vector2i operator +(Vector2i a, Vector2i b)
        {
            return new Vector2i(a.x + b.x, a.y + b.y);
        }

        public static explicit operator Vector3(Vector2i v)
        {
            return new Vector3(v.x, v.y);
        }

    }
	
}
