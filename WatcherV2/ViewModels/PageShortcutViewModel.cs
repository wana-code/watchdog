using Prism.Commands;
using Prism.Events;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Windows;
using WatcherV2.Core;
using WatcherV2.Data;
using WatcherV2.Events;
using WatcherV2.Models;
using WatcherV2.Services;
using WatcherV2.Services.DB;

namespace WatcherV2.ViewModels
{
    public class PageShortcutViewModel : BindableBase
    {
        private List<ShortcutModel> _shortcutsFromDB = new List<ShortcutModel>();
        private readonly ILocalDBService<ShortcutModel> _localDBService;
        private readonly ILocalDBService<GeneralModel> _glocalDBService;
        private readonly ICommonUtilService _commonUtilService;
        private readonly ILogService _logService;
        private readonly IEventAggregator _eventAggregator;

        public List<ShortcutModel> ShortcutsFromDB
        {
            get { return _shortcutsFromDB; }
            set { SetProperty(ref _shortcutsFromDB, value); }
        }

        private DelegateCommand<string> _btnCommand;

        public DelegateCommand<string> BtnCommand =>
            _btnCommand ?? (_btnCommand = new DelegateCommand<string>(ExecuteBtnCommand));

        public PageShortcutViewModel(ILocalDBService<ShortcutModel> localDBService, ILocalDBService<GeneralModel> glocalDBService,
            ICommonUtilService commonUtilService, IEventAggregator eventAggregator, ILogService logService)
        {
            _localDBService = localDBService;
            _glocalDBService = glocalDBService;
            _commonUtilService = commonUtilService;
            _logService = logService;
            _eventAggregator = eventAggregator;
            ShortcutsFromDB = ShortcutDB.Instance(_localDBService).ShortcutsFromDB;
            var lanType = GeneralDB.Instance(glocalDBService).GeneralModel.LanType;
            _commonUtilService.SetLocalization(lanType);
            _eventAggregator.GetEvent<ShortcutRegistEvent>().Subscribe(MessageReceived);
        }
        private void MessageReceived(string obj)
        {
            ShortcutsFromDB = ShortcutDB.Instance(_localDBService).ShortcutsFromDB;
        }
        private void ExecuteBtnCommand(string obj)
        {
            string procName = string.Empty;
            string workingDirectory = string.Empty;
            string procFile = string.Empty;

            if (obj.Contains("Icon"))
            {
                var idx = _commonUtilService.ExtractAndConvert(obj) - 1;
                getProcNameAndWorkingDir(ShortcutsFromDB[idx].ShortcutProcFile, out procName, out workingDirectory);
                procFile = ShortcutsFromDB[idx].ShortcutProcFile;
            }

            if (procName == string.Empty)
            {
                return;
            }

            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = procFile,
                WorkingDirectory = workingDirectory // 작업 디렉토리 설정
            };

            if (!IsRunningProcess(procName))
            {
                var useLog = GeneralDB.Instance(_glocalDBService).GeneralModel.UseLog;
                var LogPath = GeneralDB.Instance(_glocalDBService).GeneralModel.LogPath;

                try
                {
                    Process.Start(startInfo);
                    _logService.Log(useLog, LogPath,(int)LogLv.Info, MethodBase.GetCurrentMethod().DeclaringType, $"{procName} execute.");
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                    _logService.Log(useLog, LogPath,(int)LogLv.Error, MethodBase.GetCurrentMethod().DeclaringType, $"{procName}, {ex.Message}");
                    return;
                }
            }
        }

        private void getProcNameAndWorkingDir(string ShortcutProc, out string procName, out string WorkingDir)
        {
            procName = string.Empty;
            WorkingDir = string.Empty;

            if (ShortcutProc == string.Empty)
                return;

            try
            {
                procName = Path.GetFileNameWithoutExtension(ShortcutProc);
                WorkingDir = Path.GetDirectoryName(ShortcutProc);
            }
            catch
            {

            }

            Debug.WriteLine("ShortcutViwMode:getProcNameAndWorkingDir");

        }

        private bool IsRunningProcess(string processName)
        {
            // 실행 중인 프로세스 확인
            bool isRunning = false;

            foreach (var process in Process.GetProcesses())
            {
                if (process.ProcessName.Equals(processName, StringComparison.OrdinalIgnoreCase))
                {
                    isRunning = true;
                    return isRunning;
                }
            }

            return isRunning;
        }
    }
}
