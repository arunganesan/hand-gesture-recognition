using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Documents;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Input;
using System.Windows.Navigation;
using System.Windows.Shapes;


using System.Diagnostics;
using System.Drawing.Imaging;
using Microsoft.Kinect;

/*
 * Borrowed some code from: http://social.msdn.microsoft.com/Forums/en-US/kinectsdknuiapi/thread/c39bab30-a704-4de1-948d-307afd128dab
 */

namespace ColorGlove
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        private Manager m;
        public MainWindow()
        {
            InitializeComponent();
            m = new Manager(this);
            m.start();
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Space)
                m.toggleProcessors();
            else if (e.Key == Key.S)
                m.saveImages();
            else if (e.Key == Key.K)
                m.kMeans();
            else if (e.Key == Key.Up)  // use up/down key to adjust range
                m.increaseRange();
            else if (e.Key == Key.Down)
                m.decreaseRange();
            else if (e.Key == Key.A)
                m.AutoRange();
        }
    }



    // Michael: Maybe let the classifier region unattentive, since I am working on feature extraction now.
    
    #region classifer
    /*
    public class Classifier
    {
        Tuple<Vector, Vector>[] features;

        // "If an offset pixel lies on the background or outside the 
        // bounds of the image, the depth prove d_I(x') is given a large 
        // positive constant value"
        double outOfBounds = 10000;

        int width = 640, height = 480;

        public Classifier()
        {
            // Define features
            features = new Tuple<Vector, Vector>[2] {
                new Tuple<Vector, Vector>(new Vector(0,0), new Vector(0,-1000)), 
                new Tuple<Vector, Vector>(new Vector(1000, 1000), new Vector(-1000, 1000))
            };
        }

        public double [] extract_features(short[] depth, Point x)
        {
            double[] feature_vectors = new double[features.Length];
            for (int i = 0; i < feature_vectors.Length; i++) feature_vectors[i] = extract_feature(depth, i, x);
            return feature_vectors;

        }

        public double extract_feature(short[] depth, int idx, Point x) 
        {
            Debug.Assert(idx <= features.Length, "Trying to access nonexistent feature.");
            
            //return (double)depth[(int)(x.Y * width + x.X)];
            int x_linear = (int)(x.Y * width + x.X);
            if (depth[x_linear] < 0) return -1;
            
            // The depth in mm!
            //(double)(depth[(int)(x.Y * width + x.X)] >> DepthImageFrame.PlayerIndexBitmaskWidth);

            double depth_in_mm = (double)(depth[x_linear] >> DepthImageFrame.PlayerIndexBitmaskWidth);

            Tuple<Vector, Vector> feature = features[idx];
            
            Point x_u = x + feature.Item1 / depth_in_mm;
            double x_u_depth;
            if (x_u.X < 0 || x_u.X >= width || x_u.Y < 0 || x_u.Y >= height)
                x_u_depth = outOfBounds;
            else
            {
                int lin = (int)(x_u.Y * width + x_u.X);
                x_u_depth = depth[lin] >> DepthImageFrame.PlayerIndexBitmaskWidth;
                if (x_u_depth == -1) return -1;
            }


            Point x_v = x + feature.Item2 / depth_in_mm;
            double x_v_depth;
            if (x_v.X < 0 || x_v.X >= width || x_v.Y < 0 || x_v.Y >= height)
                x_v_depth = outOfBounds;
            else
            {
                int lin = (int)(x_v.Y * width + x_v.X);
                x_v_depth = depth[lin] >> DepthImageFrame.PlayerIndexBitmaskWidth;
                if (x_v_depth == -1) return -1;
            }

            return x_u_depth - x_v_depth;
        }
    }
     */ 
    #endregion
}
