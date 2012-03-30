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
            if (e.Key == Key.S)
                m.saveImages();
            else if (e.Key == Key.K)
                m.kMeans();
            else if (e.Key == Key.Up)  // use up/down key to adjust range
                m.increaseRange();
            else if (e.Key == Key.Down)
                m.decreaseRange();
            else if (e.Key == Key.A)
                m.AutoRange();
            else if (e.Key == Key.P)
                m.Pool();
        }
    }
}
