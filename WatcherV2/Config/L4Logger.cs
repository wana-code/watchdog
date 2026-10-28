using log4net;
using log4net.Appender;
using log4net.Layout;
using log4net.Repository.Hierarchy;
using System;
using System.IO;

namespace WatcherV2.Config
{
    public class L4Logger
    {
        private static L4Logger LoggerInstance;

        public ILog log;
        public RollingFileAppender rollingAppender;
        public PatternLayout layout;
        public log4net.Filter.LoggerMatchFilter lmf;

        public static L4Logger GetInstance(string logPath)
        {
            if (LoggerInstance == null)
            {
                LoggerInstance = new L4Logger(logPath);
            }

            return LoggerInstance;
        }

        public L4Logger(string logPath)
        {
            string logDirectory = "C:/RTWatcher/Log";
            if (logPath != string.Empty && logPath != null)
            {
                logDirectory = logPath + "\\Log";
            }
            string logFile = "RTWatcher.log";

            // Combine the directory and file path
            string logFilePath = Path.Combine(logDirectory, logFile);

            Hierarchy hierarchy = (Hierarchy)LogManager.GetRepository();
            hierarchy.Configured = true;

            RollingDateAppender rollingAppender = new RollingDateAppender();
            rollingAppender.Name = "file";
            rollingAppender.File = logFilePath;
            rollingAppender.AppendToFile = true;
            rollingAppender.StaticLogFileName = true; // 파일 이름을 고정으로 유지
            rollingAppender.CountDirection = 1;
            rollingAppender.RollingStyle = RollingFileAppender.RollingMode.Date;
            rollingAppender.MaxAgeRollBackups = TimeSpan.FromDays(7);
            rollingAppender.LockingModel = new FileAppender.MinimalLock();
            rollingAppender.DatePattern = "'_'yyyyMMdd'.log'";
            rollingAppender.MaximumFileSize = "2MB"; // 필요에 따라 조정
            rollingAppender.MaxSizeRollBackups = 3; // 최대 백업 수

            rollingAppender.Layout = new PatternLayout("%date [%thread] %level %logger - %message%newline");

            hierarchy.Root.AddAppender(rollingAppender);
            rollingAppender.ActivateOptions();
            hierarchy.Root.Level = log4net.Core.Level.All;

            log = LogManager.GetLogger("logger");
        }
    }
}
