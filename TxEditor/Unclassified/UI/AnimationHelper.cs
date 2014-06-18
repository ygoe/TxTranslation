using System;
using System.Linq;
using System.Windows;
using System.Windows.Media.Animation;

namespace Unclassified.UI
{
	internal static class AnimationHelper
	{
		#region Double animations

		public static void Animate(this UIElement obj, DependencyProperty property, double fromValue, double toValue, TimeSpan duration)
		{
			var anim = new DoubleAnimation(fromValue, toValue, new Duration(duration));
			obj.BeginAnimation(property, anim);
		}

		public static void Animate(this Animatable obj, DependencyProperty property, double fromValue, double toValue, TimeSpan duration)
		{
			var anim = new DoubleAnimation(fromValue, toValue, new Duration(duration));
			obj.BeginAnimation(property, anim);
		}

		public static void AnimateEase(this UIElement obj, DependencyProperty property, double fromValue, double toValue, TimeSpan duration)
		{
			var anim = new DoubleAnimation(fromValue, toValue, new Duration(duration));
			anim.AccelerationRatio = 0.5;
			anim.DecelerationRatio = 0.5;
			obj.BeginAnimation(property, anim);
		}

		public static void AnimateEase(this Animatable obj, DependencyProperty property, double fromValue, double toValue, TimeSpan duration)
		{
			var anim = new DoubleAnimation(fromValue, toValue, new Duration(duration));
			anim.AccelerationRatio = 0.5;
			anim.DecelerationRatio = 0.5;
			obj.BeginAnimation(property, anim);
		}

		public static void AnimateEaseIn(this UIElement obj, DependencyProperty property, double fromValue, double toValue, TimeSpan duration)
		{
			var anim = new DoubleAnimation(fromValue, toValue, new Duration(duration));
			anim.AccelerationRatio = 1;
			obj.BeginAnimation(property, anim);
		}

		public static void AnimateEaseIn(this Animatable obj, DependencyProperty property, double fromValue, double toValue, TimeSpan duration)
		{
			var anim = new DoubleAnimation(fromValue, toValue, new Duration(duration));
			anim.AccelerationRatio = 1;
			obj.BeginAnimation(property, anim);
		}

		public static void AnimateEaseOut(this UIElement obj, DependencyProperty property, double fromValue, double toValue, TimeSpan duration)
		{
			var anim = new DoubleAnimation(fromValue, toValue, new Duration(duration));
			anim.DecelerationRatio = 1;
			obj.BeginAnimation(property, anim);
		}

		public static void AnimateEaseOut(this Animatable obj, DependencyProperty property, double fromValue, double toValue, TimeSpan duration)
		{
			var anim = new DoubleAnimation(fromValue, toValue, new Duration(duration));
			anim.DecelerationRatio = 1;
			obj.BeginAnimation(property, anim);
		}

		public static void StopDoubleAnimation(this UIElement obj, DependencyProperty property)
		{
			var anim = new DoubleAnimation(0, 0, new Duration());
			anim.BeginTime = null;
			obj.BeginAnimation(property, anim);
		}

		public static void StopDoubleAnimation(this Animatable obj, DependencyProperty property)
		{
			var anim = new DoubleAnimation(0, 0, new Duration());
			anim.BeginTime = null;
			obj.BeginAnimation(property, anim);
		}

		#endregion Double animations

		#region Thickness animations

		public static void Animate(this UIElement obj, DependencyProperty property, Thickness fromValue, Thickness toValue, TimeSpan duration)
		{
			var anim = new ThicknessAnimation(fromValue, toValue, new Duration(duration));
			obj.BeginAnimation(property, anim);
		}

		public static void Animate(this Animatable obj, DependencyProperty property, Thickness fromValue, Thickness toValue, TimeSpan duration)
		{
			var anim = new ThicknessAnimation(fromValue, toValue, new Duration(duration));
			obj.BeginAnimation(property, anim);
		}

		public static void AnimateEase(this UIElement obj, DependencyProperty property, Thickness fromValue, Thickness toValue, TimeSpan duration)
		{
			var anim = new ThicknessAnimation(fromValue, toValue, new Duration(duration));
			anim.AccelerationRatio = 0.5;
			anim.DecelerationRatio = 0.5;
			obj.BeginAnimation(property, anim);
		}

		public static void AnimateEase(this Animatable obj, DependencyProperty property, Thickness fromValue, Thickness toValue, TimeSpan duration)
		{
			var anim = new ThicknessAnimation(fromValue, toValue, new Duration(duration));
			anim.AccelerationRatio = 0.5;
			anim.DecelerationRatio = 0.5;
			obj.BeginAnimation(property, anim);
		}

		public static void AnimateEaseIn(this UIElement obj, DependencyProperty property, Thickness fromValue, Thickness toValue, TimeSpan duration)
		{
			var anim = new ThicknessAnimation(fromValue, toValue, new Duration(duration));
			anim.AccelerationRatio = 1;
			obj.BeginAnimation(property, anim);
		}

		public static void AnimateEaseIn(this Animatable obj, DependencyProperty property, Thickness fromValue, Thickness toValue, TimeSpan duration)
		{
			var anim = new ThicknessAnimation(fromValue, toValue, new Duration(duration));
			anim.AccelerationRatio = 1;
			obj.BeginAnimation(property, anim);
		}

		public static void AnimateEaseOut(this UIElement obj, DependencyProperty property, Thickness fromValue, Thickness toValue, TimeSpan duration)
		{
			var anim = new ThicknessAnimation(fromValue, toValue, new Duration(duration));
			anim.DecelerationRatio = 1;
			obj.BeginAnimation(property, anim);
		}

		public static void AnimateEaseOut(this Animatable obj, DependencyProperty property, Thickness fromValue, Thickness toValue, TimeSpan duration)
		{
			var anim = new ThicknessAnimation(fromValue, toValue, new Duration(duration));
			anim.DecelerationRatio = 1;
			obj.BeginAnimation(property, anim);
		}

		#endregion Thickness animations
	}
}
