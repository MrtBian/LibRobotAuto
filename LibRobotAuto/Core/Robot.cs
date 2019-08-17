#define RUN_ON_ROBOT

using LibRobotAuto.Common;
using LibRobotAuto.Module;
using RobotOperateInterface;
using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Diagnostics;

/// <summary>
/// Robot盘点逻辑
/// </summary>
namespace LibRobotAuto.Core
{
    class Robot
    {
        // Singleton pattern
        private static Robot uniqueInstance = new Robot();

        public static Robot getInstance()
        {
            return uniqueInstance;
        }

        private MobileModule mobilePlatform;
        private ReaderModule upperReader;
        private ReaderModule bottomReader;
        private TIPModule tagInfoProcessor;

        /// <summary>
        /// The floor where the robot is located.
        /// </summary>
        private string scanFloor = "";
        public string ScanFloor
        {
            get { return scanFloor; }
            set
            {
                scanFloor = value;
                mobilePlatform.ChangeMap(ScanFloor);
            }
        }

        /// <summary>
        /// The library floor's route that the robot will follow.
        /// </summary>
        private List<LibraryRouteLine> Route;

        /// <summary>
        /// The route that the robot will follow if scans one shelf.
        /// </summary>
        private Dictionary<string, ShelfRoute> ShelfRoutes;

        private List<string> RemainingShelfs;

        /// <summary>
        /// The original station of robot.
        /// </summary>        
        private MyRobotPosition OriginStation = new MyRobotPosition();

        /// <summary>
        /// The key positions in the library map.
        /// </summary>
        private Dictionary<string, RobotPosition> MapPositions;

        /// <summary>
        /// The constructor.
        /// </summary>
        private Robot()
        {
            Trace.Listeners.Clear();
            Trace.Listeners.Add(new MyTraceListener());
            Trace.AutoFlush = true;

            mobilePlatform = new MobileModule();
            upperReader = new ReaderModule();
            bottomReader = new ReaderModule();
            tagInfoProcessor = new TIPModule();

            Trace.TraceInformation(UserConfig.SchoolCode + " " + UserConfig.RobotNumber + " initial success!");
        }

        private int ConnectReaders()
        {
            if (UserConfig.UpperReaderEnable)
            {
                if (!upperReader.Connect(UserConfig.UpperReaderHostname))
                {
                    MessageBox.Show("RFID阅读器（上）连接失败！", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    return -1;
                }
            }

            if (UserConfig.BottomReaderEnable)
            {
                if (!bottomReader.Connect(UserConfig.BottomReaderHostname))
                {
                    MessageBox.Show("RFID阅读器（下）连接失败！", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    return -1;
                }

            }

            return 0;
        }

        private void DisconnectReaders()
        {
            if (UserConfig.UpperReaderEnable)
            {
                upperReader.Disconnect();
            }
            if (UserConfig.BottomReaderEnable)
            {
                bottomReader.Disconnect();
            }
        }

        private void StartReaders()
        {
            if (UserConfig.UpperReaderEnable)
            {
                upperReader.Start();
            }
            if (UserConfig.BottomReaderEnable)
            {
                bottomReader.Start();
            }
        }

        private void StopReaders()
        {
            if (UserConfig.UpperReaderEnable)
            {
                upperReader.Stop();
            }
            if (UserConfig.BottomReaderEnable)
            {
                bottomReader.Stop();
            }
        }

        public int Connect()
        {
            #if RUN_ON_ROBOT
            if (!mobilePlatform.Connect())
            {
                MessageBox.Show("移动平台正在初始化，请稍后再试！", "警告", MessageBoxButton.OK, MessageBoxImage.Warning);
                UserConfig.RobotRunning = false;
                return -1;
            }

            mobilePlatform.GetMobileCarIndex();

            UserConfig.RobotNumber = mobilePlatform.RobotIndex.ToString();
            UserConfig.UpperReaderHostname = "192.168.2.2" + mobilePlatform.RobotIndex + "0";
            UserConfig.BottomReaderHostname = "192.168.2.2" + mobilePlatform.RobotIndex + "1";
            UserConfig.SaveUserConfig();

            if (ConnectReaders() == -1)
            {
                return -1;
            }

            mobilePlatform.ChangeMap(ScanFloor);
            #endif

            return 0;
        }

        public int Disconnect()
        {
            mobilePlatform.Disconnect();
            DBModule.Disconnect();
            DisconnectReaders();

            return 0;
        }
        /// <summary>
        /// Check power of robot. if power is low, robot charges one hour.
        /// </summary>
        /// <param name="isCharging"></param>
        private void CheckPower(ref bool isCharging)
        {
            int powerNow = mobilePlatform.GetMobileCarPower();
            Trace.TraceInformation("power now : {0}%", powerNow);
            if (powerNow <= 10 && powerNow > 0)
            {
                mobilePlatform.MoveToTargetPosition(OriginStation);
                mobilePlatform.ExecuteCharge(3600);
                isCharging = true;
            }
        }

        public void ExcuteCharge()
        {
            if (UserConfig.RobotRunning)
            {
                MessageBox.Show("请等待盘点任务结束后，再进行操作！", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            else
            {
                //连接机器人
                if (Connect() == -1)
                {
                    return;
                }
                mobilePlatform.ExecuteCharge(0);
            }
        }

        /*
         获取定点对应的坐标信息，mapPositionsFilename文件读取的每行为：点 x坐标 y坐标 角度
         便于后期路线点的寻找
        */
        private void LoadMapPositions(string mapPositionsFilename)
        {
            MapPositions = new Dictionary<string, RobotPosition>();
            StreamReader sr = new StreamReader("MapPositions\\" + mapPositionsFilename, Encoding.Default);
            string line;

            while ((line = sr.ReadLine()) != null)
            {
                if (line == "")
                {
                    continue;
                }

                string positionCode;
                RobotPosition pos = new RobotPosition();
                List<string> temp = new List<string>();
                string[] sArray = line.Split('\t');
                foreach (string str in sArray)
                {
                    if (str.Replace(" ", "") != "")
                    {
                        temp.Add(str.Replace(" ", ""));
                    }
                }

                positionCode = temp[0];
                pos.coorX = (float)System.Convert.ToDouble(temp[1]);
                pos.coorY = (float)System.Convert.ToDouble(temp[2]);
                pos.direciton = (float)System.Convert.ToDouble(temp[3]);

                MapPositions.Add(positionCode, pos);
            }
        }


        /*
         读取每行的route信息， 1 1012 1011 0 50 N00 1 4，提取路线信息调整设置
        */
        private LibraryRouteLine analyzeLibarayRouteLine(string line)
        {
            LibraryRouteLine mapLine = new LibraryRouteLine();
            List<string> temp = new List<string>();
            string startPositionCode,endPositionCode;
            string[] sArray = line.Split(' ');
            foreach (string str in sArray)
            {
                if (str != "")
                {
                    temp.Add(str);
                }
            }

            //路径类型，0：移动，1：扫描，2：充电，3：扫描方式移动
            mapLine.lineType = (MapLineType)System.Convert.ToInt32(temp[0]);
            startPositionCode = temp[1];
            mapLine.startPoint.PositionNo = startPositionCode;
            endPositionCode = temp[2];
            mapLine.endPoint.PositionNo = endPositionCode;
            mapLine.height = (LiftStatus)System.Convert.ToInt32(temp[3]);
            mapLine.distanceToShelf = System.Convert.ToByte(temp[4]);
            mapLine.group = temp[1][0] - '0' + 1;
            mapLine.column = mapLine.group;
            mapLine.row = System.Convert.ToInt32(temp[1].Substring(1, 2));
            //根据奇数排还是偶数拍进行换方向
            if (mapLine.row % 2 == 1)
            {
                if (temp[1][3] < temp[2][3])
                {
                    mapLine.moveDirection = RobotMoveDirection.LEFT_TO_RIGHT;
                }
                else
                {
                    mapLine.moveDirection = RobotMoveDirection.RIGHT_TO_LEFT;
                }
            }
            else
            {
                if (temp[1][3] < temp[2][3])
                {
                    mapLine.moveDirection = RobotMoveDirection.RIGHT_TO_LEFT;
                }
                else
                {
                    mapLine.moveDirection = RobotMoveDirection.LEFT_TO_RIGHT;
                }
            }

            mapLine.shelfType = temp[5];
            mapLine.startShelfNo = System.Convert.ToByte(temp[6]);
            mapLine.shelfCount = System.Convert.ToByte(temp[7]);
            //增加下位机需要信息，书架类型会有区别 防止碰撞
            if(temp.Count > 8)
            {
                mapLine.shelfFlag = System.Convert.ToByte(temp[8]);
            }
            if (mapLine.lineType == MapLineType.Charge)
            {
                return mapLine;
            }
            mapLine.startPoint.Position = MapPositions[startPositionCode];
            mapLine.endPoint.Position = MapPositions[endPositionCode];
            return mapLine;
        }


        /*
         获取整图书馆路线信息，调用上面的analyzeLibarayRouteLine一步一步进行调整
             */
        private void LoadFullRoute(string routeFilename)
        {
            Route = new List<LibraryRouteLine>();
            StreamReader sr = new StreamReader("Routes\\" + routeFilename, Encoding.Default);
            string line;

            while ((line = sr.ReadLine()) != null)
            {
                if (line == "")
                {
                    continue;
                }               

                Route.Add(analyzeLibarayRouteLine(line));
            }
        }


        /*
         shelfRoute为局部定位时使用，规定局部盘点时书架的正反两面都需要扫描
         例如：
            101 1012 3
            1 1012 1011 0 50 N00 1 9
            0 1011 1021 0 50 N00 0 0
            1 1021 1022 0 50 N00 1 9
            
            局部路径每一个书架的导航路径用空行隔开。
            第一行有三个信息，分别为书架编号，中转点和路径数目。
             */
        private void LoadShelfRoute(string shelfRouteFilename)
        {
            ShelfRoutes = new Dictionary<string, ShelfRoute>();
            StreamReader sr = new StreamReader("Routes\\" + shelfRouteFilename, Encoding.Default);
            string line;

            while ((line = sr.ReadLine()) != null)
            {
                if (line == "")
                {
                    continue;
                }

                string[] sArray = line.Split(' ');
                string shelfNo = sArray[0];
                string aisleNo = sArray[1];
                List<string> route = new List<string>();
                int routeLineCount = System.Convert.ToInt32(sArray[2]);
                for (int i = 0; i < routeLineCount; i++)
                {
                    string routeLine = sr.ReadLine();
                    route.Add(routeLine);
                }

                ShelfRoute shelfRoute = new ShelfRoute(shelfNo, aisleNo, route);
                ShelfRoutes.Add(shelfNo, shelfRoute);
            }
        }

        /*
         加载入各种路径和点信息便于之后的导航
             */
        private void LoadMapInfosAndRoute()
        {
            Trace.TraceInformation("load map positions in MapPositions_" + UserConfig.SchoolCode + "_" + ScanFloor + "_v2.txt");
            LoadMapPositions("MapPositions_"+ UserConfig.SchoolCode + "_" + ScanFloor + "_v2.txt");

            Trace.TraceInformation("load route in Route_full_" + UserConfig.SchoolCode + "_" + ScanFloor + ".txt");
            LoadFullRoute("Route_full_" + UserConfig.SchoolCode + "_" + ScanFloor + ".txt");

            Trace.TraceInformation("load shelf route in Route_part_" + UserConfig.SchoolCode + "_" + ScanFloor + ".txt");
            LoadShelfRoute("Route_part_" + UserConfig.SchoolCode + "_" + ScanFloor + ".txt");

            Trace.TraceInformation("route load success!");

            RemainingShelfs = new List<string>();

            OriginStation.PositionNo = "0000";
            OriginStation.Position = MapPositions[OriginStation.PositionNo];
        }

        /// <summary>
        /// Generate an inventory data file directory to store the rfid raw data
        /// </summary>
        /// <param name="inventoryType"></param>
        private void GenerateInventoryDatafileDirectory(string inventoryType)
        {
            //InventoryDatafileDirectory：盘点数据文件夹：A4_2019-07-08_14-34-47_full
            UserConfig.InventoryDatafileDirectory = string.Format("{0}_{1}_{2}", ScanFloor, DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss"), inventoryType);
            //路径，存在云端/Tooker/data/CUHKSZ/1/
            UserConfig.inventoryDatafilePath = Path.Combine(UserConfig.rootPath, "data", UserConfig.InventoryDatafileDirectory);

            Directory.CreateDirectory(UserConfig.inventoryDatafilePath);

        }

        /// <summary>
        /// Generate an file names "end" to show the inventory is finished.
        /// </summary>
        private void GenerateInventoryEndfile()
        {
            if (UserConfig.UploadDataToClound && (UserConfig.UpperReaderEnable || UserConfig.BottomReaderEnable))
            {
                // 此处延迟不能删去
                Thread.Sleep(3000);
                string path = Path.Combine(UserConfig.inventoryDatafilePath, "end");
                FileStream endFile = File.Create(path);
                endFile.Close();
                UserConfig.autoResetEvent.Set();
            }
        }

        /// <summary>
        /// Check inventory data file directory, generate a new one if it's not exist or it's finished.
        /// 先创建存放raw data文件夹
        /// </summary>
        private void CheckInventoryDatafileDirectory()
        {
            if (!Directory.Exists(UserConfig.inventoryDatafilePath))
            {
                UserConfig.StartNewInventory = true;
            }

            if (UserConfig.StartNewInventory)
            {
                GenerateInventoryDatafileDirectory("full");
                //UserConfig.StartNewInventory = false;
                //UserConfig.SaveUserConfig();
            }
        }

        /// <summary>
        /// Execute move command.
        /// </summary>
        /// <param name="startPos">start postion</param>
        /// <param name="endPos">end postion</param>
        /// <param name="isSkipToNextShelf">the flag shows whether should skip to the next shelf or not</param>
        private void ExecuteMoveCommand(MyRobotPosition startPos, MyRobotPosition endPos, ref bool isSkipToNextShelf)
        {
            int MoveResult;

            MoveResult = mobilePlatform.MoveToTargetPosition(endPos);
            if (MoveResult == -1)
            {
                if (mobilePlatform.MoveToTargetPosition(startPos) == -1)
                {
                    isSkipToNextShelf = true;
                    return;
                }

                if (mobilePlatform.MoveToTargetPosition(endPos) == -1)
                {
                    if (mobilePlatform.MoveToTargetPosition(startPos) == -1)
                    {
                        isSkipToNextShelf = true;
                        return;
                    }

                    if (mobilePlatform.MoveToTargetPosition(endPos) == -1)
                    {
                        isSkipToNextShelf = true;
                    }
                }
            }
        }

        /// <summary>
        /// Execute scan command. return to start point if failed, and try again if possible.
        /// </summary>
        /// <param name="line">library route line</param>
        /// <param name="positionBeforeScan">the position before VGA move to this shelf</param>
        /// <param name="isSkipToNextShelf">the flag shows whether should skip to the next shelf or not</param>
        /// <returns>execute result</returns>
        private int ExecuteScanCommand(LibraryRouteLine line, MyRobotPosition positionBeforeScan, ref bool isSkipToNextShelf)
        {
            int ScanResult;

            ScanResult = ScanShelf(line);
            if (ScanResult != 0)
            {
                if (ScanResult == -3)
                {
                    if (mobilePlatform.MoveToTargetPosition(positionBeforeScan) == -1)
                    {
                        isSkipToNextShelf = true;
                        return -1;
                    }

                    if (mobilePlatform.MoveToTargetPosition(line.startPoint) == -1)
                    {
                        isSkipToNextShelf = true;
                        return -1;
                    }

                    ScanResult = ScanShelf(line);
                    if (ScanResult != 0)
                    {
                        mobilePlatform.MoveToTargetPosition(line.startPoint);
                        isSkipToNextShelf = true;
                        return -1;
                    }
                }
                else
                {
                    mobilePlatform.MoveToTargetPosition(line.startPoint);
                    isSkipToNextShelf = true;
                }
            }

            return ScanResult;
        }

        /// <summary>
        /// Scan the shelf and save raw rfid data
        /// </summary>
        /// <param name="line">library route line</param>
        /// <param name="positionBeforeScan">the position before VGA move to this shelf</param>
        /// <param name="isSkipToNextShelf">the flag shows whether should skip to the next shelf or not</param>
        private void ScanShelfAndSaveRawData(LibraryRouteLine line, MyRobotPosition positionBeforeScan, ref bool isSkipToNextShelf)
        {
            if (ExecuteScanCommand(line, positionBeforeScan, ref isSkipToNextShelf) == 0)
            {
                string realShelfType = GetRealShelfType(line.shelfType, line.moveDirection);
                if (UserConfig.UpperReaderEnable)
                {
                    tagInfoProcessor.SaveRawTagsToFile(upperReader.RawTagInfos, line, (int)line.height + 3, ScanFloor, realShelfType);
                    upperReader.ClearTags();
                }

                if (UserConfig.BottomReaderEnable)
                {
                    tagInfoProcessor.SaveRawTagsToFile(bottomReader.RawTagInfos, line, (int)line.height, ScanFloor, realShelfType);
                    bottomReader.ClearTags();
                }

                if (UserConfig.UploadDataToClound && (UserConfig.UpperReaderEnable || UserConfig.BottomReaderEnable))
                {
                    UserConfig.autoResetEvent.Set();
                }
            }
            else
            {
                RemainingShelfs.Add(line.startPoint.PositionNo.Substring(0, 3) + (int)line.height);
                Trace.TraceInformation("add {0} to RemainingShelfs", line.startPoint.PositionNo.Substring(0, 3) + (int)line.height);
            }
        }
        /// <summary>
        /// 根据机器人实际情况做调整
        /// </summary>
        /// <param name="shelfType"></param>
        /// <param name="direction"></param>
        /// <returns></returns>
        private string GetRealShelfType(string shelfType, RobotMoveDirection direction)
        {
            bool enterSidePillarExist = mobilePlatform.EnterSidePillarExist;
            bool leaveSidePillarExist = mobilePlatform.LeaveSidePillarExist;

            if (shelfType[0] != 'D' && shelfType[0] != 'S' && shelfType[0] != 'T')
            {
                return shelfType;
            }

            if (enterSidePillarExist && leaveSidePillarExist)
            {
                return "D11";
            }
            else if ( (enterSidePillarExist && direction == RobotMoveDirection.LEFT_TO_RIGHT) ||
                      (leaveSidePillarExist && direction == RobotMoveDirection.RIGHT_TO_LEFT))
            {
                return shelfType[0] + "L" + shelfType[2];                
            }
            else if ((enterSidePillarExist && direction == RobotMoveDirection.RIGHT_TO_LEFT) ||
                      (leaveSidePillarExist && direction == RobotMoveDirection.LEFT_TO_RIGHT))
            {
                return shelfType[0] + "R" + shelfType[2];
            }
            else
            {
                return "N00";
            }
        }

        /// <summary>
        /// Execute the designed route
        /// </summary>
        private void ExecuteRoute()
        {            
            bool isFirstLine = true;        // flag, shows whether the line now moving is the first line of route or not.
            bool isCharging = false;        // flag, shows whether robot is charging or not.
            bool isSkipToNextShelf = false;     // flag, shows whether should skip to the next shelf or not.
            MyRobotPosition positionBeforeScan = new MyRobotPosition();     // the last key position before the robot move to this shelf.

            ConnectReaders();

            foreach (LibraryRouteLine line in Route)
            {
                //预防机器人第一条命令或者充电之后的命令直接执行扫描命令
                //if first line is scan order or robot executes scan order after charging, it will add an move line which targets start position of scan order.
                if (isFirstLine || isCharging)
                {
                    if (line.lineType == MapLineType.ScanShelf)
                    {
                        ExecuteMoveCommand(OriginStation, line.startPoint, ref isSkipToNextShelf);
                        positionBeforeScan = OriginStation;
                    }
                    isFirstLine = false;
                    isCharging = false;
                }

                if (isSkipToNextShelf)
                {
                    //跳过所有盘点命令，直到遇到移动或充电命令
                    //Skip all scan orders until finding moving or charging order
                    if (line.lineType == MapLineType.Move || line.lineType == MapLineType.Charge)
                    {
                        isSkipToNextShelf = false;
                    }
                    else
                    {
                        if (line.lineType == MapLineType.ScanShelf)
                        {
                            RemainingShelfs.Add(line.startPoint.PositionNo.Substring(0, 3) + (int)line.height);
                            Trace.TraceInformation("add {0} to RemainingShelfs", line.startPoint.PositionNo.Substring(0, 3) + (int)line.height);
                        }
                        continue;
                    }
                }

                if (line.lineType == MapLineType.Move)
                {
                    ExecuteMoveCommand(line.startPoint, line.endPoint, ref isSkipToNextShelf);
                    positionBeforeScan = line.startPoint;
                }
                else if (line.lineType == MapLineType.ScanShelf)
                {
                    ScanShelfAndSaveRawData(line, positionBeforeScan, ref isSkipToNextShelf);
                }
                else if (line.lineType == MapLineType.Charge)
                {
                    int chargeTime = Convert.ToInt32(line.startPoint.PositionNo);
                    mobilePlatform.ExecuteCharge(chargeTime);
                }
                else if (line.lineType == MapLineType.MoveAlongShelf)
                {
                    if(MoveAlongShelfWithoutScan(line) != 0)
                    {
                        isSkipToNextShelf = true;
                    }
                }
                //TODO:防止机器人不在通道内回去充电
                string currentPosition = line.endPoint.PositionNo;
                if (currentPosition == null)
                {
                    continue;
                }
                if (currentPosition.StartsWith("1")){
                    if (currentPosition.EndsWith("1")){
                        continue;
                    }
                }
                else if(line.endPoint.PositionNo.StartsWith("3")){
                    if (currentPosition.EndsWith("2")||currentPosition.EndsWith("4")|| currentPosition.EndsWith("3"))
                    {
                        continue;
                    }
                }
                CheckPower(ref isCharging);
            }

            DisconnectReaders();
        }

        private List<string> FormatShelfs(List<string> shelfs)
        {
            HashSet<String> shelfsSet = new HashSet<string>();

            for (int i = 0; i < shelfs.Count; i++)
            {
                int rowNo = System.Convert.ToInt32(shelfs[i].Substring(1, 2));
                if (rowNo % 2 == 0)
                {
                    rowNo = rowNo - 1;
                }

                if (shelfs[i].Length == 3)
                {
                    //shelfs[i] = string.Format("{0}{1:d2}", shelfs[i][0], rowNo);
                    shelfsSet.Add(string.Format("{0}{1:d2}", shelfs[i][0], rowNo));
                }
                else
                {
                    //shelfs[i] = string.Format("{0}{1:d2}{2}", shelfs[i][0], rowNo, shelfs[i][3]);
                    shelfsSet.Add(string.Format("{0}{1:d2}{2}", shelfs[i][0], rowNo, shelfs[i][3]));
                }
            }

            shelfs = shelfsSet.ToList();
            shelfs.Sort();

            return shelfs;
        }

        private LibraryRouteLine GenerateMoveRouteLine(MyRobotPosition startPos, MyRobotPosition endPos)
        {
            LibraryRouteLine line = new LibraryRouteLine();

            line.lineType = MapLineType.Move;
            line.startPoint = startPos;
            line.endPoint = endPos;
            line.height = LiftStatus.LowLevel;
            line.distanceToShelf = 50;
            line.shelfType = "N00";
            line.startShelfNo = 0;
            line.shelfCount = 0;

            return line;
        }

        /// <summary>
        /// Add a moving line to the route now
        /// </summary>
        /// <param name="RouteNow">store all lines</param>
        /// <param name="target">target position</param>
        /// <param name="positionNow">current position</param>
        /// <returns></returns>
        private MyRobotPosition AddRouteLineMoveToTarget(List<LibraryRouteLine> RouteNow, string target, MyRobotPosition positionNow)
        {
            MyRobotPosition tempPos = new MyRobotPosition();
            LibraryRouteLine tempLine = new LibraryRouteLine();

            tempPos.PositionNo = target;
            tempPos.Position = MapPositions[target];
            tempLine = GenerateMoveRouteLine(positionNow, tempPos);
            RouteNow.Add(tempLine);

            return tempPos;
        }

        /// <summary>
        /// For part inventory.<br>Generate route for all shelfs in one level.
        /// </summary>
        /// <param name="shelfs">all shelfs in part inventory</param>
        /// <returns></returns>
        private List<LibraryRouteLine> GenerateOneLevelRoute(List<string> shelfs)
        {
            List<LibraryRouteLine> tempRoute = new List<LibraryRouteLine>();
            MyRobotPosition positionNow = new MyRobotPosition();
            string aisleNow;

            positionNow.PositionNo = "0000";
            positionNow.Position = MapPositions["0000"];

            positionNow = AddRouteLineMoveToTarget(tempRoute, ShelfRoutes[shelfs[0]].aisleNo, positionNow);
            aisleNow = ShelfRoutes[shelfs[0]].aisleNo;

            for (int i = 0; i < shelfs.Count; i++)
            {
                if (!ShelfRoutes[shelfs[i]].aisleNo.Equals(aisleNow))
                {
                    positionNow = AddRouteLineMoveToTarget(tempRoute, aisleNow, positionNow);
                    positionNow = AddRouteLineMoveToTarget(tempRoute, ShelfRoutes[shelfs[i]].aisleNo, positionNow);
                    aisleNow = ShelfRoutes[shelfs[i]].aisleNo;
                }

                bool isFirstLine = true;
                foreach (string lineStr in ShelfRoutes[shelfs[i]].route)
                {
                    LibraryRouteLine line = analyzeLibarayRouteLine(lineStr);

                    if (isFirstLine)
                    {
                        positionNow = AddRouteLineMoveToTarget(tempRoute, line.startPoint.PositionNo, positionNow);
                        isFirstLine = false;
                    }

                    tempRoute.Add(line);
                    positionNow = line.endPoint;
                }
            }

            positionNow = AddRouteLineMoveToTarget(tempRoute, aisleNow, positionNow);
            positionNow = AddRouteLineMoveToTarget(tempRoute, "0000", positionNow);

            return tempRoute;
        }

        /// <summary>
        /// For part inventory.<br>Generate the whole route for all shelfs.
        /// </summary>
        /// <param name="shelfs"></param>
        private void GeneratePartInventoryRouteAccordingToShelfs(List<string> shelfs)
        {
            shelfs =  FormatShelfs(shelfs);
            Route = new List<LibraryRouteLine>();
            MyRobotPosition positionNow = new MyRobotPosition();
            List<LibraryRouteLine> oneLevelRoute = GenerateOneLevelRoute(shelfs);

            positionNow.PositionNo = "0000";
            positionNow.Position = MapPositions["0000"];

            positionNow = AddRouteLineMoveToTarget(Route, "0000", positionNow);

            foreach (LiftStatus level in Enum.GetValues(typeof(LiftStatus)))
            {
                foreach (LibraryRouteLine line in oneLevelRoute)
                {
                    LibraryRouteLine newLine = LibraryRouteLine.Copy(line);
                    newLine.height = level;
                    Route.Add(newLine);
                }
            }

            LibraryRouteLine chargeLine = new LibraryRouteLine();
            chargeLine.lineType = MapLineType.Charge;
            Route.Add(chargeLine);
        }

        private void GenerateRemainingShelfsInventoryRoute(List<string> shelfs)
        {
            shelfs = FormatShelfs(shelfs);

            Route = new List<LibraryRouteLine>();
            MyRobotPosition positionNow = new MyRobotPosition();
            string aisleNow;

            positionNow.PositionNo = "0000";
            positionNow.Position = MapPositions["0000"];

            positionNow = AddRouteLineMoveToTarget(Route, "0000", positionNow);

            positionNow = AddRouteLineMoveToTarget(Route, ShelfRoutes[shelfs[0].Substring(0, 3)].aisleNo, positionNow);
            aisleNow = ShelfRoutes[shelfs[0].Substring(0, 3)].aisleNo;

            for (int i = 0; i < shelfs.Count; i++)
            {
                string shelfNo = shelfs[i].Substring(0, 3);
                LiftStatus liftStatus = (LiftStatus)Int32.Parse(shelfs[i].Substring(3, 1));

                if (!ShelfRoutes[shelfNo].aisleNo.Equals(aisleNow))
                {
                    positionNow = AddRouteLineMoveToTarget(Route, aisleNow, positionNow);
                    positionNow = AddRouteLineMoveToTarget(Route, ShelfRoutes[shelfNo].aisleNo, positionNow);
                    aisleNow = ShelfRoutes[shelfNo].aisleNo;
                }

                bool isFirstLine = true;
                foreach (string lineStr in ShelfRoutes[shelfNo].route)
                {
                    LibraryRouteLine line = analyzeLibarayRouteLine(lineStr);
                    line.height = liftStatus;

                    if (isFirstLine)
                    {
                        positionNow = AddRouteLineMoveToTarget(Route, line.startPoint.PositionNo, positionNow);
                        isFirstLine = false;
                    }

                    Route.Add(line);
                    positionNow = line.endPoint;
                }
            }

            positionNow = AddRouteLineMoveToTarget(Route, aisleNow, positionNow);
            positionNow = AddRouteLineMoveToTarget(Route, "0000", positionNow);

            LibraryRouteLine chargeLine = new LibraryRouteLine();
            chargeLine.lineType = MapLineType.Charge;
            Route.Add(chargeLine);

            foreach (LibraryRouteLine line in Route)
            {
                System.Console.WriteLine("{0} {1} {2}", line.lineType, line.startPoint.PositionNo, line.endPoint.PositionNo);
            }
        }

        /// <summary>
        /// An full inventory task.
        /// </summary>
        public void Inventory()
        {
            // 连接机器人
            if (Connect() == -1)
            {
                return;
            }
            tagInfoProcessor.ClearBooks();
            CheckInventoryDatafileDirectory();
            LoadMapInfosAndRoute();

            ExecuteRoute();

            //TODO:暂时搁置跳过书架检测
            //while (RemainingShelfs.Count > 0)
            //{
                //MessageBoxResult dr = MessageBox.Show("有部分书架有突出图书，请工作人员整理后按\"确定\"进行遗漏扫描。如果放弃请按取消",
                //    "提示", MessageBoxButton.OKCancel, MessageBoxImage.Information);
                //if (dr == MessageBoxResult.OK)
                //{
                //    GenerateRemainingShelfsInventoryRoute(RemainingShelfs);
                //    RemainingShelfs.Clear();
                //    ExecuteRoute();
                //}
                //else
                //{
                //    break;
                //}
            //}
            GenerateInventoryEndfile();
            string content;
            bool toWHU = false;
            int floor = UserConfig.FloorDefaultSelectedIndex + 2;
            string robotNo = UserConfig.RobotNumber;
            string remainShelfsString="无";
            if (RemainingShelfs.Count > 0)
            {
                toWHU = true;
                remainShelfsString = string.Join(", ", RemainingShelfs.ToArray());
                content = robotNo + "号机器盘点完成！跳过了" + RemainingShelfs.Count + "个书架.跳过书架："+remainShelfsString;
                File.WriteAllText(@"Config_I&Q\RemainShelfs.txt", remainShelfsString);
            }
            else
            {
                toWHU = true;
                content = robotNo + "号机器盘点完成，没有跳过书架！";
            }
            UserConfig.email.trySendEmail(UserConfig.SchoolCode+"-"+ robotNo + ",A"+floor +",盘点完成", content);

            UserConfig.RobotRunning = false;
            UserConfig.StartNewInventory = true;
            UserConfig.SaveUserConfig();
            Trace.TraceInformation("Inventory finished!");
            MessageBox.Show("盘点完成.跳过书架：" + remainShelfsString, "提示", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        public void PartInventory(object obj)
        {

            // 连接机器人
            if (Connect() == -1)
            {
                return;
            }
            List<string> shelfs = (List<string>)obj;

            tagInfoProcessor.ClearBooks();
            GenerateInventoryDatafileDirectory("part");
            UserConfig.StartNewInventory = true;
            UserConfig.SaveUserConfig();

            LoadMapInfosAndRoute();

            GeneratePartInventoryRouteAccordingToShelfs(shelfs);
            ExecuteRoute();

            if (RemainingShelfs.Count > 0)
            {
                MessageBoxResult dr = MessageBox.Show("有部分书架有突出图书，请工作人员整理后按\"确定\"进行遗漏扫描。如果放弃请按取消",
                    "提示", MessageBoxButton.OKCancel, MessageBoxImage.Information);
                if (dr == MessageBoxResult.OK)
                {
                    GenerateRemainingShelfsInventoryRoute(RemainingShelfs);
                    ExecuteRoute();
                }
            }
            
            int floor = UserConfig.FloorDefaultSelectedIndex + 2;
            string robotNo = UserConfig.RobotNumber;
            string content = robotNo + "号机器盘点完成，没有跳过书架！";
            
            UserConfig.email.trySendEmail(UserConfig.SchoolCode + "-" + robotNo + ",A" + floor + ",盘点完成", content);

            GenerateInventoryEndfile();
            UserConfig.RobotRunning = false;
            MessageBox.Show("盘点完成,正在生成报表中,请耐心等待！在拿到报表之前请勿退出软件！！", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            Trace.TraceInformation("Inventory finished!");
        }

        /// <summary>
        /// Move along the shelf without scanning
        /// </summary>
        /// <param name="line">library route line</param>
        /// <returns>execute result</returns>
        private int MoveAlongShelfWithoutScan(LibraryRouteLine line)
        {
            Trace.TraceInformation("move without scanning!");
            mobilePlatform.ScanShelf(line.startPoint, line.endPoint, line.height, line.distanceToShelf, line.shelfType, line.shelfFlag);

            while (!mobilePlatform.AtStartPoint)
            {
                if (mobilePlatform.ScanStartPointAbnormal)
                {
                    return -3;
                }

                Thread.Sleep(100);
            }

            //mobilePlatform.ChangeMissionStatus(0x02, 0x02);

            while (!mobilePlatform.isIdle)
            {
                if (mobilePlatform.AtEndPoint)
                {
                    //mobilePlatform.ChangeMissionStatus(0x02, 0x02);
                }

                if (mobilePlatform.MeetBarrier)
                {
                    mobilePlatform.ReleaseMobileCar();
                    return -1;
                }

                if (mobilePlatform.NavigationFailure)
                {
                    mobilePlatform.ReleaseMobileCar();
                    return -2;
                }

                Thread.Sleep(100);
            }

            return 0;
        }

        /// <summary>
        /// Scan the shelf with readers
        /// </summary>
        /// <param name="line">library route line</param>
        /// <returns>scan result</returns>
        private int ScanShelf(LibraryRouteLine line)
        {
            mobilePlatform.ScanShelf(line.startPoint, line.endPoint, line.height, line.distanceToShelf, line.shelfType, line.shelfFlag);

            while (!mobilePlatform.AtStartPoint)
            {
                if (mobilePlatform.ScanStartPointAbnormal)
                {
                    mobilePlatform.ReleaseMobileCar();
                    return -3;
                }
                //处理进入书架时发生导航失败的情况
                if (mobilePlatform.NavigationFailure)
                {
                    StopReaders();
                    mobilePlatform.ReleaseMobileCar();
                    return -2;
                }
                //处理进入书架时发生遇障的情况
                if (mobilePlatform.MeetBarrier)
                {
                    StopReaders();
                    mobilePlatform.ReleaseMobileCar();
                    return -1;
                }
                //TODO:Timeout处理，防止陷入死循环
                Thread.Sleep(100);
            }

           // mobilePlatform.ChangeMissionStatus(0x02, 0x02);
            Thread.Sleep(ComputeEnterShelfDelayTime());
            StartReaders();

            int scanTime = 0;
            while (!mobilePlatform.isIdle && scanTime < UserConfig.scanShelfTimeout)
            {
                if (mobilePlatform.AtEndPoint)
                {
                    //mobilePlatform.ChangeMissionStatus(0x02, 0x02);
                    Thread.Sleep(ComputeLeaveShelfDelayTime());
                    StopReaders();
                }

                if (mobilePlatform.MeetBarrier)
                {
                    StopReaders();
                    mobilePlatform.ReleaseMobileCar();
                    return -1;
                }

                if (mobilePlatform.NavigationFailure)
                {
                    StopReaders();
                    mobilePlatform.ReleaseMobileCar();
                    return -2;
                }                

                Thread.Sleep(100);
                scanTime += 100;
            }
            
            if (scanTime >= UserConfig.scanShelfTimeout)
            {
                StopReaders();
            }

            return 0;
        }


        /*
         * 这里有些乱，来整理一下
         * 没柱子的时候：
         *      在机器人进入书架之前，机器人一侧与书架相接，整个机身在书架外。从该位置到机器人完全进入书架
         *      需要一段时间，这段时间内读取的数据我们是丢弃的，所以需要设置一个延迟时间，把这段时间内读取
         *      的数据丢弃
         * 有柱子的时候：
         *      可以参照书架类型6，进入侧有柱子，此时机器人就处于书架内部，所以从开始读取的信息即为需要的信息，
         *      因此我们不需要延迟时间，将其设置为0
         */
        private int ComputeEnterShelfDelayTime()
        {
            if (mobilePlatform.EnterSidePillarExist)
            {
                return 0;
            }
            else
            {
                return UserConfig.enterShelfDelayTime;
            }
        }

        private int ComputeLeaveShelfDelayTime()
        {
            if (mobilePlatform.LeaveSidePillarExist)
            {
                return 0;
            }
            else
            {
                return UserConfig.leaveShelfDelayTime;
            }           
        }
    }
}
