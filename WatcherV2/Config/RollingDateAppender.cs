using log4net.Appender;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WatcherV2.Config
{
    public class RollingDateAppender : RollingFileAppender
    {
        public TimeSpan MaxAgeRollBackups { get; set; }

        public RollingDateAppender() : base () 
        {
            PreserveLogFileNameExtension = true;
            StaticLogFileName = false;
        }

        protected override void AdjustFileBeforeAppend()
        {
            base.AdjustFileBeforeAppend();

            string LogFolder = Path.GetDirectoryName(File);
            var CheckTime = DateTime.Now.Subtract(MaxAgeRollBackups);
            foreach (string file in Directory.GetFiles(LogFolder, "*.log"))
            {
                if (System.IO.File.GetLastWriteTime(file) < CheckTime)
                    DeleteFile(file);
            }
        }
    }
}
