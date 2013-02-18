using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Controls.Primitives;
using System.Text.RegularExpressions;

namespace TxEditor
{
	public partial class DecoratedTextBox : UserControl
	{
		private List<FrameworkElement> decos = new List<FrameworkElement>();
		private Popup popup;

		public static DependencyProperty TextProperty = DependencyProperty.Register(
			"Text",
			typeof(string),
			typeof(DecoratedTextBox),
			new FrameworkPropertyMetadata("", null));

		public string Text
		{
			get { return (string) GetValue(TextProperty); }
			set { SetValue(TextProperty, value); }
		}

		public DecoratedTextBox()
		{
			InitializeComponent();

			//var scrollViewer = textBox1.Template.FindName("PART_ContentHost", textBox1) as ScrollViewer;
		}

		private void textBox1_TextChanged(object sender, TextChangedEventArgs e)
		{
			foreach (FrameworkElement el in decos)
			{
				grid1.Children.Remove(el);
			}
			decos.Clear();

			Match m = Regex.Match(textBox1.Text, @"\{.*?\}");
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
						rect.Fill = new SolidColorBrush(Color.FromRgb(229, 237, 193));
						grid1.Children.Insert(0, rect);
						decos.Add(rect);
					}
					else
					{
						// Spanning multiple lines
						// Need more rectangles
					}
				}
				m = m.NextMatch();
			}

			int pos = -1;
			while (true)
			{
				pos = textBox1.Text.IndexOfAny(new char[] { ' ', '\n', '\t', '\xa0' }, pos + 1);
				if (pos < 0) break;

				Rect startRect = textBox1.GetRectFromCharacterIndex(pos);
				Rect endRect = textBox1.GetRectFromCharacterIndex(pos + 1);

				if (!startRect.IsEmpty && !endRect.IsEmpty)
				{
					TextBlock tb;
					if ((int) startRect.Top == (int) endRect.Top)
					{
						// Single line
						switch (textBox1.Text[pos])
						{
							case ' ':
								//Ellipse ell = new Ellipse();
								//ell.HorizontalAlignment = HorizontalAlignment.Left;
								//ell.VerticalAlignment = VerticalAlignment.Top;
								//ell.Margin = new Thickness(Math.Round((startRect.Left + endRect.Left) / 2) - 1, Math.Round((startRect.Top + startRect.Bottom) / 2), 0, 0);
								//ell.Width = 2;
								//ell.Height = 2;
								//ell.Fill = Brushes.DarkGray;
								//grid1.Children.Insert(0, ell);
								//decos.Add(ell);
								tb = new TextBlock();
								tb.HorizontalAlignment = HorizontalAlignment.Left;
								tb.VerticalAlignment = VerticalAlignment.Top;
								tb.Margin = new Thickness(startRect.Left, startRect.Top, 0, 0);
								tb.Text = "·";
								tb.Foreground = Brushes.DarkGray;
								grid1.Children.Insert(0, tb);
								decos.Add(tb);
								break;
							case '\t':
								tb = new TextBlock();
								tb.HorizontalAlignment = HorizontalAlignment.Left;
								tb.VerticalAlignment = VerticalAlignment.Top;
								tb.Margin = new Thickness(startRect.Left, startRect.Top, 0, 0);
								tb.Text = "→";
								tb.Foreground = Brushes.DarkGray;
								grid1.Children.Insert(0, tb);
								decos.Add(tb);
								break;
							case '\xa0':
								tb = new TextBlock();
								tb.HorizontalAlignment = HorizontalAlignment.Left;
								tb.VerticalAlignment = VerticalAlignment.Top;
								tb.Margin = new Thickness(startRect.Left - 1, startRect.Top, 0, 0);
								tb.Text = "°";
								tb.Foreground = Brushes.DarkGray;
								grid1.Children.Insert(0, tb);
								decos.Add(tb);
								break;
						}
					}
					else
					{
						// Spanning multiple lines
						switch (textBox1.Text[pos])
						{
							case '\n':
								tb = new TextBlock();
								tb.HorizontalAlignment = HorizontalAlignment.Left;
								tb.VerticalAlignment = VerticalAlignment.Top;
								tb.Margin = new Thickness(startRect.Left, startRect.Top, 0, 0);
								tb.Text = "¶";
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

		private void textBox1_SelectionChanged(object sender, RoutedEventArgs e)
		{
			// Remove the popup if the caret goes out of the { } range
			// Remember this range when opening the popup
			// Update the range as the input is changed, also when a } is added
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
	}
}
