using Impinj.OctaneSdk;
using LibRobotAuto.Common;
using System;
using System.IO;
using System.Collections.Generic;
using System.Windows;

namespace LibRobotAuto.Module
{
    class ReaderModule
    {
        // 英频杰阅读器
        private ImpinjReader impinjReader;
        private string hostName;

        // 原始标签数据
        private List<RawTagInfo> rawTagInfos;
        public List<RawTagInfo> RawTagInfos
        {
            get { return rawTagInfos; }
        }

        // 标签数据
        private Dictionary<string, BookTag> tags;
        public Dictionary<string, BookTag> Tags
        {
            get { return tags; }
        }

        // 构造函数
        public ReaderModule()
        {
            impinjReader = new ImpinjReader();
            tags = new Dictionary<string, BookTag>();
            rawTagInfos = new List<RawTagInfo>();
        }

        // 连接阅读器
        public bool Connect(string hostname)
        {
            hostName = hostname;
            if (!impinjReader.IsConnected)
            {
                try
                {
                    impinjReader.Connect(hostname);
                    ConfigSettings(hostname);
                    impinjReader.TagsReported += OnTagsReported;
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                    return false;
                }
            }

            return true;
        }

        ////针对四天线情况重载connect
        //public bool Connect(string hostname, bool[] port)
        //{
        //    hostName = hostname;
        //    if (!impinjReader.IsConnected)
        //    {
        //        try
        //        {
        //            impinjReader.Connect(hostname);
        //            ConfigSettings(hostname, port);
        //            //impinjReader.TagsReported += OnTagsReported;  //多个监听数据冗余
        //        }
        //        catch (Exception ex)
        //        {
        //            MessageBox.Show(ex.Message);
        //            return false;
        //        }
        //    }

        //    return true;
        //}

        // 断开阅读器
        public void Disconnect()
        {
            if (impinjReader.IsConnected)
            {
                //impinjReader.; //todo
                impinjReader.Disconnect();
            }
        }

        // 配置阅读器
        public void ConfigSettings(string hostname)
        {
            Settings settings = impinjReader.QueryDefaultSettings();

            // 常规
            settings.Report.IncludePeakRssi = true;
            settings.Report.IncludePhaseAngle = false;
            settings.Report.IncludeFirstSeenTime = true;
            settings.Report.IncludeFastId = true;
            settings.Report.IncludeAntennaPortNumber = true;
            //settings.Report.IncludeChannel = true;
            settings.Report.Mode = ReportMode.Individual;

            settings.ReaderMode = ReaderMode.MaxThroughput;
            settings.SearchMode = SearchMode.DualTarget;
            settings.TagPopulationEstimate = 128;
            settings.Session = 1;

            if (hostname.Equals(UserConfig.UpperReaderHostname))
            {
                //settings.TxFrequenciesInMhz.Add(920.625);
                //settings.TxFrequenciesInMhz.Add(920.875);
                //settings.TxFrequenciesInMhz.Add(921.125);
                //settings.TxFrequenciesInMhz.Add(921.375);
                settings.TxFrequenciesInMhz.Add(921.625);
                //settings.TxFrequenciesInMhz.Add(921.875);
            }
            else
            {
                //settings.TxFrequenciesInMhz.Add(920.625);
                //settings.TxFrequenciesInMhz.Add(923.125);
                //settings.TxFrequenciesInMhz.Add(923.375);
                settings.TxFrequenciesInMhz.Add(923.625);
                //settings.TxFrequenciesInMhz.Add(923.875);
                //settings.TxFrequenciesInMhz.Add(924.125);
                //settings.TxFrequenciesInMhz.Add(924.375);
            }

            // 天线
            //List<ushort> antennaPorts = new List<ushort>();
            //antennaPorts.Add(2);
            settings.Antennas.EnableAll();
            //settings.Antennas.DisableAll();
            //settings.Antennas.EnableById(antennaPorts);

            settings.Antennas.TxPowerInDbm = UserConfig.MaxPower;
            settings.Antennas.RxSensitivityMax = true;

            settings.Save("Config_I&Q\\ReaderSettings.json");
            impinjReader.ApplySettings(settings);
        }

        //重载  用于解决不开所有天线端口的情况
        //public void ConfigSettings(string hostname,bool[] port)
        //{
        //    Settings settings = impinjReader.QueryDefaultSettings();

        //    // 常规
        //    settings.Report.IncludePeakRssi = true;
        //    settings.Report.IncludePhaseAngle = false;
        //    settings.Report.IncludeFirstSeenTime = true;
        //    settings.Report.IncludeFastId = false;
        //    settings.Report.IncludeAntennaPortNumber = true;
        //    //settings.Report.IncludeChannel = true;
        //    settings.Report.Mode = ReportMode.Individual;

        //    settings.ReaderMode = ReaderMode.MaxThroughput;
        //    settings.SearchMode = SearchMode.DualTarget;
        //    settings.TagPopulationEstimate = 128;
        //    settings.Session = 1;

        //    if (hostname.Equals(UserConfig.UpperReaderHostname))
        //    {
        //        //settings.TxFrequenciesInMhz.Add(920.625);
        //        //settings.TxFrequenciesInMhz.Add(920.875);
        //        //settings.TxFrequenciesInMhz.Add(921.125);
        //        //settings.TxFrequenciesInMhz.Add(921.375);
        //        settings.TxFrequenciesInMhz.Add(921.625);
        //        //settings.TxFrequenciesInMhz.Add(921.875);
        //    }
        //    else
        //    {
        //        //settings.TxFrequenciesInMhz.Add(920.625);
        //        //settings.TxFrequenciesInMhz.Add(923.125);
        //        //settings.TxFrequenciesInMhz.Add(923.375);
        //        settings.TxFrequenciesInMhz.Add(923.625);
        //        //settings.TxFrequenciesInMhz.Add(923.875);
        //        //settings.TxFrequenciesInMhz.Add(924.125);
        //        //settings.TxFrequenciesInMhz.Add(924.375);
        //    }

        //    // 天线
        //    List<ushort> antennaPorts = new List<ushort>();
        //    for (int i=0;i<4;i++)
        //    {
        //        if (port[i])
        //            antennaPorts.Add((ushort)(i+1));
        //    }
        //    //antennaPorts.Add(2);
        //    //settings.Antennas.EnableAll();
        //    settings.Antennas.DisableAll();
        //    settings.Antennas.EnableById(antennaPorts);

        //    settings.Antennas.TxPowerInDbm = UserConfig.MaxPower;
        //    settings.Antennas.RxSensitivityMax = true;

        //    settings.Save("Config_I&Q\\ReaderSettings.json");
        //    impinjReader.ApplySettings(settings);
        //}

        public void ClearTags()
        {
            tags.Clear();
        }

        // 开始扫描
        public void Start()
        {
          
            if (impinjReader.IsConnected)
            {
                tags.Clear();
                rawTagInfos.Clear();
                impinjReader.Start();
            }
            else
            {
                MessageBox.Show("阅读器未连接...");
            }
        }

        // 停止扫描
        public void Stop()
        {
            if (impinjReader.IsConnected)
            {
                impinjReader.Stop();
            }
            else
            {
                MessageBox.Show("阅读器未连接...");
            }
        }

        public void OutputRawTagInfo(Tag tag)
        {
            RawTagInfo temp = new RawTagInfo();

            temp.time = tag.FirstSeenTime.Utc;
            temp.epc = tag.Tid.ToHexString();
            temp.rssi = tag.PeakRssiInDbm;
            temp.antennaPortNum = tag.AntennaPortNumber;

            rawTagInfos.Add(temp);
        }

        // 读标签响应函数
        public void OnTagsReported(ImpinjReader sender, TagReport report)
        {
            foreach (Tag tag in report)
            {
                OutputRawTagInfo(tag);
            }
        }
    }
}
