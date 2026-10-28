using Microsoft.Win32;
using Prism.Commands;
using Prism.Events;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using WatcherV2.Data;
using WatcherV2.Events;
using WatcherV2.Models;
using WatcherV2.Services;
using WatcherV2.Services.DB;

namespace WatcherV2.ViewModels
{
    public class PageSetupShortcutViewModel : BindableBase
    {
        private List<ShortcutModel> _shortcutsFromDB = new List<ShortcutModel>();
        private readonly ILocalDBService<ShortcutModel> _localDBService;
        private readonly ICommonUtilService _commonUtilService;
        private readonly IEventAggregator _eventAggregator;

        public List<ShortcutModel> ShortcutsFromDB
        {
            get { return _shortcutsFromDB; }
            set { SetProperty(ref _shortcutsFromDB, value); }
        }

        private DelegateCommand<string> _btnCommand;

        public DelegateCommand<string> BtnCommand =>
            _btnCommand ?? (_btnCommand = new DelegateCommand<string>(async param => await ExecuteBtnCommand(param), _ => true));

        public PageSetupShortcutViewModel(ILocalDBService<ShortcutModel> localDBService, ILocalDBService<GeneralModel> glocalDBService, ICommonUtilService commonUtilService, IEventAggregator eventAggregator)
        {
            _localDBService = localDBService;
            _commonUtilService = commonUtilService;
            _eventAggregator = eventAggregator;
            ShortcutsFromDB = ShortcutDB.Instance(_localDBService).ShortcutsFromDB;
            var lanType = GeneralDB.Instance(glocalDBService).GeneralModel.LanType;
            _commonUtilService.SetLocalization(lanType);
        }

        private async Task ExecuteBtnCommand(string obj)
        {
            if (obj == "BtnAdd")
            {
                if (ShortcutsFromDB.Count < 4)
                    await _localDBService.Insert(new ShortcutModel());
                else
                    MessageBox.Show("Cannot Add Shortcut");
                ShortcutsFromDB = ShortcutDB.Instance(_localDBService).ShortcutsFromDB;
            }
            else if (obj == "BtnRegist")
            {
                try
                {
                    foreach (var shortcut in ShortcutsFromDB)
                        await _localDBService.Update(shortcut, shortcut.Id);
                    _eventAggregator.GetEvent<ShortcutRegistEvent>().Publish(obj);
                    MessageBox.Show("Success");
                }
                catch(Exception e)
                {
                    MessageBox.Show($"Failed : {e.Message}");
                }
            }
            else
            {
                var idx = _commonUtilService.ExtractAndConvert(obj)-1;
                OpenFileDialog openFileDialog = new OpenFileDialog();
                openFileDialog.Filter = "All files (*.*)|*.*|image files (*.jpg,*jpeg)|*.jpg;*.jpeg|image files (*.png)|*.png|exe file (*.exe)|*.exe"; // 필터 설정
                openFileDialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments); // 초기 디렉토리 설정

                if ((bool)openFileDialog.ShowDialog())
                {
                    string filePath = openFileDialog.FileName;

                    if (obj.Contains("Icon"))
                    {
                        if (!_commonUtilService.IsImgFile(filePath))
                            return;
                        ShortcutsFromDB[idx].ShortcutIconFile = filePath;
                    }
                    else if (obj.Contains("Process"))
                    {
                        if (!_commonUtilService.IsExeFile(filePath))
                            return;
                        ShortcutsFromDB[idx].ShortcutProcFile = filePath;
                    }
                }
            }
        }
    }
}
