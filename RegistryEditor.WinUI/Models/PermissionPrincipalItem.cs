namespace RegistryEditor.WinUI.Models
{
	public class PermissionPrincipalItem
	{
		public SID_NAME_USE SidType { get; set; }

		public string SidTypeImagePath
		{
			get
			{
				switch (SidType)
				{
					case SID_NAME_USE.SidTypeAlias:
					case SID_NAME_USE.SidTypeGroup:
					case SID_NAME_USE.SidTypeWellKnownGroup:
						return "ms-appx:///Assets/Images/GroupImage.png";
					case SID_NAME_USE.SidTypeUser:
						return "ms-appx:///Assets/Images/UserImage.png";
					default:
						return "ms-appx:///Assets/Images/UnknownImage.png";
				}
			}
		}

		public string Sid { get; set; }

		public string Domain { get; set; }

		public string Name { get; set; }

		public string DisplayName
			=> string.IsNullOrEmpty(Name) ? Sid : Name;

		public string FullName
			=> string.IsNullOrEmpty(Domain) ? string.Empty : $"{Domain}\\{Name}";

		public AccessRuleAdvancedItem AccessRuleAdvanced { get; set; }

		public AccessRuleMergedItem AccessRuleMerged { get; set; }
	}
}
