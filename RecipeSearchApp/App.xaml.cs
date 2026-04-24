using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.UI.Xaml.Shapes;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.Foundation;
using Windows.Foundation.Collections;


using RecipeSearchApp.Views;
using WinRT.Interop;

namespace RecipeSearchApp;

public partial class App : Application
{
    private Window? _window;

    public App()
    {
        InitializeComponent();
    }

    protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
    {
        _window = new MainWindow();

        // Центрирование окна
        var hwnd = WindowNative.GetWindowHandle(_window);
        var windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hwnd);
        var appWindow = Microsoft.UI.Windowing.AppWindow.GetFromWindowId(windowId);

        if (appWindow != null)
        {
            var displayArea = Microsoft.UI.Windowing.DisplayArea.GetFromWindowId(
                windowId,
                Microsoft.UI.Windowing.DisplayAreaFallback.Nearest
            );

            if (displayArea != null)
            {
                var workArea = displayArea.WorkArea;

                appWindow.MoveAndResize(
                    new Windows.Graphics.RectInt32(
                        (workArea.Width - 1200) / 2,
                        (workArea.Height - 800) / 2,
                        1200,
                        800
                    )
                );
            }
        }

        _window.Activate();
    }
}
