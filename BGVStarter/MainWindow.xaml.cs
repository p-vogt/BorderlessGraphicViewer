using System;
using System.Collections.Generic;
using System.Diagnostics;
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

namespace BGVStarter
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            new HotKey(Key.PrintScreen, KeyModifier.None, (_) =>
            {
                string path = Assembly.GetExecutingAssembly().CodeBase;
                path = @"D:\Repos\BorderlessGraphicViewer\bin\Debug\BorderlessGraphicViewer.exe";
                Process.Start(path);
            }, true);
        }
    }
}
