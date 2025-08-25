using Microsoft.UI.Xaml;
using RegistryValley.App.Services;
using Windows.UI;

namespace RegistryValley.App.Helpers
{
    public static class AppThemeResourcesHelpers
    {
        /// <summary>
        /// Forces the application to use the correct resource styles
        /// </summary>
        public static void ApplyResources()
        {
            // Get the index of the current theme
            var selTheme = ThemeModeServices.RootTheme;

            // Toggle between the themes to force the controls to use the new resource styles
            ThemeModeServices.RootTheme = ElementTheme.Dark;
            ThemeModeServices.RootTheme = ElementTheme.Light;

            // Restore the theme to the correct theme
            ThemeModeServices.RootTheme = selTheme;
        }

        public static void SetAppThemeBackgroundColor(Color appThemeBackgroundColor)
        {
            Application.Current.Resources["AppThemeBackgroundBrush"] = appThemeBackgroundColor;
        }

        public static void SetCompactSpacing(bool useCompactSpacing)
        {
            if (useCompactSpacing)
            {
                Application.Current.Resources["AppListViewItemHeight"] = 28;
                Application.Current.Resources["AppHalfBranchLineHeight"] = 4;
            }
            else
            {
                Application.Current.Resources["AppListViewItemHeight"] = 36;
                Application.Current.Resources["AppHalfBranchLineHeight"] = 8;
            }
        }
    }
}
