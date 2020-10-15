using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;

namespace BorderlessGraphicViewer
{
    public class Sketch
    {
        readonly ViewModel viewModel;
        readonly MainWindow window;
        public Sketch(ViewModel viewModel, MainWindow window)
        {
            this.viewModel = viewModel;
            this.window = window;
        }
        WinEventDelegate dele = null;

        delegate void WinEventDelegate(IntPtr hWinEventHook, uint eventType, IntPtr hwnd, int idObject, int idChild, uint dwEventThread, uint dwmsEventTime);
        [DllImport("user32.dll")]
        static extern IntPtr SetWinEventHook(uint eventMin, uint eventMax, IntPtr hmodWinEventProc, WinEventDelegate lpfnWinEventProc, uint idProcess, uint idThread, uint dwFlags);

        private const uint WINEVENT_OUTOFCONTEXT = 0;
        private const uint EVENT_SYSTEM_FOREGROUND = 3;
        readonly uint EVENT_OBJECT_DESTROY = 0x8001;
        readonly int OBJID_WINDOW = 0;
        readonly int INDEXID_CONTAINER = 0;
        [DllImport("user32.dll")]
        static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        static extern int GetWindowText(IntPtr hWnd, StringBuilder text, int count);

        private string GetActiveWindowTitle()
        {
            const int nChars = 256;
            StringBuilder buffer = new StringBuilder(nChars);
            IntPtr handle = GetForegroundWindow();

            if (GetWindowText(handle, buffer, nChars) > 0)
            {
                return buffer.ToString();
            }
            return null;
        }

        private IntPtr screenClipWindowHandle = IntPtr.Zero;
        private bool HadImageInClipboard => imageBefore != null;
        private byte[] imageBefore;
        private readonly System.Timers.Timer timeoutTimer = new System.Timers.Timer(20000);
        private byte[] BitmapSourceToArray(BitmapSource bitmapSource)
        {
            // Stride = (width) x (bytes per pixel)
            int stride = (int)bitmapSource.PixelWidth * (bitmapSource.Format.BitsPerPixel / 8);
            byte[] pixels = new byte[(int)bitmapSource.PixelHeight * stride];

            bitmapSource.CopyPixels(pixels, stride, 0);

            return pixels;
        }
        public void WinEventProc(IntPtr hWinEventHook, uint eventType, IntPtr hwnd, int idObject, int idChild, uint dwEventThread, uint dwmsEventTime)
        {

            if (eventType == EVENT_SYSTEM_FOREGROUND)
            {
                var title = GetActiveWindowTitle();
                if (title != null)
                {
                    if (screenClipWindowHandle == IntPtr.Zero)
                    {
                        screenClipWindowHandle = hwnd;
                        timeoutTimer.Start();
                    }


                }
            }
            else if (eventType == EVENT_OBJECT_DESTROY &&
                 hwnd == screenClipWindowHandle &&
                 idObject == OBJID_WINDOW &&
                 idChild == INDEXID_CONTAINER)
            {
                screenClipWindowHandle = IntPtr.Zero;
                if (!Clipboard.ContainsImage())
                {
                    return;
                }
                else if (this.HadImageInClipboard)
                {
                    var image = Clipboard.GetImage();
                    var isEqual = BitmapSourceToArray(image).SequenceEqual(imageBefore);
                    if (isEqual)
                    {
                        //return;
                    }
                }
                if (timeoutTimer.Enabled)
                {
                    timeoutTimer.Stop();
                    ScreenClipExited();
                }
            }

        }

        public void Start()
        {
            dele = new WinEventDelegate(WinEventProc);
            SetWinEventHook(EVENT_SYSTEM_FOREGROUND, EVENT_OBJECT_DESTROY, IntPtr.Zero, dele, 0, 0, WINEVENT_OUTOFCONTEXT);
            imageBefore = null;
            if (Clipboard.ContainsImage())
            {
                imageBefore = BitmapSourceToArray(Clipboard.GetImage());
            }
            var info = new ProcessStartInfo
            {
                FileName = "ms-screenclip:"
            };
            var process = new Process
            {
                StartInfo = info,
                EnableRaisingEvents = true
            };
            process.Start();
        }
        private void ScreenClipExited()
        {
            bool hasNewClipboardContent = true;


            if (hasNewClipboardContent)
            {
                viewModel.LoadImageFromClipboard();
            }
            else
            {
                window.Close();
            }
        }
        

    }
}
