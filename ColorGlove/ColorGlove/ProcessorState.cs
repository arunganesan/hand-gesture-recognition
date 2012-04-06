using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FeatureExtractionLib;

namespace ColorGlove
{
    class ProcessorState
    {
        public Ref<System.Drawing.Rectangle> crop;
        public short[] depth;
        public byte[] rgb, bitmap_bits;
        
        public ProcessorState(Ref<System.Drawing.Rectangle> crop, short[] depth, byte[] rgb, byte[] bitmap_bits)
        {
            this.crop = crop;
            this.depth = depth;
            this.rgb = rgb;
            this.bitmap_bits = bitmap_bits;
        }
    }
}
