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
using Impinj.OctaneSdk;
using System.Threading;
using System.Diagnostics;
using LibRobotAuto.Module;
using LibRobotAuto.Core;
using LibRobotAuto.Common;

namespace LibRobotAuto
{
    /// <summary>
    /// ScanWindow.xaml 的交互逻辑
    /// </summary>
    public partial class ScanWindow : UserControl
    {
        public ScanWindow()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Inventory start!
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void scan_Click(object sender, RoutedEventArgs e)
        {
            string message = string.Format("即将开始盘点 {0} {1}\n如果楼层有误请取消后在系统设置内修改楼层", UserConfig.SchoolCode, Robot.getInstance().ScanFloor);
            MessageBoxResult dr = MessageBox.Show(message, "提示", MessageBoxButton.OKCancel, MessageBoxImage.Information);
            if (dr == MessageBoxResult.Cancel)      //用户选择取消的操作
            {
                return;
            }

            if (!UserConfig.RobotRunning)
            {
                //MessageBox.Show("盘点开始！", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                Thread thread_inventory = new Thread(Robot.getInstance().Inventory);
                thread_inventory.Start();

                if (UserConfig.UploadDataToClound)
                {
                    Thread thread_fileTrans = new Thread(FileTransportModule.Run);
                    thread_fileTrans.Start();
                }
                UserConfig.RobotRunning = true;
            }
            else
            {
                MessageBox.Show("正在盘点，请稍后再试！", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void timingScan_Click(object sender, RoutedEventArgs e)
        {
            string message = string.Format("即将于 {0}:{1:D2} 在 {2} {3} 启用定时盘点\n如果楼层有误请取消后在系统设置内修改楼层\n定时盘点启动后可在设置中关闭",UserConfig.ScanHour, UserConfig.ScanMinute, UserConfig.SchoolCode, Robot.getInstance().ScanFloor);
            MessageBoxResult dr = MessageBox.Show(message, "提示", MessageBoxButton.OKCancel, MessageBoxImage.Information);
            if (dr == MessageBoxResult.Cancel)      //用户选择取消的操作
            {
                return;
            }

            if (UserConfig.RobotRunning)
            {
                MessageBox.Show("正在盘点，请稍后再试！", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            } 
            else
            {
                //TimingScanWindow timingScanWindow = new TimingScanWindow();
                //timingScanWindow.ShowDialog();
                if (!UserConfig.AutoRunning)
                {
                    TimingScanModule.StartTimingScan();
                }
            }
        }

        private void partScan_Click(object sender, RoutedEventArgs e)
        {
            //string message = string.Format("即将开始局部盘点 {0} {1}，如果楼层有误请取消后在系统设置内修改楼层", UserConfig.SchoolCode, Robot.getInstance().ScanFloor);
            //MessageBoxResult dr = MessageBox.Show(message, "提示", MessageBoxButton.OKCancel, MessageBoxImage.Information);
            //if (dr == MessageBoxResult.Cancel)      //用户选择取消的操作
            //{
                //return;
            //}

            if (UserConfig.RobotRunning)
            {
                MessageBox.Show("正在盘点，请稍后再试！", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                PartScanWindow partScanWindow = new PartScanWindow();
                partScanWindow.ShowDialog();
            }
        }

        private void book_Click(object sender, RoutedEventArgs e)
        {
            //if(!isScanning)
            //{
            //    ResultWindow win = new LibRobotAuto.ResultWindow();
            //    win.ShowDialog();
            //}
            //else
            //{
            //    MessageBox.Show("正在盘点，请稍后再试！", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            //}
            
        }
    }
}
