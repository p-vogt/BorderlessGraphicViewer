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
using System.Windows.Controls;
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

        private ViewModel viewModel => DataContext as ViewModel;
        public MainWindow(string[] args)
        {
            InitializeComponent();
            foreach (var resourceName in Resources.Keys)
            {
                if (FindResource(resourceName) is ContextMenu contextMenu)
                {
                    contextMenu.DataContext = viewModel;
                }
            }

            viewModel.Init(img);
            ProcessArguments(args);
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
            }
            else
            {
                MessageBox.Show($"{AppDomain.CurrentDomain.FriendlyName}:\nNo file specified (app argument)!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                keepWindowOpen = false;
            }

            if (keepWindowOpen)
            {
                keepWindowOpen = viewModel.LoadImage(filePath);
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

        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            viewModel.SizeChangedCommand.Execute(e);
        }
    }
}
