using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using FeatureExtractionLib;

namespace ColorGlove
{
    class Pooled
    {
        private Point center_;
        private int center_depth_;
        private Util.HandGestureFormat gesture_;

        public Pooled(Point center, int center_depth, Util.HandGestureFormat gesture)
        {
            center_ = center;
            center_depth_ = center_depth;
            gesture_ = gesture;
        }

        public override string ToString()
        {
            string message = String.Format("({0},{1},{2},{3})", gesture_, center_.X, center_.Y, center_depth_);
            return message;
        }

        public Point center() { return center_; }
        public int center_depth() { return center_depth_; }
    }
}
