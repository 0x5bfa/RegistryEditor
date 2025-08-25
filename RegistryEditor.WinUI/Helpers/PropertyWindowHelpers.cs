using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;
using RegistryEditor.WinUI.Models;
using RegistryEditor.WinUI.Views.Properties;
using System.IO;
using Windows.Graphics;

namespace RegistryEditor.WinUI.Helpers
{
	public static class PropertyWindowHelpers
	{
		public static void CreatePropertyWindow(KeyItem item)
		{
			var frame = new Frame
			{
				RequestedTheme = Services.ThemeModeServices.RootTheme
			};

			frame.Navigate(typeof(MainPropertyPage), item, new SuppressNavigationTransitionInfo());

			var propertiesWindow = new WinUIEx.WindowEx
			{
				//IsAlwaysOnTop = true,
				IsMinimizable = false,
				IsMaximizable = false,
				MinWidth = 460,
				MinHeight = 550,
				Content = frame,
				Backdrop = new WinUIEx.MicaSystemBackdrop(),
			};

			var appWindow = propertiesWindow.AppWindow;
			appWindow.Title = "Property";
			appWindow.TitleBar.ExtendsContentIntoTitleBar = true;
			appWindow.TitleBar.ButtonBackgroundColor = Colors.Transparent;
			appWindow.TitleBar.ButtonInactiveBackgroundColor = Colors.Transparent;

			appWindow.Resize(new SizeInt32(460, 550));
			appWindow.SetIcon(Path.Combine(Windows.ApplicationModel.Package.Current.InstalledLocation.Path, Constants.AssetPaths.Logo));

			if (frame.Content is MainPropertyPage properties)
			{
				properties.AppWindow = appWindow;
				properties.KeyItem = item;
			}

			appWindow.Show();

			// WinUI3: move window to cursor position
			if (true)
			{
				UWPToWinAppSDKUpgradeHelpers.InteropHelpers.GetCursorPos(out var pointerPosition);
				var displayArea = DisplayArea.GetFromPoint(new PointInt32(pointerPosition.X, pointerPosition.Y), DisplayAreaFallback.Nearest);

				var appWindowPos = new PointInt32
				{
					X = displayArea.WorkArea.X
						+ Math.Max(0, Math.Min(displayArea.WorkArea.Width - appWindow.Size.Width, pointerPosition.X - displayArea.WorkArea.X)),
					Y = displayArea.WorkArea.Y
						+ Math.Max(0, Math.Min(displayArea.WorkArea.Height - appWindow.Size.Height, pointerPosition.Y - displayArea.WorkArea.Y)),
				};

				appWindow.Move(appWindowPos);
			}
		}
	}
}
