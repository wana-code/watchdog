using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WatcherV2.Models;

namespace WatcherV2.Services
{
    public interface IResourceCheckService
    {
        public bool IsProcessRunning(ResourceModel resourceModel, float _totalMem, out float memUsage, out bool processOffed);
        public void GetCurrentResourceUsage(ResourceModel resourceModel, PerformanceCounter cpuCounter, float _totalMem,
            out float cpuUsage, out float memUsage, out bool processOffed);
        public float GetTotalPhysicalMemoryInMB();
        public void SearchNVDAGPU(out List<string> gpuDevices);
    }
}
