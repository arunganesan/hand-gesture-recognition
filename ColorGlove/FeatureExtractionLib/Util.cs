using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;

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
            Two = 4,
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
    }
}
