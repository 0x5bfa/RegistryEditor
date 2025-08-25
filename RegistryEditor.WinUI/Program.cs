using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.Windows.AppLifecycle;
using System.Security.Principal;

namespace RegistryEditor.WinUI
{
	public class Program
	{
		[STAThread]
		private static async Task<int> Main()
		{
			WinRT.ComWrappersSupport.InitializeComWrappers();

			bool isRedirect = await DecideRedirection();
			if (!isRedirect)
			{
				Application.Start((p) =>
				{
					var context = new DispatcherQueueSynchronizationContext(DispatcherQueue.GetForCurrentThread());
					SynchronizationContext.SetSynchronizationContext(context);

					_ = new App();
				});
			}

			return 0;
		}

		private static bool IsAdministrator()
		{
			using var identity = WindowsIdentity.GetCurrent();
			var principal = new WindowsPrincipal(identity);
			return principal.IsInRole(WindowsBuiltInRole.Administrator);
		}

		private static void OnActivated(object? sender, AppActivationArguments args)
		{
		}

		private static async Task<bool> DecideRedirection()
		{
			bool isRedirect = false;
			AppActivationArguments args = AppInstance.GetCurrent().GetActivatedEventArgs();
			ExtendedActivationKind kind = args.Kind;
			AppInstance keyInstance = AppInstance.FindOrRegisterForKey("RegistryValley");

			if (keyInstance.IsCurrent)
			{
				keyInstance.Activated += OnActivated;
			}
			else
			{
				isRedirect = true;
				await keyInstance.RedirectActivationToAsync(args);
			}

			return isRedirect;
		}
	}
}
