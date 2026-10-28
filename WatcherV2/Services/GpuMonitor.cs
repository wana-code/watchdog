using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace WatcherV2.Services
{
    /// <summary>
    /// 외장(디스크리트) GPU 1개 분량의 상태 스냅샷.
    /// </summary>
    public class GpuStatus
    {
        public int Index;
        public string Name = string.Empty;
        public float UtilizationPercent;   // 0 ~ 100
        public float MemoryUsedMB;
        public float MemoryTotalMB;
        public int Temperature;            // ℃ (0 = 미지원)

        public float MemoryPercent
        {
            get { return MemoryTotalMB > 0 ? (MemoryUsedMB / MemoryTotalMB) * 100f : 0f; }
        }
    }

    public interface IGpuMonitor : IDisposable
    {
        /// <summary>외장 GPU를 하나라도 찾았는지</summary>
        bool IsAvailable { get; }

        /// <summary>실제로 사용 중인 백엔드 ("NVML" / "nvidia-smi" / "None")</summary>
        string BackendName { get; }

        /// <summary>발견된 GPU 이름 목록</summary>
        IList<string> DeviceNames { get; }

        /// <summary>주기적으로 호출. NVML은 즉시 갱신, nvidia-smi는 백그라운드로 갱신(논블로킹).</summary>
        void Refresh();

        /// <summary>마지막으로 갱신된 값 조회.</summary>
        bool TryGetStatus(int index, out GpuStatus status);
    }

    /// <summary>
    /// NVIDIA 외장 GPU 사용량 수집기.
    ///  1순위 : nvml.dll 직접 호출 (x64 프로세스, 호출당 1~3ms)
    ///  2순위 : nvidia-smi 프로세스 호출 (x86 프로세스거나 nvml 로드 실패 시)
    /// 둘 다 실패하면 IsAvailable = false → UI에서 GPU 영역을 숨기면 된다.
    /// </summary>
    public sealed class GpuMonitor : IGpuMonitor
    {
        #region NVML P/Invoke

        private const string NvmlDll = "nvml.dll";
        private const uint NVML_TEMPERATURE_GPU = 0;

        [StructLayout(LayoutKind.Sequential)]
        private struct NvmlUtilization
        {
            public uint gpu;      // 지난 샘플 구간 중 커널이 돈 시간 비율(%)
            public uint memory;   // 메모리 R/W가 일어난 시간 비율(%)
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct NvmlMemory
        {
            public ulong total;
            public ulong free;
            public ulong used;
        }

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern IntPtr LoadLibrary(string lpFileName);

        [DllImport(NvmlDll, EntryPoint = "nvmlInit_v2", CallingConvention = CallingConvention.Cdecl)]
        private static extern int NvmlInit();

        [DllImport(NvmlDll, EntryPoint = "nvmlShutdown", CallingConvention = CallingConvention.Cdecl)]
        private static extern int NvmlShutdown();

        [DllImport(NvmlDll, EntryPoint = "nvmlDeviceGetCount_v2", CallingConvention = CallingConvention.Cdecl)]
        private static extern int NvmlDeviceGetCount(out uint count);

        [DllImport(NvmlDll, EntryPoint = "nvmlDeviceGetHandleByIndex_v2", CallingConvention = CallingConvention.Cdecl)]
        private static extern int NvmlDeviceGetHandleByIndex(uint index, out IntPtr device);

        [DllImport(NvmlDll, EntryPoint = "nvmlDeviceGetName", CallingConvention = CallingConvention.Cdecl)]
        private static extern int NvmlDeviceGetName(IntPtr device, StringBuilder name, uint length);

        [DllImport(NvmlDll, EntryPoint = "nvmlDeviceGetUtilizationRates", CallingConvention = CallingConvention.Cdecl)]
        private static extern int NvmlDeviceGetUtilizationRates(IntPtr device, out NvmlUtilization utilization);

        [DllImport(NvmlDll, EntryPoint = "nvmlDeviceGetMemoryInfo", CallingConvention = CallingConvention.Cdecl)]
        private static extern int NvmlDeviceGetMemoryInfo(IntPtr device, out NvmlMemory memory);

        [DllImport(NvmlDll, EntryPoint = "nvmlDeviceGetTemperature", CallingConvention = CallingConvention.Cdecl)]
        private static extern int NvmlDeviceGetTemperature(IntPtr device, uint sensorType, out uint temp);

        #endregion

        private enum Backend { None, Nvml, SmiProcess }

        private readonly object _sync = new object();
        private readonly List<GpuStatus> _statuses = new List<GpuStatus>();
        private readonly List<IntPtr> _handles = new List<IntPtr>();

        private Backend _backend = Backend.None;
        private bool _nvmlInitialized;
        private string _smiPath;
        private int _smiBusy;                 // 0 = idle, 1 = 실행 중 (중복 실행 방지)
        private DateTime _lastSmiCall = DateTime.MinValue;
        private readonly TimeSpan _smiMinInterval = TimeSpan.FromMilliseconds(1000);
        private bool _disposed;

        public bool IsAvailable
        {
            get { lock (_sync) { return _backend != Backend.None && _statuses.Count > 0; } }
        }

        public string BackendName
        {
            get
            {
                switch (_backend)
                {
                    case Backend.Nvml: return "NVML";
                    case Backend.SmiProcess: return "nvidia-smi";
                    default: return "None";
                }
            }
        }

        public IList<string> DeviceNames
        {
            get
            {
                lock (_sync)
                {
                    var names = new List<string>();
                    foreach (var s in _statuses) names.Add(s.Name);
                    return names;
                }
            }
        }

        public GpuMonitor()
        {
            AppDomain.CurrentDomain.ProcessExit += (s, e) => Dispose();

            if (TryInitNvml())
            {
                _backend = Backend.Nvml;
            }
            else if (TryInitSmi())
            {
                _backend = Backend.SmiProcess;
            }
            else
            {
                _backend = Backend.None;
            }

            // 초기값 1회 채우기
            Refresh();
        }

        #region 초기화

        private bool TryInitNvml()
        {
            try
            {
                // 최신 드라이버는 System32, 구형은 NVSMI 폴더에 위치.
                // 32비트 프로세스는 System32가 SysWOW64로 리다이렉트되어 로드에 실패한다 → smi로 폴백.
                string[] candidates =
                {
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "nvml.dll"),
                    @"C:\Program Files\NVIDIA Corporation\NVSMI\nvml.dll"
                };

                foreach (var p in candidates)
                {
                    if (File.Exists(p) && LoadLibrary(p) != IntPtr.Zero) break;
                }

                if (NvmlInit() != 0) return false;
                _nvmlInitialized = true;

                uint count;
                if (NvmlDeviceGetCount(out count) != 0 || count == 0)
                {
                    NvmlShutdown();
                    _nvmlInitialized = false;
                    return false;
                }

                for (uint i = 0; i < count; i++)
                {
                    IntPtr dev;
                    if (NvmlDeviceGetHandleByIndex(i, out dev) != 0) continue;

                    var sb = new StringBuilder(128);
                    string name = NvmlDeviceGetName(dev, sb, (uint)sb.Capacity) == 0
                        ? sb.ToString()
                        : "NVIDIA GPU " + i;

                    _handles.Add(dev);
                    _statuses.Add(new GpuStatus { Index = (int)i, Name = name });
                }

                return _statuses.Count > 0;
            }
            catch (DllNotFoundException) { return false; }   // 드라이버 없음
            catch (BadImageFormatException) { return false; } // x86 프로세스에서 x64 dll 로드
            catch (EntryPointNotFoundException) { return false; }
            catch (Exception) { return false; }
        }

        private bool TryInitSmi()
        {
            string[] candidates =
            {
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "nvidia-smi.exe"),
                @"C:\Program Files\NVIDIA Corporation\NVSMI\nvidia-smi.exe",
                "nvidia-smi.exe"
            };

            foreach (var p in candidates)
            {
                try
                {
                    if (p == "nvidia-smi.exe" || File.Exists(p))
                    {
                        _smiPath = p;
                        var lines = RunSmi();
                        if (lines != null && lines.Count > 0)
                        {
                            ParseSmi(lines);
                            return _statuses.Count > 0;
                        }
                    }
                }
                catch (Exception) { /* 다음 후보 */ }
            }

            _smiPath = null;
            return false;
        }

        #endregion

        #region 갱신

        public void Refresh()
        {
            if (_disposed) return;

            if (_backend == Backend.Nvml) RefreshNvml();
            else if (_backend == Backend.SmiProcess) RefreshSmiAsync();
        }

        private void RefreshNvml()
        {
            lock (_sync)
            {
                for (int i = 0; i < _handles.Count; i++)
                {
                    var dev = _handles[i];
                    var st = _statuses[i];

                    NvmlUtilization util;
                    if (NvmlDeviceGetUtilizationRates(dev, out util) == 0)
                        st.UtilizationPercent = util.gpu;

                    NvmlMemory mem;
                    if (NvmlDeviceGetMemoryInfo(dev, out mem) == 0)
                    {
                        st.MemoryUsedMB = mem.used / (1024f * 1024f);
                        st.MemoryTotalMB = mem.total / (1024f * 1024f);
                    }

                    uint temp;
                    if (NvmlDeviceGetTemperature(dev, NVML_TEMPERATURE_GPU, out temp) == 0)
                        st.Temperature = (int)temp;
                }
            }
        }

        /// <summary>
        /// nvidia-smi는 호출당 30~100ms가 걸리므로 UI 스레드에서 직접 돌리지 않는다.
        /// 이전 호출이 안 끝났으면 이번 tick은 건너뛴다.
        /// </summary>
        private void RefreshSmiAsync()
        {
            if (DateTime.Now - _lastSmiCall < _smiMinInterval) return;
            if (Interlocked.CompareExchange(ref _smiBusy, 1, 0) != 0) return;

            _lastSmiCall = DateTime.Now;

            Task.Run(() =>
            {
                try
                {
                    var lines = RunSmi();
                    if (lines != null) ParseSmi(lines);
                }
                catch (Exception) { /* 값 유지 */ }
                finally { Interlocked.Exchange(ref _smiBusy, 0); }
            });
        }

        private List<string> RunSmi()
        {
            if (string.IsNullOrEmpty(_smiPath)) return null;

            var psi = new ProcessStartInfo
            {
                FileName = _smiPath,
                Arguments = "--query-gpu=index,name,utilization.gpu,memory.used,memory.total,temperature.gpu " +
                            "--format=csv,noheader,nounits",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                StandardOutputEncoding = Encoding.UTF8
            };

            using (var proc = Process.Start(psi))
            {
                if (proc == null) return null;

                string output = proc.StandardOutput.ReadToEnd();
                if (!proc.WaitForExit(3000))
                {
                    try { proc.Kill(); } catch { }
                    return null;
                }
                if (proc.ExitCode != 0) return null;

                var lines = new List<string>();
                foreach (var line in output.Split('\n'))
                {
                    var t = line.Trim();
                    if (t.Length > 0) lines.Add(t);
                }
                return lines;
            }
        }

        private void ParseSmi(List<string> lines)
        {
            lock (_sync)
            {
                for (int i = 0; i < lines.Count; i++)
                {
                    var cols = lines[i].Split(',');
                    if (cols.Length < 6) continue;

                    if (_statuses.Count <= i) _statuses.Add(new GpuStatus { Index = i });
                    var st = _statuses[i];

                    st.Name = cols[1].Trim();
                    st.UtilizationPercent = ParseFloat(cols[2]);
                    st.MemoryUsedMB = ParseFloat(cols[3]);
                    st.MemoryTotalMB = ParseFloat(cols[4]);
                    st.Temperature = (int)ParseFloat(cols[5]);
                }
            }
        }

        private static float ParseFloat(string s)
        {
            float v;
            return float.TryParse(s.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out v) ? v : 0f;
        }

        #endregion

        public bool TryGetStatus(int index, out GpuStatus status)
        {
            lock (_sync)
            {
                if (index < 0 || index >= _statuses.Count)
                {
                    status = null;
                    return false;
                }

                var src = _statuses[index];
                // UI 스레드가 값을 읽는 동안 갱신되지 않도록 복사본을 준다.
                status = new GpuStatus
                {
                    Index = src.Index,
                    Name = src.Name,
                    UtilizationPercent = src.UtilizationPercent,
                    MemoryUsedMB = src.MemoryUsedMB,
                    MemoryTotalMB = src.MemoryTotalMB,
                    Temperature = src.Temperature
                };
                return true;
            }
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            if (_nvmlInitialized)
            {
                try { NvmlShutdown(); } catch { }
                _nvmlInitialized = false;
            }
        }
    }
}
