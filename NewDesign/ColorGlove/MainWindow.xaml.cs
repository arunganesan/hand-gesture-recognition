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
            SetContent.SetMainWindowControler(this);
            InitializeComponent();
            // test
            SetContent.SetMetaInformation("Hi!!");
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
            else if (e.Key == Key.P)
                m.Pool();
        }

        private void RadiusValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            SetContent.SetRadiusText(String.Format("radius: {0}",Radius.Value.ToString()));
            if (m != null)
                m.setRadius((int)Radius.Value);
            //            Console.WriteLine("radius: {0}", Radius.Value.ToString());
        }
         private void DensityValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            double density = (Density.Value/100);
            SetContent.SetDensityText(String.Format("density: {0}", density ));
           if (m != null)
             m.setDensity(density);
             //            Console.WriteLine("radius: {0}", Radius.Value.ToString());
        }
    }
}
