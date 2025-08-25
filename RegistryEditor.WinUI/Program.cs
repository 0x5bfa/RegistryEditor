// Copyright (c) 2025 0x5BFA.
// Licensed under the MIT License.

using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.Windows.AppLifecycle;

namespace RegistryEditor.WinUI
{
	public class Program
	{
		[STAThread]
		private static void Main()
		{
			WinRT.ComWrappersSupport.InitializeComWrappers();

			bool isRedirect = false;
			AppActivationArguments args = AppInstance.GetCurrent().GetActivatedEventArgs();
			ExtendedActivationKind kind = args.Kind;
			AppInstance keyInstance = AppInstance.FindOrRegisterForKey("RegistryEditor");

			if (keyInstance.IsCurrent)
			{
				keyInstance.Activated += OnActivated;
			}
			else
			{
				isRedirect = true;
				keyInstance.RedirectActivationToAsync(args).AsTask().Wait();
			}

			if (!isRedirect)
			{
				Application.Start((p) =>
				{
					var context = new DispatcherQueueSynchronizationContext(DispatcherQueue.GetForCurrentThread());
					SynchronizationContext.SetSynchronizationContext(context);
					_ = new App();
				});
			}
		}

		private static void OnActivated(object? sender, AppActivationArguments args)
		{
		}
	}
}
