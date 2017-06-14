using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
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
        //initial image (without drawings)
        BitmapImage imageInit;
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
#if DEBUG
                String appDirPath = System.Reflection.Assembly.GetEntryAssembly().Location;
                String projectPath = Directory.GetParent(appDirPath).Parent.Parent.FullName;

                
                
                filename = projectPath + @"\debug.png";
#endif
            }
            try
            {

                imageInit = new BitmapImage(new Uri(@filename, UriKind.Absolute));
                image = imageInit;
                img.Source = imageInit;


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
                image = imageInit;
                img.Source = imageInit;

                FitWindowSize();
            }
            else if (Keyboard.IsKeyDown(Key.F3))
            {
                Topmost = !Topmost;
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

        System.Windows.Point lastMousePos = new System.Windows.Point(-1.0,-1.0);
        private void img_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                if(lastMousePos.X == -1.0)
                {
                    lastMousePos = Mouse.GetPosition(this);
                }
                DrawingVisual dv = new DrawingVisual();
                using (DrawingContext dc = dv.RenderOpen())
                {
                    var converter = new BrushConverter();
                    var brush = (System.Windows.Media.Brush)converter.ConvertFromString("#FF0000");
                    var pen = new System.Windows.Media.Pen();
                    pen.Brush = brush;
                    pen.Thickness = 2;
                    

                    System.Windows.Point newMousePos = Mouse.GetPosition(this);

                    dc.DrawImage(image, new Rect(0, 0, image.PixelWidth, image.PixelHeight));
                    
                    dc.DrawLine(pen, lastMousePos,newMousePos);
                    lastMousePos = newMousePos;
                }

                RenderTargetBitmap rtb = new RenderTargetBitmap(image.PixelWidth, image.PixelHeight, 96, 96, PixelFormats.Pbgra32);
                rtb.Render(dv);

                var bitmapEncoder = new PngBitmapEncoder();

                bitmapEncoder.Frames.Add(BitmapFrame.Create(rtb));

                using (var stream = new MemoryStream())
                {
                    bitmapEncoder.Save(stream);
                    stream.Seek(0, SeekOrigin.Begin);
                    BitmapImage newImage = new BitmapImage();
                    newImage.BeginInit();
                    newImage.CacheOption = BitmapCacheOption.OnLoad;
                    newImage.StreamSource = stream;
                    newImage.EndInit();
                    image = newImage;
                    img.Source = image;
                }

            }
        }

        private Bitmap ConvertToBitmap(BitmapSource target)
        {
            Bitmap bitmap;

            using (MemoryStream outStream = new MemoryStream())
            {
                BitmapEncoder enc = new BmpBitmapEncoder();
                enc.Frames.Add(BitmapFrame.Create(target));
                enc.Save(outStream);
                bitmap = new Bitmap(outStream);
            }

            return bitmap;
        }


        private void img_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            lastMousePos = new System.Windows.Point(-1.0, -1.0);
        }
    }
}
