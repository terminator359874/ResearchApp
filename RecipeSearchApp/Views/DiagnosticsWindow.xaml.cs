using Microsoft.UI.Xaml;
using RecipeSearchApp.Services;
using RecipeSearchApp.ViewModels;

namespace RecipeSearchApp.Views
{
    public sealed partial class DiagnosticsWindow : Window
    {
        public DiagnosticsWindow(DatabaseService db)
        {
            this.InitializeComponent();

            // Setup Window
            AppWindow.Title = "Диагностика алгоритмов";
            AppWindow.Resize(new Windows.Graphics.SizeInt32(900, 700));

            var viewModel = new DiagnosticsViewModel(db);
            this.RootGrid.DataContext = viewModel;
        }

    }
}
