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
using LibRobotAuto.Core;
using LibRobotAuto.Common;
using System.Threading;

namespace LibRobotAuto
{
    /// <summary>
    /// SettingWindow.xaml 的交互逻辑
    /// </summary>
    public partial class SettingWindow : UserControl
    {
        private int scanHour;
        private int scanMinute;
        public SettingWindow()
        {
            InitializeComponent();

            List<CategoryInfo> categoryList = new List<CategoryInfo>();

            categoryList.Add(new CategoryInfo { Name = "A栋二楼", Value = "A2" });
            categoryList.Add(new CategoryInfo { Name = "A栋三楼", Value = "A3" });
            categoryList.Add(new CategoryInfo { Name = "A栋四楼", Value = "A4" });
            categoryList.Add(new CategoryInfo { Name = "A栋五楼", Value = "A5" });
            CheckBox_Auto.IsChecked = UserConfig.AutoRunning;

            ComboBox_Floor.ItemsSource = categoryList;
            ComboBox_Floor.DisplayMemberPath = "Name";//显示出来的值
            ComboBox_Floor.SelectedValuePath = "Value";//实际选中后获取的结果的值
            ComboBox_Floor.SelectedIndex = UserConfig.FloorDefaultSelectedIndex;
            //Robot.getInstance().ScanFloor = ComboBox_Floor.SelectedValue.ToString();

            List<CategoryInfo> categoryList_Hour = new List<CategoryInfo>();
            List<CategoryInfo> categoryList_Minute = new List<CategoryInfo>();
            for (int i = 0; i < 24; i++)
            {
                categoryList_Hour.Add(new CategoryInfo { Name = i.ToString(), Value = i.ToString() });
            }
            int interval = 5;
            for (int i = 0; i < 60; i += interval)
            {
                categoryList_Minute.Add(new CategoryInfo { Name = string.Format("{0:D2}", i), Value = i.ToString()});
            }

            ComboBox_ScanHour.ItemsSource = categoryList_Hour;
            ComboBox_ScanHour.DisplayMemberPath = "Name";
            ComboBox_ScanHour.SelectedValuePath = "Value";
            ComboBox_ScanHour.SelectedIndex = UserConfig.ScanHour;

            ComboBox_ScanMinute.ItemsSource = categoryList_Minute;
            ComboBox_ScanMinute.DisplayMemberPath = "Name";
            ComboBox_ScanMinute.SelectedValuePath = "Value";
            ComboBox_ScanMinute.SelectedIndex = UserConfig.ScanMinute/interval;
        }

        public class CategoryInfo
        {
            public string Name { get; set; }
            public string Value { get; set; }
        }
        private void ComboBox_ScanHour_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            scanHour = System.Convert.ToInt32(ComboBox_ScanHour.SelectedValue);
            UserConfig.ScanHour = scanHour;
            TimingScanModule.ScanHour = scanHour;
            UserConfig.SaveUserConfig();
        }

        private void ComboBox_ScanMinute_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            scanMinute = System.Convert.ToInt32(ComboBox_ScanMinute.SelectedValue);
            UserConfig.ScanMinute = scanMinute;
            TimingScanModule.ScanMinute = scanMinute;
            UserConfig.SaveUserConfig();
        }

        private void ComboBox_Floor_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Robot.getInstance().ScanFloor = ComboBox_Floor.SelectedValue.ToString();
            UserConfig.FloorDefaultSelectedIndex = ComboBox_Floor.SelectedIndex;
            UserConfig.SaveUserConfig();
        }

        private void CheckBox_Auto_Checked(object sender, RoutedEventArgs e)
        {
            CheckBox_Auto.IsChecked = true;
            TimingScanModule.StartTimingScan();
        }

        private void CheckBox_Auto_Unchecked(object sender, RoutedEventArgs e)
        {
            CheckBox_Auto.IsChecked = false;
            TimingScanModule.StopTimingScan();
        }

        private void Button_Charge_Click(object sender, RoutedEventArgs e)
        {
            Thread thread = new Thread(Robot.getInstance().ExcuteCharge);
            thread.Start();
        }
    }
}
