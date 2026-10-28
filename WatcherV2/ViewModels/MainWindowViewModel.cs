using Prism.Commands;
using Prism.Events;
using Prism.Mvvm;
using System.Reflection;
using WatcherV2.Core;
using WatcherV2.Data;
using WatcherV2.Events;
using WatcherV2.Models;
using WatcherV2.Services;
using WatcherV2.Services.DB;

namespace WatcherV2.ViewModels
{
    public class MainWindowViewModel : BindableBase
    {
        private string _title = "Prism Application";
        public string Title
        {
            get { return _title; }
            set { SetProperty(ref _title, value); }
        }

        private string winState = string.Empty;
        public string WinState
        {
            get { return winState; }
            set { SetProperty(ref winState, value); }
        }

        private GeneralModel _generalModel;
        public GeneralModel GenParamModel
        {
            get { return _generalModel; }
            set { SetProperty(ref _generalModel, value); }
        }

        private DelegateCommand<string> _windowbtnCommand;
        private readonly ILocalDBService<GeneralModel> _localDBService;

        public DelegateCommand<string> WindowBtnCommand =>
            _windowbtnCommand ?? (_windowbtnCommand = new DelegateCommand<string>(WindowExecuteBtnCommand));

        public MainWindowViewModel(ILocalDBService<GeneralModel> localDBService, IEventAggregator eventAggregator, ILogService logService)
        {
            _localDBService = localDBService;
            GenParamModel = GeneralDB.Instance(_localDBService).GeneralModel;
            var useLog = GenParamModel.UseLog;
            logService.Log(useLog, GenParamModel.LogPath, (int)LogLv.Info, MethodBase.GetCurrentMethod().DeclaringType, "Program Start...");
            eventAggregator.GetEvent<GeneralRegistEvent>().Subscribe(MessageReceived);
        }
        private void MessageReceived(string obj)
        {
            GenParamModel = GeneralDB.Instance(_localDBService).GeneralModel;
        }

        private void WindowExecuteBtnCommand(string obj)
        {
            WinState = obj;
            WinState = string.Empty;
        }
    }
}
