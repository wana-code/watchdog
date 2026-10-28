using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using WatcherV2.Models;

namespace WatcherV2.Services
{
    public class ResourceCheckService : IResourceCheckService
    {
        public void GetCurrentResourceUsage(ResourceModel resourceModel, PerformanceCounter cpuCounter, float _totalMem, out float cpuUsage, out float memUsage, out bool processOffed)
        {
            resourceModel.DpCPUUsage = "";
            cpuUsage = 0;
            memUsage = 0;

            processOffed = false;

            if (!IsProcessRunning(resourceModel, _totalMem, out float out_memusage, out processOffed))
            {
                return;
            }

            memUsage = out_memusage;


            float rtVal = 0;

            float upper = cpuCounter.NextValue();
            float lower = (float)Environment.ProcessorCount;

            if (lower == 0)
            {
                resourceModel.DpCPUUsage = "0%";
                return;
            }

            rtVal = upper / lower;
            rtVal = (float)Math.Round(rtVal, 2);
            resourceModel.DpCPUUsage = rtVal.ToString() + "%";

            cpuUsage = rtVal;
        }
        public bool IsProcessRunning(ResourceModel resourceModel, float _totalMem, out float memUsage, out bool processOffed)
        {
            processOffed = false;
            memUsage = 0;
            resourceModel.DpMemUsage = "";

            // 해당 이름을 가진 프로세스가 실행 중인지 확인
            Process[] processes = Process.GetProcessesByName(resourceModel.ProcessName);

            if (processes.Length > 0)
            {
                resourceModel.ProcessStatus = "Running";
                long mbUsed = processes[0].WorkingSet64 / (1024 * 1024);
                float memUsageRatio = (float)mbUsed * 100 / (float)_totalMem;

                memUsageRatio = (float)Math.Round(memUsageRatio, 2);
                resourceModel.DpMemUsage = $"{memUsageRatio.ToString()} %    - Used {mbUsed} MBytes, - Total {_totalMem} MBytes";
                memUsage = memUsageRatio;
                resourceModel.ProcessAlive = true;
                return true;
            }

            if (resourceModel.ProcessAlive)
            {
                processOffed = true;
                //process가 running중, off 상태가 된 경우
                Debug.WriteLine("Process Off changed!");
            }

            resourceModel.ProcessStatus = "Off";
            resourceModel.ProcessAlive = false;

            return false;
        }
        public float GetTotalPhysicalMemoryInMB()
        {
            var query = new ObjectQuery("SELECT TotalPhysicalMemory FROM Win32_ComputerSystem");
            ManagementObjectSearcher searcher = new ManagementObjectSearcher(query);

            foreach (ManagementObject item in searcher.Get())
            {
                return Convert.ToSingle(item["TotalPhysicalMemory"]) / (1024 * 1024);
            }
            return 0;
        }
        public void SearchNVDAGPU(out List<string> gpuDevices)
        {
            ProcessStartInfo psiSearchDevice = new ProcessStartInfo
            {
                FileName = @"nvidia-smi.exe", // 경로 수정
                Arguments = "--query-gpu=index,name --format=csv,noheader,nounits",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                CreateNoWindow = true
            };

            gpuDevices = new List<string>();
            gpuDevices.Clear();

            using (Process process = Process.Start(psiSearchDevice))
            {
                using (StreamReader reader = process.StandardOutput)
                {
                    string result = reader.ReadToEnd();

                    Regex regex = new Regex(@"(\d+),\s*(.+)");

                    MatchCollection matches = regex.Matches(result);

                    foreach (Match match in matches)
                    {
                        if (match.Success)
                        {
                            string index = match.Groups[1].Value;
                            string deviceName = match.Groups[2].Value;
                            deviceName = deviceName.Replace("\r", "");
                            gpuDevices.Add(deviceName);
                        }
                    }
                }
            }
        }
    }
}
