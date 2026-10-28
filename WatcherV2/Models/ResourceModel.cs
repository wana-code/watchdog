using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WatcherV2.Models
{
    public class ResourceModel : BindableBase
    {
        private string _processName;
        private string _dpCPUUsage;
        private string _dpMemUsage;
        private bool _processAlive;
        private string _processStatus;
        private string _startRemains;

        public string ProcessName
        {
            get { return _processName; }
            set { SetProperty(ref _processName, value); }
        }
               
        public string DpCPUUsage
        {
            get { return _dpCPUUsage; }
            set { SetProperty(ref _dpCPUUsage, value); }
        }
                
        public string DpMemUsage
        {
            get { return _dpMemUsage; }
            set { SetProperty(ref _dpMemUsage, value); }
        }
                
        public bool ProcessAlive
        {
            get { return _processAlive; }
            set { SetProperty(ref _processAlive, value); }
        }
                
        public string ProcessStatus
        {
            get { return _processStatus; }
            set { SetProperty(ref _processStatus, value); }
        }
                
        public string StartRemains
        {
            get { return _startRemains; }
            set { SetProperty(ref _startRemains, value); }
        }

        // ResourceModel.cs 에 아래 프로퍼티만 추가하면 된다.
        // (기존 DpCPUUsage / DpMemUsage 와 동일한 BindableBase 패턴)

        private bool _hasGPU;
        public bool HasGPU
        {
            get { return _hasGPU; }
            set { SetProperty(ref _hasGPU, value); }
        }

        private string _gpuName = string.Empty;
        public string GPUName
        {
            get { return _gpuName; }
            set { SetProperty(ref _gpuName, value); }
        }

        private string _dpGPUUsage = "-";
        public string DpGPUUsage            // 예) "63 %" (감시 대상 프로세스 기준)
        {
            get { return _dpGPUUsage; }
            set { SetProperty(ref _dpGPUUsage, value); }
        }

        private string _dpGPUMemUsage = "-";
        public string DpGPUMemUsage         // 예) "VRAM 1,204 MB / 8,192 MB"
        {
            get { return _dpGPUMemUsage; }
            set { SetProperty(ref _dpGPUMemUsage, value); }
        }

        private string _dpGPUEngine = "-";
        public string DpGPUEngine           // 예) "연산 61% · 디코드 44%"
        {
            get { return _dpGPUEngine; }
            set { SetProperty(ref _dpGPUEngine, value); }
        }

        private string _dpGPUTemperature = "-";
        public string DpGPUTemperature      // 예) "61 ℃"
        {
            get { return _dpGPUTemperature; }
            set { SetProperty(ref _dpGPUTemperature, value); }
        }
    }
}
