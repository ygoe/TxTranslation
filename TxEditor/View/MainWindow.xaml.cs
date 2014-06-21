using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Unclassified.TxEditor.ViewModel;
using Unclassified.UI;
using Unclassified.Util;

namespace Unclassified.TxEditor.View
{
	public partial class MainWindow : Window
	{
		#region Static constructor

		static MainWindow()
		{
			ViewCommandManager.SetupMetadata<MainWindow>();
		}

		#endregion Static constructor

		#region Static data

		public static MainWindow Instance { get; private set; }

		#endregion Static data

		#region Private data

		/// <summary>
		/// Reference to all HotKey instances to prevent garbage collection.
		/// </summary>
		private List<HotKey> hotKeys = new List<HotKey>();

		#endregion Private data

		#region Constructors

		public MainWindow()
		{
			Instance = this;

			InitializeComponent();

			WindowStartupLocation = WindowStartupLocation.Manual;
			Left = App.Settings.GetInt("window.left", (int) SystemParameters.WorkArea.Left + 20);
			Top = App.Settings.GetInt("window.top", (int) SystemParameters.WorkArea.Top + 20);
			Width = App.Settings.GetInt("window.width", 950);
			Height = App.Settings.GetInt("window.height", 600);
			WindowState = (WindowState) App.Settings.GetInt("window.state", (int) WindowState.Normal);
		}

		#endregion Constructors

		#region Window event handlers

		private void Window_Loaded(object sender, RoutedEventArgs args)
		{
			hotKeys.Add(new HotKey(Key.T, HotKeyModifier.Ctrl | HotKeyModifier.Shift, OnHotKey));

			TextKeysTreeView.Focus();

			// Let all other contents load
			TaskHelper.WhenLoaded(MainViewModel.Instance.InitCommand.Execute);
		}

		private void OnHotKey(HotKey hotKey)
		{
			var vm = DataContext as MainViewModel;
			if (vm != null)
			{
				vm.TextKeyWizardFromHotKey();
			}
		}

		private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs args)
		{
			MainViewModel vm = DataContext as MainViewModel;
			if (vm != null && !vm.CheckModifiedSaved())
			{
				args.Cancel = true;
				return;
			}
		}

		private void Window_Closed(object sender, EventArgs args)
		{
			App.Settings.SaveNow();
		}

		private void Window_LocationChanged(object sender, EventArgs args)
		{
			if (App.Settings != null)
			{
				App.Settings.Set("window.left", (int) RestoreBounds.Left);
				App.Settings.Set("window.top", (int) RestoreBounds.Top);
				App.Settings.Set("window.width", (int) RestoreBounds.Width);
				App.Settings.Set("window.height", (int) RestoreBounds.Height);
				App.Settings.Set("window.state", (int) WindowState);
			}
		}

		private void Window_SizeChanged(object sender, SizeChangedEventArgs args)
		{
			Window_LocationChanged(this, EventArgs.Empty);
		}

		private void Window_PreviewKeyDown(object sender, KeyEventArgs args)
		{
			if (args.Key == Key.F && args.KeyboardDevice.Modifiers == ModifierKeys.Control)
			{
				SearchText.Focus();
			}
			if (args.Key == Key.Escape && args.KeyboardDevice.Modifiers == ModifierKeys.None)
			{
				if (SearchText.IsKeyboardFocused)
				{
					SearchText.Text = "";
				}
				TextKeysTreeView.Focus();
			}
			if (args.Key == Key.F2 && args.KeyboardDevice.Modifiers == ModifierKeys.None)
			{
				(DataContext as MainViewModel).RenameTextKeyCommand.TryExecute();
			}
		}

		private void Window_Drop(object sender, DragEventArgs args)
		{
			MainViewModel vm = DataContext as MainViewModel;
			if (vm != null)
			{
				var fileNames = args.Data.GetData(DataFormats.FileDrop) as string[];
				if (fileNames != null && fileNames.Length > 0)
				{
					if (fileNames.Length == 1 && System.IO.Directory.Exists(fileNames[0]))
					{
						vm.DoLoadFolder(fileNames[0]);
					}
					else
					{
						vm.DoLoadFiles(fileNames);
					}
					Activate();
				}
			}
		}

		#endregion Window event handlers

		#region Tool grid layouting

		private bool collapsingItems;
		private double lastToolGridWidth;

		private void InnerToolGrid_LayoutUpdated(object sender, EventArgs args)
		{
			// NOTE: The collapsing method is only called when the layout was updated after a
			// change in the window width. Other layout events include resizing of tool buttons
			// (when changing the button text), but also click events on some buttons.
			// ToggleButtons fail when they and the next collapse priority have been collapsed.
			// As a work-around, no further UpdateLayout (and hence also no AutoCollapseItems)
			// call shall be made if the window size has not changed.
			if (ToolGrid.ActualWidth != lastToolGridWidth)
			{
				lastToolGridWidth = ToolGrid.ActualWidth;
				AutoCollapseItems();
			}
		}

		private void AutoCollapseItems()
		{
			if (collapsingItems) return;
			if (ToolGrid.ActualWidth < 10) return;   // Something is wrong, maybe the window is minimised?

			// Prevent further calls before the layouting is completely finished
			collapsingItems = true;
			Dispatcher.BeginInvoke((Action) (() => { collapsingItems = false; }), System.Windows.Threading.DispatcherPriority.Loaded);

			// Collapse toolbar items in their specified priority to save space until all items
			// fit in the toolbar. When collapsing, the item's display style is reduced from
			// image and text to image-only. This is only applied to items with a specified
			// collapse priority.

			// Restore the display style of all items that have a collpase priority.
			var items = new List<ICollapsableToolbarItem>();
			EnumCollapsableItems(InnerToolGrid, items);
			Dictionary<ICollapsableToolbarItem, int> collapsePriorities = new Dictionary<ICollapsableToolbarItem, int>();
			foreach (var item in items)
			{
				if (item.CollapsePriority > 0)
				{
					item.ContentVisibility = Visibility.Visible;
					collapsePriorities[item] = item.CollapsePriority;
				}
			}

			// Find the width of the right-most separator if there is one.
			// This width can be subtracted from the toolbar's preferred width because the last
			// separator doesn't need to be shown.
			// TODO
			//int extentWidth = 0;
			//var lastItem = Items.OfType<ToolStripItem>().OrderByDescending(i => GetColumn(i)).FirstOrDefault();
			//if (lastItem is ToolStripSeparator)
			//{
			//    extentWidth = lastItem.Width;
			//}
			int extentWidth = 6;

			// Group all items by their descending collapse priority and set their display style
			// to image-only as long as all items don't fit in the toolbar.
			var itemGroups =
				from kvp in collapsePriorities
				where kvp.Value > 0
				group kvp by kvp.Value into g
				orderby g.Key descending
				select g;
			foreach (var grp in itemGroups)
			{
				InnerToolGrid.UpdateLayout();
				if (InnerToolGrid.RenderSize.Width - extentWidth <= ToolGrid.ActualWidth) break;
				foreach (var kvp in grp)
				{
					kvp.Key.ContentVisibility = Visibility.Collapsed;
				}
			}
		}

		private void EnumCollapsableItems(Panel root, List<ICollapsableToolbarItem> items)
		{
			foreach (var child in root.Children)
			{
				ICollapsableToolbarItem item = child as ICollapsableToolbarItem;
				if (item != null)
				{
					items.Add(item);
					continue;
				}
				Panel childPanel = child as Panel;
				if (childPanel != null)
				{
					EnumCollapsableItems(childPanel, items);
				}
			}
		}

		#endregion Tool grid layouting

		#region Toolbar event handlers

		private void CharMapButton_ToolTipOpening(object sender, ToolTipEventArgs args)
		{
			FrameworkElement obj = sender as FrameworkElement;
			if (obj != null)
			{
				ToolTipService.SetPlacement(obj, System.Windows.Controls.Primitives.PlacementMode.Relative);
				ToolTipService.SetVerticalOffset(obj, obj.ActualHeight + 6);
			}
		}

		private void CharmapButton_Click(object sender, RoutedEventArgs args)
		{
			Button button = sender as Button;
			string text = button.Content as string;

			var target = Keyboard.FocusedElement;
			var routedEvent = TextCompositionManager.TextInputEvent;

			target.RaiseEvent(new TextCompositionEventArgs(
				InputManager.Current.PrimaryKeyboardDevice,
				new TextComposition(InputManager.Current, target, text)) { RoutedEvent = routedEvent }
			);
		}

		#endregion Toolbar event handlers

		#region Tree event handlers

		private void TextKeysTreeView_SelectionChanged(object sender, EventArgs args)
		{
			var vm = DataContext as MainViewModel;
			if (vm != null)
			{
				vm.TextKeySelectionChanged(TextKeysTreeView.SelectedItems);
			}
		}

		private void TextKeysTreeView_ContextMenuOpening(object sender, ContextMenuEventArgs args)
		{
			Dispatcher.BeginInvoke(new Action(() =>
			{
				// Wait until DataBind until the Commands are available
				foreach (var menuItem in TextKeysTreeView.ContextMenu.Items.OfType<MenuItem>())
				{
					menuItem.Visibility = menuItem.Command != null && menuItem.Command.CanExecute(null) ? Visibility.Visible : Visibility.Collapsed;
				}

				TextKeysTreeView.ContextMenu.ReduceSeparators();

				// If no menu item is visible, don't show the menu
				if (!TextKeysTreeView.ContextMenu.Items.OfType<MenuItem>().Any(mi => mi.Visibility == Visibility.Visible))
				{
					TextKeysTreeView.ContextMenu.IsOpen = false;
				}
			}), System.Windows.Threading.DispatcherPriority.DataBind);

			//// If no menu item is visible, don't show the menu
			//if (!TextKeysTreeView.ContextMenu.Items.OfType<MenuItem>().Any(mi => mi.Visibility == Visibility.Visible))
			//{
			//    args.Handled = true;
			//}
		}

		private void TextKeysTreeView_KeyDown(object sender, KeyEventArgs args)
		{
			if (args.Key == Key.Delete && args.KeyboardDevice.Modifiers == 0)
			{
				(DataContext as MainViewModel).DeleteTextKeyCommand.TryExecute();
				args.Handled = true;
			}
		}

		#endregion Tree event handlers

		#region View commands

		[ViewCommand]
		public void SelectTextKey(object textKey)
		{
			TextKeysTreeView.SelectedItems.Clear();
			if (textKey != null)
			{
				TextKeysTreeView.SelectedItems.Add(textKey);
				TextKeysTreeView.FocusItem(textKey, true);
			}
			else if (TextKeysTreeView.Items.Count > 0)
			{
				TextKeysTreeView.FocusItem(TextKeysTreeView.Items[0], true);
			}
			else
			{
				TextKeysTreeView.Focus();
			}
		}

		[ViewCommand]
		public void SelectPreviousTextKey(string cultureName)
		{
			TextKeysTreeView.Focus();
			Dispatcher.BeginInvoke(
				new Action(delegate
					{
						TextKeysTreeView.SelectPreviousItem();
						var tkVM = TextKeysTreeView.LastSelectedItem as TextKeyViewModel;
						if (tkVM != null && tkVM.IsFullKey)
						{
							var ctVM = tkVM.CultureTextVMs.Where(ct => ct.CultureName == cultureName).FirstOrDefault();
							if (ctVM != null)
								ctVM.ViewCommandManager.InvokeLoaded("FocusText");
						}
					}),
				System.Windows.Threading.DispatcherPriority.Loaded);
		}

		[ViewCommand]
		public void SelectNextTextKey(string cultureName)
		{
			TextKeysTreeView.Focus();
			Dispatcher.BeginInvoke(
				new Action(delegate
				{
					TextKeysTreeView.SelectNextItem();
					var tkVM = TextKeysTreeView.LastSelectedItem as TextKeyViewModel;
					if (tkVM != null && tkVM.IsFullKey)
					{
						var ctVM = tkVM.CultureTextVMs.Where(ct => ct.CultureName == cultureName).FirstOrDefault();
						if (ctVM != null)
							ctVM.ViewCommandManager.InvokeLoaded("FocusText");
					}
				}),
				System.Windows.Threading.DispatcherPriority.Loaded);
		}

		private DelayedCall statusTextAnimationDc;

		[ViewCommand]
		public void AnimateStatusText(string newText)
		{
			int durationMs = 300;
			TimeSpan duration = TimeSpan.FromMilliseconds(durationMs);
			double offset = Math.Max(15, StatusText.ActualHeight);

			// Prepare animation
			if (statusTextAnimationDc != null)
			{
				statusTextAnimationDc.Fire();
			}
			StatusTextShadow.Text = newText;

			// Animate both text blocks
			AnimationHelper.AnimateEaseOut(StatusTextTranslateTransform, TranslateTransform.YProperty, 0, -offset, duration);
			AnimationHelper.AnimateEaseOut(StatusText, TextBlock.OpacityProperty, 1, 0, duration);
			AnimationHelper.AnimateEaseOut(StatusTextShadowTranslateTransform, TranslateTransform.YProperty, offset, 0, duration);
			AnimationHelper.AnimateEaseOut(StatusTextShadow, TextBlock.OpacityProperty, 0, 1, duration);

			// Finish up animation
			statusTextAnimationDc = DelayedCall.Start(StatusTextAnimationFinished, durationMs);
		}

		private void StatusTextAnimationFinished()
		{
			StatusText.Text = StatusTextShadow.Text;

			StatusTextShadowTranslateTransform.Y = -StatusTextTranslateTransform.Y;
			StatusTextShadow.Opacity = 0;
			StatusTextTranslateTransform.Y = 0;
			StatusText.Opacity = 1;

			statusTextAnimationDc = null;
		}

		#endregion View commands
	}
}
