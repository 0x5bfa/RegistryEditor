namespace RegistryValley.App.Models
{
	public class AccessRuleAdvancedItem : ObservableObject
	{
		#region Humanized Strings Properties
		public string HumanizedAccessControlType { get; set; }

		public string HumanizedAccessControl { get; set; }

		public string HumanizedIsInheritance { get; set; }

		public string HumanizedAppliesTo { get; set; }
		#endregion

		#region Basic Permission Properties
		private bool _allowFullControl;
		public bool AllowFullControl { get => _allowFullControl; set => SetProperty(ref _allowFullControl, value); }

		private bool _allowRead;
		public bool AllowRead { get => _allowRead; set => SetProperty(ref _allowRead, value); }

		private bool _allowSpecialPermissions;
		public bool AllowSpecialPermissions { get => _allowSpecialPermissions; set => SetProperty(ref _allowSpecialPermissions, value); }
		#endregion

		#region Advanced Permission Properties
		#endregion

		#region Methods
		#endregion
	}
}
