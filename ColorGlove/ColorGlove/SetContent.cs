using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
namespace ColorGlove
{
    class SetContent
    {
        public static MainWindow main_window_;
        public static void SetMainWindowControler(MainWindow main_window){
            main_window_ = main_window; 
        }
        public static void SetMetaInformation(string s)
        {
           
            main_window_.Dispatcher.BeginInvoke(new Action(() =>
            //main_window_.MetaLabel.Dispatcher.BeginInvoke(new Action(() =>
           {
              main_window_.MetaLabel.Content = s;            }));
              
        }
        public static void SetColorImageLabel(string s)
        {

            main_window_.Dispatcher.BeginInvoke(new Action(() =>
            {
                main_window_.colorImageLabel.Text = s;
            }));
        }
        public static void SetDepthImageLabel(string s)
        {
            main_window_.Dispatcher.BeginInvoke(new Action(() =>
            {
                main_window_.depthImageLabel.Text = s;
            })); 
        }
        public static void SetPoolImageLabel(string s)
        {
            main_window_.Dispatcher.BeginInvoke(new Action(() =>
           {
               main_window_.poolImageLabel.Text = s;
           })); 
       }
        public static void SetRadiusText(string s)
        {
            main_window_.Dispatcher.BeginInvoke(new Action(() =>
            {
                main_window_.radiusText.Text = s;
            }));
        }

        public static void SetDensityText(string s)
        {
            main_window_.Dispatcher.BeginInvoke(new Action(() =>
            {
                main_window_.densityText.Text = s;
            }));
        }
    }
}
