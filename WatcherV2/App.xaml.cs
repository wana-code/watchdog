using Prism.Ioc;
using Prism.Regions;
using System;
using System.Diagnostics;
using System.IO;
using System.Management;
using System.Security.Cryptography;
using System.Text;
using System.Windows;
using WatcherV2.Core;
using WatcherV2.Data;
using WatcherV2.Models;
using WatcherV2.Services;
using WatcherV2.Services.DB;
using WatcherV2.Views;

namespace WatcherV2
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App
    {
        protected override Window CreateShell()
        {
            return Container.Resolve<MainWindow>();
        }

        protected override void RegisterTypes(IContainerRegistry containerRegistry)
        {
            // DB 등록
            containerRegistry.RegisterSingleton<DataContext>();

            // 서비스 등록
            containerRegistry.RegisterSingleton<ILocalDBService<GeneralModel>, LocalDBService<GeneralModel>>();
            containerRegistry.RegisterSingleton<ILocalDBService<ShortcutModel>, LocalDBService<ShortcutModel>>();
            containerRegistry.RegisterSingleton<ICommonUtilService, CommonUtilService>();
            containerRegistry.RegisterSingleton<IResourceCheckService, ResourceCheckService>();
            containerRegistry.RegisterSingleton<ILogService, LogService>();

            // View 등록
            containerRegistry.RegisterForNavigation<SettingBar>("SettingBar");
            containerRegistry.RegisterForNavigation<PageSetupGeneral>("PageSetupGeneral");
            containerRegistry.RegisterForNavigation<PageSetupShortcut>("PageSetupShortcut");
            containerRegistry.RegisterForNavigation<PageShortcut>("PageShortcut");
            containerRegistry.RegisterForNavigation<PageResource>("PageResource");

            containerRegistry.RegisterDialogWindow<CustomDialogWindow>();
            containerRegistry.RegisterDialog<SetupWindow>();
        }

        protected override void OnInitialized()
        {
            base.OnInitialized();

            //View를 Region에 맵핑
            var regionManager = Container.Resolve<IRegionManager>();

            regionManager.RequestNavigate(RegionNames.SettingBarRegion, "SettingBar");
            regionManager.RequestNavigate(RegionNames.SetupContentRegion, "PageSetupGeneral");
            regionManager.RequestNavigate(RegionNames.MainShortcutRegion, "PageShortcut");
            regionManager.RequestNavigate(RegionNames.MainResourceRegion, "PageResource");
        }
    }
}
