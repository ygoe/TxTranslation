using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;

namespace Unclassified.UI
{
	public partial class IconToggleButton : ToggleButton, ICollapsableToolbarItem
	{
		#region Dependency properties

		public static DependencyProperty IconSourceProperty = DependencyProperty.Register(
			"IconSource",
			typeof(ImageSource),
			typeof(IconToggleButton));

		public static DependencyProperty ContentVisibilityProperty = DependencyProperty.Register(
			"ContentVisibility",
			typeof(Visibility),
			typeof(IconToggleButton),
			new PropertyMetadata(Visibility.Collapsed, ContentVisibilityChanged));

		public static DependencyProperty OrientationProperty = DependencyProperty.Register(
			"Orientation",
			typeof(Orientation),
			typeof(IconToggleButton),
			new PropertyMetadata(Orientation.Horizontal));

		public static DependencyProperty HotkeyTextProperty = DependencyProperty.Register(
			"HotkeyText",
			typeof(string),
			typeof(IconToggleButton),
			new PropertyMetadata(HotkeyTextChanged));

		public static DependencyProperty ExtendedToolTipTextProperty = DependencyProperty.Register(
			"ExtendedToolTipText",
			typeof(string),
			typeof(IconToggleButton),
			new PropertyMetadata(HotkeyTextChanged));   // Does the same anyway

		public static DependencyProperty ShowMenuProperty = DependencyProperty.Register(
			"ShowMenu",
			typeof(bool),
			typeof(IconToggleButton));

		public ImageSource IconSource
		{
			get { return (ImageSource) GetValue(IconSourceProperty); }
			set { SetValue(IconSourceProperty, value); }
		}

		public Visibility ContentVisibility
		{
			get { return (Visibility) GetValue(ContentVisibilityProperty); }
			set { SetValue(ContentVisibilityProperty, value); }
		}

		public Orientation Orientation
		{
			get { return (Orientation) GetValue(OrientationProperty); }
			set { SetValue(OrientationProperty, value); }
		}

		public string HotkeyText
		{
			get { return (string) GetValue(HotkeyTextProperty); }
			set { SetValue(HotkeyTextProperty, value); }
		}

		public string ExtendedToolTipText
		{
			get { return (string) GetValue(ExtendedToolTipTextProperty); }
			set { SetValue(ExtendedToolTipTextProperty, value); }
		}

		public bool ShowMenu
		{
			get { return (bool) GetValue(ShowMenuProperty); }
			set { SetValue(ShowMenuProperty, value); }
		}

		#endregion Dependency properties

		#region Constructor

		public IconToggleButton()
		{
			InitializeComponent();
		}

		#endregion Constructor

		#region Public properties

		public int CollapsePriority { get; set; }

		#endregion Public properties

		#region ToolTip handling

		protected override void OnContentChanged(object oldContent, object newContent)
		{
			base.OnContentChanged(oldContent, newContent);
			ContentVisibility = (newContent is string ? !string.IsNullOrEmpty((string) newContent) : newContent != null) ? Visibility.Visible : Visibility.Collapsed;
			UpdateToolTip();
		}

		private static void ContentVisibilityChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			IconToggleButton button = d as IconToggleButton;
			button.UpdateToolTip();
		}

		private static void HotkeyTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			IconToggleButton button = d as IconToggleButton;
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
						extText.Margin = new Thickness(6, 6, 0, 0);
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

		protected override void OnToolTipOpening(ToolTipEventArgs e)
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
			base.OnToolTipOpening(e);
		}

		#endregion ToolTip handling

		#region ShowMenu handling

		private bool justPressed;
		private bool cancelOpen;

		protected override void OnPreviewMouseLeftButtonDown(System.Windows.Input.MouseButtonEventArgs e)
		{
			if (ShowMenu && ContextMenu != null)
			{
				justPressed = true;
				DelayedCall.Start(() => { justPressed = false; }, 50);
			}
			base.OnPreviewMouseLeftButtonDown(e);
		}

		protected override void OnChecked(RoutedEventArgs e)
		{
			if (ShowMenu && ContextMenu != null)
			{
				base.OnChecked(e);
				if (cancelOpen)
				{
					DelayedCall.Start(() => { cancelOpen = false; }, 50);
					IsChecked = false;
					return;
				}

				ContextMenu.Closed += ContextMenu_Closed;

				ContextMenu.Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom;
				ContextMenu.PlacementTarget = this;
				ContextMenu.IsOpen = true;
			}
		}

		private void ContextMenu_Closed(object sender, RoutedEventArgs e)
		{
			ContextMenu.Closed -= ContextMenu_Closed;

			IsChecked = false;
			if (justPressed)
			{
				cancelOpen = true;
			}
		}

		#endregion ShowMenu handling
	}
}
