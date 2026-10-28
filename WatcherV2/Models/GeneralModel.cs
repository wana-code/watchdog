using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WatcherV2.Core;

namespace WatcherV2.Models
{
    [Table("SetupGeneral")]
    public class GeneralModel : BindableBase
    {
        [Key]
        private int _id = 1;
        public int Id
        {
            get { return _id; }
            set { SetProperty(ref _id, value); }
        }

        private string _procName = string.Empty;
        private bool _useShutdownDefence = false;
        private PermitShutdownTime _permitShutdownTime = PermitShutdownTime.Seconds_30;
        private bool _useRebootProcess = false;
        private DefineHours _rebootProcessHours = DefineHours.Hours_00;
        private DefineMins _rebootProcessMins = DefineMins.Mins_00;
        private DefineSeconds _rebootProcessSeconds = DefineSeconds.Secs_00;
        private bool _useLog = false;
        private string _logPath = string.Empty;
        private DispWithRatio _screenWidthRatio = DispWithRatio.Disp_30P;
        private LocalizationType _lanType = LocalizationType.LocalTypeDefault;

        public string ProcName
        {
            get { return _procName; }
            set { SetProperty(ref _procName, value); }
        }
        public bool UseShutdownDefence
        {
            get { return _useShutdownDefence; }
            set { SetProperty(ref _useShutdownDefence, value); }
        }
        public PermitShutdownTime PermitShutdownTime
        {
            get { return _permitShutdownTime; }
            set { SetProperty(ref _permitShutdownTime, value); }
        }
        public bool UseRebootProcess
        {
            get { return _useRebootProcess; }
            set { SetProperty(ref _useRebootProcess, value); }
        }
        public DefineHours RebootProcessHours
        {
            get { return _rebootProcessHours; }
            set { SetProperty(ref _rebootProcessHours, value); }
        }
        public DefineMins RebootProcessMins
        {
            get { return _rebootProcessMins; }
            set { SetProperty(ref _rebootProcessMins, value); }
        }
        public DefineSeconds RebootProcessSeconds
        {
            get { return _rebootProcessSeconds; }
            set { SetProperty(ref _rebootProcessSeconds, value); }
        }
        public bool UseLog
        {
            get { return _useLog; }
            set { SetProperty(ref _useLog, value); }
        }
        public string LogPath
        {
            get { return _logPath; }
            set { SetProperty(ref _logPath, value); }
        }
        public DispWithRatio ScreenWidthRatio
        {
            get { return _screenWidthRatio; }
            set { SetProperty(ref _screenWidthRatio, value); }
        }
        public LocalizationType LanType
        {
            get { return _lanType; }
            set { SetProperty(ref _lanType, value); }
        }
    }
}
