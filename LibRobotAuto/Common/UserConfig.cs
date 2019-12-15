using System;
using System.IO;
using System.Threading;
using System.Windows;
using System.Xml;
using System.Collections.Generic;
using System.Xml.XPath;
using LibRobotAuto.Module;
using LibRobotAuto.Core;
using System.Text.RegularExpressions;
using System.Diagnostics;

namespace LibRobotAuto.Common
{
    public class UserConfig
    {
        public static EmailModule email;
        public static XmlDocument xmlDoc = new XmlDocument();

        // DataBase config
        public static string Server = "114.212.80.13";
        public static string Database = "MiniLibrary";
        public static string Uid = "sa";
        public static string Pwd = "NetLab624";

        // Reader config
        public static string UpperReaderHostname = "192.168.2.220";
        public static string BottomReaderHostname = "192.168.2.221";
        public static double MaxPower = 32.5;
        public static double NormalPower = 28.0;
        public static double MinPower = 23.0;

        public static bool UpperReaderEnable = true;
        public static bool BottomReaderEnable = true;
        public static string ReaderMode = "EPC";

        // Shelves config

        public static Dictionary<string, Dictionary<string,ushort>> ShelvesConfig = new Dictionary<string, Dictionary<string, ushort>>();
        public static ushort LayerCount = 6;
        public static ushort LifterHeightOffset = 0;
        public static ushort BottomLifterBaseHeight = 65;
        public static ushort UpperLifterBaseHeight = 1050;

        public static ushort FirstLayerRelativeHeight = 185;
        public static ushort SecondLayerRelativeHeight = 515;
        public static ushort ThirdLayerRelativeHeight = 810;
        public static ushort FourthLayerRelativeHeight = 190;
        public static ushort FifthLayerRelativeHeight = 520;
        public static ushort SixthLayerRelativeHeight = 800;

        public static ushort SixthLayerAbsoluteHeight;
        public static ushort FifthLayerAbsoluteHeight;
        public static ushort FourthLayerAbsoluteHeight;
        public static ushort ThirdLayerAbsoluteHeight;
        public static ushort SecondLayerAbsoluteHeight;
        public static ushort FirstLayerAbsoluteHeight;

        public static byte DistanceToBookshelf = 40;

        public static long StartTime = DateTime.Now.ToFileTimeUtc();

        // Other config
        public static int FloorDefaultSelectedIndex = 3;        //默认楼层选择框下标
        public static string SchoolCode;                        //学校代号
        public static string RobotNumber;                       //机器人编号
        public static int ChargingTime;                         //充电时间，单位秒    
        public static bool UploadDataToClound = true;           //是否将数据长传云端
        public static bool StartNewInventory;                   //是否开始一次新的盘点任务
        public static string InventoryDatafileDirectory;        //盘点数据文件夹
        public static bool RobotRunning;                        //机器人是否正在运行的标志位
        public static bool AutoRunning = false;                 //机器人是否自动运行
        public static int ScanHour;                             //机器人定时盘点的小时
        public static int ScanMinute;                           //机器人定时盘点的分钟
        public static string remoteIpAndPort;                   //机器人传输的服务器地址

        //email config
        public static bool EnableMail=false;
        public static string FromName="";
        public static string FromPassword="";
        public static List<string> ToList=new List<string>();
        public static string EmailHost="";
        public static int EmailPort=25;


        // 这些参数待加入设置文件，还未加入
        public static int maxFolderCapacity = 10;        
        public static int enterShelfDelayTime = 2000;
        public static int leaveShelfDelayTime = 2000;
        public static int autoChargingTryCounts = 3;
        public static string scanFloor;

        // 扫描书架超时时间，单位毫秒
        public static int scanShelfTimeout = 600000;

        // 设置中楼层
        public static Dictionary<string, string> FloorTextDic = new Dictionary<string, string>();

        //public static bool NEW = false;
        public static AutoResetEvent autoResetEvent = new AutoResetEvent(false);

        public static string rootPath = System.Environment.CurrentDirectory;

        public static string inventoryDatafilePath;

        static UserConfig()
        {
            XmlReaderSettings settings = new XmlReaderSettings();
            settings.IgnoreComments = true;//忽略文档里面的注释            
            XmlReader reader = XmlReader.Create(@"Config_I&Q\UserConfig.xml", settings);

            try
            {
                xmlDoc.Load(reader);
                XmlNode xn = xmlDoc.SelectSingleNode("UserConfig");

                // Get database config
                XmlNode xn1 = xn.ChildNodes.Item(0);
                XmlNodeList xnl = xn1.ChildNodes;
                Server = xnl.Item(0).InnerText.ToString();
                Database = xnl.Item(1).InnerText.ToString();
                Uid = xnl.Item(2).InnerText.ToString();
                Pwd = xnl.Item(3).InnerText.ToString();

                // Get reader config
                xn1 = xn.ChildNodes.Item(1);
                xnl = xn1.ChildNodes;
                UpperReaderHostname = xnl.Item(0).InnerText.ToString();
                BottomReaderHostname = xnl.Item(1).InnerText.ToString();
                MaxPower = System.Convert.ToDouble(xnl.Item(2).InnerText.ToString());
                NormalPower = System.Convert.ToDouble(xnl.Item(3).InnerText.ToString());
                MinPower = System.Convert.ToDouble(xnl.Item(4).InnerText.ToString());
                UpperReaderEnable = (xnl.Item(5).InnerText.ToString() == "True") ? true : false;
                BottomReaderEnable = (xnl.Item(6).InnerText.ToString() == "True") ? true : false;
                ReaderMode = xnl.Item(7).InnerText.ToString();

                // Get shelf config
                xn1 = xn.SelectSingleNode("ShelvesConfig");
                xnl = xn1.ChildNodes;
                int NumOfShelvesConfig = xnl.Count;
                for (int i = 0; i < NumOfShelvesConfig; i++)
                {
                    string regexp = xnl.Item(i).Attributes["regexp"].Value;
                    Dictionary<string, ushort> shelfconfig = new Dictionary<string, ushort>();
                    XmlNode xn2 = xnl.Item(i);
                    XmlNodeList xnl2 = xn2.ChildNodes;
                    int NumOfShelfConfig = xnl2.Count;
                    for (int j = 0; j < NumOfShelfConfig; j++)
                    {
                        string name = xnl2.Item(j).Name;
                        ushort value = Convert.ToUInt16(xnl2.Item(j).InnerText.ToString());
                        shelfconfig.Add(name, value);
                    }
                    ShelvesConfig.Add(regexp, shelfconfig);
                }
                //xn1 = xn.ChildNodes.Item(2);
                //xnl = xn1.ChildNodes;
                //LayerCount = System.Convert.ToUInt16(xnl.Item(0).InnerText.ToString());
                //LifterHeightOffset = System.Convert.ToUInt16(xnl.Item(1).InnerText.ToString());
                //BottomLifterBaseHeight = System.Convert.ToUInt16(xnl.Item(2).InnerText.ToString());
                //UpperLifterBaseHeight = System.Convert.ToUInt16(xnl.Item(3).InnerText.ToString());

                //FirstLayerRelativeHeight = System.Convert.ToUInt16(xnl.Item(4).InnerText.ToString());
                //SecondLayerRelativeHeight = System.Convert.ToUInt16(xnl.Item(5).InnerText.ToString());
                //ThirdLayerRelativeHeight = System.Convert.ToUInt16(xnl.Item(6).InnerText.ToString());
                //FourthLayerRelativeHeight = System.Convert.ToUInt16(xnl.Item(7).InnerText.ToString());
                //FifthLayerRelativeHeight = System.Convert.ToUInt16(xnl.Item(8).InnerText.ToString());
                //SixthLayerRelativeHeight = System.Convert.ToUInt16(xnl.Item(9).InnerText.ToString());
                //SixthLayerAbsoluteHeight = (ushort)(BottomLifterBaseHeight + LifterHeightOffset + FirstLayerRelativeHeight);
                //FifthLayerAbsoluteHeight = (ushort)(BottomLifterBaseHeight + LifterHeightOffset + SecondLayerRelativeHeight);
                //FourthLayerAbsoluteHeight = (ushort)(BottomLifterBaseHeight + LifterHeightOffset + ThirdLayerRelativeHeight);
                //ThirdLayerAbsoluteHeight = (ushort)(UpperLifterBaseHeight + LifterHeightOffset + FourthLayerRelativeHeight);
                //SecondLayerAbsoluteHeight = (ushort)(UpperLifterBaseHeight + LifterHeightOffset + FifthLayerRelativeHeight);
                //FirstLayerAbsoluteHeight = (ushort)(UpperLifterBaseHeight + LifterHeightOffset + SixthLayerRelativeHeight);

                //DistanceToBookshelf = System.Convert.ToByte(xnl.Item(10).InnerText.ToString());

                // Get other config
                xn1 = xn.ChildNodes.Item(3);
                xnl = xn1.ChildNodes;
                FloorDefaultSelectedIndex = Convert.ToInt32(xnl.Item(0).InnerText.ToString());
                SchoolCode = xnl.Item(1).InnerText.ToString();
                RobotNumber = xnl.Item(2).InnerText.ToString();
                ChargingTime = System.Convert.ToInt32(xnl.Item(3).InnerText.ToString());
                UploadDataToClound = (xnl.Item(4).InnerText.ToString() == "True") ? true : false;
                StartNewInventory = (xnl.Item(5).InnerText.ToString() == "True") ? true : false;
                InventoryDatafileDirectory = xnl.Item(6).InnerText.ToString();
                string[] tmp = xnl.Item(7).InnerText.ToString().Split('-');
                if (tmp.Length == 2)
                {
                    ScanHour = System.Convert.ToInt32(tmp[0]);
                    ScanMinute = System.Convert.ToInt32(tmp[1]);
                }
                else
                {
                    ScanHour = 22;
                    ScanMinute = 15;
                }

                inventoryDatafilePath = Path.Combine(UserConfig.rootPath, "data", UserConfig.InventoryDatafileDirectory);
                scanFloor = "A" + (FloorDefaultSelectedIndex + 2).ToString();
                AutoRunning = (xnl.Item(8).InnerText.ToString() == "True") ? true : false;
                remoteIpAndPort = xnl.Item(9).InnerText.ToString();

                //Get email config
                xn1 = xn.ChildNodes.Item(4);
                //xnl = xn.SelectSingleNode("EmailConfig").ChildNodes;
                xnl = xn1.ChildNodes;
                EnableMail = (xnl.Item(0).InnerText.ToString() == "True") ? true : false;
                FromName = xnl.Item(1).InnerText.ToString();
                FromPassword = xnl.Item(2).InnerText.ToString();
                XmlNodeList EmailList = xnl.Item(3).ChildNodes;
                foreach (XmlNode ToNode in EmailList)
                {
                    ToList.Add(ToNode.InnerText.ToString());
                }
                EmailHost = xnl.Item(4).InnerText.ToString();
                EmailPort = System.Convert.ToInt32(xnl.Item(5).InnerText.ToString());

                // Get FloorText config
                xn1 = xn.SelectSingleNode("FloorListConfig");
                xnl = xn1.ChildNodes;
                int NumOfFloor = xnl.Count;
                for (int i = 0; i < NumOfFloor; i++)
                {
                    string name = xnl.Item(i).Attributes["name"].Value;
                    string value = xnl.Item(i).InnerText;
                    FloorTextDic.Add(name, value);
                }
            }
            catch (XmlException e)
            {
                MessageBox.Show("配置文件损坏！", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            finally
            {
                reader.Close();
            }

            email = new EmailModule();
        }

        public static void SetAbsoluteHeight(String startPoint)
        {
            Dictionary<string, ushort> shelfConfig = GetShelfConfig(startPoint);
            shelfConfig.TryGetValue("LifterHeightOffset",out LifterHeightOffset);
            shelfConfig.TryGetValue("BottomLifterBaseHeight", out BottomLifterBaseHeight);
            shelfConfig.TryGetValue("UpperLifterBaseHeight", out UpperLifterBaseHeight);
            shelfConfig.TryGetValue("FirstLayerRelativeHeight", out FirstLayerRelativeHeight);
            shelfConfig.TryGetValue("SecondLayerRelativeHeight", out SecondLayerRelativeHeight);
            shelfConfig.TryGetValue("ThirdLayerRelativeHeight", out ThirdLayerRelativeHeight);
            shelfConfig.TryGetValue("FourthLayerRelativeHeight", out FourthLayerRelativeHeight);
            shelfConfig.TryGetValue("FifthLayerRelativeHeight", out FifthLayerRelativeHeight);
            shelfConfig.TryGetValue("SixthLayerRelativeHeight", out SixthLayerRelativeHeight);
            SixthLayerAbsoluteHeight = (ushort)(BottomLifterBaseHeight + LifterHeightOffset + SixthLayerRelativeHeight);
            FifthLayerAbsoluteHeight = (ushort)(BottomLifterBaseHeight + LifterHeightOffset + FifthLayerRelativeHeight);
            FourthLayerAbsoluteHeight = (ushort)(BottomLifterBaseHeight + LifterHeightOffset + FourthLayerRelativeHeight);
            ThirdLayerAbsoluteHeight = (ushort)(UpperLifterBaseHeight + LifterHeightOffset + ThirdLayerRelativeHeight);
            SecondLayerAbsoluteHeight = (ushort)(UpperLifterBaseHeight + LifterHeightOffset + SecondLayerRelativeHeight);
            FirstLayerAbsoluteHeight = (ushort)(UpperLifterBaseHeight + LifterHeightOffset + FirstLayerRelativeHeight);
        }
        public static int GetLayerCount(String startPoint)
        {
            Dictionary<string, ushort> shelfConfig = GetShelfConfig(startPoint);
            ushort count = 6;
            try
            {
                shelfConfig.TryGetValue("LayerCount",out count);
            }
            catch(Exception e)
            {
                Trace.TraceError(e.Message.ToString());
            }
            return count;
        }
        private static Dictionary<string,ushort> GetShelfConfig(String startPoint)
        {
            Dictionary<string, ushort> firstDic = null;
            foreach(var dic in ShelvesConfig)
            {
                if (firstDic == null)
                {
                    firstDic = dic.Value;
                }
                string regexp = dic.Key;
                Regex rgx = new Regex(regexp);
                if (rgx.IsMatch(startPoint))
                {
                    return dic.Value;
                }
            }
            return firstDic;
        }
        public static void SaveUserConfig()
        {
            XmlNodeList xnl_DatabaseConfig = xmlDoc.SelectSingleNode("UserConfig").ChildNodes.Item(0).ChildNodes;
            XmlNode xn_Server = xnl_DatabaseConfig.Item(0);
            XmlNode xn_Database = xnl_DatabaseConfig.Item(1);
            XmlNode xn_Uid = xnl_DatabaseConfig.Item(2);
            XmlNode xn_Pwd = xnl_DatabaseConfig.Item(3);

            XmlNodeList xnl_ReaderConfig = xmlDoc.SelectSingleNode("UserConfig").ChildNodes.Item(1).ChildNodes;
            XmlNode xn_UpperReaderHostname = xnl_ReaderConfig.Item(0);
            XmlNode xn_BottomReaderHostname = xnl_ReaderConfig.Item(1);
            XmlNode xn_MaxPower = xnl_ReaderConfig.Item(2);
            XmlNode xn_NormalPower = xnl_ReaderConfig.Item(3);
            XmlNode xn_MinPower = xnl_ReaderConfig.Item(4);
            XmlNode xn_UpperReaderEnable = xnl_ReaderConfig.Item(5);
            XmlNode xn_BottomReaderEnable = xnl_ReaderConfig.Item(6);

            //XmlNodeList xnl_ShelfConfig = xmlDoc.SelectSingleNode("UserConfig").ChildNodes.Item(2).ChildNodes;
            //XmlNode xn_LayerCount = xnl_ShelfConfig.Item(0);
            //XmlNode xn_LifterHeightOffset = xnl_ShelfConfig.Item(1);
            //XmlNode xn_BottomLifterBaseHeight = xnl_ShelfConfig.Item(2);
            //XmlNode xn_UpperLifterBaseHeight = xnl_ShelfConfig.Item(3);
            //XmlNode xn_FirstLayerRelativeHeight = xnl_ShelfConfig.Item(4);
            //XmlNode xn_SecondLayerRelativeHeight = xnl_ShelfConfig.Item(5);
            //XmlNode xn_ThirdLayerRelativeHeight = xnl_ShelfConfig.Item(6);
            //XmlNode xn_FourthLayerRelativeHeight = xnl_ShelfConfig.Item(7);
            //XmlNode xn_FifthLayerRelativeHeight = xnl_ShelfConfig.Item(8);
            //XmlNode xn_SixthLayerRelativeHeight = xnl_ShelfConfig.Item(9);
            //XmlNode xn_DistanceToBookshelf = xnl_ShelfConfig.Item(10);

            XmlNodeList xnl_OtherConfig = xmlDoc.SelectSingleNode("UserConfig").ChildNodes.Item(3).ChildNodes;
            XmlNode xn_FloorDefaultSelectedIndex = xnl_OtherConfig.Item(0);
            XmlNode xn_SchoolCode = xnl_OtherConfig.Item(1);
            XmlNode xn_RobotNumber = xnl_OtherConfig.Item(2);
            XmlNode xn_ChargingTime = xnl_OtherConfig.Item(3);
            XmlNode xn_UploadDataToClound = xnl_OtherConfig.Item(4);
            XmlNode xn_StartNewInventory = xnl_OtherConfig.Item(5);
            XmlNode xn_InventoryDatafileDirectory = xnl_OtherConfig.Item(6);
            XmlNode xn_ScanTime = xnl_OtherConfig.Item(7);
            XmlNode xn_AutoScan = xnl_OtherConfig.Item(8);
            XmlNode xn_remoteIpAndPort = xnl_OtherConfig.Item(9);

            XmlNodeList xnl_EmailConfig = xmlDoc.SelectSingleNode("UserConfig").ChildNodes.Item(4).ChildNodes;
            XmlNode xn_EnableEmail = xnl_EmailConfig.Item(0);
            //XmlNode xn_FromName = xnl_EmailConfig.Item(1);
            //XmlNode xn_FromPassword = xnl_EmailConfig.Item(2);
            //XmlNodeList xn_EmailList = xnl_EmailConfig.Item(3).ChildNodes;// ?
            //XmlNode xn_EmailHost = xnl_EmailConfig.Item(4);
            //XmlNode xn_EmailPort = xnl_EmailConfig.Item(5);

            // Save database config
            xn_Server.InnerText = Server;
            xn_Database.InnerText = Database;
            xn_Uid.InnerText = Uid;
            xn_Pwd.InnerText = Pwd;

            // Save reader config
            xn_UpperReaderHostname.InnerText = UpperReaderHostname;
            xn_BottomReaderHostname.InnerText = BottomReaderHostname;
            xn_MaxPower.InnerText = MaxPower.ToString();
            xn_NormalPower.InnerText = NormalPower.ToString();
            xn_MinPower.InnerText = MinPower.ToString();
            xn_UpperReaderEnable.InnerText = UpperReaderEnable.ToString();
            xn_BottomReaderEnable.InnerText = BottomReaderEnable.ToString();

            // Save shelf config
            //xn_LayerCount.InnerText = LayerCount.ToString();
            //xn_LifterHeightOffset.InnerText = LifterHeightOffset.ToString();
            //xn_BottomLifterBaseHeight.InnerText = BottomLifterBaseHeight.ToString();
            //xn_UpperLifterBaseHeight.InnerText = UpperLifterBaseHeight.ToString();
            //xn_FirstLayerRelativeHeight.InnerText = FirstLayerRelativeHeight.ToString();
            //xn_SecondLayerRelativeHeight.InnerText = SecondLayerRelativeHeight.ToString();
            //xn_ThirdLayerRelativeHeight.InnerText = ThirdLayerRelativeHeight.ToString();
            //xn_FourthLayerRelativeHeight.InnerText = FourthLayerRelativeHeight.ToString();
            //xn_FifthLayerRelativeHeight.InnerText = FifthLayerRelativeHeight.ToString();
            //xn_SixthLayerRelativeHeight.InnerText = SixthLayerRelativeHeight.ToString();
            //xn_DistanceToBookshelf.InnerText = DistanceToBookshelf.ToString();

            // Save other config
            xn_FloorDefaultSelectedIndex.InnerText = FloorDefaultSelectedIndex.ToString();
            xn_SchoolCode.InnerText = SchoolCode;
            xn_RobotNumber.InnerText = RobotNumber.ToString();
            xn_ChargingTime.InnerText = ChargingTime.ToString();
            xn_UploadDataToClound.InnerText = UploadDataToClound.ToString();
            xn_StartNewInventory.InnerText = StartNewInventory.ToString();
            xn_InventoryDatafileDirectory.InnerText = InventoryDatafileDirectory;
            xn_ScanTime.InnerText = ScanHour + "-" + ScanMinute;
            xn_AutoScan.InnerText = AutoRunning.ToString();
            xn_remoteIpAndPort.InnerText = remoteIpAndPort;

            //Save email config 
            xn_EnableEmail.InnerText = EnableMail.ToString();
            //xn_FromName.InnerText = FromName;
            //xn_FromPassword.InnerText = FromName;
            
            //xn_EmailHost.InnerText = EmailHost;
            //xn_EmailPort.InnerText = EmailPort.ToString();

            xmlDoc.Save(@"Config_I&Q\UserConfig.xml");
        }
    }

    public enum LiftStatus { LowLevel, MiddleLevel, HighLevel}

    public enum MapLineType { Move, ScanShelf, Charge, MoveAlongShelf }

    public enum RobotMoveDirection { LEFT_TO_RIGHT, RIGHT_TO_LEFT}

    public struct TimeLine
    {
        public ulong Start;
        public ulong End;
    }

    public struct RawTagInfo
    {
        public ulong time;
        public string epc;
        public double rssi;
        public int antennaPortNum;
        public double channelInMhz;
        public int row;
        public int layer;
        public int direction;
    }
}
