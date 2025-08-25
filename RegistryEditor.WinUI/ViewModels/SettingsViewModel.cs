using CommunityToolkit.WinUI.Helpers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using RegistryValley.App.Data;
using RegistryValley.App.Helpers;
using RegistryValley.App.Models;
using RegistryValley.App.Services;
using Windows.UI;

namespace RegistryValley.App.ViewModels
{
    public class SettingsViewModel : ObservableObject
    {
        public SettingsViewModel()
        {
            UserSettingsServices = App.Current.Services.GetRequiredService<UserSettingsServices>();

            _runAsAdminOnStartup = UserSettingsServices.RunAsAdminOnStartup;
            _useCompactLayout = UserSettingsServices.UseCompactLayout;

            ColorModes = new List<string>()
            {
                "Default",
                "Light",
                "Dark",
            }
            .AsReadOnly();

            AppThemeResources = AppThemeResourceFactory.AppThemeResources;

            SelectedAppThemeResources = AppThemeResources
                .Where(p => p.BackgroundColor == AppThemeBackgroundColor)
                .FirstOrDefault() ?? AppThemeResources.FirstOrDefault();
        }

        private UserSettingsServices UserSettingsServices { get; }

        public ReadOnlyCollection<string> ColorModes { get; set; }

        public ObservableCollection<AppThemeResourceItem> AppThemeResources { get; }

        private int _selectedColorModeIndex = (int)Enum.Parse(typeof(ElementTheme), ThemeModeServices.RootTheme.ToString());
        public int SelectedColorModeIndex
        {
            get => _selectedColorModeIndex;
            set
            {
                if (SetProperty(ref _selectedColorModeIndex, value))
                {
                    ThemeModeServices.RootTheme = (ElementTheme)value;
                }
            }
        }

        private AppThemeResourceItem selectedAppThemeResources;
        public AppThemeResourceItem SelectedAppThemeResources
        {
            get => selectedAppThemeResources;
            set
            {
                if (SetProperty(ref selectedAppThemeResources, value))
                {
                    AppThemeBackgroundColor = SelectedAppThemeResources.BackgroundColor;
                }
            }
        }

        public Color AppThemeBackgroundColor
        {
            get => ColorHelper.ToColor(UserSettingsServices.AppThemeBackgroundColor);
            set
            {
                if (value != ColorHelper.ToColor(UserSettingsServices.AppThemeBackgroundColor))
                {
                    UserSettingsServices.AppThemeBackgroundColor = value.ToString();

                    AppThemeResourcesHelpers.SetAppThemeBackgroundColor(AppThemeBackgroundColor);
                    AppThemeResourcesHelpers.ApplyResources();
                }
            }
        }

        private bool _runAsAdminOnStartup;
        public bool RunAsAdminOnStartup
        {
            get => _runAsAdminOnStartup;
            set
            {
                if (SetProperty(ref _runAsAdminOnStartup, value))
                {
                    UserSettingsServices.RunAsAdminOnStartup = value;
                }
            }
        }

        private bool _useCompactLayout;
        public bool UseCompactLayout
        {
            get => _useCompactLayout;
            set
            {
                if (SetProperty(ref _useCompactLayout, value))
                {
                    UserSettingsServices.UseCompactLayout = value;

                    AppThemeResourcesHelpers.SetCompactSpacing(value);
                    AppThemeResourcesHelpers.ApplyResources();
                }
            }
        }
    }
}
