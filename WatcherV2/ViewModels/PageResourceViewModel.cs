using LiveCharts;
using LiveCharts.Defaults;
using LiveCharts.Wpf;
using Prism.Commands;
using Prism.Events;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.NetworkInformation;
using System.Reflection;
using System.Text;
using System.Windows;
using System.Windows.Threading;
using WatcherV2.Core;
using WatcherV2.Data;
using WatcherV2.Events;
using WatcherV2.Models;
using WatcherV2.Services;
using WatcherV2.Services.DB;

namespace WatcherV2.ViewModels
{
    public class PageResourceViewModel : BindableBase
    {
        private GeneralModel _generalModel;
        public GeneralModel GenParamModel
        {
            get { return _generalModel; }
            set { SetProperty(ref _generalModel, value); }
        }

        // ResourceCheckService 사용을 위한 변수
        private ResourceModel _resourceModel;
        public ResourceModel ResourceModel
        {
            get { return _resourceModel; }
            set { SetProperty(ref _resourceModel, value); }
        }

        public PerformanceCounter cpuCounter;
        private List<string> listGPUs = new List<string>();
        private float _totalMem = 0;

        // 외장 GPU 장치 정보 (모델명 / 전체 VRAM / 온도) - NVML → nvidia-smi 폴백
        private IGpuMonitor _gpuMonitor;
        private int _gpuIndex = 0;

        // 감시 대상 프로세스의 GPU 사용률 (차트에 그리는 값)
        private IProcessGpuMonitor _procGpuMonitor;
        private float _gpuTotalMem = 0;

        private readonly IResourceCheckService _resourceCheckService;
        private readonly ILocalDBService<GeneralModel> _localDBService;
        private readonly ILocalDBService<ShortcutModel> _slocalDBService;
        private readonly ICommonUtilService _commonUtilService;
        private readonly IEventAggregator _eventAggregator;
        private readonly ILogService _logService;

        // LiveCharts
        private SeriesCollection _cpuSeriesCollection;
        private SeriesCollection _memSeriesCollection;
        private SeriesCollection _gpuSeriesCollection;
        private Func<double, string> _xLabelFormatter;
        private double _axisMax;
        private double _axisMin;
        public SeriesCollection CpuSeriesCollection
        {
            get { return _cpuSeriesCollection; }
            set { SetProperty(ref _cpuSeriesCollection, value); }
        }
        public SeriesCollection MemSeriesCollection
        {
            get { return _memSeriesCollection; }
            set { SetProperty(ref _memSeriesCollection, value); }
        }
        public SeriesCollection GpuSeriesCollection
        {
            get { return _gpuSeriesCollection; }
            set { SetProperty(ref _gpuSeriesCollection, value); }
        }
        public Func<double, string> XLabelFormatter
        {
            get { return _xLabelFormatter; }
            set { SetProperty(ref _xLabelFormatter, value); }
        }
        public double AxisMax
        {
            get { return _axisMax; }
            set { SetProperty(ref _axisMax, value); }
        }
        public double AxisMin
        {
            get { return _axisMin; }
            set { SetProperty(ref _axisMin, value); }
        }

        //Heatmap 
        public string[] Days { get; set; }
        public string[] Hours { get; set; }
        public ChartValues<HeatPoint> Values { get; set; }

        private int permitShutdownTime = 0;        //seconds
        private int processOffstatusDuration = 0;
        private string targetProcess = string.Empty;
        private DispatcherTimer timer;

        private DelegateCommand<string> _btnCommand;
        public DelegateCommand<string> BtnCommand =>
            _btnCommand ?? (_btnCommand = new DelegateCommand<string>(ExecuteBtnCommand));

        public PageResourceViewModel(IResourceCheckService resourceCheckService,
            ILocalDBService<GeneralModel> localDBService, ILocalDBService<ShortcutModel> slocalDBService,
            ICommonUtilService commonUtilService, IEventAggregator eventAggregator, ILogService logService)
        {
            _resourceCheckService = resourceCheckService;
            _localDBService = localDBService;
            _slocalDBService = slocalDBService;
            _commonUtilService = commonUtilService;
            _eventAggregator = eventAggregator;
            _logService = logService;
            _resourceModel = new ResourceModel();
            _generalModel = new GeneralModel();
            _eventAggregator.GetEvent<GeneralRegistEvent>().Subscribe(MessageReceived);
            _eventAggregator.GetEvent<ShortcutRegistEvent>().Subscribe(MessageReceived);

            // Heatmap
            Values = new ChartValues<HeatPoint>();

            for (int y = 0; y < 24; y++)         //y축 1시간 간격 시간
            {
                for (int x = 0; x < 7; x++)      //x축 요일
                {
                    Values.Add(new HeatPoint(y, x, 0));
                }
            }

            Initalizated();
        }
        private void MessageReceived(string obj)
        {
            timer.Stop();
            Initalizated();
        }

        private void ExecuteBtnCommand(string obj)
        {
            if (obj == "ResetHeatmap")
            {
                for (int y = 0; y < 24; y++)         //y축 1시간 간격 시간
                {
                    for (int x = 0; x < 7; x++)      //x축 요일
                    {
                        int idx = y * 7 + x;
                        Values[idx].Weight = 0;
                    }
                }
            }

            if (obj == "SaveHeatmap")
            {
                DateTime now = DateTime.Now;
                string heatmapDirectory = "C:/RTWatcher/HeatMap";
                if (GenParamModel.LogPath != string.Empty && GenParamModel.LogPath != null)
                {
                    heatmapDirectory = GenParamModel.LogPath + "/HeatMap";
                }
                string heatmapFile = $"heatmap_{now:yyyyMMdd_HHmmss}.csv";

                // Combine the directory and file path
                string heatmapFilePath = Path.Combine(heatmapDirectory, heatmapFile);
                _commonUtilService.CreateDirectory(heatmapFilePath);
                string filePath = heatmapFilePath;
                try
                {
                    using (StreamWriter sw = new StreamWriter(filePath, false, Encoding.UTF8))
                    {
                        sw.Write($"Process Name : {ResourceModel.ProcessName}");
                        sw.WriteLine();

                        sw.Write($"Export Time : {now:yyyy/MM/dd HH:mm:ss}");
                        sw.WriteLine();

                        // 행 헤더 작성 (시간 인덱스)
                        sw.Write(" ,"); // 열 인덱스 열을 위한 빈 셀
                        for (int y = 0; y < 24; y++)
                        {
                            sw.Write($"{y},");
                        }
                        sw.WriteLine(); // 다음 줄로 이동

                        // 데이터 작성
                        for (int x = 0; x < 7; x++)
                        {
                            sw.Write($"{Mapping.Week[x]},"); // 열 인덱스
                            for (int y = 0; y < 24; y++)
                            {
                                int idx = y * 7 + x;
                                HeatPoint currentPoint = Values[idx];

                                // 이 부분에서 HeatPoint의 속성을 사용하여 적절한 값을 가져올 수 있습니다.
                                sw.Write($"{currentPoint.Weight},");
                            }
                            sw.WriteLine(); // 다음 줄로 이동
                        }
                    }
                    Console.WriteLine($"CSV 파일이 작성되었습니다: {filePath}");
                    MessageBox.Show($"Success\nFile Location: {heatmapDirectory}");
                }
                catch (Exception e)
                {
                    MessageBox.Show("File is Opened");
                    Console.WriteLine(e.ToString());
                    return;
                }
            }
        }

        private void Initalizated()
        {
            permitShutdownTime = 0;        //seconds
            processOffstatusDuration = 0;

            GenParamModel = GeneralDB.Instance(_localDBService).GeneralModel;
            _commonUtilService.SetLocalization(GenParamModel.LanType);

            // resourceCheckService
            _totalMem = _resourceCheckService.GetTotalPhysicalMemoryInMB();
            _resourceCheckService.SearchNVDAGPU(out List<string> gpuDevices);
            listGPUs = new List<string>(gpuDevices);

            // 외장 GPU 탐색 (Initalizated는 설정 변경 시 재호출되므로 1회만 생성)
            if (_gpuMonitor == null)
                _gpuMonitor = new GpuMonitor();

            ResourceModel.HasGPU = _gpuMonitor.IsAvailable;
            if (ResourceModel.HasGPU)
            {
                GpuStatus gpu0;
                if (_gpuMonitor.TryGetStatus(_gpuIndex, out gpu0))
                {
                    ResourceModel.GPUName = gpu0.Name;
                    _gpuTotalMem = gpu0.MemoryTotalMB;
                }
                else
                {
                    ResourceModel.GPUName = "GPU";
                }
            }
            else
            {
                ResourceModel.GPUName = string.Empty;
                ResourceModel.DpGPUUsage = "-";
                ResourceModel.DpGPUMemUsage = "-";
            }

            // permitShutdownTime 초 단위로 변환
            if (GenParamModel.PermitShutdownTime == PermitShutdownTime.Seconds_30)
                permitShutdownTime = 30; // seconds
            else
                permitShutdownTime = (int)(GenParamModel.PermitShutdownTime) * 60; // seconds

            if (GenParamModel.ProcName == string.Empty)
                targetProcess = ShortcutDB.Instance(_slocalDBService).ShortcutsFromDB[0].ShortcutProcFile;
            else
                targetProcess = GenParamModel.ProcName;
            ResourceModel.ProcessName = _commonUtilService.ExtractProcessNameFromPath(targetProcess);
            cpuCounter = new PerformanceCounter("Process", "% Processor Time", ResourceModel.ProcessName);

            // 프로세스 단위 GPU 사용률 (CPU/MEM과 동일하게 감시 대상 프로세스 기준)
            if (_procGpuMonitor == null)
                _procGpuMonitor = new ProcessGpuMonitor(ResourceModel.ProcessName);
            else
                _procGpuMonitor.SetTargetProcess(ResourceModel.ProcessName);

            if (!_procGpuMonitor.IsAvailable)
                ResourceModel.HasGPU = false;   // GPU Engine 카운터를 못 쓰면 GPU 카드 숨김

            _commonUtilService.ScheduleDailyEvent((int)GenParamModel.RebootProcessHours,
                (int)GenParamModel.RebootProcessMins,
                (int)GenParamModel.RebootProcessSeconds,
                this.RebootProcess);

            // CPU, MEM USEAGE
            CpuSeriesCollection = new SeriesCollection
            {
                new LineSeries
                {
                    Values = new ChartValues<double>(),
                    PointGeometry = null
                }
            };
            MemSeriesCollection = new SeriesCollection
            {
                new LineSeries
                {
                    Values = new ChartValues<double>(),
                    PointGeometry = null
                }
            };
            GpuSeriesCollection = new SeriesCollection
            {
                new LineSeries
                {
                    Values = new ChartValues<double>(),
                    PointGeometry = null
                }
            };
            XLabelFormatter = val =>
            {
                var label = -(val - 180);
                return label.ToString();
            };

            // X축 범위 초기화
            AxisMax = 180; // 초기 최대값
            AxisMin = 0;   // 초기 최소값

            for (int i = 0; i < 180; i++)
            {
                CpuSeriesCollection[0].Values.Add((double)(0));
                MemSeriesCollection[0].Values.Add((double)(0));
                GpuSeriesCollection[0].Values.Add((double)(0));
            }

            // heatmap 초기화
            for (int y = 0; y < 24; y++)         //y축 1시간 간격 시간
            {
                for (int x = 0; x < 7; x++)      //x축 요일
                {
                    int idx = y * 7 + x;
                    Values[idx].Weight = 0;
                }
            }

            timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(1);
            timer.Tick += Timer_Tick;
            timer.Start();
        }

        //차트 값 업데이트
        //프로세스 상태 확인 및 프로세스 재시작
        private void Timer_Tick(object sender, EventArgs e)
        {
            var useLog = GenParamModel.UseLog;
            _resourceCheckService.GetCurrentResourceUsage(ResourceModel, cpuCounter, _totalMem,
                out float cpuUsage, out float memUsage, out bool processOffed);

            //process가 off인 time을 heatmap에 기록
            if (processOffed)
            {
                // 현재 요일을 가져옵니다.
                DayOfWeek currentDay = DateTime.Now.DayOfWeek;
                DateTime now = DateTime.Now;
                int nDay = ((int)currentDay + 6) % 7;        //x축
                int nDayTime = now.Hour;                         //y축

                //heatmap index 계산
                int hetmapIdx = nDayTime * 7 + nDay;

                //heatmap 변경 test
                double hitmapValue = Values[hetmapIdx].Weight;
                hitmapValue = hitmapValue + 1;
                if (hitmapValue > 100)
                    hitmapValue = 100;

                Values[hetmapIdx] = new HeatPoint(nDayTime, nDay, hitmapValue);

                _logService.Log(useLog, GenParamModel.LogPath, (int)LogLv.Warn, MethodBase.GetCurrentMethod().DeclaringType, $"{ResourceModel.ProcessName} exited.");
            }

            // 새 데이터를 컬렉션의 끝에 추가
            CpuSeriesCollection[0].Values.Add((double)cpuUsage);

            // 데이터 컬렉션에서 가장 오래된 데이터 제거
            if (CpuSeriesCollection[0].Values.Count > 180)
            {
                CpuSeriesCollection[0].Values.RemoveAt(0);
            }

            MemSeriesCollection[0].Values.Add((double)memUsage);

            // 데이터 컬렉션에서 가장 오래된 데이터 제거
            if (MemSeriesCollection[0].Values.Count > 180)
            {
                MemSeriesCollection[0].Values.RemoveAt(0);
            }

            // GPU 사용량 (감시 대상 프로세스 기준)
            if (ResourceModel.HasGPU)
            {
                _procGpuMonitor.Refresh();
                var pgpu = _procGpuMonitor.GetStatus();

                double gpuUsage = pgpu.Alive ? pgpu.UtilizationPercent : 0;

                ResourceModel.DpGPUUsage = pgpu.Alive ? $"{pgpu.UtilizationPercent:0} %" : "-";

                // 엔진별 내역 (0인 엔진은 표시하지 않음)
                var parts = new List<string>();
                if (pgpu.Utilization3D > 0.5f) parts.Add($"3D {pgpu.Utilization3D:0}%");
                if (pgpu.UtilizationCompute > 0.5f) parts.Add($"연산 {pgpu.UtilizationCompute:0}%");
                if (pgpu.UtilizationDecode > 0.5f) parts.Add($"디코드 {pgpu.UtilizationDecode:0}%");
                if (pgpu.UtilizationEncode > 0.5f) parts.Add($"인코드 {pgpu.UtilizationEncode:0}%");
                ResourceModel.DpGPUEngine = pgpu.Alive && parts.Count > 0 ? string.Join(" · ", parts) : "-";

                // 장치 정보는 갱신 주기가 느려도 되므로 같은 tick에서 가볍게만 갱신
                _gpuMonitor.Refresh();
                GpuStatus dev;
                if (_gpuMonitor.TryGetStatus(_gpuIndex, out dev))
                {
                    _gpuTotalMem = dev.MemoryTotalMB;
                    ResourceModel.DpGPUTemperature = dev.Temperature > 0 ? $"{dev.Temperature} ℃" : "-";
                }

                ResourceModel.DpGPUMemUsage = _gpuTotalMem > 0
                    ? $"VRAM {pgpu.DedicatedMemoryMB:N0} MB / {_gpuTotalMem:N0} MB"
                    : $"VRAM {pgpu.DedicatedMemoryMB:N0} MB";

                GpuSeriesCollection[0].Values.Add(gpuUsage);

                if (GpuSeriesCollection[0].Values.Count > 180)
                {
                    GpuSeriesCollection[0].Values.RemoveAt(0);
                }
            }

            AxisMax = 180;
            AxisMin = 0;

            //Process 상태 체크
            //Process 가 Off인 시간 체크
            //Process 가 Setting된 시간 이상 Off 상태인 경우, 프로세스 재구동
            if (GenParamModel.UseShutdownDefence)
            {
                if (!ResourceModel.ProcessAlive)
                {
                    if (processOffstatusDuration == 0)
                    {
                        _logService.Log(useLog, GenParamModel.LogPath, (int)LogLv.Info, MethodBase.GetCurrentMethod().DeclaringType, $"{ResourceModel.ProcessName} Start After {permitShutdownTime} Seconds.");
                    }

                    processOffstatusDuration += 1;
                    ResourceModel.StartRemains = $"Start After {permitShutdownTime - processOffstatusDuration} Seconds";
                }
                else
                {
                    processOffstatusDuration = 0;
                    ResourceModel.StartRemains = "";
                }

                if (permitShutdownTime <= processOffstatusDuration)
                {
                    string workingDirectory = Path.GetDirectoryName(targetProcess);

                    ProcessStartInfo startInfo = new ProcessStartInfo
                    {
                        FileName = targetProcess,
                        WorkingDirectory = workingDirectory // 작업 디렉토리 설정
                    };

                    try
                    {
                        Process.Start(startInfo);
                        _logService.Log(useLog, GenParamModel.LogPath, (int)LogLv.Info, MethodBase.GetCurrentMethod().DeclaringType, $"{ResourceModel.ProcessName} execute.");
                    }
                    catch (Exception ex)
                    {
                        processOffstatusDuration = 0;
                        MessageBox.Show(ex.Message);
                        _logService.Log(useLog, GenParamModel.LogPath, (int)LogLv.Error, MethodBase.GetCurrentMethod().DeclaringType, $"{ResourceModel.ProcessName}, {ex.Message}");
                        return;
                    }
                }
            }
            else
            {
                ResourceModel.StartRemains = $"ShutDown Defense Option is Off";
            }
        }

        /// <summary>
        /// 페이지/앱 종료 시 호출. NVML 핸들 정리.
        /// </summary>
        public void Cleanup()
        {
            if (timer != null)
            {
                timer.Stop();
                timer.Tick -= Timer_Tick;
            }

            if (_gpuMonitor != null)
            {
                _gpuMonitor.Dispose();
                _gpuMonitor = null;
            }

            if (_procGpuMonitor != null)
            {
                _procGpuMonitor.Dispose();
                _procGpuMonitor = null;
            }
        }

        public void RebootProcess()
        {
            if (GenParamModel.UseRebootProcess)
            {
                Debug.WriteLine("RebootProcess");

                // 실행 중인 프로세스 확인
                Process[] runningProcesses = Process.GetProcessesByName(ResourceModel.ProcessName);
                var useLog = GeneralDB.Instance(_localDBService).GeneralModel.UseLog;
                var LogPath = GeneralDB.Instance(_localDBService).GeneralModel.LogPath;

                if (runningProcesses.Length > 0)
                {
                    runningProcesses[0].Kill();
                    runningProcesses[0].WaitForExit();
                    _logService.Log(useLog, LogPath, (int)LogLv.Warn, MethodBase.GetCurrentMethod().DeclaringType, $"Reboot - {ResourceModel.ProcessName} exited.");
                }

                string workingDirectory = Path.GetDirectoryName(targetProcess);

                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = targetProcess,
                    WorkingDirectory = workingDirectory // 작업 디렉토리 설정
                };

                try
                {
                    Process.Start(startInfo);
                    _logService.Log(useLog, LogPath, (int)LogLv.Info, MethodBase.GetCurrentMethod().DeclaringType, $"Reboot - {ResourceModel.ProcessName} execute.");
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                    _logService.Log(useLog, LogPath, (int)LogLv.Error, MethodBase.GetCurrentMethod().DeclaringType, $"Reboot - {ResourceModel.ProcessName}, {ex.Message}");
                    return;
                }
            }
        }
    }
}