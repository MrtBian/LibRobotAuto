using LibRobotAuto.Common;
using LibRobotAuto.Core;
using LibRobotAuto.Module;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace LibRobotAuto
{
    public static class TimingScanModule
    {
        private static System.Timers.Timer timer = new System.Timers.Timer();

        public static int ScanHour;
        public static int ScanMinute;

        static TimingScanModule()
        {
            ScanHour = UserConfig.ScanHour;
            ScanMinute = UserConfig.ScanMinute;
            timer.Enabled = true;
            timer.Interval = 5000;//执行间隔时间,单位为毫秒  
            timer.Elapsed += new System.Timers.ElapsedEventHandler(Timer1_Elapsed);
        }

        public static void StartTimingScan()
        {
            Trace.TraceInformation("TimingScan Starts!");
            timer.Start();
            UserConfig.AutoRunning = true;
            UserConfig.SaveUserConfig();
        }
        public static void StopTimingScan()
        {
            Trace.TraceInformation("TimingScan Stops!");
            timer.Stop();
            UserConfig.AutoRunning = false;
            UserConfig.SaveUserConfig();
        }
        private static void Timer1_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            //Trace.TraceInformation("Timer is running until " + ScanHour+" "+ScanMinute);
            // 得到 hour minute 如果等于某个值就开始执行某个程序。  
            int intHour = e.SignalTime.Hour;
            int intMinute = e.SignalTime.Minute;

            // 设置　固定时间开始执行程序  
            if (intHour == ScanHour && intMinute == ScanMinute && UserConfig.AutoRunning)
            {
                timer.Stop();
                if (UserConfig.RobotRunning)
                {
                    Trace.TraceInformation("The LibRobot is running!");
                    return;
                }
                //MessageBox.Show("盘点开始！", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                Trace.TraceInformation("AutoRunning start!");
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
