namespace LibRobotAuto.Common
{
    public class BookTag
    {
        public string Epc;
        public TimeLine ScanTimeLine;
        public int ScanCount;
        public double TotalRSSI;
        public double MaxRSSI;
        public double MinRSSI;

        public BookTag(string epc, ulong time, double rssi)
        {
            this.Epc = epc;
            this.ScanTimeLine.Start = this.ScanTimeLine.End = time;
            this.ScanCount = 1;
            this.TotalRSSI = this.MaxRSSI = this.MinRSSI = rssi;
        }
    }
}
