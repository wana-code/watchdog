using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using WatcherV2.Core;

namespace WatcherV2.Services
{
    public class CommonUtilService : ICommonUtilService
    {
        private Timer timer;
        public string ExtractProcessNameFromPath(string filePath)
        {
            // 파일 경로에서 파일명만 추출
            string fileName = Path.GetFileNameWithoutExtension(filePath);

            // 파일명에서 프로세스 이름 추출
            string pattern = @"^(.+)$";  // 단순히 파일명 전체를 추출하는 경우
                                         // 또는 더 복잡한 경우: string pattern = @"^(.+?)_\w+$";

            Regex regex = new Regex(pattern);
            Match match = regex.Match(fileName);

            if (match.Success)
            {
                return match.Groups[1].Value;
            }

            // 매치가 없을 경우 기본값 반환
            return "UnknownProcess";
        }

        public void ScheduleDailyEvent(int hour, int minute, int second, Action eventAction)
        {
            // 현재 시간과 사용자가 지정한 시간을 DateTime 형식으로 변환
            DateTime now = DateTime.Now;
            DateTime scheduledTime = new DateTime(now.Year, now.Month, now.Day, hour, minute, second);

            // 사용자가 지정한 시간이 이미 지났다면, 다음 날로 설정
            if (scheduledTime < now)
            {
                scheduledTime = scheduledTime.AddDays(1);
            }

            // 타이머 설정
            TimeSpan timeToGo = scheduledTime - now;
            TimeSpan oneDay = TimeSpan.FromDays(1);

            timer = new Timer(x =>
            {
                eventAction.Invoke();
            }, null, timeToGo, oneDay); // 매일 반복
        }

        public void SetLocalization(LocalizationType localizationType)
        {
            if (localizationType == LocalizationType.LocalTypeDefault)
            {
                System.Threading.Thread.CurrentThread.CurrentUICulture = new System.Globalization.CultureInfo("en-US");
            }

            if (localizationType == LocalizationType.LocalTypeENUS)
            {
                System.Threading.Thread.CurrentThread.CurrentUICulture = new System.Globalization.CultureInfo("en-US");
            }

            if (localizationType == LocalizationType.LocalTypeKOKR)
            {
                System.Threading.Thread.CurrentThread.CurrentUICulture = new System.Globalization.CultureInfo("ko-KR");
            }
        }

        public bool IsImgFile(string filePath)
        {
            string extension = Path.GetExtension(filePath).ToLower();
            return extension == ".jpg" || extension == ".jpeg" || extension == ".png";
        }
        public bool IsExeFile(string filePath)
        {
            string extension = Path.GetExtension(filePath).ToLower();
            return extension == ".exe";
        }

        public int ExtractAndConvert(string input)
        {
            // Use regular expression to match the numeric part at the end
            Match match = Regex.Match(input, @"\d+$");

            if (match.Success)
            {
                // Convert the matched numeric part to an int
                if (int.TryParse(match.Value, out int result))
                {
                    return result;
                }
                else
                {
                    throw new InvalidOperationException("Conversion failed");
                }
            }
            else
            {
                throw new InvalidOperationException("No numeric part found");
            }
        }

        public void CreateDirectory(string _path)
        {
            string folderPath = System.IO.Path.GetDirectoryName(_path);
            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }
        }
    }
}
