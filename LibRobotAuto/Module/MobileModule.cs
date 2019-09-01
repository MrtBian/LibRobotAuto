using LibRobotAuto.Common;
using RobotOperateInterface;
using System.Threading;
using System.Windows;
using System.Diagnostics;

namespace LibRobotAuto.Module
{
    class MobileModule
    {
        private RobotInterface mobileCar;

        private byte robotIndex = 2;
        public byte RobotIndex
        {
            get { return robotIndex; }
        }

        private bool enterSidePillarExist;      // 扫描书架时进入一侧有柱子
        private bool leaveSidePillarExist;      // 扫描书架时离开一侧有柱子

        public bool EnterSidePillarExist
        {
            get { return enterSidePillarExist; }
        }

        public bool LeaveSidePillarExist
        {
            get { return leaveSidePillarExist; }
        }

        // AGV状态变量
        public bool isIdle;                     // AGV空闲
        private bool atStartPoint;              // AGV在书架起始点，可以开启阅读器进行扫描
        private bool atEndPoint;                // AGV在书架终点，可以关闭阅读器        
        private bool meetBarrier;               // AGV遇到凸出的图书
        private bool navigationFailure;         // AGV导航失败
        private bool scanStartPointAbnormal;    // AGV检测到扫描任务起点异常
        private bool startupNormal;             // AGV开机自定位正常
        private bool isExecutingAutoCharging;   // AGV正在执行自动充电命令
        private bool autoChargingResult;        // AGV执行自动充电命令的结果

        public bool AtStartPoint    
        {
            get { return atStartPoint; }
        }
        
        public bool AtEndPoint
        {
            get { return atEndPoint; }
        }       

        public bool MeetBarrier
        {
            get { return meetBarrier; }
        }

        public bool NavigationFailure
        {
            get { return navigationFailure; }
        }

        public bool ScanStartPointAbnormal
        {
            get { return scanStartPointAbnormal; }
        }

        public bool StartupNormal
        {
            get { return startupNormal; }
        }

        public bool IsConnected
        {
            get { return mobileCar.RobotConnect; }
        }

        public MobileModule()
        {
            mobileCar = RobotInterface.GetInstance();
            mobileCar.RobotNoticeAsyncEvent += new RobotInterface.RobotNoticeAsyncEventHandler(NoticeHandler);
            mobileCar.DisconnectAsyncEvent += new RobotInterface.DisconnectAsyncEventHandler(DisconnectHandler);

            atStartPoint = false;
            atEndPoint = false;
            meetBarrier = false;
            navigationFailure = false;
            scanStartPointAbnormal = false;
            startupNormal = true;

            enterSidePillarExist = false;
            leaveSidePillarExist = false;

            isIdle = true;
            Trace.TraceInformation("mobile car initialize successfully,ready to go!");
        }

        public bool Connect()
        {
            if (mobileCar.ConnectRobot() == 0)
            {
                return true;
            }

            return false;
        }

        public int Disconnect()
        {
            mobileCar.DisconnectRobot();
            return 0;
        }

        public void OccupyMobileCar()
        {
            isIdle = false;
            Trace.TraceInformation("mobile car status change to occupied.");
        }

        public void ReleaseMobileCar()
        {
            isIdle = true;
            Trace.TraceInformation("release mobile car, status change to idle.");
        }

        public void GetMobileCarIndex()
        {
            mobileCar.queryOperation.GetRobotIndex(out robotIndex);
        }

        public void ChangeMap(string floor)
        {
            byte mapIndex = (byte)(floor[floor.Length - 1] - '0');
            mobileCar.basicOperation.SelectMap(mapIndex);
        }

        /// <summary>
        /// 将升降杆将至最低层次扫描
        /// </summary>
        public void FallingLifter()
        {
            OccupyMobileCar();
            mobileCar.basicOperation.ControlRfidHeight(0x00, 0x00);
        }

        /// <summary>
        /// update by wing in 18/11/13
        /// add param 'shelfType'
        /// update by wing in 19/1/9
        /// add param 'flag'
        /// </summary>
        /// <param name="startPoint"></param>
        /// <param name="endPoint"></param>
        /// <param name="status"></param>
        /// <param name="distance"></param>
        /// <param name="shelfType">书架类型</param>
        /// <param name="flag">下位机需要的信息</param>
        public void ScanShelf(MyRobotPosition startPoint, MyRobotPosition endPoint, LiftStatus status, byte distance, string shelfType, byte flag)
        {
            //下位机程序需要的书架信息
            byte shelfFlag = flag;
            string tmp = shelfType.Substring(0, 1);
            if (shelfFlag == 0x01)
            {
                if (tmp.CompareTo("S") == 0 || tmp.CompareTo("T") == 0)
                {
                    shelfFlag = 0x03;
                }
                if (tmp.CompareTo("D") == 0)
                {
                    shelfFlag = 0x02;
                }

                if (UserConfig.FloorDefaultSelectedIndex == 3)
                {
                    //五楼过道过窄，所以1，3两列书架信息均为0x03
                    tmp = startPoint.PositionNo.Substring(0, 1);
                    if (tmp.CompareTo("1") == 0 || tmp.CompareTo("3") == 0)
                    {
                        shelfFlag = 0x03;
                    }
                }
            }

            int result;

            WaitUntilIdle();
            Trace.TraceInformation("starting scan shelf!");

            atStartPoint = false;
            atEndPoint = false;
            meetBarrier = false;
            navigationFailure = false;
            scanStartPointAbnormal = false;

            //Adjust height of lift
            ushort upperScannerHeight;
            ushort bottomScannerHeight;
            switch (status)
            {
                case LiftStatus.HighLevel:
                    upperScannerHeight = UserConfig.FirstLayerAbsoluteHeight;
                    bottomScannerHeight = UserConfig.FourthLayerAbsoluteHeight;
                    break;
                case LiftStatus.MiddleLevel:
                    upperScannerHeight = UserConfig.SecondLayerAbsoluteHeight;
                    bottomScannerHeight = UserConfig.FifthLayerAbsoluteHeight;
                    break;
                default:
                    upperScannerHeight = UserConfig.ThirdLayerAbsoluteHeight;
                    bottomScannerHeight = UserConfig.SixthLayerAbsoluteHeight;
                    break;
            }

            Trace.TraceInformation("send command :scan shelf from {0} to {1}, liftStatus:{2}, distance:{3}", 
                startPoint.PositionNo, endPoint.PositionNo, (int)status, distance);
            result = mobileCar.executeMissionOperation.StartBookshelfScan(startPoint.Position, endPoint.Position, bottomScannerHeight, upperScannerHeight, distance, shelfFlag);
            if (result == 0x00)
            {
                Trace.TraceInformation("execute: scan shelf from {0} to {1}", startPoint.PositionNo, endPoint.PositionNo);
                OccupyMobileCar();
            }
            else if (result == 0x01)
            {
                Trace.TraceInformation("mobile car is occupied, command failed");
            }
            else if (result == 0x02)
            {
                Trace.TraceError("command's parameter error!!!");
            }
            else if (result == -2)
            {
                Trace.TraceInformation("may execute: scan shelf from {0} to {1}", startPoint.PositionNo, endPoint.PositionNo);
                OccupyMobileCar();
            }
            else
            {
                Trace.TraceError("unknown error in sending command（return code:{0}）.",result);
            }
        }

        public int MoveToTargetPosition(MyRobotPosition target)
        {
            int result;
            WaitUntilIdle();
            Trace.TraceInformation("starting move mission!");

            navigationFailure = false;

            Trace.TraceInformation("send command: move to target position {0}", target.PositionNo);
            result = mobileCar.basicOperation.MoveToTargetPosition(target.Position);

            if (result == 0x00)
            {
                Trace.TraceInformation("execute: move to target position {0}", target.PositionNo);
                OccupyMobileCar();
            }
            else if(result == 0x01)
            {
                Trace.TraceInformation("mobile car is occupied, command failed");
                return 1;
            }
            else if(result == 0x02)
            {
                Trace.TraceError("command's parameter error!!!");
                return 1;
            }
            else if (result == -2)
            {
                Trace.TraceInformation("may execute: move to target position {0}", target.PositionNo);
                OccupyMobileCar();
            }
            else
            {
                Trace.TraceError("unknown error in sending command（return code:{0}）.", result);
                return 1;
            }

            while (!isIdle)
            {
                if (NavigationFailure)
                {
                    ReleaseMobileCar();
                    return -1;
                }

                Thread.Sleep(100);
            }

            return 0;
        }

        public void ChangeMissionStatus(byte missionType, byte statusCode)
        {
            mobileCar.executeMissionOperation.ChangeMissionStatus(missionType, statusCode);
        }

        /// <summary>
        /// Execute charge order
        /// </summary>
        /// <param name="chargeTime"></param>
        public void ExecuteCharge(int chargeTime)
        {
            WaitUntilIdle();

            Trace.TraceInformation("execute charging.");
            OccupyMobileCar();

            for (int i = 0; i < UserConfig.autoChargingTryCounts; i++)
            {
                mobileCar.basicOperation.StartCharge();
                isExecutingAutoCharging = true;
                while (isExecutingAutoCharging)
                {
                    Thread.Sleep(100);
                }

                if (autoChargingResult == true)
                {
                    break;
                }
                else if (i == UserConfig.autoChargingTryCounts - 1)
                {
                    Trace.TraceInformation("autocharge failed "+ UserConfig.autoChargingTryCounts + " times");
                    //MessageBox.Show("机器人自动充电失败，请检查自动充电装置是否正常！确认情况正常后按确定继续", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    Trace.TraceInformation("try again!");
                }
            }

            Thread.Sleep(chargeTime * 1000);

            ReleaseMobileCar();
        }

        public RobotPosition GetCurrentPosition()
        {
            RobotPosition position = new RobotPosition();
            mobileCar.queryOperation.GetRobotPosition(out position);
            return position;
        }

        public int GetMobileCarPower()
        {
            float power = 0;

            for (int i = 0; i < 5; i++)
            {
                int result = mobileCar.queryOperation.GetPowerValue(out power);
                if (result == 0)
                {
                    break;
                }
            }

            return (int)power;
        }

        private void DisconnectHandler()
        {
            Trace.TraceWarning("disconnect from the mobile car!");
            Thread.Sleep(3000);
            mobileCar.ConnectRobot();

        }

        private void NoticeHandler(NoticeInfo noticeInfo)
        {
            int floor = UserConfig.FloorDefaultSelectedIndex + 2;
            string robotNo = UserConfig.RobotNumber;
            Trace.TraceInformation("recv NoticeInfo: noticeType={0}, noticeCode={1}", noticeInfo.noticeType, noticeInfo.noticeCode);
            switch (noticeInfo.noticeType)
            {
                case 1:
                    // 行动至指定坐标的执行结果
                    if(noticeInfo.noticeCode == 0)
                    {
                        Trace.TraceInformation("move to target position finished.");
                        ReleaseMobileCar();
                    }
                    else
                    {
                        Trace.TraceError("navigation failure in moving to target position!");
                        navigationFailure = true;
                    }
                    break;
                case 2:
                    // 自动充电任务反馈
                    if(noticeInfo.noticeCode == 0)
                    {
                        Trace.TraceInformation("auto charging success!");
                        isExecutingAutoCharging = false;
                        autoChargingResult = true;
                    }
                    else if(noticeInfo.noticeCode == 1)
                    {
                        Trace.TraceInformation("auto charging failure!");
                        isExecutingAutoCharging = false;
                        autoChargingResult = false;
                        UserConfig.email.trySendEmail(UserConfig.SchoolCode + "-" + robotNo + ",A" + floor + ",自动充电失败!!!", robotNo + "号机器人自动充电失败了！");
                    }
                    break;
                case 3:
                    // 升降杆执行结果 
                    if (noticeInfo.noticeCode == 0)
                    {
                        Trace.TraceInformation("fall lifter finished.");
                        ReleaseMobileCar();
                    }
                    else if (noticeInfo.noticeCode == 1)
                    {
                        Trace.TraceError("fall lifter failed!");
                        ReleaseMobileCar();
                        FallingLifter();
                    }
                    break;
                case 4:
                    // 书架扫面任务执行结果
                    if (noticeInfo.noticeCode == 0)
                    {
                        Trace.TraceInformation("scan shelf finished.");
                        ReleaseMobileCar();
                    }
                    else if (noticeInfo.noticeCode == 1)
                    {
                        Trace.TraceError("navigation failure (start position abnormal) in scanning shelf!");
                        scanStartPointAbnormal = true;
                        ReleaseMobileCar();
                    }
                    else if (noticeInfo.noticeCode == 2)
                    {
                        // 扫描时遇到障碍物
                        Trace.TraceWarning("meet barrier in scanning shelf!");
                        meetBarrier = true;
                    }
                    else
                    {
                        Trace.TraceError("navigation failure (timeout) in scanning shelf!");
                        navigationFailure = true;
                    }
                    break;
                case 5:
                    // 进入或退出书架状态
                    if (noticeInfo.noticeCode == 1)
                    {
                        Trace.TraceInformation("enter the shelf!");
                        atStartPoint = true;
                        enterSidePillarExist = false;
                    }
                    else if (noticeInfo.noticeCode == 2)
                    {
                        Trace.TraceInformation("leave the shelf!");
                        atEndPoint = true;
                        leaveSidePillarExist = false;
                    }
                    else if (noticeInfo.noticeCode == 3)
                    {
                        Trace.TraceInformation("enter the shelf!");
                        atStartPoint = true;
                        enterSidePillarExist = true;
                    }
                    else if (noticeInfo.noticeCode == 4)
                    {
                        Trace.TraceInformation("leave the shelf!");
                        atEndPoint = true;
                        leaveSidePillarExist = true;
                    }
                    break;
                case 6:
                    // 暂不启用                
                    break;
                case 7:
                    // AGV电量过低
                    UserConfig.email.trySendEmail(UserConfig.SchoolCode + "-" + robotNo + ",A" + floor + ",低电量!", RobotIndex + "号机器人低电量");
                    Trace.TraceWarning("low power, under 10%!");                
                    break;
                case 8:
                    UserConfig.email.trySendEmail(UserConfig.SchoolCode + "-" + robotNo + ",A" + floor + ",紧急停止!", RobotIndex + "号机器人急停按钮启动");
                    Trace.TraceWarning("emergency button turn on!");
                    break;
                case 9:
                    OccupyMobileCar();
                    Trace.TraceWarning("chassis driver warning! code: {0}", noticeInfo.noticeCode);
                    break;
                case 10:
                    OccupyMobileCar();
                    Trace.TraceWarning("lifter driver warning! code: {0}", noticeInfo.noticeCode);
                    break;
                case 11:
                    OccupyMobileCar();
                    UserConfig.email.trySendEmail(UserConfig.SchoolCode + "-" + robotNo + ",A" + floor + ",机器人卡死!!!", RobotIndex + "号机器人卡死了！");
                    Trace.TraceWarning("机器人意外停止! code: {0}", noticeInfo.noticeCode);
                    MessageBox.Show("机器人意外停止，请重启盘点软件尝试充电按钮，不行则将机器人移动至充电桩位置后重启！", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                    UserConfig.RobotRunning = false;
                    break;
                case 12:
                    Trace.TraceError("start up localization failure!");
                    startupNormal = false;
                    MessageBox.Show("机器人初始化异常，请将机器人移动至充电桩位置后重启！", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                    break;
                default:
                    break;
            }
        }

        public void WaitUntilIdle()
        {
            while (!isIdle)
            {
                Thread.Sleep(100);
            }
        }
    }
}
