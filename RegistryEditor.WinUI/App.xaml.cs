using CommunityToolkit.WinUI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.UI.Windowing;
using Microsoft.Windows.AppLifecycle;
using RegistryValley.App.ViewModels;
using Windows.ApplicationModel;
using Windows.Storage;

namespace RegistryValley.App
{
    public partial class App : Application
    {
        public new static App Current
            => (App)Application.Current;

        public IServiceProvider Services { get; }

        public App()
        {
            InitializeComponent();

            Services = ConfigureServices();
        }

        private static IServiceProvider ConfigureServices()
        {
            return new ServiceCollection()
                //.AddSingleton<Utils.ILogger>(new SerilogWrapperLogger(Serilog.Log.Logger))
                //.AddSingleton<ToastService>()
                .AddSingleton<IMessenger>(StrongReferenceMessenger.Default)
                // ViewModels
                .AddTransient<RegistryValley.App.Services.UserSettingsServices>()
                // ViewModels
                .AddTransient<ViewModels.Dialogs.ValueAddingDialogViewModel>()
                .AddTransient<ViewModels.Dialogs.ValueEditingDialogViewModel>()
                .AddTransient<ViewModels.Properties.GeneralViewModel>()
                .AddTransient<ViewModels.Properties.MainPropertyViewModel>()
                .AddTransient<ViewModels.Properties.SecurityAdvancedViewModel>()
                .AddTransient<ViewModels.Properties.SecurityViewModel>()
                .AddTransient<ViewModels.UserControls.TreeViewViewModel>()
                .AddSingleton<MainViewModel>()
                .AddTransient<SettingsViewModel>()
                .AddSingleton<ValuesViewerViewModel>()
                .BuildServiceProvider();
        }

        protected override void OnLaunched(LaunchActivatedEventArgs e)
        {
            Window = new MainWindow();
            Window.Activate();
            WindowHandle = WinRT.Interop.WindowNative.GetWindowHandle(Window);
        }

        public static AppWindow GetAppWindow(Window w)
        {
            var hWnd = WinRT.Interop.WindowNative.GetWindowHandle(w);
            WindowId windowId = Win32Interop.GetWindowIdFromWindow(hWnd);

            return AppWindow.GetFromWindowId(windowId);
        }

        public static MainWindow Window { get; private set; } = null!;

        public static IntPtr WindowHandle { get; private set; }
    }
}
