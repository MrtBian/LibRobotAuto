using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using LibRobotAuto.Module;
using LibRobotAuto.Core;
using LibRobotAuto.Common;
using System.Threading;


namespace LibRobotAuto
{
    /// <summary>
    /// PartScanWindow.xaml 的交互逻辑
    /// </summary>
    public partial class PartScanWindow : Window
    {
        public PartScanWindow()
        {
            InitializeComponent();
        }

        private void TextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            TextBox tb = (TextBox)sender;
            tb.Text = string.Empty;
            tb.GotFocus -= TextBox_GotFocus;
        }

        private void Button_Cancel_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void Button_OK_Click(object sender, RoutedEventArgs e)
        {
            List<string> shelfs = new List<string>();
            string[] sArray = Regex.Split(TextBox_Shelfs.Text.Trim(), "\\s+");
            this.Close();
            //MessageBoxResult dr = MessageBox.Show("确定要开始局部盘点吗？", "提示", MessageBoxButton.OKCancel, MessageBoxImage.Information);
            //if (dr == MessageBoxResult.OK)      //用户选择确认的操作
            //{
            //    this.Close();
            //}
            //else if (dr == MessageBoxResult.Cancel)     //用户选择取消的操作
            //{
            //    return;
            //}

            foreach (string shelf in sArray)
            {
                if (Regex.IsMatch(shelf, "^\\d{3}$"))
                {
                    shelfs.Add(shelf);
                }
                else
                {
                    MessageBox.Show("输入有误, 请重新尝试!", "警告", MessageBoxButton.OK, MessageBoxImage.Warning);
                    TextBox_Shelfs.Text = string.Empty;
                    return;
                }
            }

            //MessageBox.Show("盘点开始！", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            Thread thread_partInventory = new Thread(new ParameterizedThreadStart(Robot.getInstance().PartInventory));
            thread_partInventory.Start(shelfs);

            if (UserConfig.UploadDataToClound)
            {
                Thread thread_fileTrans = new Thread(FileTransportModule.Run);
                thread_fileTrans.Start();
            }
            
            UserConfig.RobotRunning = true;
        }
    }
}
