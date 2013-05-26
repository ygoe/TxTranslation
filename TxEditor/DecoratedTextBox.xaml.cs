using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace TxEditor
{
	public partial class DecoratedTextBox : UserControl
	{
		#region Private data

		private bool hasFocus;
		private List<FrameworkElement> decos = new List<FrameworkElement>();
		private bool isEditing;
		private DispatcherTimer editTimer; 
		//private Popup popup;
		// Popup see: http://www.codeproject.com/Articles/22803/Intellisense-like-Method-Selection-Pop-up-Window

		#endregion Private data

		#region Dependency properties

		public static DependencyProperty InnerBorderThicknessProperty = DependencyProperty.Register(
			"InnerBorderThickness",
			typeof(Thickness),
			typeof(DecoratedTextBox),
			new FrameworkPropertyMetadata(new Thickness(1)));
		public Thickness InnerBorderThickness
		{
			get { return (Thickness) GetValue(InnerBorderThicknessProperty); }
			set { SetValue(InnerBorderThicknessProperty, value); }
		}

		public static DependencyProperty InnerPaddingProperty = DependencyProperty.Register(
			"InnerPadding",
			typeof(Thickness),
			typeof(DecoratedTextBox),
			new FrameworkPropertyMetadata(new Thickness(1)));
		public Thickness InnerPadding
		{
			get { return (Thickness) GetValue(InnerPaddingProperty); }
			set { SetValue(InnerPaddingProperty, value); }
		}

		public static DependencyProperty TextProperty = DependencyProperty.Register(
			"Text",
			typeof(string),
			typeof(DecoratedTextBox),
			new FrameworkPropertyMetadata("", TextChanged));
		public string Text
		{
			get { return (string) GetValue(TextProperty); }
			set { SetValue(TextProperty, value); }
		}
		private static void TextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			DecoratedTextBox dt = d as DecoratedTextBox;
			if (dt != null)
			{
				dt.PlaceholderVisibility = string.IsNullOrEmpty(e.NewValue as string) ? Visibility.Visible : Visibility.Collapsed;
				dt.IsEditing = true;
			}
		}

		public static DependencyProperty CursorCharProperty = DependencyProperty.Register(
			"CursorChar",
			typeof(string),
			typeof(DecoratedTextBox));
		public string CursorChar
		{
			get { return (string) GetValue(CursorCharProperty); }
			set { SetValue(CursorCharProperty, value); }   // Should be private, but somehow cannot be. Giving up.
		}

		public static DependencyProperty PlaceholderTextProperty = DependencyProperty.Register(
			"PlaceholderText",
			typeof(string),
			typeof(DecoratedTextBox),
			new FrameworkPropertyMetadata("", null));
		public string PlaceholderText
		{
			get { return (string) GetValue(PlaceholderTextProperty); }
			set { SetValue(PlaceholderTextProperty, value); }
		}

		public static DependencyProperty PlaceholderVisibilityProperty = DependencyProperty.Register(
			"PlaceholderVisibility",
			typeof(Visibility),
			typeof(DecoratedTextBox),
			new FrameworkPropertyMetadata(Visibility.Collapsed));
		public Visibility PlaceholderVisibility
		{
			get { return (Visibility) GetValue(PlaceholderVisibilityProperty); }
			set { SetValue(PlaceholderVisibilityProperty, value); }
		}

		public static DependencyProperty HiddenCharsProperty = DependencyProperty.Register(
			"HiddenChars",
			typeof(bool),
			typeof(DecoratedTextBox),
			new FrameworkPropertyMetadata(false, HiddenCharsChanged));
		public bool HiddenChars
		{
			get { return (bool) GetValue(HiddenCharsProperty); }
			set { SetValue(HiddenCharsProperty, value); }
		}
		private static void HiddenCharsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			DecoratedTextBox dt = d as DecoratedTextBox;
			if (dt != null)
			{
				dt.UpdateDecorations();
			}
		}

		#endregion Dependency properties

		#region Properties

		public TextBox InnerTextBox
		{
			get { return textBox1; }
		}

		public bool IsEditing
		{
			get { return isEditing; }
			set
			{
				isEditing = value;
				if (editTimer != null && editTimer.IsEnabled)
					editTimer.Stop();
				editTimer = new DispatcherTimer(TimeSpan.FromSeconds(1), DispatcherPriority.Input, EditTimeout, Dispatcher);
				editTimer.Start();
			}
		}

		#endregion Properties

		#region Static constructor

		// No longer required, left as reference
		//static DecoratedTextBox()
		//{
		//    UserControl.FontSizeProperty.OverrideMetadata(
		//        typeof(DecoratedTextBox),
		//        new FrameworkPropertyMetadata(FontSizeChanged));
		//}

		#endregion Static constructor

		#region Constructors

		public DecoratedTextBox()
		{
			InitializeComponent();

			//var scrollViewer = textBox1.Template.FindName("PART_ContentHost", textBox1) as ScrollViewer;
		}

		#endregion Constructors

		#region Event handlers

		protected override void OnRender(DrawingContext drawingContext)
		{
			base.OnRender(drawingContext);
			UpdateDecorations();
		}

		private void textBox1_TextChanged(object sender, TextChangedEventArgs e)
		{
			UpdateDecorations();
			UpdateCursorChar();
		}

		private void textBox1_SelectionChanged(object sender, RoutedEventArgs e)
		{
			// Remove the popup if the caret goes out of the { } range
			// Remember this range when opening the popup
			// Update the range as the input is changed, also when a } is added

			UpdateCursorChar();
		}

		private void textBox1_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.Key == Key.Space &&
				(e.KeyboardDevice.IsKeyDown(Key.LeftShift) || e.KeyboardDevice.IsKeyDown(Key.RightShift)))
			{
				int sel = textBox1.SelectionStart;
				textBox1.Text = textBox1.Text.Insert(textBox1.CaretIndex, "\xa0");
				textBox1.SelectionStart = sel + 1;
				e.Handled = true;
			}
		}

		private void textBox1_GotFocus(object sender, RoutedEventArgs e)
		{
			hasFocus = true;
			UpdateCursorChar();
		}

		private void textBox1_LostFocus(object sender, RoutedEventArgs e)
		{
			hasFocus = false;
			UpdateCursorChar();
		}

		private void UpdateCursorChar()
		{
			if (hasFocus && textBox1.SelectionStart < textBox1.Text.Length)
				CursorChar = textBox1.Text.Substring(textBox1.SelectionStart, 1);
			else
				CursorChar = null;
		}

		private void EditTimeout(object sender, EventArgs e)
		{
			editTimer.Stop();
			isEditing = false;
			UpdateDecorations();
		}

		#endregion Event handlers

		#region Decoration work

		public event EventHandler<ValidateKeyEventArgs> ValidateKey;

		private bool DoValidateKey(string textKey)
		{
			var e = new ValidateKeyEventArgs(textKey);
			var handler = ValidateKey;
			if (handler != null)
			{
				handler(this, e);
			}
			return e.IsValid;
		}

		public void UpdateDecorations()
		{
			foreach (FrameworkElement el in decos)
			{
				grid1.Children.Remove(el);
			}
			decos.Clear();

			Match m = Regex.Match(textBox1.Text, @"\{(?!=).*?\}");
			while (m.Success)
			{
				Rect startRect = textBox1.GetRectFromCharacterIndex(m.Groups[0].Index);
				Rect endRect = textBox1.GetRectFromCharacterIndex(m.Groups[0].Index + m.Groups[0].Length);

				if (!startRect.IsEmpty && !endRect.IsEmpty)
				{
					if ((int) startRect.Top == (int) endRect.Top)
					{
						// Single line
						Rectangle rect = new Rectangle();
						rect.HorizontalAlignment = HorizontalAlignment.Left;
						rect.VerticalAlignment = VerticalAlignment.Top;
						rect.Margin = new Thickness(startRect.Left, startRect.Top, 0, 0);
						rect.Width = endRect.Left - startRect.Left;
						rect.Height = startRect.Height;
						rect.Fill = new SolidColorBrush(Color.FromRgb(242, 245, 225));
						grid1.Children.Insert(0, rect);
						decos.Add(rect);
					}
					else
					{
						// Spanning multiple lines
						// Need more rectangles
						// TODO
					}
				}
				m = m.NextMatch();
			}

			m = Regex.Match(textBox1.Text, @"\{=([^#]*?)(?:#(.*?))?\}");
			while (m.Success)
			{
				Rect startRect = textBox1.GetRectFromCharacterIndex(m.Groups[0].Index);
				Rect endRect = textBox1.GetRectFromCharacterIndex(m.Groups[0].Index + m.Groups[0].Length);

				bool isValidKey = true;
				if (!IsEditing)
					isValidKey = DoValidateKey(m.Groups[1].Value);

				if (!startRect.IsEmpty && !endRect.IsEmpty)
				{
					if ((int) startRect.Top == (int) endRect.Top)
					{
						// Single line
						Rectangle rect = new Rectangle();
						rect.HorizontalAlignment = HorizontalAlignment.Left;
						rect.VerticalAlignment = VerticalAlignment.Top;
						rect.Margin = new Thickness(startRect.Left, startRect.Top, 0, 0);
						rect.Width = endRect.Left - startRect.Left;
						rect.Height = startRect.Height;
						rect.Fill = new SolidColorBrush(Color.FromRgb(232, 246, 248));
						grid1.Children.Insert(0, rect);
						decos.Add(rect);

						if (!isValidKey)
						{
							Rect startRectKey = textBox1.GetRectFromCharacterIndex(m.Groups[0].Index + 2);
							Rect endRectKey = textBox1.GetRectFromCharacterIndex(m.Groups[0].Index + 2 + m.Groups[1].Length);

							rect = new Rectangle();
							rect.HorizontalAlignment = HorizontalAlignment.Left;
							rect.VerticalAlignment = VerticalAlignment.Top;
							rect.Margin = new Thickness(startRectKey.Left, startRectKey.Bottom - 2, 0, -2);
							rect.Width = endRectKey.Left - startRectKey.Left;
							if (rect.Width < 5)
								rect.Width = 5;
							rect.Height = 3;
							rect.Fill = Resources["SquiggleBrush"] as Brush;
							grid1.Children.Insert(grid1.Children.Count - 2, rect);
							decos.Add(rect);
						}
					}
					else
					{
						// Spanning multiple lines
						// Need more rectangles
						// TODO
					}
				}
				m = m.NextMatch();
			}

			if (HiddenChars)
			{
				int pos = -1;
				while (true)
				{
					pos = textBox1.Text.IndexOfAny(new char[] { ' ', '\n', '\t', '\xa0', '\x2002', '\x2003', '\x2004', '\x2005', '\x2006', '\x2007', '\x2008', '\x2009', '\x200a', '\x200b', '\x202f', '\x205f' }, pos + 1);
					if (pos < 0) break;

					Rect startRect = textBox1.GetRectFromCharacterIndex(pos);
					Rect endRect = textBox1.GetRectFromCharacterIndex(pos + 1);

					if (!startRect.IsEmpty && !endRect.IsEmpty)
					{
						TextBlock tb;
						double width = double.NaN;
						if ((int) startRect.Top == (int) endRect.Top)
						{
							// Single line
							width = endRect.X - startRect.X;
						}
						else
						{
							// Spanning multiple lines
						}
						
						switch (textBox1.Text[pos])
						{
							case '\t':
								tb = new TextBlock();
								tb.HorizontalAlignment = HorizontalAlignment.Left;
								tb.VerticalAlignment = VerticalAlignment.Top;
								if (width != double.NaN)
								{
									tb.Margin = new Thickness(startRect.Left - 10, startRect.Top, 0, 0);
									tb.Width = width + 20;
									tb.TextAlignment = TextAlignment.Center;
								}
								else
								{
									tb.Margin = new Thickness(startRect.Left, startRect.Top, 0, 0);
								}
								tb.Text = "→";
								tb.Foreground = Brushes.DarkGray;
								grid1.Children.Insert(0, tb);
								decos.Add(tb);
								break;
							case ' ':
							case '\u2002':
							case '\u2003':
							case '\u2004':
							case '\u2005':
							case '\u2006':
							case '\u2008':
							case '\u2009':
							case '\u200a':
							case '\u200b':
							case '\u205f':
								tb = new TextBlock();
								tb.HorizontalAlignment = HorizontalAlignment.Left;
								tb.VerticalAlignment = VerticalAlignment.Top;
								if (width != double.NaN)
								{
									tb.Margin = new Thickness(startRect.Left - 10, startRect.Top, 0, 0);
									tb.Width = width + 20;
									tb.TextAlignment = TextAlignment.Center;
								}
								else
								{
									tb.Margin = new Thickness(startRect.Left, startRect.Top, 0, 0);
								}
								tb.Text = "·";
								tb.Foreground = Brushes.DarkGray;
								grid1.Children.Insert(0, tb);
								decos.Add(tb);
								break;
							case '\xa0':
							case '\u2007':
							case '\u202f':
								tb = new TextBlock();
								tb.HorizontalAlignment = HorizontalAlignment.Left;
								tb.VerticalAlignment = VerticalAlignment.Top;
								if (width != double.NaN)
								{
									tb.Margin = new Thickness(startRect.Left - 10, startRect.Top, 0, 0);
									tb.Width = width + 20;
									tb.TextAlignment = TextAlignment.Center;
								}
								else
								{
									tb.Margin = new Thickness(startRect.Left, startRect.Top, 0, 0);
								}
								tb.Text = "°";
								tb.Foreground = Brushes.DarkGray;
								grid1.Children.Insert(0, tb);
								decos.Add(tb);
								break;
							case '\n':
								tb = new TextBlock();
								tb.HorizontalAlignment = HorizontalAlignment.Left;
								tb.VerticalAlignment = VerticalAlignment.Top;
								tb.Margin = new Thickness(startRect.Left, startRect.Top, 0, 0);
								tb.Text = "↲";   // "¶";
								tb.Foreground = Brushes.DarkGray;
								grid1.Children.Insert(0, tb);
								decos.Add(tb);
								break;
						}
					}
				}
			}

			//foreach (var change in e.Changes)
			//{
			//    if (change.AddedLength == 1 &&
			//        textBox1.Text[change.Offset] == '{')
			//    {
			//        Rect caretRect = textBox1.GetRectFromCharacterIndex(change.Offset + 1);

			//        ListBox listBox = new ListBox();
			//        listBox.Items.Add("abc");
			//        listBox.Items.Add("def 123");
			//        listBox.Items.Add("xyz with extras");
			//        listBox.SelectedIndex = 0;

			//        popup = new Popup();
			//        popup.Child = listBox;
			//        popup.Placement = PlacementMode.Bottom;
			//        popup.PlacementRectangle = caretRect;

			//        grid1.Children.Add(popup);
			//        popup.IsOpen = true;
			//    }
			//}
		}

		#endregion Decoration work

		#region Overridden methods

		public new bool Focus()
		{
			return textBox1.Focus();
		}

		#endregion Overridden methods
	}

	public class ValidateKeyEventArgs : EventArgs
	{
		public string TextKey { get; private set; }
		public bool IsValid { get; set; }

		public ValidateKeyEventArgs(string textKey)
		{
			TextKey = textKey;
		}
	}
}
