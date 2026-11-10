// Copyright (c) Files Community
// SPDX-License-Identifier: MPL-2.0

using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;

namespace RegistryEditor.Controls
{
	/// <summary>
	/// Initializes a new instance of the BreadcrumbBarItemAutomationPeer class.
	/// </summary>
	/// <param name="owner"></param>
	public partial class BreadcrumbBarItemAutomationPeer(BreadcrumbBarItem owner) : FrameworkElementAutomationPeer(owner), IInvokeProvider
	{

		// IAutomationPeerOverrides
		protected override string GetLocalizedControlTypeCore()
		{
			return "breadcrumb bar item";
		}

		protected override object GetPatternCore(PatternInterface patternInterface)
		{
			if (patternInterface is PatternInterface.ExpandCollapse or PatternInterface.Invoke)
			{
				return this;
			}

			return base.GetPatternCore(patternInterface);
		}

		protected override string GetClassNameCore()
		{
			return nameof(BreadcrumbBarItem);
		}

		protected override AutomationControlType GetAutomationControlTypeCore()
		{
			return AutomationControlType.SplitButton;
		}

		protected override bool IsControlElementCore()
		{
			return true;
		}

		/// <inheritdoc/>
		public void Invoke()
		{
			if (Owner is not BreadcrumbBarItem item)
			{
				return;
			}

			item.OnItemClicked();
		}
	}
}
