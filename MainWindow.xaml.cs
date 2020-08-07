using System;
using System.Collections.Generic;
using System.Diagnostics;

using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows;

namespace BorderlessGraphicViewer
{
    /// <summary>
    /// Interaktionslogik für MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
#if DEBUG
        private bool isAppInDebugMode = true;
#else
  private bool isAppInDebugMode = false;
#endif

        private const string INTERNAL_CALL_FLAG = "-internalCall";
        private ViewModel viewModel;

        public MainWindow(string[] args)
        {

            InitializeComponent();
            viewModel = (ViewModel)DataContext;
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

        private static void StartMirroredSession(string filePath)
        {
            string path = Assembly.GetExecutingAssembly().CodeBase;
            Process.Start(path, $"{filePath} {INTERNAL_CALL_FLAG}");
        }
    }
}
