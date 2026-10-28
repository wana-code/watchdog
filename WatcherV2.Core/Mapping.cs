using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Documents;

namespace WatcherV2.Core
{
    public static class Mapping
    {
        static public Dictionary<int, string> Week = new Dictionary<int, string>
        {
            {0, "Mon" },
            {1, "Tue" },
            {2, "Wed" },
            {3, "Thu" },
            {4, "Fri" },
            {5, "Sat" },
            {6, "Sun" },
        };
    }
}
