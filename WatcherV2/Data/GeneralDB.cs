using Prism.Mvvm;
using System.Threading.Tasks;
using WatcherV2.Models;
using WatcherV2.Services.DB;

namespace WatcherV2.Data
{
    public class GeneralDB : BindableBase
    {
        private GeneralModel _generalModel;
        public GeneralModel GeneralModel
        {
            get { return _generalModel; }
            set { SetProperty(ref _generalModel, value); }
        }

        private static GeneralDB _instance;

        // Lock object for thread safety in lazy initialization
        private static readonly object _lock = new object();
        private readonly ILocalDBService<GeneralModel> _localDBService;

        private GeneralDB(ILocalDBService<GeneralModel> localDBService)
        {
            _localDBService = localDBService;
        }

        private async void InitializeAsync()
        {
            await LoadDataAsync();
        }

        private async Task LoadDataAsync()
        {
            var result = await _localDBService.GetFistRow();
            if (result != null)
            {
                GeneralModel = result;
            }
        }

        public static GeneralDB Instance(ILocalDBService<GeneralModel> localDBService)
        {

            if (_instance == null)
            {
                lock (_lock)
                {
                    if (_instance == null)
                    {
                        _instance = new GeneralDB(localDBService);
                    }
                }
            }
            _instance.InitializeAsync();
            return _instance;
        }
    }
}
