using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace RegistryValley.App.UserControls
{
	public sealed partial class BranchDisplay : UserControl
	{
		#region propdp
		public static readonly DependencyProperty NumberOfBranchProperty =
			DependencyProperty.Register(
				nameof(NumberOfBranch),
				typeof(int),
				typeof(BranchDisplay),
				new PropertyMetadata(null)
				);

		public int NumberOfBranch
		{
			get => (int)GetValue(NumberOfBranchProperty);
			set
			{
				SetValue(NumberOfBranchProperty, value);

				_branches.Clear();
				for (int i = 0; i < value - 1; i++)
					_branches.Add(true);
			}
		}

		public static readonly DependencyProperty HasChildrenProperty =
			DependencyProperty.Register(
				nameof(HasChildren),
				typeof(bool),
				typeof(BranchDisplay),
				new PropertyMetadata(null)
				);

		public bool HasChildren
		{
			get => (bool)GetValue(HasChildrenProperty);
			set => SetValue(HasChildrenProperty, value);
		}
		#endregion

		public BranchDisplay()
		{
			InitializeComponent();

			_branches = new();
			Branches = new(_branches);
		}

		private readonly ObservableCollection<bool> _branches;
		public ReadOnlyObservableCollection<bool> Branches { get; }
	}
}
