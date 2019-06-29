using LibRobotAuto.Core;
using LibRobotAuto.Common;
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
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Diagnostics;
using System.Threading;

namespace LibRobotAuto
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        ScanWindow scanWindow;
        SettingWindow settingWindow;

        public MainWindow()
        {
            InitializeComponent();

            scanWindow = new ScanWindow();
            settingWindow = new SettingWindow();          
        }

        private void home_Click(object sender, RoutedEventArgs e)
        {
            home.Background = new ImageBrush(new BitmapImage(new Uri("Resource/首页_on.png", UriKind.Relative)));
            scan.Background = new ImageBrush(new BitmapImage(new Uri("Resource/图书盘点.png", UriKind.Relative)));
            setting.Background = new ImageBrush(new BitmapImage(new Uri("Resource/系统设置.png", UriKind.Relative)));
            panel.Children.Clear();
        }

        private void scan_Click(object sender, RoutedEventArgs e)
        {
            home.Background = new ImageBrush(new BitmapImage(new Uri("Resource/首页.png", UriKind.Relative)));
            scan.Background = new ImageBrush(new BitmapImage(new Uri("Resource/图书盘点_on.png", UriKind.Relative)));
            setting.Background = new ImageBrush(new BitmapImage(new Uri("Resource/系统设置.png", UriKind.Relative)));
            this.panel.Children.Clear();
            this.panel.Children.Add(scanWindow);
        }

        private void setting_Click(object sender, RoutedEventArgs e)
        {
            home.Background = new ImageBrush(new BitmapImage(new Uri("Resource/首页.png", UriKind.Relative)));
            scan.Background = new ImageBrush(new BitmapImage(new Uri("Resource/图书盘点.png", UriKind.Relative)));
            setting.Background = new ImageBrush(new BitmapImage(new Uri("Resource/系统设置_on.png", UriKind.Relative)));
            this.panel.Children.Clear();
            settingWindow.CheckBox_Auto.IsChecked = UserConfig.AutoRunning;
            this.panel.Children.Add(settingWindow);
        }

        private void close_Click(object sender, RoutedEventArgs e)
        {
            MessageBoxResult dr = MessageBox.Show("确定退出软件？", "提示", MessageBoxButton.OKCancel, MessageBoxImage.Information);
            if (dr == MessageBoxResult.Cancel)      //用户选择取消的操作
            {
                return;
            }

            Trace.TraceInformation("LibRobotAuto exit!");
            //Robot.getInstance().UpdateBooks();
            UserConfig.RobotRunning = false;
            UserConfig.SaveUserConfig();
            Robot.getInstance().Disconnect();
            this.Close();
            Application.Current.Shutdown();
            Environment.Exit(0);
        }
    }
}
