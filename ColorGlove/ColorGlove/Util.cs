using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;

namespace ColorGlove
{
    class Util
    {
        /// <summary>
        /// Function to save object to external file
        /// </summary>
        /// <param name="_Object">object to save</param>
        /// <param name="_FileName">File name to save object</param>
        /// <returns>Return true if object save successfully, if not return false</returns>
        public static bool ObjectToFile(object _Object, string _FileName)
        {
            try
            {
                // create new memory stream
                System.IO.MemoryStream _MemoryStream = new System.IO.MemoryStream();

                // create new BinaryFormatter
                System.Runtime.Serialization.Formatters.Binary.BinaryFormatter _BinaryFormatter
                            = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();

                // Serializes an object, or graph of connected objects, to the given stream.
                _BinaryFormatter.Serialize(_MemoryStream, _Object);

                // convert stream to byte array
                byte[] _ByteArray = _MemoryStream.ToArray();

                // Open file for writing
                System.IO.FileStream _FileStream = new System.IO.FileStream(_FileName, System.IO.FileMode.Create, System.IO.FileAccess.Write);

                // Writes a block of bytes to this stream using data from a byte array.
                _FileStream.Write(_ByteArray.ToArray(), 0, _ByteArray.Length);

                // close file stream
                _FileStream.Close();

                // cleanup
                _MemoryStream.Close();
                _MemoryStream.Dispose();
                _MemoryStream = null;
                _ByteArray = null;

                return true;
            }
            catch (Exception _Exception)
            {
                // Error
                Console.WriteLine("Exception caught in process: {0}", _Exception.ToString());
            }

            // Error occured, return null
            return false;
        }

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
