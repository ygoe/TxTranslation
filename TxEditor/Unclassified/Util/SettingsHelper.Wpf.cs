// Copyright (c) 2015, Yves Goergen, http://unclassified.software/source/settingsadapterfactory
//
// Copying and distribution of this file, with or without modification, are permitted provided the
// copyright notice and this notice are preserved. This file is offered as-is, without any warranty.

using System;
using System.Linq;
using System.Windows;
using System.Windows.Media;

namespace Unclassified.Util
{
	/// <summary>
	/// Provides methods for specialized settings situations.
	/// </summary>
	public static partial class SettingsHelper
	{
		#region Window state handling

		/// <summary>
		/// Binds the window location, size and state to the settings. This method should be called
		/// in the Window constructor after InitializeComponent.
		/// </summary>
		/// <param name="window">The window to update and monitor.</param>
		/// <param name="settings">The settings to use for the window.</param>
		public static void BindWindowState(Window window, IWindowStateSettings settings)
		{
			// Apply the current settings to the window, if available
			if (settings.Left != int.MinValue &&
				settings.Top != int.MinValue &&
				settings.Width != int.MinValue &&
				settings.Height != int.MinValue)
			{
				window.WindowStartupLocation = WindowStartupLocation.Manual;
				window.Left = settings.Left;
				window.Top = settings.Top;
				window.Width = settings.Width;
				window.Height = settings.Height;
				window.WindowState = settings.IsMaximized ? WindowState.Maximized : WindowState.Normal;
			}

			// Write back any changes to the settings.
			// The event signatures are different, so it's easier to just copy the handler code
			// and let the compiler figure out the inferred types.
			window.LocationChanged += (sender, args) =>
			{
				settings.Left = (int)window.RestoreBounds.Left;
				settings.Top = (int)window.RestoreBounds.Top;
				settings.Width = (int)window.RestoreBounds.Width;
				settings.Height = (int)window.RestoreBounds.Height;
				settings.IsMaximized = window.WindowState == WindowState.Maximized;
			};
			window.SizeChanged += (sender, args) =>
			{
				settings.Left = (int)window.RestoreBounds.Left;
				settings.Top = (int)window.RestoreBounds.Top;
				settings.Width = (int)window.RestoreBounds.Width;
				settings.Height = (int)window.RestoreBounds.Height;
				settings.IsMaximized = window.WindowState == WindowState.Maximized;
			};
			window.Closed += (sender, args) =>
			{
				if (window.WindowState == WindowState.Normal)
				{
					settings.Left = (int)window.Left;
					settings.Top = (int)window.Top;
					settings.Width = (int)window.Width;
					settings.Height = (int)window.Height;
				}
				settings.IsMaximized = window.WindowState == WindowState.Maximized;
			};
		}

		#endregion Window state handling

		#region Color string conversion

		/// <summary>
		/// Converts a <see cref="Color"/> value to a string for a settings string property.
		/// </summary>
		/// <param name="color">The color to convert.</param>
		/// <returns>A hexadecimal color representation.</returns>
		public static string ColorToString(Color color)
		{
			return "#" + (color.A != 255 ? color.A.ToString("x2") : "") +
				color.R.ToString("x2") +
				color.G.ToString("x2") +
				color.B.ToString("x2");
		}

		/// <summary>
		/// Converts a hexadecimal or decimal color string to a <see cref="Color"/> value.
		/// </summary>
		/// <param name="str">The string to convert.</param>
		/// <returns>A <see cref="Color"/> value.</returns>
		public static Color StringToColor(string str)
		{
			if (str.StartsWith("#"))
				str = str.Substring(1);
			if (str.Length != 6 && str.Length != 8)
				throw new FormatException("Invalid 6- or 8-digit hexadecimal color value.");
			long value = long.Parse(str, System.Globalization.NumberStyles.AllowHexSpecifier);
			if (str.Length == 6)
				value |= 0xff000000;
			return Color.FromArgb(
				(byte)((value >> 24) & 0xff),
				(byte)((value >> 16) & 0xff),
				(byte)((value >> 8) & 0xff),
				(byte)(value & 0xff));
		}

		#endregion Color string conversion
	}
}
