using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WatcherV2.Data
{
    public class DBConfig
    {
        public static string GetConnectionString()
        {
            string assemblyLocation = Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location);
            string dbPath = Path.Combine(assemblyLocation, "Config", "ConfigSetup.db");
            return $"Data Source={dbPath}";
        }
    }
}
