using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Media.Animation;

namespace Unclassified.UI
{
	static class AnimationHelper
	{
		public static void AnimateDouble(this UIElement obj, DependencyProperty property, double fromValue, double toValue, TimeSpan duration)
		{
			var anim = new DoubleAnimation(fromValue, toValue, new Duration(duration));
			obj.BeginAnimation(property, anim);
		}

		public static void AnimateDouble(this Animatable obj, DependencyProperty property, double fromValue, double toValue, TimeSpan duration)
		{
			var anim = new DoubleAnimation(fromValue, toValue, new Duration(duration));
			obj.BeginAnimation(property, anim);
		}

		public static void AnimateDoubleEase(this UIElement obj, DependencyProperty property, double fromValue, double toValue, TimeSpan duration)
		{
			var anim = new DoubleAnimation(fromValue, toValue, new Duration(duration));
			anim.AccelerationRatio = 0.5;
			anim.DecelerationRatio = 0.5;
			obj.BeginAnimation(property, anim);
		}

		public static void AnimateDoubleEase(this Animatable obj, DependencyProperty property, double fromValue, double toValue, TimeSpan duration)
		{
			var anim = new DoubleAnimation(fromValue, toValue, new Duration(duration));
			anim.AccelerationRatio = 0.5;
			anim.DecelerationRatio = 0.5;
			obj.BeginAnimation(property, anim);
		}

		public static void AnimateDoubleEaseIn(this UIElement obj, DependencyProperty property, double fromValue, double toValue, TimeSpan duration)
		{
			var anim = new DoubleAnimation(fromValue, toValue, new Duration(duration));
			anim.AccelerationRatio = 1;
			obj.BeginAnimation(property, anim);
		}

		public static void AnimateDoubleEaseIn(this Animatable obj, DependencyProperty property, double fromValue, double toValue, TimeSpan duration)
		{
			var anim = new DoubleAnimation(fromValue, toValue, new Duration(duration));
			anim.AccelerationRatio = 1;
			obj.BeginAnimation(property, anim);
		}

		public static void AnimateDoubleEaseOut(this UIElement obj, DependencyProperty property, double fromValue, double toValue, TimeSpan duration)
		{
			var anim = new DoubleAnimation(fromValue, toValue, new Duration(duration));
			anim.DecelerationRatio = 1;
			obj.BeginAnimation(property, anim);
		}

		public static void AnimateDoubleEaseOut(this Animatable obj, DependencyProperty property, double fromValue, double toValue, TimeSpan duration)
		{
			var anim = new DoubleAnimation(fromValue, toValue, new Duration(duration));
			anim.DecelerationRatio = 1;
			obj.BeginAnimation(property, anim);
		}
	}
}
