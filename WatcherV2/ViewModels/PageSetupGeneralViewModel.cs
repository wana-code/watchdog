using Prism.Commands;
using Prism.Events;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Forms;
using WatcherV2.Core;
using WatcherV2.Data;
using WatcherV2.Events;
using WatcherV2.Models;
using WatcherV2.Services;
using WatcherV2.Services.DB;

namespace WatcherV2.ViewModels
{
    public class PageSetupGeneralViewModel : BindableBase
    {
        private readonly ILocalDBService<GeneralModel> _localDBService;
        private readonly ILocalDBService<ShortcutModel> _slocalDBService;
        private readonly ICommonUtilService _commonUtilService;
        private readonly IEventAggregator _eventAggregator;
        private readonly ILogService _logService;
        private GeneralModel _generalModel;
        private List<ShortcutModel> _shortcutsFromDB = new List<ShortcutModel>();
        private ObservableCollection<string> _comboShortcutList = new ObservableCollection<string>();
        public GeneralModel GenParamModel
        {
            get { return _generalModel; }
            set { SetProperty(ref _generalModel, value); }
        }
        public List<ShortcutModel> ShortcutsFromDB
        {
            get { return _shortcutsFromDB; }
            set { SetProperty(ref _shortcutsFromDB, value); }
        }
        public ObservableCollection<string> ComboShortcutList
        {
            get { return _comboShortcutList; }
            set { SetProperty(ref _comboShortcutList, value); }
        }
        private DelegateCommand<string> _btnCommand;

        public DelegateCommand<string> BtnCommand =>
            _btnCommand ?? (_btnCommand = new DelegateCommand<string>(async param => await ExecuteBtnCommand(param), _ => true));

        //combobox items
        public IEnumerable<PermitShutdownTime> EnumPermitShutdownTime => Enum.GetValues(typeof(PermitShutdownTime)).Cast<PermitShutdownTime>();
        public IEnumerable<DispWithRatio> EnumDispWidthRatio => Enum.GetValues(typeof(DispWithRatio)).Cast<DispWithRatio>();
        public IEnumerable<LocalizationType> EnumLanType => Enum.GetValues(typeof(LocalizationType)).Cast<LocalizationType>();
        public IEnumerable<DefineHours> EnumHours => Enum.GetValues(typeof(DefineHours)).Cast<DefineHours>();
        public IEnumerable<DefineMins> EnumMins => Enum.GetValues(typeof(DefineMins)).Cast<DefineMins>();
        public IEnumerable<DefineSeconds> EnumSecs => Enum.GetValues(typeof(DefineSeconds)).Cast<DefineSeconds>();

        public PageSetupGeneralViewModel(ILocalDBService<GeneralModel> localDBService, ILocalDBService<ShortcutModel> slocalDBService,
            ICommonUtilService commonUtilService, IEventAggregator eventAggregator, ILogService logService)
        {
            _localDBService = localDBService;
            _slocalDBService = slocalDBService;
            _commonUtilService = commonUtilService;
            _eventAggregator = eventAggregator;
            _logService = logService;
            _generalModel = new GeneralModel(); // 초기값 할당
            GenParamModel = GeneralDB.Instance(_localDBService).GeneralModel;

            // 감시 프로세스 선택
            ShortcutsFromDB = ShortcutDB.Instance(_slocalDBService).ShortcutsFromDB;
            ComboShortcutList.Clear();
            foreach (var shortcut in ShortcutsFromDB)
            {
                ComboShortcutList.Add(shortcut.ShortcutProcFile);
            }
            _eventAggregator.GetEvent<ShortcutRegistEvent>().Subscribe(MessageReceived);

            _commonUtilService.SetLocalization(GenParamModel.LanType);

            var useLog = GenParamModel.UseLog;
            _logService.Log(useLog, GenParamModel.LogPath, (int)LogLv.Info, MethodBase.GetCurrentMethod().DeclaringType, "Setup General Opened");
        }
        private void MessageReceived(string obj)
        {
            ShortcutsFromDB = ShortcutDB.Instance(_slocalDBService).ShortcutsFromDB;
            ComboShortcutList.Clear();
            foreach (var shortcut in ShortcutsFromDB)
            {
                ComboShortcutList.Add(shortcut.ShortcutProcFile);
            }
        }
        private async Task ExecuteBtnCommand(string obj)
        {
            if (obj == "BtnRegist")
            {
                try
                {
                    await _localDBService.DeleteAll();
                    await _localDBService.Insert(GenParamModel);
                    _commonUtilService.SetLocalization(GenParamModel.LanType);
                    _eventAggregator.GetEvent<GeneralRegistEvent>().Publish(obj);

                    var useLog = GeneralDB.Instance(_localDBService).GeneralModel.UseLog;
                    var LogPath = GeneralDB.Instance(_localDBService).GeneralModel.LogPath;

                    _logService.Log(useLog, LogPath, (int)LogLv.Info, MethodBase.GetCurrentMethod().DeclaringType, "Regist General Option");
                    MessageBox.Show("Success");
                }
                catch (Exception e)
                {
                    MessageBox.Show($"Failed : {e.Message}");
                }
            }

            if (obj == "BtnSearch")
            {
                using (var dialog = new FolderBrowserDialog())
                {
                    DialogResult result = dialog.ShowDialog();

                    if (result == DialogResult.OK && !string.IsNullOrWhiteSpace(dialog.SelectedPath))
                    {
                        GenParamModel.LogPath = dialog.SelectedPath;
                    }
                    else
                    {
                        return;
                    }
                }
            }
        }
    }
}
