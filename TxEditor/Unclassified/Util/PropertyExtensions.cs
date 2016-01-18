using System;
using System.ComponentModel;
using System.Linq.Expressions;
using System.Reflection;

namespace Unclassified.Util
{
	/// <summary>
	/// Provides extension methods for class property handling.
	/// </summary>
	public static class PropertyExtensions
	{
		#region INotifyPropertyChanged helpers

		/// <summary>
		/// Invokes the specified action when the value of the source property changed.
		/// </summary>
		/// <param name="source">Instance of the type that defines the property. Must implement INotifyPropertyChanged.</param>
		/// <param name="propertyName">Name of the property.</param>
		/// <param name="handler">Action that handles the changed value.</param>
		/// <param name="notifyNow">true to invoke the handler now, too.</param>
		public static void OnPropertyChanged(
			this INotifyPropertyChanged source,
			string propertyName,
			Action handler,
			bool notifyNow = false)
		{
			source.PropertyChanged += (s, e) =>
			{
				if (e.PropertyName == propertyName)
				{
					handler();
				}
			};

			if (notifyNow)
			{
				handler();
			}
		}

		/// <summary>
		/// Invokes the specified action when the value of the source property changed.
		/// </summary>
		/// <typeparam name="TSource">Type that defines the property.</typeparam>
		/// <typeparam name="TProperty">Value type of the property.</typeparam>
		/// <param name="source">Instance of the type that defines the property. Must implement INotifyPropertyChanged.</param>
		/// <param name="expr">Lambda expression of the property.</param>
		/// <param name="handler">Action that handles the changed value.</param>
		/// <param name="notifyNow">true to invoke the handler now, too.</param>
		public static void OnPropertyChanged<TSource, TProperty>(
			this TSource source,
			Expression<Func<TSource, TProperty>> expr,
			Action handler,
			bool notifyNow = false)
			where TSource : INotifyPropertyChanged
		{
			var memberExpr = expr.Body as MemberExpression;
			if (memberExpr != null)
			{
				PropertyInfo property = memberExpr.Member as PropertyInfo;
				if (property != null)
				{
					source.PropertyChanged += (s, e) =>
					{
						if (e.PropertyName == property.Name)
						{
							handler();
						}
					};

					if (notifyNow)
					{
						handler();
					}
					return;
				}
			}
			throw new ArgumentException("Unsupported expression type.");
		}

		/// <summary>
		/// Invokes the specified action when the value of the source property changed.
		/// </summary>
		/// <typeparam name="TSource">Type that defines the property.</typeparam>
		/// <typeparam name="TProperty">Value type of the property.</typeparam>
		/// <param name="source">Instance of the type that defines the property. Must implement INotifyPropertyChanged.</param>
		/// <param name="expr">Lambda expression of the property.</param>
		/// <param name="handler">Action that handles the changed value.</param>
		/// <param name="notifyNow">true to invoke the handler now, too.</param>
		public static void OnPropertyChanged<TSource, TProperty>(
			this TSource source,
			Expression<Func<TSource, TProperty>> expr,
			Action<TProperty> handler,
			bool notifyNow = false)
			where TSource : INotifyPropertyChanged
		{
			var memberExpr = expr.Body as MemberExpression;
			if (memberExpr != null)
			{
				PropertyInfo property = memberExpr.Member as PropertyInfo;
				if (property != null)
				{
					source.PropertyChanged += (s, e) =>
					{
						if (e.PropertyName == property.Name)
						{
							handler((TProperty)property.GetValue(source, null));
						}
					};

					if (notifyNow)
					{
						handler((TProperty)property.GetValue(source, null));
					}
					return;
				}
			}
			throw new ArgumentException("Unsupported expression type.");
		}

		/// <summary>
		/// Binds the value of a property of a source object to a property of a target object.
		/// Whenever either property is changed, the other side of the binding is set to the same
		/// value. To avoid endless loops, the PropertyChanged event must only be raised when the
		/// value actually changed, not already on assignment. The target property is updated
		/// immediately after setting up the binding, if <paramref name="direction"/> is not
		/// OneWayToSource.
		/// </summary>
		/// <typeparam name="TTarget">Type that defines the target property.</typeparam>
		/// <typeparam name="TSource">Type that defines the source property.</typeparam>
		/// <typeparam name="TProperty">Value type of both properties.</typeparam>
		/// <param name="target">Instance of the type that defines the target property. Must implement INotifyPropertyChanged.</param>
		/// <param name="targetExpr">Lambda expression of the target property.</param>
		/// <param name="source">Instance of the type that defines the source property. Must implement INotifyPropertyChanged.</param>
		/// <param name="sourceExpr">Lambda expression of the source property.</param>
		/// <param name="direction">The direction of property value updates.</param>
		/// <example>
		/// Bind a source property to a local property of the same type:
		/// <code>
		/// this.BindProperty(me => me.TargetProperty, source, src => src.SourceProperty);
		/// </code>
		/// </example>
		public static void BindProperty<TTarget, TSource, TProperty>(
			this TTarget target,
			Expression<Func<TTarget, TProperty>> targetExpr,
			TSource source,
			Expression<Func<TSource, TProperty>> sourceExpr,
			PropertyBindingDirection direction = PropertyBindingDirection.TwoWay)
			where TTarget : INotifyPropertyChanged
			where TSource : INotifyPropertyChanged
		{
			// Initialise all expression parts and reflected properties
			var targetMemberExpr = targetExpr.Body as MemberExpression;
			if (targetMemberExpr == null)
				throw new ArgumentException("Unsupported target expression type.");
			PropertyInfo targetProperty = targetMemberExpr.Member as PropertyInfo;
			if (targetProperty == null)
				throw new ArgumentException("Unsupported target expression type.");

			var sourceMemberExpr = sourceExpr.Body as MemberExpression;
			if (sourceMemberExpr == null)
				throw new ArgumentException("Unsupported source expression type.");
			PropertyInfo sourceProperty = sourceMemberExpr.Member as PropertyInfo;
			if (sourceProperty == null)
				throw new ArgumentException("Unsupported source expression type.");

			if (direction != PropertyBindingDirection.OneWayToSource)
			{
				// When the source changes, update the target
				source.PropertyChanged += (s, e) =>
				{
					if (e.PropertyName == sourceProperty.Name)
					{
						targetProperty.SetValue(
							target,
							sourceProperty.GetValue(source, null),
							null);
					}
				};
			}
			if (direction != PropertyBindingDirection.OneWayToTarget)
			{
				// When the target changes, update the source
				target.PropertyChanged += (s, e) =>
				{
					if (e.PropertyName == targetProperty.Name)
					{
						sourceProperty.SetValue(
							source,
							targetProperty.GetValue(target, null),
							null);
					}
				};
			}

			if (direction != PropertyBindingDirection.OneWayToSource)
			{
				// Update the target immediately
				targetProperty.SetValue(
					target,
					sourceProperty.GetValue(source, null),
					null);
			}
		}

		/// <summary>
		/// Returns the name of the member in a lambda expression.
		/// </summary>
		/// <typeparam name="TSource">Type that defines the property.</typeparam>
		/// <typeparam name="TProperty">Value type of the property.</typeparam>
		/// <param name="source">Instance of the type that defines the property.</param>
		/// <param name="expr">Lambda expression of the property.</param>
		/// <returns></returns>
		/// <example>
		/// <code>
		/// string name = this.MemberName(x => x.MyProperty);
		/// </code>
		/// The value of name is set to "MyProperty", unless changed by a code obfuscator.
		/// </example>
		public static string MemberName<TSource, TProperty>(
			this TSource source,
			Expression<Func<TSource, TProperty>> expr)
		{
			if (expr == null)
				throw new ArgumentNullException("expr");
			var memberExpr = expr.Body as MemberExpression;
			if (memberExpr != null)
			{
				return memberExpr.Member.Name;
			}
			throw new ArgumentException("Unsupported expression type.");
		}

		#endregion INotifyPropertyChanged helpers
	}

	/// <summary>
	/// Defines values for the direction of property value bindings.
	/// </summary>
	public enum PropertyBindingDirection
	{
		/// <summary>Updates a changed value on either side to the other side.</summary>
		TwoWay,
		/// <summary>Only updates a changed value on the source to the target.</summary>
		OneWayToTarget,
		/// <summary>Only updates a changed value on the target to the source.</summary>
		OneWayToSource
	}
}
