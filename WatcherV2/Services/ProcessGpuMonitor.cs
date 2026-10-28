using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace WatcherV2.Services
{
    /// <summary>
    /// 감시 대상 프로세스 1개의 GPU 사용 현황 스냅샷.
    /// </summary>
    public class ProcessGpuStatus
    {
        public int ProcessId;
        public bool Alive;

        /// <summary>대표값. 엔진별 사용률 중 최대치 (작업관리자 GPU 열과 동일 기준)</summary>
        public float UtilizationPercent;

        public float Utilization3D;
        public float UtilizationCompute;
        public float UtilizationDecode;   // NVDEC (RTSP 디코딩)
        public float UtilizationEncode;   // NVENC
        public float UtilizationCopy;

        /// <summary>이 프로세스가 점유 중인 전용 VRAM</summary>
        public float DedicatedMemoryMB;
    }

    public interface IProcessGpuMonitor : IDisposable
    {
        /// <summary>GPU Engine 성능 카운터를 사용할 수 있는 환경인지</summary>
        bool IsAvailable { get; }

        /// <summary>감시 대상 변경. 프로세스가 재시작돼 PID가 바뀌어도 자동 추적한다.</summary>
        void SetTargetProcess(string processName);

        /// <summary>타이머에서 주기적으로 호출. 실제 수집은 백그라운드에서 수행(논블로킹).</summary>
        void Refresh();

        /// <summary>마지막으로 수집된 값 조회.</summary>
        ProcessGpuStatus GetStatus();
    }

    /// <summary>
    /// Windows "GPU Engine" / "GPU Process Memory" 성능 카운터 기반 프로세스 단위 GPU 사용률 수집기.
    ///
    /// - 벤더 무관(NVIDIA/AMD/Intel), 작업관리자 GPU 열과 같은 소스.
    /// - 인스턴스명 형식: pid_1234_luid_0x00000000_0x0000E1BE_phys_0_eng_1_engtype_3D
    /// - Utilization Percentage는 두 번째 샘플부터 유효하므로, 카운터 객체를 살려둔 채 재사용한다.
    ///   (대상 프로세스가 새로 뜬 직후 첫 1초는 0이 나오는 게 정상)
    ///
    /// 모든 카운터 생성/읽기는 백그라운드 스레드에서만 수행한다. UI 스레드는 GetStatus()로 캐시만 읽는다.
    /// </summary>
    public sealed class ProcessGpuMonitor : IProcessGpuMonitor
    {
        private const string EngineCategory = "GPU Engine";
        private const string EngineCounter = "Utilization Percentage";
        private const string ProcMemCategory = "GPU Process Memory";
        private const string ProcMemCounter = "Dedicated Usage";

        /// <summary>인스턴스 목록 재열거 주기. 매 tick 열거하면 비싸다(20~100ms).</summary>
        private static readonly TimeSpan RescanInterval = TimeSpan.FromSeconds(5);

        private static readonly TimeSpan MinPollInterval = TimeSpan.FromMilliseconds(900);

        private readonly object _sync = new object();
        private readonly Dictionary<string, PerformanceCounter> _engineCounters =
            new Dictionary<string, PerformanceCounter>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, PerformanceCounter> _memCounters =
            new Dictionary<string, PerformanceCounter>(StringComparer.OrdinalIgnoreCase);

        private ProcessGpuStatus _latest = new ProcessGpuStatus();
        private string _targetProcessName = string.Empty;
        private int _pid = -1;
        private DateTime _lastScan = DateTime.MinValue;
        private DateTime _lastPoll = DateTime.MinValue;
        private int _busy;              // 0 = idle, 1 = 백그라운드 수집 중
        private bool _available;
        private bool _disposed;

        public bool IsAvailable { get { return _available; } }

        public ProcessGpuMonitor(string processName)
        {
            _targetProcessName = NormalizeName(processName);

            try
            {
                _available = PerformanceCounterCategory.Exists(EngineCategory);
            }
            catch (Exception)
            {
                _available = false;   // 성능 카운터 손상/권한 문제
            }

            AppDomain.CurrentDomain.ProcessExit += (s, e) => Dispose();
        }

        public void SetTargetProcess(string processName)
        {
            var name = NormalizeName(processName);

            lock (_sync)
            {
                if (string.Equals(_targetProcessName, name, StringComparison.OrdinalIgnoreCase)) return;

                _targetProcessName = name;
                _pid = -1;
                _lastScan = DateTime.MinValue;
                ClearCounters();
                _latest = new ProcessGpuStatus();
            }
        }

        public ProcessGpuStatus GetStatus()
        {
            lock (_sync)
            {
                // UI 스레드가 읽는 동안 백그라운드가 덮어쓰지 않도록 복사본을 준다.
                return new ProcessGpuStatus
                {
                    ProcessId = _latest.ProcessId,
                    Alive = _latest.Alive,
                    UtilizationPercent = _latest.UtilizationPercent,
                    Utilization3D = _latest.Utilization3D,
                    UtilizationCompute = _latest.UtilizationCompute,
                    UtilizationDecode = _latest.UtilizationDecode,
                    UtilizationEncode = _latest.UtilizationEncode,
                    UtilizationCopy = _latest.UtilizationCopy,
                    DedicatedMemoryMB = _latest.DedicatedMemoryMB
                };
            }
        }

        public void Refresh()
        {
            if (_disposed || !_available) return;
            if (DateTime.Now - _lastPoll < MinPollInterval) return;
            if (Interlocked.CompareExchange(ref _busy, 1, 0) != 0) return;

            _lastPoll = DateTime.Now;

            Task.Run(() =>
            {
                try { Collect(); }
                catch (Exception) { /* 이전 값 유지 */ }
                finally { Interlocked.Exchange(ref _busy, 0); }
            });
        }

        #region 수집 (백그라운드 스레드 전용)

        private void Collect()
        {
            string procName;
            lock (_sync) { procName = _targetProcessName; }

            if (string.IsNullOrEmpty(procName))
            {
                lock (_sync) { _latest = new ProcessGpuStatus(); }
                return;
            }

            int pid = ResolvePid(procName);

            if (pid <= 0)
            {
                // 대상 프로세스가 죽은 상태 → 0으로 떨군다
                lock (_sync)
                {
                    ClearCounters();
                    _pid = -1;
                    _latest = new ProcessGpuStatus { Alive = false };
                }
                return;
            }

            bool pidChanged;
            lock (_sync)
            {
                pidChanged = (_pid != pid);
                if (pidChanged)
                {
                    ClearCounters();       // 재시작으로 PID가 바뀌면 카운터를 새로 잡는다
                    _pid = pid;
                }
            }

            if (pidChanged || DateTime.Now - _lastScan > RescanInterval || _engineCounters.Count == 0)
            {
                RebuildCounters(pid);
                _lastScan = DateTime.Now;
            }

            var status = new ProcessGpuStatus { ProcessId = pid, Alive = true };

            // 엔진 타입별로 합산 후, 그중 최대치를 대표값으로 쓴다.
            // (엔진끼리는 병렬로 도므로 전부 더하면 100%를 넘어 실제보다 부풀려진다)
            var byType = new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase);
            var dead = new List<string>();

            foreach (var kv in _engineCounters)
            {
                float v;
                try { v = kv.Value.NextValue(); }
                catch (Exception) { dead.Add(kv.Key); continue; }   // 인스턴스 소멸

                string type = ExtractEngineType(kv.Key);
                if (byType.ContainsKey(type)) byType[type] += v;
                else byType[type] = v;
            }

            foreach (var k in dead) RemoveEngineCounter(k);

            foreach (var kv in byType)
            {
                float v = Clamp(kv.Value);
                string t = kv.Key;

                if (t.IndexOf("3D", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    t.IndexOf("Graphics", StringComparison.OrdinalIgnoreCase) >= 0) status.Utilization3D = v;
                else if (t.IndexOf("Compute", StringComparison.OrdinalIgnoreCase) >= 0) status.UtilizationCompute = v;
                else if (t.IndexOf("VideoDecode", StringComparison.OrdinalIgnoreCase) >= 0 ||
                         t.IndexOf("VideoProcessing", StringComparison.OrdinalIgnoreCase) >= 0) status.UtilizationDecode = v;
                else if (t.IndexOf("VideoEncode", StringComparison.OrdinalIgnoreCase) >= 0) status.UtilizationEncode = v;
                else if (t.IndexOf("Copy", StringComparison.OrdinalIgnoreCase) >= 0) status.UtilizationCopy = v;

                if (v > status.UtilizationPercent) status.UtilizationPercent = v;
            }

            // 전용 VRAM 점유량
            float bytes = 0;
            var deadMem = new List<string>();
            foreach (var kv in _memCounters)
            {
                try { bytes += kv.Value.NextValue(); }
                catch (Exception) { deadMem.Add(kv.Key); }
            }
            foreach (var k in deadMem) RemoveMemCounter(k);

            status.DedicatedMemoryMB = bytes / (1024f * 1024f);

            lock (_sync) { _latest = status; }
        }

        private int ResolvePid(string processName)
        {
            try
            {
                var procs = Process.GetProcessesByName(processName);
                if (procs.Length == 0) return -1;

                // 여러 개면 메모리를 가장 많이 쓰는 쪽(= 실제 작업 중인 인스턴스)을 택한다
                int pid = procs.OrderByDescending(p =>
                {
                    try { return p.WorkingSet64; } catch { return 0L; }
                }).First().Id;

                foreach (var p in procs) p.Dispose();
                return pid;
            }
            catch (Exception) { return -1; }
        }

        private void RebuildCounters(int pid)
        {
            string prefix = "pid_" + pid + "_";

            try
            {
                var engineCat = new PerformanceCounterCategory(EngineCategory);
                var instances = engineCat.GetInstanceNames();

                lock (_sync)
                {
                    var wanted = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                    foreach (var inst in instances)
                    {
                        if (inst.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) == false) continue;
                        wanted.Add(inst);

                        if (!_engineCounters.ContainsKey(inst))
                        {
                            var c = new PerformanceCounter(EngineCategory, EngineCounter, inst, true);
                            c.NextValue();          // 첫 샘플(기준점) 확보
                            _engineCounters[inst] = c;
                        }
                    }

                    // 사라진 인스턴스 정리
                    foreach (var key in _engineCounters.Keys.Where(k => !wanted.Contains(k)).ToList())
                        RemoveEngineCounter(key);
                }
            }
            catch (Exception) { /* 이번 주기는 건너뛴다 */ }

            try
            {
                if (!PerformanceCounterCategory.Exists(ProcMemCategory)) return;

                var memCat = new PerformanceCounterCategory(ProcMemCategory);
                var instances = memCat.GetInstanceNames();

                lock (_sync)
                {
                    var wanted = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                    foreach (var inst in instances)
                    {
                        if (inst.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) == false) continue;
                        wanted.Add(inst);

                        if (!_memCounters.ContainsKey(inst))
                            _memCounters[inst] = new PerformanceCounter(ProcMemCategory, ProcMemCounter, inst, true);
                    }

                    foreach (var key in _memCounters.Keys.Where(k => !wanted.Contains(k)).ToList())
                        RemoveMemCounter(key);
                }
            }
            catch (Exception) { }
        }

        #endregion

        #region 유틸

        private static string NormalizeName(string processName)
        {
            if (string.IsNullOrEmpty(processName)) return string.Empty;

            var name = processName.Trim();
            if (name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
                name = name.Substring(0, name.Length - 4);
            return name;
        }

        /// <summary>"..._eng_1_engtype_3D" → "3D"</summary>
        private static string ExtractEngineType(string instanceName)
        {
            const string token = "engtype_";
            int idx = instanceName.LastIndexOf(token, StringComparison.OrdinalIgnoreCase);
            return idx < 0 ? "Unknown" : instanceName.Substring(idx + token.Length);
        }

        private static float Clamp(float v)
        {
            if (v < 0f) return 0f;
            if (v > 100f) return 100f;
            return v;
        }

        private void RemoveEngineCounter(string key)
        {
            PerformanceCounter c;
            if (_engineCounters.TryGetValue(key, out c))
            {
                try { c.Dispose(); } catch { }
                _engineCounters.Remove(key);
            }
        }

        private void RemoveMemCounter(string key)
        {
            PerformanceCounter c;
            if (_memCounters.TryGetValue(key, out c))
            {
                try { c.Dispose(); } catch { }
                _memCounters.Remove(key);
            }
        }

        private void ClearCounters()
        {
            foreach (var c in _engineCounters.Values) { try { c.Dispose(); } catch { } }
            _engineCounters.Clear();

            foreach (var c in _memCounters.Values) { try { c.Dispose(); } catch { } }
            _memCounters.Clear();
        }

        #endregion

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            lock (_sync) { ClearCounters(); }
        }
    }
}
