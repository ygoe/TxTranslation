using System;
using System.ComponentModel;
using System.Reflection;

namespace Unclassified
{
	/// <summary>
	/// Provides extension methods for class property handling.
	/// </summary>
	public static class PropertyExtensions
	{
		#region INotifyPropertyChanged helpers

		//public static void OnPropertyChanged(this INotifyPropertyChanged sender, string propertyName, Action handler)
		//{
		//    sender.PropertyChanged += (s, e) =>
		//    {
		//        if (e.PropertyName == propertyName) handler();
		//    };
		//}

		/// <summary>
		/// Links the value of a property of a source object to an action method. Whenever the
		/// source property is changed, the setter action is invoked and can update the value in
		/// another property or method.
		/// </summary>
		/// <typeparam name="TSource">Type that defines the property.</typeparam>
		/// <typeparam name="TProperty">Value type of the property.</typeparam>
		/// <param name="source">Instance of the type that defines the property. Must implement INotifyPropertyChanged.</param>
		/// <param name="expr">Lambda expression of the property.</param>
		/// <param name="handler">Action that handles the changed value.</param>
		/// <example>
		/// Link a source property to a local property of the same type:
		/// <code>
		/// source.LinkProperty(s => s.SourceProperty, v => this.MyProperty = v);
		/// </code>
		/// Link a source property to a local method that accepts the source property's value type
		/// as its only parameter:
		/// <code>
		/// source.LinkProperty(s => s.SourceProperty, v => OnUpdate);
		/// </code>
		/// </example>
		public static void LinkProperty<TSource, TProperty>(
			this TSource source,
			System.Linq.Expressions.Expression<Func<TSource, TProperty>> expr,
			Action<TProperty> handler)
			where TSource : INotifyPropertyChanged
		{
			var memberExpr = expr.Body as System.Linq.Expressions.MemberExpression;
			if (memberExpr != null)
			{
				PropertyInfo property = memberExpr.Member as PropertyInfo;
				if (property != null)
				{
					source.PropertyChanged += (s, e) =>
					{
						if (e.PropertyName == property.Name)
						{
							handler((TProperty) property.GetValue(source, null));
						}
					};

					// Set value immediately
					handler((TProperty) property.GetValue(source, null));
					return;
				}
			}
			throw new ArgumentException("Unsupported expression type.");
		}

		/// <summary>
		/// Returns the property name of a lambda expression.
		/// </summary>
		/// <typeparam name="TSource">Type that defines the property.</typeparam>
		/// <typeparam name="TProperty">Value type of the property.</typeparam>
		/// <param name="source">Instance of the type that defines the property.</param>
		/// <param name="expr">Lambda expression of the property.</param>
		/// <returns></returns>
		/// <example>
		/// <code>
		/// string name = this.ExprName(x => x.MyProperty);
		/// </code>
		/// The value of name is set to "MyProperty".
		/// </example>
		public static string ExprName<TSource, TProperty>(
			this TSource source,
			System.Linq.Expressions.Expression<Func<TSource, TProperty>> expr)
		{
			var memberExpr = expr.Body as System.Linq.Expressions.MemberExpression;
			if (memberExpr != null)
			{
				PropertyInfo property = memberExpr.Member as PropertyInfo;
				if (property != null)
				{
					return property.Name;
				}
			}
			throw new ArgumentException("Unsupported expression type.");
		}

		#endregion INotifyPropertyChanged helpers
	}
}
