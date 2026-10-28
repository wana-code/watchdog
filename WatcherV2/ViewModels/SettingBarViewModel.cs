using Prism.Commands;
using Prism.Mvvm;
using Prism.Services.Dialogs;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using WatcherV2.Views;

namespace WatcherV2.ViewModels
{
	public class SettingBarViewModel : BindableBase
	{
        private DelegateCommand<string> _btnCommand;
        private readonly IDialogService _dialogService;

        public DelegateCommand<string> BtnCommand =>
            _btnCommand ?? (_btnCommand = new DelegateCommand<string>(ExecuteBtnCommand));
        public SettingBarViewModel(IDialogService dialogService)
        {
            _dialogService = dialogService;
        }

        private void ExecuteBtnCommand(string obj)
        {
            if (obj == "Setting")
            {
                _dialogService.ShowDialog("SetupWindow");
            }
            else if (obj == "Reboot")
            {
                Debug.WriteLine("Btn_Click_Popup_Reboot");
                // 현재 실행 중인 프로세스의 정보 가져오기
                Process currentProcess = Process.GetCurrentProcess();

                // 현재 프로세스가 실행된 파일의 경로
                string exePath = currentProcess.MainModule.FileName;

                // DLL 파일로부터 실행된 경우 확인
                if (Path.GetExtension(exePath).Equals(".exe", StringComparison.OrdinalIgnoreCase))
                {
                    Process.Start(exePath);
                    Application.Current.Shutdown();
                }
            }
        }
    }
}
