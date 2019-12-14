using LibRobotAuto.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Diagnostics;

namespace LibRobotAuto.Module
{
    public class TIPModule
    {
        // 所有扫描得到的书本数据
        private Dictionary<string, Book> books;

        public TIPModule()
        {
            books = new Dictionary<string, Book>();
        }

        public void ClearBooks()
        {
            books.Clear();
        }

        public void SaveRawTagsToFile(List<RawTagInfo> rawTagsFromReader, LibraryRouteLine line, int scanLayer, string scanFloor, string realShelfType)
        {
            int layerCount = UserConfig.GetLayerCount(line.startPoint.PositionNo);
            scanLayer = layerCount - scanLayer;
            if (scanLayer < 0)
            {
                // Remove Data from useless scanning
                return;
            }
            string path = UserConfig.inventoryDatafilePath + "\\" + 
                          scanFloor + "_" + 
                          line.startPoint.PositionNo.Substring(0, 3) + "_" +
                          scanLayer.ToString() + "_" +
                          realShelfType + "_" +
                          line.startShelfNo.ToString() + "_" +
                          line.shelfCount.ToString() + "_" +
                          ((int)line.moveDirection).ToString() + ".dat";

            StreamWriter writer = new StreamWriter(path);
            foreach (RawTagInfo info in rawTagsFromReader)
            {
                writer.WriteLine("{0} {1} {2} {3}", info.time, info.epc, info.rssi, info.antennaPortNum); 
            }
            writer.Flush();
            writer.Close();
        }
    }
}
