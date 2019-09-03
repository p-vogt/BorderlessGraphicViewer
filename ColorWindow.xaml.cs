using System;
using System.Collections.Generic;
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
using System.Windows.Shapes;

namespace BorderlessGraphicViewer
{
    /// <summary>
    /// Interaction logic for ColorWindow.xaml
    /// </summary>
    public partial class ColorWindow : Window
    {

        public ColorWindow(System.Drawing.Color color)
        {
            InitializeComponent();
            colorCanvas.SelectedColor = Color.FromArgb(color.A, color.R, color.G, color.B);

        }

        private static string HexConverter(Color c)
        {
            return "#" + c.R.ToString("X2") + c.G.ToString("X2") + c.B.ToString("X2");
        }

        private static string RGBConverter(Color c)
        {
            return "rgb(" + c.R.ToString() + "," + c.G.ToString() + "," + c.B.ToString() + ")";
        }
        private static string RGBAConverter(Color c)
        {
            return "rgba(" + c.R.ToString() + "," + c.G.ToString() + "," + c.B.ToString() + "," + c.A.ToString() + ")";
        }

        private void BtnCopyHex_Click(object sender, RoutedEventArgs e)
        {
            if (colorCanvas.SelectedColor != null)
            {
                string hexColor = HexConverter((Color)colorCanvas.SelectedColor);
                Clipboard.SetText(hexColor);
            }
        }

        private void BtnCopyRgb_Click(object sender, RoutedEventArgs e)
        {
            if (colorCanvas.SelectedColor != null)
            {
               

                if (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift))
                {
                    string hexColor = RGBAConverter((Color)colorCanvas.SelectedColor);
                    Clipboard.SetText(hexColor);
                }
                else
                {
                    string hexColor = RGBConverter((Color)colorCanvas.SelectedColor);
                    Clipboard.SetText(hexColor);
                }

    
            }
        }
    }
}
