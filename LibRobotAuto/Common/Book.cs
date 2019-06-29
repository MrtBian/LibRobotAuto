namespace LibRobotAuto.Common
{
    public class Book
    {
        public string barcode;          // 图书号
        public string title;            // 书名
        public bool IsLoaned;           // 是否可借

        public ulong ScanTimeStamp;      // 扫描平均时间戳
        public ulong ScanTimeDuration;   // 扫描持续时间
        public int ScanCount;           // 扫描次数
        public double MaxRSSI;          // 最大信号强度
        public double MinRSSI;          // 最小信号强度
        public double AvgRSSI;          // 平均信号强度
        public double Probability;      // 属于该扫描层概率

        public int ShelfID;             // 书架号

        public int group;               // 组号
        public int ScanGroup;           // 扫描组号

        public int row;                 // 行号
        public int ScanRow;             // 扫描行号

        public int Layer;               // 层数
        public int ScanLayer;           // 扫描所在层数

        public int SerialNum;           // 列数
        public int ScanSerialNum;       // 扫描所在列数

        public int Type;                // 图书种类: 0（正确） / 1（错架）/ 2（未扫描）

        public Book(string barcode, string title, bool isLoaned, int shelfID, int layer, int serialNum, int group, int row)
        {
            this.barcode = barcode;
            this.title = title;
            this.IsLoaned = isLoaned;

            this.ScanTimeStamp = 0;
            this.ScanTimeDuration = 0;
            this.ScanCount = 0;
            this.MaxRSSI = 0;
            this.MinRSSI = 0;
            this.AvgRSSI = 0;

            this.ShelfID = shelfID;
            
            this.ScanLayer = this.Layer = layer;
            this.ScanSerialNum = this.SerialNum = serialNum;

            this.ScanGroup = this.group = group;
            this.ScanRow = this.row = row;

            this.Type = 2;
        }
    }
}
