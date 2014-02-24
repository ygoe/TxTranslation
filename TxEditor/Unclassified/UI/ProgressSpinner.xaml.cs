using System;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace Unclassified.UI
{
	public partial class ProgressSpinner : UserControl
	{
		public static DependencyProperty ColorProperty = DependencyProperty.Register(
			"Color",
			typeof(Color),
			typeof(ProgressSpinner),
			new PropertyMetadata(Colors.Black, ColorChanged));
		public Color Color
		{
			get { return (Color) GetValue(ColorProperty); }
			set { SetValue(ColorProperty, value); }
		}

		private static void ColorChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			ProgressSpinner ctl = d as ProgressSpinner;
			ctl.Recreate();
		}

		public static DependencyProperty BackColorProperty = DependencyProperty.Register(
			"BackColor",
			typeof(Color),
			typeof(ProgressSpinner),
			new PropertyMetadata(Colors.White, BackColorChanged));
		public Color BackColor
		{
			get { return (Color) GetValue(BackColorProperty); }
			set { SetValue(BackColorProperty, value); }
		}

		private static void BackColorChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			ProgressSpinner ctl = d as ProgressSpinner;
			ctl.Recreate();
		}

		public ProgressSpinner()
		{
			InitializeComponent();
		}

		protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
		{
			base.OnRenderSizeChanged(sizeInfo);

			MainGridRotateTransform.CenterX = sizeInfo.NewSize.Width / 2;
			MainGridRotateTransform.CenterY = sizeInfo.NewSize.Width / 2;

			Recreate();
		}

		private void Recreate()
		{
			MainGrid.Children.Clear();

			// Determine the smaller of both edge lengths
			double length = Math.Min(ActualWidth, ActualHeight);
			if (length <= 0) return;
			// Determine a stroke thickness from the control's size
			double thickness = length / 5;
			// Keep space for half the thickness on both sides
			length -= thickness;
			// Circumference is needed for other calculations
			double circumference = length * Math.PI;
			// Margin around the path centre on each side
			double margin = thickness / 2;
			// Radius for circle calculations
			double radius = length / 2;
			// Number of segments to draw
			int count = (int) circumference / 2;
			// Compute the angle step of one segment
			double step = 360 / count;
			// Compute overlap of arc segments to avoid rendering gaps
			double extraAngle = 1 / circumference * (Math.PI * 2);   // One pixel around the circle line in radian measure

			// Create a path for each step
			for (int index = 0; index < count; index++)
			{
				// Compute start and end angles
				double startAngle = Math.PI * 2 / count * index - extraAngle;
				double endAngle = Math.PI * 2 / count * (index + 1);

				// Compute start and end coordinates of the arc segment
				double x0 = margin + radius + Math.Cos(startAngle) * radius;
				double y0 = margin + radius + Math.Sin(startAngle) * radius;
				double x1 = margin + radius + Math.Cos(endAngle) * radius;
				double y1 = margin + radius + Math.Sin(endAngle) * radius;

				// Determine the path's colour
				Color c = BlendColors(Color, Colors.White, (float) index / (count - 1));
				//byte opacity = (byte) Math.Round(255 - (double) index / (count - 1) * 255);

				// Create the path object
				Path p = new Path();
				p.StrokeThickness = thickness;
				//p.Stroke = new SolidColorBrush(Color.FromArgb(255, opacity, opacity, opacity));
				p.Stroke = new SolidColorBrush(c);
				p.Data = Geometry.Parse("M" + ts(x0) + "," + ts(y0) + " A" + ts(radius) + "," + ts(radius) + " " + ts(step) + " 0 1 " + ts(x1) + "," + ts(y1));
				MainGrid.Children.Add(p);
			}
		}

		private static Color BlendColors(Color c1, Color c2, float ratio = 0.5f)
		{
			return Color.Add(Color.Multiply(c1, ratio), Color.Multiply(c2, 1 - ratio));
		}

		private static string ts(double d)
		{
			return d.ToString(CultureInfo.InvariantCulture);
		}
	}
}
