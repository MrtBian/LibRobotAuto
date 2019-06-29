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

namespace LibRobotAuto
{
    /// <summary>
    /// BookLocationWindow.xaml 的交互逻辑
    /// </summary>
    public partial class BookLocationWindow : Window
    {
        public BookLocationWindow(int layer, double order, double bookCount)
        {
            InitializeComponent();

            BitmapImage bitmapImage = new BitmapImage();
            bitmapImage.BeginInit();
            bitmapImage.UriSource = new Uri("Resource/book0.png", UriKind.RelativeOrAbsolute);
            bitmapImage.EndInit();
            bitmapImage.Freeze();

            Image image = new Image();
            image.Source = bitmapImage;
            image.Width = 10;
            image.Height = 38;
            image.Stretch = Stretch.Fill;

            image.HorizontalAlignment = HorizontalAlignment.Left;
            image.VerticalAlignment = VerticalAlignment.Top;

            int top = 382;
            switch (layer)
            {
                case 1:
                    top = 322;
                    break;
                case 2:
                    top = 262;
                    break;
                case 3:
                    top = 202;
                    break;
                case 4:
                    top = 142;
                    break;
                case 5:
                    top = 82;
                    break;
                default:
                    break;
            }

            int left = 80;
            if (bookCount > 1)
            {
                left += (int)((600.0 - 80.0) * (order / (bookCount - 1)));
            }

            image.Margin = new Thickness(left, top, 0, 0);
            this.grid.Children.Add(image);
        }
    }
}
