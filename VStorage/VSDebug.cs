using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VStorage
{
    public class VSDebug
    {
        public static void StopPoint(long variable, long value )
        {
            int QQ = 0;
            if (variable == value)
            {
                QQ++;
            }
            QQ = 1;
        }
        public static void StopPoint(string variable, string value)
        {
            int QQ = 0;
            if (variable == value)
            {
                QQ++;
            }
            QQ = 1;
        }
    }
}
