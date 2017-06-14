using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace BorderlessGraphicViewer
{
    /// <summary>
    /// Interaktionslogik für MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        BitmapImage image;
        public MainWindow(String[] args)
        {
            InitializeComponent();
            string filename = "";
            if (args.Length != 0)
            {
                filename = args[0];
            }
            else
            {
                filename = @"C:\Users\Patrick\Documents\Visual Studio 2015\Projects\Programmsammlung\BorderlessGraphicViewer\debug.png";

            }
            try
            {

                image = new BitmapImage(new Uri(@filename, UriKind.Absolute));
                img.Source = image;
            }
            catch (Exception)
            {
                Close();
            }

        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl)
            && Keyboard.IsKeyDown(Key.C))
            { 
                if(image != null)
                {
                    Clipboard.SetImage(image);
                }
            }
            else if(Keyboard.IsKeyDown(Key.F5))
            {
                FitWindowSize();
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            FitWindowSize();
        }

        private void FitWindowSize()
        {
            double heightDifference = Height - img.ActualHeight;
            double widthDifference = Width - img.ActualWidth;

            Height = image.Height + heightDifference;
            Width = image.Width + widthDifference;
        }
    }
}
