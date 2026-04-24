using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using RecipeSearchApp.Services;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace RecipeSearchApp.Views
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void OpenDiagnostics_Click(object sender, RoutedEventArgs e)
        {
            var vm = (ViewModels.MainViewModel)RootGrid.DataContext;
            var wnd = new Views.DiagnosticsWindow((DatabaseService)vm.Database);
            wnd.Activate();
        }

        private void OpenCacheMonitor_Click(object sender, RoutedEventArgs e)
        {
            var vm = (ViewModels.MainViewModel)RootGrid.DataContext;
            var wnd = new Views.CacheMonitorWindow((DatabaseService)vm.Database);
            wnd.Activate();
        }
    }
}
