
using System;

using System.Collections.Generic;
using System.Linq;
using System.Text;
using StoreClouding.SandBoxEngine.Terrain.Utils;
using UnityEngine;

namespace StoreClouding.SandBoxEngine.Terrain.Generator
{
    public class PerlinNoise3D
    {

        private const int GradientSizeTable = 256;
        private float[] gradients = new float[GradientSizeTable * 3];
        private readonly ushort[] perm = new ushort[] {
	        225, 155, 210, 108, 175, 199, 221, 144, 203, 116, 70, 213, 69, 158, 33, 252,
	        5, 82, 173, 133, 222, 139, 174, 27, 9, 71, 90, 246, 75, 130, 91, 191,
	        169, 138, 2, 151, 194, 235, 81, 7, 25, 113, 228, 159, 205, 253, 134, 142,
	        248, 65, 224, 217, 22, 121, 229, 63, 89, 103, 96, 104, 156, 17, 201, 129,
	        36, 8, 165, 110, 237, 117, 231, 56, 132, 211, 152, 20, 181, 111, 239, 218,
	        170, 163, 51, 172, 157, 47, 80, 212, 176, 250, 87, 49, 99, 242, 136, 189,
	        162, 115, 44, 43, 124, 94, 150, 16, 141, 247, 32, 10, 198, 223, 255, 72,
	        53, 131, 84, 57, 220, 197, 58, 50, 208, 11, 241, 28, 3, 192, 62, 202,
	        18, 215, 153, 24, 76, 41, 15, 179, 39, 46, 55, 6, 128, 167, 23, 188,
	        106, 34, 187, 140, 164, 73, 112, 182, 244, 195, 227, 13, 35, 77, 196, 185,
	        26, 200, 226, 119, 31, 123, 168, 125, 249, 68, 183, 230, 177, 135, 160, 180,
	        12, 1, 243, 148, 102, 166, 38, 238, 251, 37, 240, 126, 64, 74, 161, 40,
	        184, 149, 171, 178, 101, 66, 29, 59, 146, 61, 254, 107, 42, 86, 154, 4,
	        236, 232, 120, 21, 233, 209, 45, 98, 193, 114, 78, 19, 206, 14, 118, 127,
	        48, 79, 147, 85, 30, 207, 219, 54, 88, 234, 190, 122, 95, 67, 143, 109,
	        137, 214, 145, 93, 92, 100, 245, 0, 216, 186, 60, 83, 105, 97, 204, 52
	    };
        private float scale;
        private ProtoVector2[] randVals = new ProtoVector2[256];

        public ProtoVector2[] RandVals
        {
            get
            {
                return randVals;
            }
        }

        public PerlinNoise3D(float scale, bool randomize, ProtoVector2[] noise3dRandomValues)
        {
            this.scale = scale;
            for (int i = 0; i < GradientSizeTable; i++)
            {
                float z, r, theta;

                if (randomize)
                    randVals[i] = new ProtoVector2(Utils.Random.value, Utils.Random.value);
                else
                    randVals[i] = noise3dRandomValues[i];

                z = 1f - 2f * randVals[i].x;
                r = Mathf.Sqrt(1 - z * z);
                theta = 2 * Mathf.PI * randVals[i].y;

                gradients[i * 3] = r * Mathf.Cos(theta);
                gradients[i * 3 + 1] = r * Mathf.Sin(theta);
                gradients[i * 3 + 2] = z;
            }
        }

        public float Noise(float x, float y, float z)
        {
            return PerlinNoise(x * scale, y * scale, z * scale);
        }

        private float PerlinNoise(float x, float y, float z)
        {
            int ix = Mathf.FloorToInt(x);
            float fx0 = x - ix;
            float fx1 = fx0 - 1;
            float wx = Smooth(fx0);

            int iy = Mathf.FloorToInt(y);
            float fy0 = y - iy;
            float fy1 = fy0 - 1;
            float wy = Smooth(fy0);

            int iz = Mathf.FloorToInt(z);
            float fz0 = z - iz;
            float fz1 = fz0 - 1;
            float wz = Smooth(fz0);

            float vx0 = Lattice(ix, iy, iz, fx0, fy0, fz0);
            float vx1 = Lattice(ix + 1, iy, iz, fx1, fy0, fz0);
            float vy0 = Mathf.Lerp(vx0, vx1, wx);

            vx0 = Lattice(ix, iy + 1, iz, fx0, fy1, fz0);
            vx1 = Lattice(ix + 1, iy + 1, iz, fx1, fy1, fz0);
            float vy1 = Mathf.Lerp(vx0, vx1, wx);

            float vz0 = Mathf.Lerp(vy0, vy1, wy);

            vx0 = Lattice(ix, iy, iz + 1, fx0, fy0, fz1);
            vx1 = Lattice(ix + 1, iy, iz + 1, fx1, fy0, fz1);
            vy0 = Mathf.Lerp(vx0, vx1, wx);

            vx0 = Lattice(ix, iy + 1, iz + 1, fx0, fy1, fz1);
            vx1 = Lattice(ix + 1, iy + 1, iz + 1, fx1, fy1, fz1);
            vy1 = Mathf.Lerp(vx0, vx1, wx);

            float vz1 = Mathf.Lerp(vy0, vy1, wy);
            return Mathf.Lerp(vz0, vz1, wz);
        }

        private int Permutate(int x)
        {
            int mask = GradientSizeTable - 1;
            return perm[x & mask];
        }

        private int Index(int ix, int iy, int iz)
        {
            // Turn an XYZ triplet into a single gradient table index.
            return Permutate(ix + Permutate(iy + Permutate(iz)));
        }

        private float Lattice(int ix, int iy, int iz, float fx, float fy, float fz)
        {
            // Look up a random gradient at [ix,iy,iz] and dot it with the [fx,fy,fz] vector.
            int index = Index(ix, iy, iz);
            int g = index * 3;
            return gradients[g] * fx + gradients[g + 1] * fy + gradients[g + 2] * fz;
        }

        private static float Smooth(float x)
        {
            return x * x * (3 - 2 * x);
        }

    }
	
}
