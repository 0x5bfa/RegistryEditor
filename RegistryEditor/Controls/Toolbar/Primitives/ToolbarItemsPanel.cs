// Copyright (c) Files Community
// SPDX-License-Identifier: MPL-2.0

using Windows.Foundation;

namespace RegistryEditor.Controls.Primitives
{
	/// <summary>
	/// Measures all toolbar controls while arranging only controls selected for the primary row.
	/// </summary>
	public sealed partial class ToolbarItemsPanel : Panel
	{
		private readonly HashSet<UIElement> _visibleChildren = [];

		/// <summary>
		/// Identifies the <see cref="Spacing"/> dependency property.
		/// </summary>
		public static readonly DependencyProperty SpacingProperty = DependencyProperty.Register(nameof(Spacing), typeof(double), typeof(ToolbarItemsPanel), new PropertyMetadata(0d, OnSpacingChanged));

		/// <summary>
		/// Gets or sets the horizontal spacing between arranged controls.
		/// </summary>
		public double Spacing
		{
			get => (double)GetValue(SpacingProperty);
			set => SetValue(SpacingProperty, value);
		}

		/// <inheritdoc/>
		protected override Size MeasureOverride(Size availableSize)
		{
			var desiredWidth = 0d;
			var desiredHeight = 0d;
			var visibleCount = 0;
			var infiniteSize = new Size(double.PositiveInfinity, double.PositiveInfinity);
			foreach (var child in Children)
			{
				child.Measure(infiniteSize);
				if (_visibleChildren.Contains(child))
				{
					desiredWidth += child.DesiredSize.Width;
					desiredHeight = Math.Max(desiredHeight, child.DesiredSize.Height);
					visibleCount++;
				}
			}

			if (visibleCount > 1)
			{
				desiredWidth += (visibleCount - 1) * Spacing;
			}

			return new Size(desiredWidth, desiredHeight);
		}

		/// <inheritdoc/>
		protected override Size ArrangeOverride(Size finalSize)
		{
			var offset = 0d;
			var visibleCount = 0;
			foreach (var child in Children)
			{
				if (_visibleChildren.Contains(child))
				{
					if (visibleCount > 0)
					{
						offset += Spacing;
					}

					child.Arrange(new Rect(offset, 0, child.DesiredSize.Width, finalSize.Height));
					offset += child.DesiredSize.Width;
					visibleCount++;
				}
				else
				{
					child.Arrange(new Rect(0, 0, 0, 0));
				}
			}

			return finalSize;
		}

		internal void SetVisibleChildren(IEnumerable<UIElement> visibleChildren)
		{
			var updatedChildren = visibleChildren.ToHashSet();
			if (_visibleChildren.SetEquals(updatedChildren))
			{
				return;
			}

			_visibleChildren.Clear();
			_visibleChildren.UnionWith(updatedChildren);
			InvalidateMeasure();
		}

		private static void OnSpacingChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
		{
			if (sender is ToolbarItemsPanel panel)
			{
				panel.InvalidateMeasure();
			}
		}
	}
}
