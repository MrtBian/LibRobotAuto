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
using LibRobotAuto.Common;
using LibRobotAuto.Core;
using System.Threading;
using System.Diagnostics;
using LibRobotAuto.Module;

namespace LibRobotAuto
{
    /// <summary>
    /// TimingScanWindow.xaml 的交互逻辑
    /// </summary>
    public partial class TimingScanWindow : Window
    {
        private int scanHour;
        private int scanMinute;
        private System.Timers.Timer timer = new System.Timers.Timer();

        public TimingScanWindow()
        {
            InitializeComponent();

            timer.Enabled = true;
            timer.Interval = 5000;//执行间隔时间,单位为毫秒  
            timer.Elapsed += new System.Timers.ElapsedEventHandler(Timer1_Elapsed);

            List<CategoryInfo> categoryList_Hour = new List<CategoryInfo>();
            List<CategoryInfo> categoryList_Minute = new List<CategoryInfo>();

            for (int i = 0; i < 24; i++)
            {
                categoryList_Hour.Add(new CategoryInfo { Name = i.ToString(), Value = i });
            }

            for (int i = 0; i < 60; i += 15)
            {
                categoryList_Minute.Add(new CategoryInfo { Name = string.Format("{0:D2}", i), Value = i });
            }

            ComboBox_ScanHour.ItemsSource = categoryList_Hour;
            ComboBox_ScanHour.DisplayMemberPath = "Name";
            ComboBox_ScanHour.SelectedValuePath = "Value";
            ComboBox_ScanHour.SelectedIndex = 22;

            ComboBox_ScanMinute.ItemsSource = categoryList_Minute;
            ComboBox_ScanMinute.DisplayMemberPath = "Name";
            ComboBox_ScanMinute.SelectedValuePath = "Value";
            ComboBox_ScanMinute.SelectedIndex = 0;
        }

        public class CategoryInfo
        {
            public string Name { get; set; }
            public int Value { get; set; }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            string message = string.Format("确定要在{0}点{1:D2}分开始盘点吗？", scanHour, scanMinute);
            MessageBoxResult dr = MessageBox.Show(message, "提示", MessageBoxButton.OKCancel, MessageBoxImage.Information);
            if (dr == MessageBoxResult.OK)      //用户选择确认的操作
            {
                timer.Start();
                this.Close();

                //MessageBox.Show(string.Format("盘点任务将于{0}点{1:D2}分开始！", scanHour, scanMinute), "提示");                                
            }
            else if (dr == MessageBoxResult.Cancel)     //用户选择取消的操作
            {
                MessageBox.Show("取消成功");
            }
        }

        private void ComboBox_ScanHour_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            scanHour = System.Convert.ToInt32(ComboBox_ScanHour.SelectedValue);
        }

        private void ComboBox_ScanMinute_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            scanMinute = System.Convert.ToInt32(ComboBox_ScanMinute.SelectedValue);
        }

        private void Timer1_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {

            // 得到 hour minute 如果等于某个值就开始执行某个程序。  
            int intHour = e.SignalTime.Hour;
            int intMinute = e.SignalTime.Minute;            

            // 设置　固定时间开始执行程序  
            if (intHour == scanHour && intMinute == scanMinute)
            {
                timer.Stop();
                if (UserConfig.RobotRunning)
                {
                    Trace.TraceInformation("The LibRobot is running, quit TimingScan!");
                    return;
                }
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
        }
    }
}
