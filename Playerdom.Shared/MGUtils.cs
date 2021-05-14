using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Text;

namespace Playerdom.Shared
{
    public static class MGUtils
    {
        public static (float hue, float sat, float val) ToHSV(this Color c)
        {
            float r = c.R / (float)255;
            float g = c.G / (float)255;
            float b = c.B / (float)255;


            float max = Math.Max(Math.Max(r, g), b);
            float min = Math.Min(Math.Min(r, g), b);

            (float hue, float sat, float val) hsv = (max, max, max);


            float dif = max - min;
            hsv.sat = max == 0 ? 0 : dif / max;
            if (max == min)
            {
                hsv.hue = 0;
            }
            else
            {
                if (max == r)
                {
                    hsv.hue = (g - b) / dif + (g < b ? 6 : 0);
                }
                else if (max == g)
                {
                    hsv.hue = (b - r) / dif + 2;
                }
                else if(max == b)
                {
                    hsv.hue = (r - g) / dif + 4;
                }

                hsv.hue /= 6;
            }

            return hsv;
        }

        public static (float hue, float sat, float val) OffsetHSV(this (float hue, float sat, float val) hsv, (float hue, float sat, float val) offset)
        {
            hsv.hue += offset.hue;
            if (hsv.hue >= 1f) hsv.hue -= 1f;
            else if (hsv.hue < 0f) hsv.hue += 1f;

            hsv.sat += offset.sat;
            if (hsv.sat >= 1f) hsv.sat -= 1f;
            else if (hsv.sat < 0f) hsv.sat += 1f;

            hsv.val += offset.val;
            if (hsv.val >= 1f) hsv.val -= 1f;
            else if (hsv.val < 0f) hsv.val += 1f;

            return hsv;
        }



        public static Color ToColor(this (float hue, float sat, float val) hsv)
        {
            byte r = 0;
            byte g = 0;
            byte b = 0;

            byte i = (byte)Math.Floor(hsv.hue * 6);
            float f = (byte)(hsv.hue * 6 - i);
            float p = (byte)(hsv.val * (1 - hsv.sat));
            float q = (byte)(hsv.val * (1 - f * hsv.sat));
            float t = (byte)(hsv.val * (1 - (1 - f) * hsv.sat));

            switch (i % 6)
            {
                case 0:
                    r = (byte)(hsv.val * 255);
                    g = (byte)(t * 255);
                    b = (byte)(p * 255);
                    break;
                case 1:
                    r = (byte)(q * 255);
                    g = (byte)(hsv.val * 255);
                    b = (byte)(p * 255);
                    break;
                case 2:
                    r = (byte)(p * 255);
                    g = (byte)(hsv.val * 255);
                    b = (byte)(t * 255);
                    break;
                case 3:
                    r = (byte)(p * 255);
                    g = (byte)(q * 255);
                    b = (byte)(hsv.val * 255);
                    break;
                case 4:
                    r = (byte)(t * 255);
                    g = (byte)(p * 255);
                    b = (byte)(hsv.val * 255);
                    break;
                case 5:
                    r = (byte)(hsv.val * 255);
                    g = (byte)(p * 255);
                    b = (byte)(q * 255);
                    break;
            }

            return new Color(r, g, b);
        }


        public static double GetNormalRandomDouble(string seed, double mean, double standardDeviation, double? max = null, double? min = null)
        {
            Random random = new Random(seed.GetHashCode());

            double value;
            do
            {
                double r = Math.Sqrt(-2.0 * Math.Log(random.NextDouble()));
                double theta = 2.0 * Math.PI * random.NextDouble();

                value = mean + (r * Math.Sin(theta)) * standardDeviation;
            }
            while (
                !((!max.HasValue || value <= max.Value) &&
                (!min.HasValue || value >= min.Value))
            );

            return value;
        }


        public static Vector2 ToXNA(this System.Numerics.Vector2 vec)
        {
            return new Vector2(vec.X, vec.Y);
        }

        public static Rectangle ToXNA(this System.Drawing.Rectangle rec)
        {
            return new Rectangle(rec.X, rec.Y, rec.Width, rec.Height);
        }

        public static Color ToXNA(this System.Drawing.Color col)
        {
            return new Color(col.R, col.G, col.B, col.A);
        }


        public static System.Numerics.Vector2 ToGeneric(this Vector2 vec)
        {
            return new System.Numerics.Vector2(vec.X, vec.Y);
        }

        public static System.Drawing.Rectangle ToGeneric(this Rectangle rec)
        {
            return new System.Drawing.Rectangle(rec.X, rec.Y, rec.Width, rec.Height);
        }

        public static System.Drawing.Color ToGeneric(this Color col)
        {
            return System.Drawing.Color.FromArgb(col.A, col.R, col.G, col.B);
        }
    }
}
