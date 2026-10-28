using log4net;
using System;
using WatcherV2.Config;

namespace WatcherV2.Services
{
    public interface ILogService
    {
        public void Log(bool useLog, string logPath, int logLv, Type classType, string message);
    }
    public class LogService : ILogService
    {
        public void Log(bool useLog, string logPath, int logLv, Type classType, string message)
        {
            if (useLog)
            {
                L4Logger logger = L4Logger.GetInstance(logPath);
                logger.log = LogManager.GetLogger(classType);

                switch (logLv)
                {
                    case 1:
                        logger.log.Debug(message);
                        break;
                    case 2:
                        logger.log.Info(message);
                        break;
                    case 3:
                        logger.log.Warn(message);
                        break;
                    case 4:
                        logger.log.Error(message);
                        break;
                    case 5:
                        logger.log.Fatal(message);
                        break;
                    default:
                        logger.log.Info(message);
                        break;
                }
            }
        }
    }
}
