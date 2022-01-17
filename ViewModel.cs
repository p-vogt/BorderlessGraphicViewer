using GalaSoft.MvvmLight.CommandWpf;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Drawing = System.Drawing;

namespace BorderlessGraphicViewer
{
    public partial class ViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        protected bool SetField<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        private Image windowImage { get; set; }
        public void Init(Image windowImage)
        {
            this.windowImage = windowImage;

            // How can this be improved?
            MouseHook.OnMouseUp += (sender, state) =>
            {
                if (state.IsRightButtonReleased)
                {
                    CancelDrawing();
                }
                else if (isCurrentlyDrawing && state.IsLeftButtonReleased)
                {
                    AddImageToStack();
                    isCurrentlyDrawing = false;
                }
            };
        }

        private double HeightToWidthRatio => (windowImage?.Source?.Height / windowImage?.Source?.Width) ?? 0.0;
        private static readonly Point ResetPosition = new Point(double.MinValue, double.MinValue);

        private bool isNewPictureOnStack = false;

        private readonly Stack<BitmapImage> imageStack = new Stack<BitmapImage>();
        private bool isDrawingCanceled;
        private bool isCurrentlyDrawing;

        private Point lastMousePos = ResetPosition;

        private BitmapImage image;
        private BitmapImage imageWithoutDrawings;

        private double width;
        public double Width
        {
            get => width;
            set => SetField(ref width, value);
        }
        private double height;
        public double Height
        {
            get => height;
            set => SetField(ref height, value);
        }
        private double minWidth;
        public double MinWidth
        {
            get => minWidth;
            set => SetField(ref minWidth, value);
        }
        private double minHeight;
        public double MinHeight
        {
            get => minHeight;
            set => SetField(ref minHeight, value);
        }
        private WindowState windowState = WindowState.Minimized;
        public WindowState WindowState
        {
            get => windowState;
            set => SetField(ref windowState, value);
        }
        private bool isTopmost = true;
        public bool IsTopmost
        {
            get => isTopmost;
            set => SetField(ref isTopmost, value);
        }

        public BitmapImage Image
        {
            get => image;
            set => SetField(ref image, value);
        }
        public BitmapImage ImageWithoutDrawings
        {
            get => imageWithoutDrawings;
            private set
            {
                SetField(ref imageWithoutDrawings, value);
                Image = ImageWithoutDrawings;
                imageStack.Push(Image);
                FitWindowSize();
                WindowState = WindowState.Normal;
            }
        }

        public void UndoDrawing()
        {
            // first time pressing ctrl+z after new pic
            if (isNewPictureOnStack)
            {
                imageStack.Pop();
                isNewPictureOnStack = false;
            }
            if (imageStack.Count > 1)
            {
                Image = imageStack.Pop();
            }
            else
            {
                Image = imageStack.Peek();
            }
        }
        // Conversion code
        internal static BitmapImage ToBitmapImage(BitmapSource bitmapSource)
        {
            JpegBitmapEncoder encoder = new JpegBitmapEncoder();
            MemoryStream memorystream = new MemoryStream();
            BitmapImage tmpImage = new BitmapImage();
            encoder.Frames.Add(BitmapFrame.Create(bitmapSource));
            encoder.Save(memorystream);

            tmpImage.BeginInit();
            tmpImage.StreamSource = new MemoryStream(memorystream.ToArray());
            tmpImage.EndInit();

            memorystream.Close();
            return tmpImage;
        }
        public bool LoadImage(string filePath)
        {
            bool success = false;
            try
            {
                if (File.Exists(filePath))
                {
                    var fileUri = new Uri(filePath, UriKind.Absolute);
                    ImageWithoutDrawings = new BitmapImage(fileUri);
                    success = true;
                }
                else
                {
                    MessageBox.Show($"{AppDomain.CurrentDomain.FriendlyName} :\nImage does not exist: {filePath}!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception)
            {
                MessageBox.Show($"{AppDomain.CurrentDomain.FriendlyName}:\nError loading the image: {filePath}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            return success;
        }

        public bool LoadImageFromClipboard()
        {
            bool success = false;
            if (Clipboard.ContainsImage())
            {
                var source = Clipboard.GetImage();
                ImageWithoutDrawings = ToBitmapImage(source);
                success = true;
            }
            return success;
        }

        public void AddImageToStack()
        {
            imageStack.Push(Image);
            isNewPictureOnStack = true;
        }


        public ICommand StartDrawingCommand => new RelayCommand(() =>
        {
            isDrawingCanceled = false;
            isCurrentlyDrawing = true;
            lastMousePos = ResetPosition;
        });

        public ICommand DrawCommand => new RelayCommand<MouseEventArgs>((e) =>
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                Draw();
            }
        });

        public ICommand ContextMenuCancelDrawingCommand => new RelayCommand(() =>
        {
            if (isCurrentlyDrawing)
            {
                isDrawingCanceled = true;
            }
            else
            {
                windowImage.ContextMenu.IsOpen = true;
                lastMousePos = ResetPosition;
            }
        });

        private void SaveAsImageAsPng(string tmpFilePath)
        {
            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(Image));

            using (var fileStream = new FileStream(tmpFilePath, FileMode.Create))
            {
                encoder.Save(fileStream);
            }
        }
        public ICommand OpenWithPaintCommand => new RelayCommand(() =>
        {
            string tmpPicName = "temp.png";

            string tmpFilePath = AppDomain.CurrentDomain.BaseDirectory + tmpPicName;
            SaveAsImageAsPng(tmpFilePath);

            // start mspaint
            var p = new Process();
            p.StartInfo.WorkingDirectory = "C:\\";
            p.StartInfo.FileName = "mspaint";
            p.StartInfo.Arguments = $"\"{tmpFilePath}\"";
            p.Start();
            Thread.Sleep(1000);

            // delete temp file
            if (File.Exists(tmpFilePath))
            {
                File.Delete(tmpFilePath);
            }

        });
        public ICommand SaveAsPngCommand => new RelayCommand(() =>
        {
            var dlg = new SaveFileDialog
            {
                FileName = "screen_" + DateTime.Now.ToString("yyMMddHHmm"), // Default file name
                DefaultExt = ".png", // Default file extension
                Filter = "Portable Network Graphics (.png)|*.png" // Filter files by extension
            };

            // Show save file dialog box
            bool hasUserPressedYes = dlg.ShowDialog() == true;

            // Process save file dialog box results
            if (hasUserPressedYes)
            {
                // Save document
                SaveAsImageAsPng(dlg.FileName);
            }
        });
        public ICommand CopyToClipboardCommand => new RelayCommand(() => CopyImageToClipboard());
        public ICommand OpenColorPickerCommand => new RelayCommand<MouseButtonEventArgs>((e) =>
        {
            var pos = e.GetPosition(windowImage);
            var widthRatio = Image.Width / windowImage.ActualWidth;
            var heightRatio = Image.Height / windowImage.ActualHeight;
            using (Drawing.Bitmap bmp = BitmapImage2Bitmap(Image))
            {
                int x = (int)(widthRatio * pos.X);
                int y = (int)(heightRatio * pos.Y);
                try
                {
                    var color = bmp.GetPixel(x, y);
                    var win = new ColorWindow(color);
                    win.ShowDialog();
                }
                catch (Exception)
                {
                    MessageBox.Show("Error", "An error occured. Please try again.", MessageBoxButton.OK, MessageBoxImage.Error);

                }

            }
        });
        public ICommand SizeChangedCommand => new RelayCommand<SizeChangedEventArgs>((e) =>
        {
            if (windowImage != null)
            {
                windowImage.Height = double.NaN;
                windowImage.Width = double.NaN;
                if (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift))
                {
                    windowImage.Height = HeightToWidthRatio * Width;
                    SetMinWindowHeight(Width);
                }
                else
                {
                    MinHeight = 0;
                }
            }
        });
        private void FitWindowSize()
        {
            // somehow needs to be called twice for exact sizing
            _FitWindowSize();
            _FitWindowSize();
        }
        private void _FitWindowSize()
        {
            double menuHeight = 0;
            double minWidth = 120;
            double minHeight = minWidth;

            double height = windowImage.Source.Height + menuHeight;
            double width = windowImage.Source.Width;

            if (width < minWidth || height < minHeight)
            {
                if (height > width)
                {
                    // HeightToWidthRatio > 0
                    width = minWidth;
                    height = width * HeightToWidthRatio;
                }
                else
                {
                    // HeightToWidthRatio < 0
                    height = minHeight;
                    width = height / HeightToWidthRatio;
                }
            }

            windowImage.Width = width;
            windowImage.Height = height;
            Width = width;
            Height = height + GetTotalBorderHeight();
            SetMinWindowHeight(width);
            MinWidth = minWidth;
        }
        private static double GetTotalBorderHeight()
        {
            var captionHeight = SystemParameters.WindowCaptionHeight
                                + SystemParameters.ResizeFrameHorizontalBorderHeight;
            var verticalBorderWidth = SystemParameters.ResizeFrameVerticalBorderWidth;
            var bottomBorderHeight = SystemParameters.WindowNonClientFrameThickness.Bottom
                + SystemParameters.ResizeFrameVerticalBorderWidth;
            double totalBorderHeight = captionHeight + verticalBorderWidth + bottomBorderHeight;
            return totalBorderHeight;
        }
        private void SetMinWindowHeight(double imgWidth)
        {
            double totalBorderHeight = GetTotalBorderHeight();
            double height = imgWidth * HeightToWidthRatio;
            MinHeight = height + totalBorderHeight;
        }

        public ICommand KeyDownCommand => new RelayCommand(() =>
        {
            if ((Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl))
                       && Keyboard.IsKeyDown(Key.C))
            {
                CopyImageToClipboard();
            }
            else if (Keyboard.IsKeyDown(Key.F5))
            {
                ResetImage();
                FitWindowSize();
            }
            else if (Keyboard.IsKeyDown(Key.F3))
            {
                IsTopmost = !IsTopmost;
            }
            else if ((Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl))
            && Keyboard.IsKeyDown(Key.Z))
            {
                UndoDrawing();
            }
        });

        private void CopyImageToClipboard()
        {
                if (Image != null)
                {
                    int counter = 0;
                    while (counter <= 10)
                    {
                        try
                        {
                            counter++;
                            ClipboardUtil.CloseClipboard();
                            Clipboard.Clear();
                            Clipboard.SetImage(Image);
                            break;
                        }
                        catch (Exception ex)
                        {
                            if (counter > 10)
                            {
                                var process = ClipboardUtil.ProcessHoldingClipboard();
                                MessageBox.Show($"Could not copy the image to the clipboard. Possibly blocked by: '{process?.ProcessName}'", "error", MessageBoxButton.OK, MessageBoxImage.Error);
                            }
                            else
                            {
                                Thread.Sleep(10);
                            }
                        }
                    }
                }
        }

        private void ResetImage()
        {
            Image = ImageWithoutDrawings;
        }

        private void CancelDrawing()
        {
            Image = imageStack.Peek();
        }

        private void Draw()
        {
            if (!isDrawingCanceled)
            {
                DrawWithMouseOnImage();
            }
        }

        private void DrawWithMouseOnImage()
        {
            if (lastMousePos == ResetPosition)
            {
                lastMousePos = Mouse.GetPosition(windowImage);
            }
            var dv = new DrawingVisual();
            using (var dc = dv.RenderOpen())
            {
                try
                {
                    var converter = new BrushConverter();
                    var brush = (Brush)converter.ConvertFromString("#FF0000");
                    var pen = new Pen
                    {
                        Brush = brush,
                        Thickness = 2
                    };

                    Point newMousePos = Mouse.GetPosition(windowImage);

                    dc.DrawImage(Image, new Rect(0, 0, Image.PixelWidth, Image.PixelHeight));

                    var posTopLeftOfImage = windowImage.TransformToAncestor(windowImage)
                              .Transform(new Point(0, 0));

                    double xOffsetImage = posTopLeftOfImage.X;
                    double yOffsetImage = posTopLeftOfImage.Y;

                    double x1 = Image.Width * ((lastMousePos.X - posTopLeftOfImage.X) / windowImage.ActualWidth);
                    double y1 = Image.Height * ((lastMousePos.Y - yOffsetImage) / windowImage.ActualHeight);

                    double x2 = Image.Width * ((newMousePos.X - posTopLeftOfImage.X) / windowImage.ActualWidth);
                    double y2 = Image.Height * ((newMousePos.Y - yOffsetImage) / windowImage.ActualHeight);

                    Point p1 = new Point(x1, y1);
                    Point p2 = new Point(x2, y2);
                    dc.DrawLine(pen, p1, p2);

                    lastMousePos = newMousePos;
                }
                catch (Exception)
                {
                }
            }
            int dpi = 96;
            var rtb = new RenderTargetBitmap(Image.PixelWidth, Image.PixelHeight, dpi, dpi, PixelFormats.Pbgra32);
            rtb.Render(dv);

            var bitmapEncoder = new PngBitmapEncoder();

            bitmapEncoder.Frames.Add(BitmapFrame.Create(rtb));

            using (var stream = new MemoryStream())
            {
                bitmapEncoder.Save(stream);
                stream.Seek(0, SeekOrigin.Begin);
                var newImage = new BitmapImage();
                newImage.BeginInit();
                newImage.CacheOption = BitmapCacheOption.OnLoad;
                newImage.StreamSource = stream;
                newImage.EndInit();
                Image = newImage;
            }
        }

        private Drawing.Bitmap BitmapImage2Bitmap(BitmapImage bitmapImage)
        {
            using (MemoryStream outStream = new MemoryStream())
            {
                var enc = new BmpBitmapEncoder();
                enc.Frames.Add(BitmapFrame.Create(bitmapImage));
                enc.Save(outStream);
                Drawing.Bitmap bitmap = new Drawing.Bitmap(outStream);

                return new Drawing.Bitmap(bitmap);
            }
        }
    }
}
