using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
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
    public class CopyCommand : ICommand
    {
        BitmapImage image;
        event EventHandler ICommand.CanExecuteChanged
        {
            add
            {
                throw new NotImplementedException();
            }

            remove
            {
                throw new NotImplementedException();
            }
        }

        bool ICommand.CanExecute(object parameter)
        {
            throw new NotImplementedException();
        }

        void ICommand.Execute(object parameter)
        {
            if (image != null)
            {
                Clipboard.SetImage(image);
            }
        }
    }
    /// <summary>
    /// Interaktionslogik für MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        bool newPictureOnStack = false;
        Stack<BitmapImage> imageStack = new Stack<BitmapImage>();

        public CopyCommand copyCommand = new CopyCommand();
        BitmapImage image;
        //initial image (without drawings)
        BitmapImage imageInit;
        public MainWindow(string[] args)
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
                string appDirPath = System.Reflection.Assembly.GetEntryAssembly().Location;
                string projectPath = Directory.GetParent(appDirPath).Parent.Parent.FullName;
                filename = projectPath + @"\debug.png";
#endif
            }
            try
            {
     
                imageInit = new BitmapImage(new Uri(@filename, UriKind.Absolute));
                image = imageInit;
                img.Source = imageInit;
                imageStack.Push(image);

            }
            catch (Exception)
            {
                Close();
            }
        }
        
        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if ((Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl))
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
            else if ((Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl))
            && Keyboard.IsKeyDown(Key.Z))
            {
                // first time pressing ctrl+z after new pic
                if(newPictureOnStack)
                {
                    imageStack.Pop();
                    newPictureOnStack = false;
                }
                if(imageStack.Count > 1)
                {
                    image = imageStack.Pop();
 
                }
                else
                {
                    image = imageStack.Peek();
                }
                img.Source = image;
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
                    try
                    {
                        var converter = new BrushConverter();
                        var brush = (System.Windows.Media.Brush)converter.ConvertFromString("#FF0000");
                        var pen = new System.Windows.Media.Pen();
                        pen.Brush = brush;
                        pen.Thickness = 2;

                        System.Windows.Point newMousePos = Mouse.GetPosition(this);

                        dc.DrawImage(image, new Rect(0, 0, image.PixelWidth, image.PixelHeight));

                        System.Windows.Point relativeP1 = new System.Windows.Point(image.Width * (lastMousePos.X / img.ActualWidth), (image.Height * lastMousePos.Y / img.ActualHeight));
                        System.Windows.Point relativeP2 = new System.Windows.Point(image.Width * (newMousePos.X / img.ActualWidth), (image.Height * newMousePos.Y / img.ActualHeight));
                        dc.DrawLine(pen, relativeP1, relativeP2);

                        lastMousePos = newMousePos;
                    }
                    catch (Exception)
                    {
                    }
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

        private void img_MouseUp(object sender, MouseButtonEventArgs e)
        {
            imageStack.Push(image);
            newPictureOnStack = true;
        }

        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            BitmapEncoder encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(image));

            string tmpPicName = "temp.png";

            using (var fileStream = new FileStream(tmpPicName, FileMode.Create))
            {
                encoder.Save(fileStream);
            }

            // start mspaint
            Process p = new Process();
            p.StartInfo.WorkingDirectory = "C:\\";
            p.StartInfo.FileName = "mspaint";
            p.StartInfo.Arguments = AppDomain.CurrentDomain.BaseDirectory + tmpPicName;
            p.Start();
            Thread.Sleep(1000);

            // delete temp file
            if(File.Exists(AppDomain.CurrentDomain.BaseDirectory + tmpPicName))
            {
                File.Delete(AppDomain.CurrentDomain.BaseDirectory + tmpPicName);
            }
            
        }
    }

}
