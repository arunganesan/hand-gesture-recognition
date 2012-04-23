using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Kinect;

using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;


namespace ColorGlove
{
    [Serializable()]
    public class KinectData : ISerializable
    {
        private short[] _depth;
        private byte[] _color;
        private ColorImagePoint[] _mapped;

        int _width, _height;
        int _stride = 4;

        public byte[] color() { return _color; }
        public short[] depth() { return _depth; }
        public ColorImagePoint[] mapped() { return _mapped; }

        public KinectData(int width = 640, int height = 480)
        {
            this._width = width;
            this._height = height;

            _depth = new short[width * height];
            _color = new byte[width * height * _stride];
            _mapped = new ColorImagePoint[width * height];
        }

        // Serialization interface
        public KinectData(SerializationInfo info, StreamingContext ctxt)
        {
            _depth = (short[])info.GetValue("depth", typeof(short[]));
            _color = (byte[])info.GetValue("color", typeof(byte[]));
            _mapped = new ColorImagePoint[_width * _height];
        }

        // Serialization interface
        public void GetObjectData(SerializationInfo info, StreamingContext ctxt)
        {
            info.AddValue("depth", _depth);
            info.AddValue("color", _color);
        }
    }
}
