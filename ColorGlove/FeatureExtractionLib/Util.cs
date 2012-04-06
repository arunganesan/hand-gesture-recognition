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
    }

    /* Reference wrapper from 
     * http://stackoverflow.com/questions/2760087/storing-a-reference-to-an-object-in-c-sharp
     *
     * Basically, uses a lambda function on creation to refer to the same 
     * object even if it is normally copied by value. 
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
}
