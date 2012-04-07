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
            Debug.Assert(n == 3 || n == 5 || n == 10, "Only supporting 3, 5 or 10 colors for now.");

            List<Tuple<byte, byte, byte>> colors = new List<Tuple<byte, byte, byte>>();
            colors.Add(new Tuple<byte, byte, byte>(255, 255, 255));
            colors.Add(new Tuple<byte, byte, byte>(255, 0, 0));
            colors.Add(new Tuple<byte, byte, byte>(0, 255, 0));

            if (n == 3) return colors;

            colors.Add(new Tuple<byte, byte, byte>(0, 0, 255));
            colors.Add(new Tuple<byte, byte, byte>(255, 0, 255));

            if (n == 5) return colors;

            colors.Add(new Tuple<byte, byte, byte>(50, 50, 50));
            colors.Add(new Tuple<byte, byte, byte>(255, 255, 0));
            colors.Add(new Tuple<byte, byte, byte>(0, 255, 255));
            colors.Add(new Tuple<byte, byte, byte>(140, 255, 255));
            colors.Add(new Tuple<byte, byte, byte>(255, 140, 255));

            return colors;
        }

        /* Courtesy of http://stackoverflow.com/questions/462699/how-do-i-get-the-index-of-the-highest-value-in-an-array-using-linq */
        public static Tuple<int, T> MaxNonBackground<T>(IEnumerable<T> sequence)
        where T : IComparable<T>
        {
            int maxIndex = -1;
            T maxValue = default(T); // Immediately overwritten anyway

            int index = 0;
            foreach (T value in sequence)
            {
                if ((value.CompareTo(maxValue) > 0 || maxIndex == -1) && (HandGestureFormat)index != HandGestureFormat.Background)
                {
                    maxIndex = index;
                    maxValue = value;
                }

                index++;
            }
            return Tuple.Create<int, T>(maxIndex, maxValue);
        }
        
        public static double EuclideanDistance(double[] point1, double[] point2)
        {
            return Math.Sqrt(Math.Pow(point1[0] - point2[0], 2) +
                Math.Pow(point1[1] - point2[1], 2) +
                Math.Pow(point1[2] - point2[2], 2));
        }

        public static double EuclideanDistance(byte[] point1, byte[] point2)
        {
            return Math.Sqrt(Math.Pow(point1[0] - point2[0], 2) +
                Math.Pow(point1[1] - point2[1], 2) +
                Math.Pow(point1[2] - point2[2], 2));
        }

        public static double EuclideanDistance(Point3 point1, Point3 point2)
        {
            return Math.Sqrt(Math.Pow(point1.x() - point2.x(), 2) +
                Math.Pow(point1.y() - point2.y(), 2) +
                Math.Pow(point1.depth() - point2.depth(), 2));
        }
    }

    /* Reference wrapper from 
     * http://stackoverflow.com/questions/2760087/storing-a-reference-to-an-object-in-c-sharp
     *
     * Basically, uses a lambda function on creation to refer to the same 
     * object even if it is normally copied by value. The technical term for 
     * this is "closures". Check this out for an explanation: 
     * http://www.codethinked.com/c-closures-explained
     */
    public class Ref<T>
    {
        private Func<T> getter;
        private Action<T> setter;
        public Ref(Func<T> getter, Action<T> setter)
        {
            this.getter = getter;
            this.setter = setter;
        }

        public T Value
        {
            get { return getter(); }
            set { setter(value); }
        }
    }

    public class Point3
    {
        private int x_, y_;
        private int depth_;

        public Point3(int x, int y, int depth) { x_ = x; y_ = y; depth_ = depth; }
        
        public int x() { return x_; }
        public int y() { return y_; }
        public int depth() { return depth_; }

        public void update(int x, int y, int depth)
        {
            x_ = x;
            y_ = y;
            depth_ = depth;
        }
    }
}
