using Prism.Mvvm;
using System.Collections.Generic;
using System.Threading.Tasks;
using WatcherV2.Models;
using WatcherV2.Services.DB;

namespace WatcherV2.Data
{
    public class ShortcutDB : BindableBase
    {
        private List<ShortcutModel> _shortcutsFromDB = new List<ShortcutModel>();
        public List<ShortcutModel> ShortcutsFromDB
        {
            get { return _shortcutsFromDB; }
            set { SetProperty(ref _shortcutsFromDB, value); }
        }

        private static ShortcutDB _instance;

        private static readonly object _lock = new object();
        private readonly ILocalDBService<ShortcutModel> _localDBService;

        public ShortcutDB(ILocalDBService<ShortcutModel> localDBService)
        {
            _localDBService = localDBService;
        }

        private async void InitializeAsync()
        {
            await LoadDataAsync();
        }

        private async Task LoadDataAsync()
        {
            var result = await _localDBService.GetAll();
            if (result == null || result.Count == 0)
            {
                await _localDBService.Insert(new ShortcutModel());
            }
            else
            {
                ShortcutsFromDB = result;
            }
        }

        public static ShortcutDB Instance(ILocalDBService<ShortcutModel> localDBService)
        {

            if (_instance == null)
            {
                lock (_lock)
                {
                    if (_instance == null)
                    {
                        _instance = new ShortcutDB(localDBService);
                    }
                }
            }
            _instance.InitializeAsync();
            return _instance;
        }
    }
}
