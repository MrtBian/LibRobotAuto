using LibRobotAuto.Common;
using LibRobotAuto.Core;
using LibRobotAuto.Module;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Threading;
using System.Runtime.InteropServices;
using System.IO;
using RobotOperateInterface;

namespace LibRobotAuto
{
    public static class ProgramEntrance
    {
        private static string REPORT_END_PATH = "Report\\REPORT_END";
        //private static System.Timers.Timer timer = new System.Timers.Timer();
        //锁屏
        [DllImport("user32.dll")]
        public static extern bool LockWorkStation();

        private static void LockScreen()
        {
            Thread.Sleep(20000);
            if (UserConfig.AutoRunning)
            {
                LockWorkStation();
            }
        }
        /// <summary>
        /// 报表是否生成完毕
        /// </summary>
        private static void CheckReportEnd()
        {
            if (File.Exists(REPORT_END_PATH))
            {
                File.Delete(REPORT_END_PATH);
            }
            int interval = 5000;
            while (true)
            {
                if (UserConfig.RobotRunning)
                {
                    Thread.Sleep(interval * 100);
                    if (File.Exists(REPORT_END_PATH))
                    {
                        Trace.TraceInformation("删除上次冗余的关机标志");
                        File.Delete(REPORT_END_PATH);
                    }
                }
                else if (!File.Exists(REPORT_END_PATH))
                {
                    Thread.Sleep(interval);
                }
                else
                {
                    TimeSpan ts = new DateTime() - File.GetCreationTime(REPORT_END_PATH);
                    if (ts.TotalSeconds <= interval * 100)
                    {
                        break;
                    }
                    if (File.Exists(REPORT_END_PATH))
                    {
                        Trace.TraceInformation("删除上次冗余的关机标志");
                        File.Delete(REPORT_END_PATH);
                    }
                }
            }
            Trace.TraceInformation("报表已经发送，准备关机！！");
            RobotInterface robotInterface = RobotInterface.GetInstance();
            int ret = robotInterface.ConnectRobot();
            if (ret != 0)
            {
                Trace.TraceError("连接机器人失败！重试中...");
                if (robotInterface.ConnectRobot() != 0)
                {
                    Trace.TraceError("连接机器人失败！");
                    return;
                }
            }
            ret = robotInterface.basicOperation.ShutDown();
            if (ret != 0)
            {
                Trace.TraceError("关机失败！");
                return;
            }
            Trace.TraceInformation("关机中...");
        }
       
        /// <summary>
        /// Application Entry Point.
        /// </summary>
        [STAThread]
        public static void Main()
        {

            if (UserConfig.AutoRunning)
            {
                // 用户自定义代码段-开始
                TimingScanModule.StartTimingScan();
                // 用户自定义代码段-结束
                //如果自动运行，则自动锁屏
                Thread thread = new Thread(LockScreen);
                thread.Start();
            }
            Thread threadShutDown = new Thread(CheckReportEnd); ;
            threadShutDown.Start();
            LibRobotAuto.App app = new LibRobotAuto.App();//WPF项目的Application实例，用来启动WPF项目的
            app.InitializeComponent();
            //MainWindow windows = new MainWindow();
            //app.MainWindow = windows;
            app.Run();
        }
    }
}
