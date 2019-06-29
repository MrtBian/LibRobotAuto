using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RobotOperateInterface;

namespace LibRobotAuto.Common
{
    public class LibraryRouteLine
    {
        public MyRobotPosition startPoint;
        public MyRobotPosition endPoint;
        public MapLineType lineType;
        public LiftStatus height;
        public byte distanceToShelf;
        public RobotMoveDirection moveDirection;
        public int column;
        public int row;
        public int group;
        public string shelfType;
        public byte startShelfNo;
        public byte shelfCount;
        public byte shelfFlag;      //扫描书架时，下位机需要的标志信息

        public LibraryRouteLine()
        {
            shelfFlag = 0x01;
            startPoint = new MyRobotPosition();
            endPoint = new MyRobotPosition();
            lineType = new MapLineType();
        }

        public static LibraryRouteLine Copy(LibraryRouteLine line)
        {
            LibraryRouteLine newLine = new LibraryRouteLine();

            newLine.lineType = line.lineType;
            newLine.startPoint = line.startPoint;
            newLine.endPoint = line.endPoint;
            newLine.height = line.height;
            newLine.distanceToShelf = line.distanceToShelf;
            newLine.shelfType = line.shelfType;
            newLine.startShelfNo = line.startShelfNo;
            newLine.shelfCount = line.shelfCount;
            newLine.moveDirection = line.moveDirection;

            return newLine;
        }
    }
}
