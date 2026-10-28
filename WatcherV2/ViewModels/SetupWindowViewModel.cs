using Prism.Commands;
using Prism.Mvvm;
using Prism.Regions;
using Prism.Services.Dialogs;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using WatcherV2.Core;
using WatcherV2.Views;

namespace WatcherV2.ViewModels
{
	public class SetupWindowViewModel : BindableBase, IDialogAware
	{
        private string winState = string.Empty;
        private ObservableCollection<string> sideMenuList;

        private IRegionManager _scopedRegionManager;
        
        public string WinState
        {
            get { return winState; }
            set { SetProperty(ref winState, value); }
        }
        public ObservableCollection<string> SideMenuList
        {
            get { return sideMenuList; }
            set { SetProperty(ref sideMenuList, value); }
        }
        public IRegionManager ScopedRegionManager
        {
            get { return _scopedRegionManager; }
            set { SetProperty(ref _scopedRegionManager, value); }
        }

        private DelegateCommand<string> _windowbtnCommand;
        private DelegateCommand<string> _btnCommand;

        public event Action<IDialogResult> RequestClose;

        public DelegateCommand<string> WindowBtnCommand =>
            _windowbtnCommand ?? (_windowbtnCommand = new DelegateCommand<string>(WindowExecuteBtnCommand));

        public DelegateCommand<string> BtnCommand =>
            _btnCommand ?? (_btnCommand = new DelegateCommand<string>(ExecuteBtnCommand));

        private string _title = "Notification";
        public string Title
        {
            get { return _title; }
            set { SetProperty(ref _title, value); }
        }

        public SetupWindowViewModel(IRegionManager regionManager)
        {
            ScopedRegionManager = regionManager.CreateRegionManager();
            sideMenuList = new ObservableCollection<string>
            {
                "General",
                "Shortcut"
            };
        }

        private void WindowExecuteBtnCommand(string obj)
        {
            WinState = obj;
            WinState = string.Empty;
        }

        private void ExecuteBtnCommand(string obj)
        {
            switch(obj)
            {
                case "General":
                    ScopedRegionManager.RequestNavigate(RegionNames.SetupContentRegion, "PageSetupGeneral");
                    break;
                case "Shortcut":
                    ScopedRegionManager.RequestNavigate(RegionNames.SetupContentRegion, "PageSetupShortcut");
                    break;
            }
        }

        public bool CanCloseDialog()
        {
            return true;
        }

        public void OnDialogClosed()
        {
            
        }

        public void OnDialogOpened(IDialogParameters parameters)
        {
            ScopedRegionManager.RequestNavigate(RegionNames.SetupContentRegion, "PageSetupGeneral");
        }
    }
}
