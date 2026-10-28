using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WatcherV2.Core;

namespace WatcherV2.Services
{
    public interface ICommonUtilService
    {
        public void ScheduleDailyEvent(int hour, int minute, int second, Action eventAction);
        public void SetLocalization(LocalizationType localizationType);
        public string ExtractProcessNameFromPath(string filePath);
        public bool IsImgFile(string filePath);
        public bool IsExeFile(string filePath);
        public int ExtractAndConvert(string input);
        public void CreateDirectory(string _path);
    }
}
