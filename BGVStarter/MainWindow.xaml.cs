﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
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
using WpfTrayIcon;

namespace BGVStarter
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private TrayIcon notifyIcon;
        public MainWindow()
        {
            InitializeComponent();
            notifyIcon = new TrayIcon(this, true, Properties.Resources.viewer_image);
            notifyIcon.TrayText = "Bordlerless Graphic Viewer Starter";
            notifyIcon.Visible = true;
            WindowState = WindowState.Minimized;
            Visibility = Visibility.Hidden;
            StartWatching();

        }

        private ConcurrentQueue<string> ImagePaths = new ConcurrentQueue<string>();
        private void OnChanged(object sender, FileSystemEventArgs e)
        {
            ImagePaths.Enqueue(e.FullPath);
            if (ImagePaths.Count() == 2)
            {
                var paths = ImagePaths.ToList();
                ImagePaths = new ConcurrentQueue<string>();
                try
                {

                    var fileInfo = paths
                        .Select(x => new FileInfo(x))
                        .OrderByDescending(info => info.Length)
                        .First();
                    var imageFile = fileInfo.FullName;
                    var assembly = Assembly.GetExecutingAssembly();
                    string codeBasePath = assembly.CodeBase;
                    var codeBaseDirectory = System.IO.Path.GetDirectoryName(codeBasePath);
                    var preText = @"file:\";
                    var path = codeBaseDirectory + "BorderlessGraphicViewer.exe";
                    path = path.TrimStart(preText.ToCharArray());
                    Dispatcher.Invoke(() =>
                    {
                        var bgvWindow = new BorderlessGraphicViewer.MainWindow(new string[] { imageFile });

                        bgvWindow.Show();
                    });
                }
                catch (Exception ex)
                {

                }

            }
            // get Image from clipboard
        }
        List<FileSystemWatcher> fileSystemWatchers = new List<FileSystemWatcher>();
        private void StartWatching()
        {
            string appDataFolder = Environment.GetEnvironmentVariable("LocalAppData");
            string packagesFolderPath = appDataFolder + @"\Packages\";
            var taskDirectory = new DirectoryInfo(packagesFolderPath);
            var packageDirs = taskDirectory.GetDirectories();
            var shellExperienceDirs = packageDirs.Where(dir => dir.Name.StartsWith("Microsoft.Windows.ShellExperienceHost_"));

            foreach (var dir in shellExperienceDirs)
            {
                string screenclipFolderPath = dir.FullName + @"\TempState\ScreenClip";
                if (Directory.Exists(screenclipFolderPath))
                {

                    var watcher = new FileSystemWatcher
                    {
                        Path = screenclipFolderPath,
                        Filter = "*.png",
                        EnableRaisingEvents = true
                    };
                    watcher.Created += OnChanged;
                    fileSystemWatchers.Add(watcher);
                }
            }
        }
    }
}
