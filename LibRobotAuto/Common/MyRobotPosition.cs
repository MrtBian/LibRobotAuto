using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RobotOperateInterface;

namespace LibRobotAuto.Common
{
    public class MyRobotPosition
    {
        public string PositionNo;
        public RobotPosition Position;

        public MyRobotPosition()
        {
            Position = new RobotPosition();
        }
    }
}
