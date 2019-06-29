using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibRobotAuto.Common
{
    public class ShelfRoute
    {
        public string shelfNo;
        public string aisleNo;
        public List<string> route;

        public ShelfRoute(string shelfNo, string aisleNo, List<string> route)
        {
            this.shelfNo = shelfNo;
            this.aisleNo = aisleNo;
            this.route = route;
        }
    }
}
