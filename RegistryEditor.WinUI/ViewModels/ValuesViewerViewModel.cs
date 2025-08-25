using Microsoft.UI.Xaml;
using RegistryEditor.WinUI.Models;
using System.Runtime.InteropServices;
using Vanara.InteropServices;

namespace RegistryEditor.WinUI.ViewModels
{
	public class ValuesViewerViewModel : ObservableObject
	{
		public ValuesViewerViewModel()
		{
			_valueItems = new();
			ValueItems = new(_valueItems);

			_selectedKeyPathItems = new();
			SelectedKeyPathItems = new(_selectedKeyPathItems);

			InitializeBreadcrumbBarItems();
		}

		#region Fields and Properties
		private readonly ObservableCollection<ValueItem> _valueItems;
		public ReadOnlyObservableCollection<ValueItem> ValueItems { get; }

		private readonly ObservableCollection<BreadcrumbBarPathItem> _selectedKeyPathItems;
		public ReadOnlyObservableCollection<BreadcrumbBarPathItem> SelectedKeyPathItems { get; }

		private KeyItem _selectedKeyItem;
		public KeyItem SelectedKeyItem
		{
			get => _selectedKeyItem;
			set
			{
				SetProperty(ref _selectedKeyItem, value);

				_valueItems.Clear();
				InitializeBreadcrumbBarItems();

				if (value.RootHive != HKEY.NULL)
				{
					var result = EnumerateRegistryValues(value.RootHive, value.Path);
					if (result.Failed)
						StatusBarMessage = result.FormatMessage();
				}
			}
		}

		private ValueItem _selectedValueItem;
		public ValueItem SelectedValueItem { get => _selectedValueItem; set => SetProperty(ref _selectedValueItem, value); }

		private string _statusBarMessage;
		public string StatusBarMessage { get => _statusBarMessage; set => SetProperty(ref _statusBarMessage, value); }

		private GridLength _columnName = new(256d);
		public GridLength ColumnName { get => _columnName; set => SetProperty(ref _columnName, value); }

		private GridLength _columnType = new(144d);
		public GridLength ColumnType { get => _columnType; set => SetProperty(ref _columnType, value); }
		#endregion

		#region Methods
		private void InitializeBreadcrumbBarItems()
		{
			_selectedKeyPathItems.Clear();
			_selectedKeyPathItems.Add(new() { PathItem = "Computer" });
		}

		public Win32Error EnumerateRegistryValues(HKEY hRootKey, string subRoot)
		{
			_valueItems.Clear();

			SetBreadcrumbBarItems(hRootKey, subRoot);

			#region Win32API Calling
			Win32Error result;

			// Win32API
			result = RVRegOpenKey(hRootKey, subRoot, REGSAM.KEY_QUERY_VALUE | REGSAM.READ_CONTROL, out var handle);
			if (result.Failed)
			{
				return Kernel32.GetLastError();
			}

			// Win32API
			result = RegQueryInfoKey(handle, null, ref NullRef<uint>(), default, out _, out _, out _, out var cValues, out var cbMaxValueNameLen, out var cbMaxValueLen, out _, out _);
			if (result.Failed)
			{
				return result;
			}

			ValueItem defaultItem = new();
			bool hasDefaultKey = false;

			uint cchValueName;
			uint cbData;
			StringBuilder valueName;
			SafeHGlobalHandle data;

			for (uint index = 0; index < cValues; index++)
			{
				cchValueName = cbMaxValueNameLen + 4;
				valueName = new((int)cchValueName);
				cbData = cbMaxValueLen + (cbMaxValueLen % 2);
				data = new SafeHGlobalHandle(cbData);

				// Win32API
				result = RegEnumValue(handle, index, valueName, ref cchValueName, default, out var type, data, ref cbData);
				if (result.Failed)
				{
					return result;
				}

				ValueItem item = new()
				{
					Name = valueName.ToString(),
					DisplayName = valueName.ToString(),
					TypeString = type.ToString(),
					DataSize = cbData,
					Type = type,
				};

				if (string.IsNullOrEmpty(item.Name) && !hasDefaultKey)
				{
					defaultItem = new()
					{
						Name = valueName.ToString(),
						DataSize = 0,
						DisplayName = "(Default)",
						IsRenamable = false,
						DisplayValue = data.ToString(-1, CharSet.Auto),
						EditableValue = "",
						Type = REG_VALUE_TYPE.REG_SZ,
						TypeString = "REG_SZ",
					};

					hasDefaultKey = true;

					data.Close();
					continue;
				}
				// TODO: Buggy. Because of Vanara?
				else if (string.IsNullOrEmpty(item.Name))
					continue;

				switch (type)
				{
					case REG_VALUE_TYPE.REG_SZ:
						{
							var value = data.ToString(-1, CharSet.Auto);

							item.DisplayValue = value;
							item.EditableValue = item.DisplayValue;
						}
						break;

					case REG_VALUE_TYPE.REG_EXPAND_SZ:
						{
							var value = data.ToString(-1, CharSet.Auto);

							item.DisplayValue = value;
							item.EditableValue = item.DisplayValue;
						}
						break;

					case REG_VALUE_TYPE.REG_BINARY:
						{
							var value = data.ToStructure<byte[]>();
							value = value.Take((int)item.DataSize).ToArray();

							if (value.Length == 0)
							{
								item.DisplayValue = $"(zero-length binary value)";
								item.EditableValue = "";
								break;
							}

							foreach (var atom in value)
							{
								item.DisplayValue += string.Format("{0,2:x2} ", Convert.ToUInt32(atom));
							}

							item.DisplayValue = item.DisplayValue.TrimEnd();
							item.EditableValue = item.DisplayValue;
						}
						break;

					case REG_VALUE_TYPE.REG_DWORD:
						{
							item.TypeString = "REG_DWORD";

							var value = data.ToStructure<uint>();

							item.DisplayValue = string.Format("0x{0,8:x8} ({1})", value, value);
							item.EditableValue = value.ToString();
						}
						break;

					case REG_VALUE_TYPE.REG_MULTI_SZ:
						{
							var value = data.ToString(-1, CharSet.Auto);

							foreach (var atom in value.Split('\n'))
							{
								item.DisplayValue += $"{atom} ";
							}

							item.DisplayValue = item.DisplayValue.TrimEnd();
							item.EditableValue = value;
						}
						break;

					case REG_VALUE_TYPE.REG_QWORD:
						{
							item.TypeString = "REG_QWORD";

							var value = data.ToStructure<ulong>();

							item.DisplayValue = string.Format("0x{0,16:x16} ({1})", value, value);
							item.EditableValue = value.ToString();
						}
						break;
				}

				_valueItems.Add(item);
				data.Close();
			}

			if (!hasDefaultKey)
			{
				defaultItem = new()
				{
					Name = "",
					DataSize = 0,
					DisplayName = "(Default)",
					IsRenamable = false,
					DisplayValue = "(Value not set)",
					EditableValue = "",
					Type = REG_VALUE_TYPE.REG_SZ,
					TypeString = "REG_SZ",
				};
			}
			#endregion

			var alphabetic = new ObservableCollection<ValueItem>(_valueItems.OrderBy(x => x.DisplayName));
			_valueItems.Clear();
			foreach (var item in alphabetic)
				_valueItems.Add(item);

			_valueItems.Insert(0, defaultItem);

			return Win32Error.ERROR_SUCCESS;
		}

		public void SetBreadcrumbBarItems(HKEY hkey, string subRoot)
		{
			if (hkey == HKEY.HKEY_CLASSES_ROOT)
				_selectedKeyPathItems.Add(new() { PathItem = "HKEY_CLASSES_ROOT" });
			else if (hkey == HKEY.HKEY_CURRENT_CONFIG)
				_selectedKeyPathItems.Add(new() { PathItem = "HKEY_CURRENT_CONFIG" });
			else if (hkey == HKEY.HKEY_CURRENT_USER)
				_selectedKeyPathItems.Add(new() { PathItem = "HKEY_CURRENT_USER" });
			else if (hkey == HKEY.HKEY_LOCAL_MACHINE)
				_selectedKeyPathItems.Add(new() { PathItem = "HKEY_LOCAL_MACHINE" });
			else if (hkey == HKEY.HKEY_USERS)
				_selectedKeyPathItems.Add(new() { PathItem = "HKEY_USERS" });

			if (string.IsNullOrEmpty(subRoot) || subRoot.Split('\\').Length == 0)
			{
				_selectedKeyPathItems[^1].IsLast = true;
				return;
			}

			subRoot = subRoot.TrimEnd('\\');
			var items = subRoot.Split('\\');

			foreach (var item in items)
			{
				_selectedKeyPathItems.Add(new() { PathItem = item });
			}

			_selectedKeyPathItems[^1].IsLast = true;
		}
		#endregion
	}
}
