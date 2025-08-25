using System.Runtime.CompilerServices;
using Windows.Storage;

namespace RegistryValley.App.Services
{
	internal abstract class BaseSettingsServices : ObservableObject, IBaseSettingsServices
	{
		private readonly ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;

		protected TValue Get<TValue>(TValue defaultValue, [CallerMemberName] string propertyName = null)
		{
			if (string.IsNullOrEmpty(propertyName))
				return defaultValue;

			if (localSettings.Values.TryGetValue(propertyName, out object value))
			{
				if (value is not TValue tValue)
				{
					// Put the corrected value in settings.
					TValue originalValue = default;
					Set(originalValue, propertyName);

					return originalValue;
				}

				return (TValue)value;
			}

			localSettings.Values[propertyName] = defaultValue;

			return defaultValue;
		}

		protected bool Set<TValue>(TValue value, [CallerMemberName] string propertyName = null)
		{
			TValue originalValue = default;

			if (localSettings.Values.ContainsKey(propertyName))
			{
				originalValue = Get(originalValue, propertyName);
				localSettings.Values[propertyName] = value;

				if (!SetProperty(ref originalValue, value, propertyName))
				{
					return false;
				}
			}
			else
			{
				localSettings.Values[propertyName] = value;
			}

			return true;
		}
	}
}
