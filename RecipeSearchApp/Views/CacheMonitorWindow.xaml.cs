using Microsoft.UI.Xaml;
using RecipeSearchApp.Services;
using RecipeSearchApp.ViewModels;

namespace RecipeSearchApp.Views
{
    public sealed partial class CacheMonitorWindow : Window
    {
        private readonly CacheMonitorViewModel _viewModel;

        public CacheMonitorWindow(DatabaseService db)
        {
            this.InitializeComponent();

            AppWindow.Title = "Монитор Кеша";
            AppWindow.Resize(new Windows.Graphics.SizeInt32(700, 500));

            _viewModel = new CacheMonitorViewModel(db);
            this.RootGrid.DataContext = _viewModel;
            this.Closed += (s,e) => _viewModel.Stop();
        }
    }
}
