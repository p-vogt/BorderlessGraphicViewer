using System;
using System.Diagnostics;

using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;

namespace BorderlessGraphicViewer
{
    /// <summary>
    /// Interaktionslogik für MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
#if DEBUG
        private readonly bool isAppInDebugMode = true;
#else
  private readonly bool isAppInDebugMode = false;
#endif

        private const string INTERNAL_CALL_FLAG = "-internalCall";
        private readonly ViewModel viewModel;

        public MainWindow(string[] args)
        {
            InitializeComponent();
            viewModel = (ViewModel)DataContext;
            viewModel.Init(img);
            ProcessArguments(args);
            ScreenSketch();
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

        IntPtr screenClipWindowHandle = IntPtr.Zero;
        private bool hadImageInClipboard => imageBefore != null;
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
                else if (this.hadImageInClipboard)
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

        private void ScreenSketch()
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
                Close();
            }
        }
        private void ProcessArguments(string[] args)
        {
            if (isAppInDebugMode)
            {
                args = HandleDebugArgs(args);
            }

            bool keepWindowOpen = true;
            string filePath = "";

            if (args.Length > 0)
            {
                filePath = args[0];
                if (args.Length == 1
                    && !isAppInDebugMode)
                {
                    StartMirroredSession(filePath);
                    // close window to send "ack" to Greenshot
                    keepWindowOpen = false;
                }
            }
            else
            {
                MessageBox.Show($"{AppDomain.CurrentDomain.FriendlyName}:\nNo file specified (app argument)!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                keepWindowOpen = false;
            }

            if (keepWindowOpen)
            {
                //keepWindowOpen = viewModel.LoadImage(filePath);
            }
            if (!keepWindowOpen)
            {
                Close();
            }
        }

        private static string[] HandleDebugArgs(string[] args)
        {
            string[] newArgs = args;
            if (args?.Length == 0)
            {
                string appDirPath = Assembly.GetEntryAssembly().Location;
                string projectPath = Directory.GetParent(appDirPath).Parent.Parent.FullName;
                newArgs = new string[] { $@"{projectPath}\debug_10to1.png" };
            }
            return newArgs;
        }

        private static void StartMirroredSession(string filePath)
        {
            string path = Assembly.GetExecutingAssembly().CodeBase;
            Process.Start(path, $"{filePath} {INTERNAL_CALL_FLAG}");
        }

        private void window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            viewModel.SizeChangedCommand.Execute(e);
        }
    }
}
