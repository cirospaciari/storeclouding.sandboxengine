using StoreClouding.SandBoxEngine.Terrain.Utils;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System;
using UnityEngine;

namespace StoreClouding.SandBoxEngine.Terrain.Generator
{
    public class PerlinNoise2D
    {

        private float scale;
        private float persistence = 0.5f;
        private int octaves = 5;
        private ProtoVector2 offset;

        public ProtoVector2 Offset
        {
            get
            {
                return offset;
            }
        }

        public PerlinNoise2D(float scale, bool randomize, ProtoVector2 randomValues)
        {
            this.scale = scale;
            if (randomize)
                offset = new ProtoVector2(Utils.Random.Range(-100f, 100f), Utils.Random.Range(-100f, 100f));
            else
                offset = randomValues;
        }

        public PerlinNoise2D SetPersistence(float persistence)
        {
            this.persistence = persistence;
            return this;
        }

        public PerlinNoise2D SetOctaves(int octaves)
        {
            this.octaves = octaves;
            return this;
        }

        public void Noise(float[,] map, float offsetX, float offsetY)
        {
            int width = map.GetLength(0);
            int height = map.GetLength(1);
            const int delta = 4;
            for (int x = 0; x < width; x += delta)
            {
                for (int y = 0; y < height; y += delta)
                {
                    float x1 = x + offsetX;
                    float y1 = y + offsetY;
                    float x2 = x + delta + offsetX;
                    float y2 = y + delta + offsetY;

                    float v1 = Noise(x1, y1);
                    float v2 = Noise(x2, y1);
                    float v3 = Noise(x1, y2);
                    float v4 = Noise(x2, y2);

                    for (int tx = 0; tx < delta && x + tx < width; tx++)
                    {
                        for (int ty = 0; ty < delta && y + ty < height; ty++)
                        {
                            float fx = (float)tx / delta;
                            float fy = (float)ty / delta;
                            float i1 = Mathf.Lerp(v1, v2, fx);
                            float i2 = Mathf.Lerp(v3, v4, fx);
                            int px = x + tx;
                            int py = y + ty;
                            map[px, py] = Mathf.Lerp(i1, i2, fy);
                        }
                    }
                }
            }
        }

        public float Noise(float x, float y)
        {
            x = x * scale + offset.x;
            y = y * scale + offset.y;
            float total = 0;
            float frq = 1, amp = 1;
            for (int i = 0; i < octaves; i++)
            {
                if (i >= 1)
                {
                    frq *= 2;
                    amp *= persistence;
                }
                total += InterpolatedSmoothNoise(x * frq, y * frq) * amp;
            }
            return total;
        }

        private static float InterpolatedSmoothNoise(float X, float Y)
        {
            int ix = Mathf.FloorToInt(X);
            float fx = X - ix;
            int iy = Mathf.FloorToInt(Y);
            float fy = Y - iy;

            float v1 = SmoothNoise(ix, iy);
            float v2 = SmoothNoise(ix + 1, iy);
            float v3 = SmoothNoise(ix, iy + 1);
            float v4 = SmoothNoise(ix + 1, iy + 1);

            float i1 = Mathf.Lerp(v1, v2, fx);
            float i2 = Mathf.Lerp(v3, v4, fx);

            return Mathf.Lerp(i1, i2, fy);
        }

        private static float SmoothNoise(int x, int y)
        {
            float corners = (Noise(x - 1, y - 1) + Noise(x + 1, y - 1) + Noise(x - 1, y + 1) + Noise(x + 1, y + 1)) / 16f;
            float sides = (Noise(x - 1, y) + Noise(x + 1, y) + Noise(x, y - 1) + Noise(x, y + 1)) / 8f;
            float center = Noise(x, y) / 4f;
            return corners + sides + center;
        }

        private static float Noise(int x, int y)
        {
            int n = x + y * 57;
            n = (n << 13) ^ n;
            return (1 - ((n * (n * n * 15731 + 789221) + 1376312589) & 0x7fffffff) / 1073741824f);
        }

    }
	
}
