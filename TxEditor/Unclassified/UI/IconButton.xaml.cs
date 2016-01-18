using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Unclassified.UI
{
	public partial class IconButton : Button, ICollapsableToolbarItem
	{
		public static DependencyProperty IconSourceProperty = DependencyProperty.Register(
			"IconSource",
			typeof(ImageSource),
			typeof(IconButton));

		public static DependencyProperty ContentVisibilityProperty = DependencyProperty.Register(
			"ContentVisibility",
			typeof(Visibility),
			typeof(IconButton),
			new PropertyMetadata(Visibility.Collapsed, ContentVisibilityChanged));

		public static DependencyProperty OrientationProperty = DependencyProperty.Register(
			"Orientation",
			typeof(Orientation),
			typeof(IconButton),
			new PropertyMetadata(Orientation.Horizontal));

		public static DependencyProperty HotkeyTextProperty = DependencyProperty.Register(
			"HotkeyText",
			typeof(string),
			typeof(IconButton),
			new PropertyMetadata(HotkeyTextChanged));

		public static DependencyProperty ExtendedToolTipTextProperty = DependencyProperty.Register(
			"ExtendedToolTipText",
			typeof(string),
			typeof(IconButton),
			new PropertyMetadata(HotkeyTextChanged));   // Does the same anyway

		public ImageSource IconSource
		{
			get { return (ImageSource)GetValue(IconSourceProperty); }
			set { SetValue(IconSourceProperty, value); }
		}

		public Visibility ContentVisibility
		{
			get { return (Visibility)GetValue(ContentVisibilityProperty); }
			set { SetValue(ContentVisibilityProperty, value); }
		}

		public Orientation Orientation
		{
			get { return (Orientation)GetValue(OrientationProperty); }
			set { SetValue(OrientationProperty, value); }
		}

		public string HotkeyText
		{
			get { return (string)GetValue(HotkeyTextProperty); }
			set { SetValue(HotkeyTextProperty, value); }
		}

		public string ExtendedToolTipText
		{
			get { return (string)GetValue(ExtendedToolTipTextProperty); }
			set { SetValue(ExtendedToolTipTextProperty, value); }
		}

		public int CollapsePriority { get; set; }

		public IconButton()
		{
			InitializeComponent();
		}

		protected override void OnContentChanged(object oldContent, object newContent)
		{
			base.OnContentChanged(oldContent, newContent);
			ContentVisibility = (newContent is string ? !string.IsNullOrEmpty((string)newContent) : newContent != null) ? Visibility.Visible : Visibility.Collapsed;
			UpdateToolTip();
		}

		private static void ContentVisibilityChanged(DependencyObject depObj, DependencyPropertyChangedEventArgs args)
		{
			IconButton button = depObj as IconButton;
			button.UpdateToolTip();
		}

		private static void HotkeyTextChanged(DependencyObject depObj, DependencyPropertyChangedEventArgs args)
		{
			IconButton button = depObj as IconButton;
			button.UpdateToolTip();
		}

		private void UpdateToolTip()
		{
			bool showToolTip = ContentVisibility == Visibility.Collapsed || !string.IsNullOrEmpty(HotkeyText) || !string.IsNullOrEmpty(ExtendedToolTipText);

			if (showToolTip)
			{
				if (!string.IsNullOrEmpty(HotkeyText) || !string.IsNullOrEmpty(ExtendedToolTipText))
				{
					Grid tipGrid = new Grid();
					tipGrid.RowDefinitions.Add(new RowDefinition());
					tipGrid.RowDefinitions.Add(new RowDefinition());
					tipGrid.ColumnDefinitions.Add(new ColumnDefinition());
					tipGrid.MaxWidth = 250;

					TextBlock contentText = new TextBlock();
					contentText.Text = Content as string;
					contentText.Margin = new Thickness(0, 1, 0, 0);
					if (!string.IsNullOrEmpty(HotkeyText))
					{
						contentText.Text += " (" + HotkeyText + ")";
					}
					if (!string.IsNullOrEmpty(ExtendedToolTipText))
					{
						contentText.FontWeight = FontWeights.Bold;
					}
					contentText.TextWrapping = TextWrapping.Wrap;
					tipGrid.Children.Add(contentText);

					if (!string.IsNullOrEmpty(ExtendedToolTipText))
					{
						TextBlock extText = new TextBlock();
						extText.Text = ExtendedToolTipText;
						extText.Margin = new Thickness(0, 6, 0, 0);
						extText.TextWrapping = TextWrapping.Wrap;
						tipGrid.Children.Add(extText);
						Grid.SetRow(extText, 1);
					}

					ToolTip = tipGrid;
				}
				else
				{
					ToolTip = Content;
				}
			}
			else
			{
				ToolTip = null;
			}
		}

		protected override void OnToolTipOpening(ToolTipEventArgs args)
		{
			Grid toolGrid = null;

			FrameworkElement parent = this;
			while (true)
			{
				parent = parent.Parent as FrameworkElement;
				if (parent == null)
				{
					break;
				}
				if (parent is Grid)
				{
					toolGrid = parent as Grid;
					break;
				}
			}

			if (toolGrid != null)
			{
				Point offset = TranslatePoint(new Point(0, 0), toolGrid);
				ToolTipService.SetPlacement(this, System.Windows.Controls.Primitives.PlacementMode.Relative);
				ToolTipService.SetVerticalOffset(this, toolGrid.ActualHeight - offset.Y + 1);
			}

			ToolTipService.SetShowDuration(this, 20000);
			base.OnToolTipOpening(args);
		}
	}
}
