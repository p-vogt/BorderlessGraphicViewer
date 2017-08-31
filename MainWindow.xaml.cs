using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace BorderlessGraphicViewer
{
    /// <summary>
    /// Interaktionslogik für MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private const string INTERNAL_CALL_FLAG = "-internalCall";
        private bool newPictureOnStack = false;
        private Stack<BitmapImage> imageStack = new Stack<BitmapImage>();
        private bool isDrawingCanceled;
        private bool isCurrentlyDrawing;
        private BitmapImage image;
        //initial image (without drawings)
        private BitmapImage imageInit;
        public MainWindow(string[] args)
        {
            InitializeComponent();
            string filename = "";
            if (args.Length > 0)
            {
                List<string> argList = new List<string>(args);
                filename = args[0];
                if (args.Length == 1 && !argList.Contains(INTERNAL_CALL_FLAG)) // prevent endless loop
                {
                    // 1.) start a mirrored session 
                    string path = System.Reflection.Assembly.GetExecutingAssembly().CodeBase;
                    Process.Start(path, filename + " " + INTERNAL_CALL_FLAG);
                    // 2.) send "ack" to Greenshot / calling program by closing this exe (return code)
                    Close();
                    return;
                }
            }
            else
            {
#if DEBUG
                string appDirPath = System.Reflection.Assembly.GetEntryAssembly().Location;
                string projectPath = Directory.GetParent(appDirPath).Parent.Parent.FullName;
                filename = projectPath + @"\debug.png";
#else   
                MessageBox.Show(AppDomain.CurrentDomain.FriendlyName + ":\nNo file specified (app argument)!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Close();
                return;
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
                MessageBox.Show(AppDomain.CurrentDomain.FriendlyName + ":\nError loading the image!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Close();
                return;
            }
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if ((Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl))
            && Keyboard.IsKeyDown(Key.C))
            {
                if (image != null)
                {
                    Clipboard.SetImage(image);
                }
            }
            else if (Keyboard.IsKeyDown(Key.F5))
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
                UndoDrawing();
            }
        }

        private void UndoDrawing()
        {
            // first time pressing ctrl+z after new pic
            if (newPictureOnStack)
            {
                imageStack.Pop();
                newPictureOnStack = false;
            }
            if (imageStack.Count > 1)
            {
                image = imageStack.Pop();

            }
            else
            {
                image = imageStack.Peek();
            }
            img.Source = image;
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

        System.Windows.Point lastMousePos = new System.Windows.Point(-1.0, -1.0);
        private void img_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                if (!isDrawingCanceled)
                {
                    DrawWithMouseOnImage();
                }

            }
        }

        private void DrawWithMouseOnImage()
        {
            if (lastMousePos.X == -1.0)
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
            if (e.RightButton == MouseButtonState.Pressed)
            {
                if (isCurrentlyDrawing)
                {
                    isDrawingCanceled = true;
                }
                if (e.LeftButton != MouseButtonState.Pressed)
                {
                    img.ContextMenu.IsOpen = true;
                }
            }
            else
            {
                isDrawingCanceled = false;
                isCurrentlyDrawing = true;
                lastMousePos = new System.Windows.Point(-1.0, -1.0);
            }
        }

        private bool __ignoreOneMouseUp; // see @ usage
        private void img_MouseUp(object sender, MouseButtonEventArgs e)
        {
            imageStack.Push(image);
            newPictureOnStack = true;
            if (isDrawingCanceled)
            {
                // little hack to prevent 2 img to be removed from the stack when releasing left and right button
                // this event gets called twice...
                __ignoreOneMouseUp = !__ignoreOneMouseUp;
                if (__ignoreOneMouseUp)
                {
                    UndoDrawing();
                }
            }
            isCurrentlyDrawing = false;
        }

        private void MenuItemOpenWithPaint_Click(object sender, RoutedEventArgs e)
        {
            string tmpPicName = "temp.png";

            string tmpFilePath = AppDomain.CurrentDomain.BaseDirectory + tmpPicName;
            SaveAsImageAsPng(tmpFilePath);

            // start mspaint
            Process p = new Process();
            p.StartInfo.WorkingDirectory = "C:\\";
            p.StartInfo.FileName = "mspaint";
            p.StartInfo.Arguments = "\"" + tmpFilePath + "\"";
            p.Start();
            Thread.Sleep(1000);

            // delete temp file
            if (File.Exists(tmpFilePath))
            {
                File.Delete(tmpFilePath);
            }

        }

        private void SaveAsImageAsPng(string tmpFilePath)
        {
            BitmapEncoder encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(image));

            using (var fileStream = new FileStream(tmpFilePath, FileMode.Create))
            {
                encoder.Save(fileStream);
            }
        }

        private void MenuItemPng_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog dlg = new SaveFileDialog();
            dlg.FileName = "screen_" + DateTime.Now.ToString("yyMMddHHmm"); // Default file name
            dlg.DefaultExt = ".png"; // Default file extension
            dlg.Filter = "Portable Network Graphics (.png)|*.png"; // Filter files by extension

            // Show save file dialog box
            bool? result = dlg.ShowDialog();

            // Process save file dialog box results
            if (result == true)
            {
                // Save document
                this.SaveAsImageAsPng(dlg.FileName);
            }
        }
    }

}
