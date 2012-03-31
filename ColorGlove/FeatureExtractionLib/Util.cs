using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Diagnostics;

namespace FeatureExtractionLib
{
    public class Util
    {
        public enum HandGestureFormat
        {
            Background = 0,
            OpenHand = 1,
            CloseHand = 2,
            One = 3,
            Fist = 4,
        };
        
        // Returns the XY position of the index.
        public static Point toXY(int idx, int width, int height, int stride)
        {
            Point p = new Point();
            p.X = (int)(idx / stride) % width;
            p.Y = (int)(idx / stride) / width;
            return p;
        }

        // Returns the linear index corresponding to the XY position
        public static int toID(int x, int y, int width, int height, int stride)
        {
            return (y * width + x) * stride;
        }

        // Returns n [evenly spaced out ]RGB values
        public static List<Tuple<byte, byte, byte>> GiveMeNColors(int n)
        {
            Debug.Assert(n == 3 || n == 5, "Only supporting 3 or 5 colors for now.");

            List<Tuple<byte, byte, byte>> colors = new List<Tuple<byte, byte, byte>>();
            colors.Add(new Tuple<byte, byte, byte>(255, 255, 255));
            colors.Add(new Tuple<byte, byte, byte>(255, 0, 0));
            colors.Add(new Tuple<byte, byte, byte>(0, 255, 0));

            if (n == 3) return colors;

            colors.Add(new Tuple<byte, byte, byte>(0, 0, 255));
            colors.Add(new Tuple<byte, byte, byte>(255, 0, 255));
            return colors;
            
            /* A failed attempt at generating N evenly spaced out colors.
            int full_rgb = 256 * 256 * 256;

            List<Tuple<byte, byte, byte>> colors = new List<Tuple<byte, byte, byte>>();
            if (n == 0) return colors;

            colors.Add(new Tuple<byte, byte, byte>(255, 255, 255));
            for (int i = n; i > 0; i--)
            {
                int color_linear = full_rgb / n * i;
                colors.Add(new Tuple<byte, byte, byte>(
                    (byte)(color_linear / (256*256) % 256),
                    (byte)(color_linear / 256 % 256),
                    (byte)(color_linear % 256)));
            }

            return colors;
            */
        }
    }
}
