using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading;

namespace Unclassified.Util
{
	#region SettingsAdapterFactory class

	// Sources:
	// http://www.codeproject.com/Articles/22832/Automatic-Interface-Implementer-An-Example-of-Runt
	// http://www.codeproject.com/Articles/13337/Introduction-to-Creating-Dynamic-Types-with-Reflec
	// http://www.codeproject.com/Articles/13969/Introduction-to-Creating-Dynamic-Types-with-Refl-2 (unread)
	// http://grahammurray.wordpress.com/2010/04/13/dynamically-generating-types-to-implement-inotifypropertychanged/
	//     via http://stackoverflow.com/questions/3639479/implementing-inotifypropertychanged-with-reflection-emit

	/// <summary>
	/// Generates dynamic types that implement an interface with properties that bind to a settings
	/// store and implement INotifyPropertyChanged.
	/// </summary>
	public static class SettingsAdapterFactory
	{
		#region Private data

		private const string MyClass = "SettingsAdapterFactory";

		private static AssemblyBuilder assemblyBuilder;
		private static ModuleBuilder moduleBuilder;

		/// <summary>
		/// Contains the generated type for each interface.
		/// </summary>
		private static Dictionary<Type, Type> generatedTypes = new Dictionary<Type, Type>();

		#endregion Private data

		#region Static constructor

		static SettingsAdapterFactory()
		{
			// All types created by this class go into a single dynamic assembly.
			assemblyBuilder = Thread.GetDomain().DefineDynamicAssembly(
				new AssemblyName() { Name = MyClass + "Asm" },
				AssemblyBuilderAccess.RunAndSave);
			moduleBuilder = assemblyBuilder.DefineDynamicModule(
				MyClass + "Module",
				MyClass + "Asm.dll",
				true);
			// If the module defines a different file name than where the assembly will be written,
			// there a two files written: one with the assembly manifest and another one with the
			// module. So the module should have the same file name as the assembly name.
		}

		#endregion Static constructor

		#region Public methods

		/// <summary>
		/// Returns an object for the specified interface type.
		/// </summary>
		/// <typeparam name="TInterface">The interface type to implement.</typeparam>
		/// <param name="settingsStore">The <see cref="ISettingsStore"/> instance to bind the generated properties to.</param>
		/// <returns>An object that implements the <typeparamref name="TInterface"/> interface.</returns>
		public static TInterface New<TInterface>(ISettingsStore settingsStore)
			where TInterface : class
		{
			Type interfaceType = typeof(TInterface);
			if (!generatedTypes.ContainsKey(interfaceType))
			{
				CreateType(interfaceType);
			}

			// Old:
			// <param name="prefix">The settings path prefix for the new object. (For internal use, leave it unset.)</param>

			//string fileName = assemblyBuilder.GetName().Name + ".dll";
			//if (!File.Exists(fileName))
			//{
			//    assemblyBuilder.Save(fileName);
			//}
			// Verify the assembly with "peverify.exe" from the .NET command prompt.

			return (TInterface) Activator.CreateInstance(generatedTypes[interfaceType], settingsStore, null);
		}

		#endregion Public methods

		#region Type creation methods

		/// <summary>
		/// Creates a class type that implements the specified interface and adds it to the
		/// generatedTypes dictionary.
		/// </summary>
		/// <param name="interfaceType">The interface type to implement.</param>
		private static void CreateType(Type interfaceType)
		{
			if (!interfaceType.IsInterface)
				throw new ArgumentException("The specified type is not an interface: " + interfaceType.FullName);
			if (!typeof(INotifyPropertyChanged).IsAssignableFrom(interfaceType))
				throw new ArgumentException("The specified type does not implement INotifyPropertyChanged: " + interfaceType.FullName);

			string className = interfaceType.Name;
			if (className.StartsWith("I") && className.Length >= 2 && char.IsUpper(className[1]))
				className = className.Substring(1);
			className += "Impl";

			List<FieldBuilder> fields = new List<FieldBuilder>();

			// Create the type
			TypeBuilder typeBuilder = moduleBuilder.DefineType(className, TypeAttributes.Class | TypeAttributes.Public);
			typeBuilder.AddInterfaceImplementation(interfaceType);

			// Create private instance fields
			FieldBuilder settingsStoreField = typeBuilder.DefineField("settingsStore", typeof(ISettingsStore), FieldAttributes.Private);
			fields.Add(settingsStoreField);
			FieldBuilder prefixField = typeBuilder.DefineField("prefix", typeof(string), FieldAttributes.Private);
			fields.Add(prefixField);

			// Create INotifyPropertyChanged implementation
			FieldBuilder eventField = CreatePropertyChangedEvent(typeBuilder);
			MethodBuilder onPropertyChanged = CreateOnPropertyChanged(typeBuilder, eventField);

			// Get a list of all methods and properties of the interface, including in inherited interfaces
			List<MethodInfo> methods = new List<MethodInfo>();
			List<PropertyInfo> properties = new List<PropertyInfo>();
			AddMethodsToList(methods, interfaceType);
			AddPropertiesToList(properties, interfaceType);

			// Create accessors for each property
			foreach (PropertyInfo propertyInfo in properties)
			{
				MethodBuilder newGetMethod = null;
				MethodBuilder newSetMethod = null;

				// Create backing field for interface properties
				if (propertyInfo.PropertyType == typeof(ISettingsStore))
				{
					// This is the SettingsStore property, create a getter for our field
					MethodInfo getMethod = propertyInfo.GetGetMethod();
					if (getMethod == null)
					{
						throw new NotSupportedException("The property " + propertyInfo.Name + " must have a getter.");
					}
					// Don't create a plain method for this property getter method
					methods.Remove(getMethod);
					newGetMethod = CreateFieldGetter(typeBuilder, getMethod, propertyInfo, settingsStoreField);
				}
				else if (propertyInfo.PropertyType.IsInterface)
				{
					FieldBuilder backingField = typeBuilder.DefineField("_" + propertyInfo.Name, propertyInfo.PropertyType, FieldAttributes.Private);
					fields.Add(backingField);

					// Create a getter in the new type
					MethodInfo getMethod = propertyInfo.GetGetMethod();
					if (getMethod == null)
					{
						throw new NotSupportedException("The interface-type property " + propertyInfo.Name + " must have a getter.");
					}
					// Don't create a plain method for this property getter method
					methods.Remove(getMethod);
					newGetMethod = CreateFieldGetter(typeBuilder, getMethod, propertyInfo, backingField);
				}
				else
				{
					// If there is a getter in the interface, create a getter in the new type
					MethodInfo getMethod = propertyInfo.GetGetMethod();
					if (getMethod != null)
					{
						// Don't create a plain method for this property getter method
						methods.Remove(getMethod);
						newGetMethod = CreateStoreGetter(typeBuilder, getMethod, propertyInfo, fields);
					}

					// If there is a setter in the interface, create a setter in the new type
					MethodInfo setMethod = propertyInfo.GetSetMethod();
					if (setMethod != null)
					{
						// Don't create a plain method for this property setter method
						methods.Remove(setMethod);
						newSetMethod = CreateStoreSetter(typeBuilder, setMethod, propertyInfo, fields);
					}
				}

				// Add a property to the type to support WPF binding and reflection
				CreateProperty(typeBuilder, propertyInfo, newGetMethod, newSetMethod);
			}

			// Create empty methods of the interface
			foreach (MethodInfo methodInfo in methods)
			{
				if (methodInfo.Name == "add_PropertyChanged" ||
					methodInfo.Name == "remove_PropertyChanged")
				{
					// These are already defined
					continue;
				}
				CreateEmptyMethod(typeBuilder, methodInfo);
			}

			// Create the instance constructor, now that we know the fields to initialise
			ConstructorBuilder constructor = CreateConstructor(typeBuilder, interfaceType, fields, onPropertyChanged);

			// Create the type
			Type createdType = typeBuilder.CreateType();
			generatedTypes[interfaceType] = createdType;
		}

		/// <summary>
		/// Creates the instance constructor method.
		/// </summary>
		/// <param name="typeBuilder">The TypeBuilder instance.</param>
		/// <param name="interfaceType">The interface type to implement.</param>
		/// <param name="fields">A list of all created fields.</param>
		/// <param name="onPropertyChanged">The OnPropertyChanged method.</param>
		/// <returns>The created constructor.</returns>
		private static ConstructorBuilder CreateConstructor(TypeBuilder typeBuilder, Type interfaceType, List<FieldBuilder> fields, MethodBuilder onPropertyChanged)
		{
			ConstructorInfo baseConstructorInfo = typeof(object).GetConstructor(new Type[0]);

			ConstructorBuilder constructorBuilder = typeBuilder.DefineConstructor(
				MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName,
				CallingConventions.Standard,
				new[] { typeof(ISettingsStore), typeof(string) });

			ILGenerator ilGen = constructorBuilder.GetILGenerator();

			// : base()
			ilGen.Emit(OpCodes.Ldarg_0);
			ilGen.Emit(OpCodes.Call, baseConstructorInfo);

			// Debugging helper:
			//if (interfaceType == typeof(IMainWindow))
			//    ilGen.EmitCall(OpCodes.Call, typeof(Console).GetMethod("Beep", new Type[0]), null);

			// this.settingsStore = parameter_1;
			ilGen.Emit(OpCodes.Ldarg_0);
			ilGen.Emit(OpCodes.Ldarg_1);
			ilGen.Emit(OpCodes.Stfld, fields.First(f => f.Name == "settingsStore"));

			// this.prefix = parameter_2;
			ilGen.Emit(OpCodes.Ldarg_0);
			ilGen.Emit(OpCodes.Ldarg_2);
			ilGen.Emit(OpCodes.Stfld, fields.First(f => f.Name == "prefix"));

			// Assign current settings value to each backing field
			foreach (FieldBuilder field in fields.Where(f => f.Name.StartsWith("_")))
			{
				string propertyName = field.Name.Substring(1);   // Cut away leading "_"

				if (field.FieldType.IsInterface)
				{
					// This is another interface type. Create an implementation of it and write
					// code that creates an instance of it.

					if (!generatedTypes.ContainsKey(field.FieldType))
					{
						CreateType(field.FieldType);
					}

					ilGen.Emit(OpCodes.Ldarg_0);
					ilGen.Emit(OpCodes.Ldarg_1);   // settingsStore
					// -> prefix + [propertyName]
					ilGen.Emit(OpCodes.Ldarg_2);   // prefix
					ilGen.Emit(OpCodes.Ldstr, propertyName);
					ilGen.GenerateCall<string, string, string>("Concat");
					// -> + "."
					ilGen.Emit(OpCodes.Ldstr, ".");
					ilGen.GenerateCall<string, string, string>("Concat");
					// -> new [created implementation type](settingsStore, ...)
					ilGen.Emit(OpCodes.Newobj, generatedTypes[field.FieldType].GetConstructor(new[] { typeof(ISettingsStore), typeof(string) }));
					// -> [field] = new ...
					ilGen.Emit(OpCodes.Stfld, field);
				}
			}

			// if (prefix == null)
			//     this.settingsStore.PropertyChanged += this.OnPropertyChanged;
			Label exitLabel = ilGen.DefineLabel();
			ilGen.Emit(OpCodes.Ldarg_2);
			ilGen.Emit(OpCodes.Brtrue, exitLabel);

			ilGen.Emit(OpCodes.Ldarg_0);
			ilGen.Emit(OpCodes.Ldfld, fields.First(f => f.Name == "settingsStore"));
			ilGen.Emit(OpCodes.Ldarg_0);
			ilGen.Emit(OpCodes.Ldftn, onPropertyChanged);
			ilGen.Emit(OpCodes.Newobj, typeof(PropertyChangedEventHandler).GetConstructor(new[] { typeof(object), typeof(IntPtr) }));
			ilGen.GenerateCallVirt<INotifyPropertyChanged, PropertyChangedEventHandler>("add_PropertyChanged");

			// return;
			ilGen.MarkLabel(exitLabel);
			ilGen.Emit(OpCodes.Ret);

			return constructorBuilder;
		}

		/// <summary>
		/// Creates the PropertyChanged event and assigns new add and remove methods.
		/// </summary>
		/// <param name="typeBuilder">The TypeBuilder instance.</param>
		/// <returns>The created field.</returns>
		private static FieldBuilder CreatePropertyChangedEvent(TypeBuilder typeBuilder)
		{
			// public event PropertyChangedEventHandler PropertyChanged;
			FieldBuilder fieldBuilder = typeBuilder.DefineField(
				"PropertyChanged",
				typeof(PropertyChangedEventHandler),
				FieldAttributes.Private);
			EventBuilder eventBuilder = typeBuilder.DefineEvent(
				"PropertyChanged",
				EventAttributes.None,
				typeof(PropertyChangedEventHandler));

			eventBuilder.SetAddOnMethod(CreateAddRemoveMethod(typeBuilder, fieldBuilder, true));
			eventBuilder.SetRemoveOnMethod(CreateAddRemoveMethod(typeBuilder, fieldBuilder, false));

			return fieldBuilder;
		}

		/// <summary>
		/// Creates a PropertyChanged event add or remove method.
		/// </summary>
		/// <param name="typeBuilder">The TypeBuilder instance.</param>
		/// <param name="eventField">The PropertyChanged event field.</param>
		/// <param name="isAdd">true to create an add method, false to create a remove method.</param>
		/// <returns>The created method.</returns>
		private static MethodBuilder CreateAddRemoveMethod(TypeBuilder typeBuilder, FieldBuilder eventField, bool isAdd)
		{
			string prefix = "add_";
			string delegateAction = "Combine";
			if (!isAdd)
			{
				prefix = "remove_";
				delegateAction = "Remove";
			}

			MethodBuilder methodBuilder = typeBuilder.DefineMethod(
				prefix + "PropertyChanged",
				MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.NewSlot |
				MethodAttributes.HideBySig | MethodAttributes.Virtual | MethodAttributes.Final,
				null,
				new[] { typeof(PropertyChangedEventHandler) });
			methodBuilder.SetImplementationFlags(MethodImplAttributes.Managed | MethodImplAttributes.Synchronized);

			ILGenerator ilGen = methodBuilder.GetILGenerator();

			// PropertyChanged += value; / PropertyChanged -= value;
			ilGen.Emit(OpCodes.Ldarg_0);
			ilGen.Emit(OpCodes.Ldarg_0);
			ilGen.Emit(OpCodes.Ldfld, eventField);
			ilGen.Emit(OpCodes.Ldarg_1);   // value
			ilGen.GenerateCall<Delegate, Delegate, Delegate>(delegateAction);
			ilGen.Emit(OpCodes.Castclass, typeof(PropertyChangedEventHandler));
			ilGen.Emit(OpCodes.Stfld, eventField);
			ilGen.Emit(OpCodes.Ret);

			MethodInfo intAddRemoveMethod = typeof(INotifyPropertyChanged).GetMethod(prefix + "PropertyChanged");
			typeBuilder.DefineMethodOverride(methodBuilder, intAddRemoveMethod);

			return methodBuilder;
		}

		/// <summary>
		/// Creates the OnPropertyChanged method.
		/// </summary>
		/// <param name="typeBuilder">The TypeBuilder instance.</param>
		/// <param name="eventField">The PropertyChanged event field.</param>
		/// <returns>The created method.</returns>
		private static MethodBuilder CreateOnPropertyChanged(TypeBuilder typeBuilder, FieldBuilder eventField)
		{
			MethodBuilder methodBuilder = typeBuilder.DefineMethod(
				"OnPropertyChanged",
				MethodAttributes.Assembly,
				null,
				new[] { typeof(object), typeof(PropertyChangedEventArgs) });

			ILGenerator ilGen = methodBuilder.GetILGenerator();
			Label elseLabel = ilGen.DefineLabel();
			Label exitLabel = ilGen.DefineLabel();

			// Complete C# code overview:
			//
			// void OnPropertyChanged(object sender, PropertyChangedEventArgs args)
			// {
			//     int pointIndex = args.PropertyName.IndexOf('.');
			//     if (pointIndex >= 0)
			//     {
			//         var subProperty = this.GetType().GetProperty(args.PropertyName.Substring(0, pointIndex));
			//         if (subProperty == null) return;
			//         var propVal = subProperty.GetValue(this, null);
			//         var subHandler = propVal.GetType().GetMethod("OnPropertyChanged", BindingFlags.Instance | BindingFlags.NonPublic);
			//         if (subHandler == null) return;
			//         subHandler.Invoke(propVal, new object[] { this, new PropertyChangedEventArgs(args.PropertyName.Substring(pointIndex + 1)) });
			//     }
			//     else
			//     {
			//         if (this.GetType().GetProperty(args.PropertyName) == null) return;
			//         handler = this.PropertyChanged;
			//         if (handler == null) return;
			//         handler(this, args);
			//     }
			// }

			// int pointIndex = args.PropertyName.IndexOf('.');
			LocalBuilder pointIndexLoc = ilGen.DeclareLocal(typeof(int));
			ilGen.Emit(OpCodes.Ldarg_2);   // args
			ilGen.GenerateCallVirt<PropertyChangedEventArgs>("get_PropertyName");
			ilGen.Emit(OpCodes.Ldc_I4_S, 46);   // '.'
			ilGen.GenerateCallVirt<string, char>("IndexOf");
			ilGen.Emit(OpCodes.Stloc, pointIndexLoc);

			// if (pointIndex >= 0)
			// -> if (pointIndex < 0) goto else;
			ilGen.Emit(OpCodes.Ldloc, pointIndexLoc);
			ilGen.Emit(OpCodes.Ldc_I4_0);
			ilGen.Emit(OpCodes.Clt);
			ilGen.Emit(OpCodes.Brtrue, elseLabel);

			// var subProperty = this.GetType().GetProperty(args.PropertyName.Substring(0, pointIndex));
			LocalBuilder subPropertyLoc = ilGen.DeclareLocal(typeof(PropertyInfo));
			// -> this.GetType()
			ilGen.Emit(OpCodes.Ldarg_0);
			ilGen.GenerateCall<object>("GetType");
			// -> args.PropertyName
			ilGen.Emit(OpCodes.Ldarg_2);   // args
			ilGen.GenerateCallVirt<PropertyChangedEventArgs>("get_PropertyName");
			// -> Substring(0, pointIndex)
			ilGen.Emit(OpCodes.Ldc_I4_0);
			ilGen.Emit(OpCodes.Ldloc, pointIndexLoc);
			ilGen.GenerateCallVirt<string, int, int>("Substring");
			// -> subProperty = GetProperty(...)
			ilGen.GenerateCallVirt<Type, string>("GetProperty");
			ilGen.Emit(OpCodes.Stloc, subPropertyLoc);

			// if (subProperty == null) return;
			ilGen.Emit(OpCodes.Ldloc, subPropertyLoc);
			ilGen.Emit(OpCodes.Brfalse, exitLabel);

			// var propVal = subProperty.GetValue(this, null);
			LocalBuilder propValLoc = ilGen.DeclareLocal(typeof(object));
			ilGen.Emit(OpCodes.Ldloc, subPropertyLoc);
			ilGen.Emit(OpCodes.Ldarg_0);
			ilGen.Emit(OpCodes.Ldnull);
			ilGen.GenerateCallVirt<PropertyInfo, object, object[]>("GetValue");
			ilGen.Emit(OpCodes.Stloc, propValLoc);

			// var subHandler = propVal.GetType().GetMethod("OnPropertyChanged", BindingFlags.Instance | BindingFlags.NonPublic);
			LocalBuilder subHandlerLoc = ilGen.DeclareLocal(typeof(MethodInfo));
			ilGen.Emit(OpCodes.Ldloc, propValLoc);
			// -> GetType()
			ilGen.GenerateCall<object>("GetType");
			// -> GetMethod(...)
			ilGen.Emit(OpCodes.Ldstr, "OnPropertyChanged");
			ilGen.Emit(OpCodes.Ldc_I4, (int) (BindingFlags.Instance | BindingFlags.NonPublic));
			ilGen.GenerateCallVirt<Type, string, BindingFlags>("GetMethod");
			// -> subHandler = ...
			ilGen.Emit(OpCodes.Stloc, subHandlerLoc);

			// if (subHandler == null) return;
			ilGen.Emit(OpCodes.Ldloc, subHandlerLoc);
			ilGen.Emit(OpCodes.Brfalse, exitLabel);

			// subHandler.Invoke(propVal, new object[] { this, new PropertyChangedEventArgs(args.PropertyName.Substring(pointIndex + 1)) });
			ilGen.Emit(OpCodes.Ldloc, subHandlerLoc);
			ilGen.Emit(OpCodes.Ldloc, propValLoc);
			// -> new object[] { ..., ... }
			ilGen.Emit(OpCodes.Ldc_I4_2);
			ilGen.Emit(OpCodes.Newarr, typeof(object));
			LocalBuilder objArrLoc = ilGen.DeclareLocal(typeof(object[]));
			ilGen.Emit(OpCodes.Stloc, objArrLoc);
			// -> { this }
			ilGen.Emit(OpCodes.Ldloc, objArrLoc);
			ilGen.Emit(OpCodes.Ldc_I4_0);
			ilGen.Emit(OpCodes.Ldarg_0);
			ilGen.Emit(OpCodes.Stelem_Ref);
			// -> args.PropertyName
			ilGen.Emit(OpCodes.Ldloc, objArrLoc);
			ilGen.Emit(OpCodes.Ldc_I4_1);
			ilGen.Emit(OpCodes.Ldarg_2);   // args
			ilGen.GenerateCallVirt<PropertyChangedEventArgs>("get_PropertyName");
			// -> pointIndex + 1
			ilGen.Emit(OpCodes.Ldloc, pointIndexLoc);
			ilGen.Emit(OpCodes.Ldc_I4_1);
			ilGen.Emit(OpCodes.Add);
			// -> Substring(...)
			ilGen.GenerateCallVirt<string, int>("Substring");
			// -> new PropertyChangedEventArgs(...)
			ilGen.Emit(OpCodes.Newobj, typeof(PropertyChangedEventArgs).GetConstructor(new[] { typeof(string) }));
			// -> { ... }
			ilGen.Emit(OpCodes.Stelem_Ref);
			// -> Invoke(...)
			ilGen.Emit(OpCodes.Ldloc, objArrLoc);
			ilGen.GenerateCallVirt<MethodBase, object, object[]>("Invoke");
			ilGen.Emit(OpCodes.Pop);   // Discard return value, it's a void method
			ilGen.Emit(OpCodes.Br, exitLabel);

			// else
			ilGen.MarkLabel(elseLabel);

			// if (this.GetType().GetProperty(args.PropertyName) == null) return;
			// -> this.GetType()
			ilGen.Emit(OpCodes.Ldarg_0);
			ilGen.GenerateCall<object>("GetType");
			// -> args.PropertyName
			ilGen.Emit(OpCodes.Ldarg_2);   // args
			ilGen.GenerateCallVirt<PropertyChangedEventArgs>("get_PropertyName");
			ilGen.GenerateCallVirt<Type, string>("GetProperty");
			ilGen.Emit(OpCodes.Brfalse, exitLabel);

			// handler = this.PropertyChanged;
			LocalBuilder handlerLoc = ilGen.DeclareLocal(typeof(PropertyChangedEventHandler));
			ilGen.Emit(OpCodes.Ldarg_0);
			ilGen.Emit(OpCodes.Ldfld, eventField);
			ilGen.Emit(OpCodes.Stloc, handlerLoc);

			// if (handler == null) return;
			ilGen.Emit(OpCodes.Ldloc, handlerLoc);
			ilGen.Emit(OpCodes.Brfalse, exitLabel);

			// handler(this, args);
			ilGen.Emit(OpCodes.Ldloc, handlerLoc);
			ilGen.Emit(OpCodes.Ldarg_0);
			ilGen.Emit(OpCodes.Ldarg_2);   // args
			ilGen.GenerateCallVirt<PropertyChangedEventHandler>("Invoke");

			// exit:
			// return;
			ilGen.MarkLabel(exitLabel);
			ilGen.Emit(OpCodes.Ret);

			return methodBuilder;
		}

		/// <summary>
		/// Creates the getter method for a property.
		/// </summary>
		/// <param name="typeBuilder">The TypeBuilder instance.</param>
		/// <param name="getMethod">The interface property method to create.</param>
		/// <param name="propertyInfo">The PropertyInfo object.</param>
		/// <param name="backingField">The field that stores the property's value.</param>
		/// <returns>The created property getter method.</returns>
		private static MethodBuilder CreateFieldGetter(
			TypeBuilder typeBuilder,
			MethodInfo getMethod,
			PropertyInfo propertyInfo,
			FieldBuilder backingField)
		{
			MethodBuilder methodBuilder = typeBuilder.DefineMethod(
				getMethod.Name,
				MethodAttributes.Public | MethodAttributes.Virtual,
				propertyInfo.PropertyType,
				Type.EmptyTypes);

			ILGenerator ilGen = methodBuilder.GetILGenerator();

			// return this.backingField;
			ilGen.Emit(OpCodes.Ldarg_0);
			ilGen.Emit(OpCodes.Ldfld, backingField);
			ilGen.Emit(OpCodes.Ret);

			// Associate new method with the getter method in the interface
			typeBuilder.DefineMethodOverride(methodBuilder, getMethod);

			return methodBuilder;
		}

		/// <summary>
		/// Creates the getter method for a property.
		/// </summary>
		/// <param name="typeBuilder">The TypeBuilder instance.</param>
		/// <param name="getMethod">The interface property method to create.</param>
		/// <param name="propertyInfo">The PropertyInfo object.</param>
		/// <param name="fields">A list of all created fields.</param>
		/// <returns>The created property getter method.</returns>
		private static MethodBuilder CreateStoreGetter(
			TypeBuilder typeBuilder,
			MethodInfo getMethod,
			PropertyInfo propertyInfo,
			List<FieldBuilder> fields)
		{
			MethodBuilder methodBuilder = typeBuilder.DefineMethod(
				getMethod.Name,
				MethodAttributes.Public | MethodAttributes.Virtual,
				propertyInfo.PropertyType,
				Type.EmptyTypes);

			ILGenerator ilGen = methodBuilder.GetILGenerator();

			// Enums seamlessly cast to their underlying type, just consider that type
			// NOTE: This doesn't handle arrays of enums
			Type propType = propertyInfo.PropertyType;
			if (propType.IsEnum)
			{
				propType = propType.GetEnumUnderlyingType();
			}
			
			string storeGetMethod;
			if (propType == typeof(bool))
			{
				storeGetMethod = "GetBool";
			}
			else if (propType == typeof(bool[]))
			{
				storeGetMethod = "GetBoolArray";
			}
			else if (propType == typeof(int))
			{
				storeGetMethod = "GetInt";
			}
			else if (propType == typeof(int[]))
			{
				storeGetMethod = "GetIntArray";
			}
			else if (propType == typeof(long))
			{
				storeGetMethod = "GetLong";
			}
			else if (propType == typeof(long[]))
			{
				storeGetMethod = "GetLongArray";
			}
			else if (propType == typeof(double))
			{
				storeGetMethod = "GetDouble";
			}
			else if (propType == typeof(double[]))
			{
				storeGetMethod = "GetDoubleArray";
			}
			else if (propType == typeof(string))
			{
				storeGetMethod = "GetString";
			}
			else if (propType == typeof(string[]))
			{
				storeGetMethod = "GetStringArray";
			}
			else if (propType == typeof(DateTime))
			{
				storeGetMethod = "GetDateTime";
			}
			else if (propType == typeof(DateTime[]))
			{
				storeGetMethod = "GetDateTimeArray";
			}
			else if (propType == typeof(TimeSpan))
			{
				storeGetMethod = "GetTimeSpan";
			}
			else if (propType == typeof(TimeSpan[]))
			{
				storeGetMethod = "GetTimeSpanArray";
			}
			else
			{
				throw new NotSupportedException(
					"Property type " + propType.Name + " is not supported for code generation: " +
					propertyInfo.DeclaringType.Name + "." + propertyInfo.Name);
			}

			// return settingsStore.GetXXX(prefix + "PropertyName");
			// -or-
			// return settingsStore.GetXXX(prefix + "PropertyName", defaultValue);
			ilGen.Emit(OpCodes.Ldarg_0);
			ilGen.Emit(OpCodes.Ldfld, fields.First(f => f.Name == "settingsStore"));
			ilGen.Emit(OpCodes.Ldarg_0);
			ilGen.Emit(OpCodes.Ldfld, fields.First(f => f.Name == "prefix"));
			ilGen.Emit(OpCodes.Ldstr, propertyInfo.Name);
			ilGen.GenerateCall<string, string, string>("Concat");

			// Consider default value, specified as attribute on the property in the source interface
			object[] attrs = propertyInfo.GetCustomAttributes(typeof(DefaultValueAttribute), false);
			if (attrs.Length == 1)
			{
				DefaultValueAttribute defAttr = (DefaultValueAttribute) attrs[0];
				if (propertyInfo.PropertyType.IsArray)
				{
					throw new NotSupportedException(
						"An array-type property cannot have a default value: " +
						propertyInfo.DeclaringType.Name + "." + propertyInfo.Name);
				}
				if (propType == typeof(bool))
				{
					if ((bool) defAttr.Value)
						ilGen.Emit(OpCodes.Ldc_I4_1);
					else
						ilGen.Emit(OpCodes.Ldc_I4_0);
					ilGen.GenerateCallVirt<ISettingsStore, string, bool>(storeGetMethod);
				}
				else if (propType == typeof(int))
				{
					ilGen.Emit(OpCodes.Ldc_I4, Convert.ToInt32(defAttr.Value, CultureInfo.InvariantCulture));
					ilGen.GenerateCallVirt<ISettingsStore, string, int>(storeGetMethod);
				}
				else if (propType == typeof(long))
				{
					ilGen.Emit(OpCodes.Ldc_I8, Convert.ToInt64(defAttr.Value, CultureInfo.InvariantCulture));
					ilGen.GenerateCallVirt<ISettingsStore, string, long>(storeGetMethod);
				}
				else if (propType == typeof(double))
				{
					ilGen.Emit(OpCodes.Ldc_R8, Convert.ToDouble(defAttr.Value, CultureInfo.InvariantCulture));
					ilGen.GenerateCallVirt<ISettingsStore, string, double>(storeGetMethod);
				}
				else if (propType == typeof(string))
				{
					ilGen.Emit(OpCodes.Ldstr, Convert.ToString(defAttr.Value, CultureInfo.InvariantCulture));
					ilGen.GenerateCallVirt<ISettingsStore, string, string>(storeGetMethod);
				}
				else
				{
					throw new NotSupportedException(
						"The property type does not support default values: " +
						propertyInfo.DeclaringType.Name + "." + propertyInfo.Name);
				}
			}
			else
			{
				ilGen.GenerateCallVirt<ISettingsStore, string>(storeGetMethod);
			}
			ilGen.Emit(OpCodes.Ret);

			// Associate new method with the getter method in the interface
			typeBuilder.DefineMethodOverride(methodBuilder, getMethod);

			return methodBuilder;
		}

		/// <summary>
		/// Creates the setter method for a property.
		/// </summary>
		/// <param name="typeBuilder">The TypeBuilder instance.</param>
		/// <param name="setMethod">The interface property method to create.</param>
		/// <param name="propertyInfo">The PropertyInfo object.</param>
		/// <param name="fields">A list of all created fields.</param>
		/// <returns>The created property setter method.</returns>
		private static MethodBuilder CreateStoreSetter(
			TypeBuilder typeBuilder,
			MethodInfo setMethod,
			PropertyInfo propertyInfo,
			List<FieldBuilder> fields)
		{
			MethodBuilder methodBuilder = typeBuilder.DefineMethod(
				setMethod.Name,
				MethodAttributes.Public | MethodAttributes.Virtual,
				typeof(void),
				new[] { propertyInfo.PropertyType });

			ILGenerator ilGen = methodBuilder.GetILGenerator();
			Label exitLabel = ilGen.DefineLabel();

			// settingsStore.Set(prefix + "PropertyName", value);
			string propertyName = propertyInfo.Name;
			ilGen.Emit(OpCodes.Ldarg_0);
			ilGen.Emit(OpCodes.Ldfld, fields.First(f => f.Name == "settingsStore"));
			ilGen.Emit(OpCodes.Ldarg_0);
			ilGen.Emit(OpCodes.Ldfld, fields.First(f => f.Name == "prefix"));
			ilGen.Emit(OpCodes.Ldstr, propertyName);
			ilGen.GenerateCall<string, string, string>("Concat");
			ilGen.Emit(OpCodes.Ldarg_1);
			if (propertyInfo.PropertyType.IsValueType)
			{
				ilGen.Emit(OpCodes.Box, propertyInfo.PropertyType);
			}
			ilGen.GenerateCallVirt<ISettingsStore, string, object>("Set");

			// return;
			ilGen.MarkLabel(exitLabel);
			ilGen.Emit(OpCodes.Ret);

			// Associate new method with the setter method in the interface
			typeBuilder.DefineMethodOverride(methodBuilder, setMethod);

			return methodBuilder;
		}

		/// <summary>
		/// Creates a property.
		/// </summary>
		/// <param name="typeBuilder">The TypeBuilder instance.</param>
		/// <param name="propertyInfo">The interface property to create.</param>
		/// <param name="getMethod">The property getter method, if available; null otherwise.</param>
		/// <param name="setMethod">The property setter method, if available; null otherwise.</param>
		/// <returns></returns>
		private static PropertyBuilder CreateProperty(
			TypeBuilder typeBuilder,
			PropertyInfo propertyInfo,
			MethodBuilder getMethod,
			MethodBuilder setMethod)
		{
			PropertyBuilder propertyBuilder = typeBuilder.DefineProperty(
				propertyInfo.Name,
				PropertyAttributes.None,
				propertyInfo.PropertyType,
				Type.EmptyTypes);

			if (getMethod != null)
			{
				propertyBuilder.SetGetMethod(getMethod);
			}
			if (setMethod != null)
			{
				propertyBuilder.SetSetMethod(setMethod);
			}

			return propertyBuilder;
		}

		/// <summary>
		/// Creates an empty method, just to implement the interface.
		/// </summary>
		/// <param name="typeBuilder">The TypeBuilder instance.</param>
		/// <param name="methodInfo">The interface method to create.</param>
		/// <returns>The created method.</returns>
		private static MethodBuilder CreateEmptyMethod(TypeBuilder typeBuilder, MethodInfo methodInfo)
		{
			MethodBuilder methodBuilder = typeBuilder.DefineMethod(
				methodInfo.Name,
				MethodAttributes.Public | MethodAttributes.Virtual,
				methodInfo.ReturnType,
				methodInfo.GetParameters().Select(pi => pi.ParameterType).ToArray());

			ILGenerator ilGen = methodBuilder.GetILGenerator();

			// If there's a return type, create a default value to return
			if (methodInfo.ReturnType != typeof(void))
			{
				LocalBuilder localBuilder = ilGen.DeclareLocal(methodInfo.ReturnType);
				ilGen.Emit(OpCodes.Ldloc, localBuilder);
			}

			// return; / return default(Type);
			ilGen.Emit(OpCodes.Ret);

			// Associate new method with the method in the interface
			typeBuilder.DefineMethodOverride(methodBuilder, methodInfo);

			return methodBuilder;
		}

		#endregion Type creation methods

		#region Interface scanning methods

		/// <summary>
		/// Collects all MethodInfo objects from the interface, recursing into inherited interfaces.
		/// </summary>
		/// <param name="methods">The list to add new methods to.</param>
		/// <param name="type">The interface type to scan.</param>
		private static void AddMethodsToList(List<MethodInfo> methods, Type type)
		{
			methods.AddRange(type.GetMethods());
			foreach (Type subInterface in type.GetInterfaces())
			{
				AddMethodsToList(methods, subInterface);
			}
		}

		/// <summary>
		/// Collects all PropertyInfo objects from the interface, recursing into inherited interfaces.
		/// </summary>
		/// <param name="properties">The list to add new properties to.</param>
		/// <param name="type">The interface type to scan.</param>
		private static void AddPropertiesToList(List<PropertyInfo> properties, Type type)
		{
			properties.AddRange(type.GetProperties());
			foreach (Type subInterface in type.GetInterfaces())
			{
				AddPropertiesToList(properties, subInterface);
			}
		}

		#endregion Interface scanning methods

		#region ILGenerator extension methods

		private static void GenerateCall<TType>(this ILGenerator ilGen, string methodName)
		{
			ilGen.EmitCall(OpCodes.Call, typeof(TType).GetMethod(methodName), null);
		}

		private static void GenerateCall<TType, TArg1>(this ILGenerator ilGen, string methodName)
		{
			ilGen.EmitCall(OpCodes.Call, typeof(TType).GetMethod(methodName, new[] { typeof(TArg1) }), null);
		}

		private static void GenerateCall<TType, TArg1, TArg2>(this ILGenerator ilGen, string methodName)
		{
			ilGen.EmitCall(OpCodes.Call, typeof(TType).GetMethod(methodName, new[] { typeof(TArg1), typeof(TArg2) }), null);
		}

		private static void GenerateCall<TType, TArg1, TArg2, TArg3>(this ILGenerator ilGen, string methodName)
		{
			ilGen.EmitCall(OpCodes.Call, typeof(TType).GetMethod(methodName, new[] { typeof(TArg1), typeof(TArg2), typeof(TArg3) }), null);
		}

		private static void GenerateCall<TType, TArg1, TArg2, TArg3, TArg4>(this ILGenerator ilGen, string methodName)
		{
			ilGen.EmitCall(OpCodes.Call, typeof(TType).GetMethod(methodName, new[] { typeof(TArg1), typeof(TArg2), typeof(TArg3), typeof(TArg4) }), null);
		}

		private static void GenerateCallVirt<TType>(this ILGenerator ilGen, string methodName)
		{
			ilGen.EmitCall(OpCodes.Callvirt, typeof(TType).GetMethod(methodName), null);
		}

		private static void GenerateCallVirt<TType, TArg1>(this ILGenerator ilGen, string methodName)
		{
			ilGen.EmitCall(OpCodes.Callvirt, typeof(TType).GetMethod(methodName, new[] { typeof(TArg1) }), null);
		}

		private static void GenerateCallVirt<TType, TArg1, TArg2>(this ILGenerator ilGen, string methodName)
		{
			ilGen.EmitCall(OpCodes.Callvirt, typeof(TType).GetMethod(methodName, new[] { typeof(TArg1), typeof(TArg2) }), null);
		}

		private static void GenerateCallVirt<TType, TArg1, TArg2, TArg3>(this ILGenerator ilGen, string methodName)
		{
			ilGen.EmitCall(OpCodes.Callvirt, typeof(TType).GetMethod(methodName, new[] { typeof(TArg1), typeof(TArg2), typeof(TArg3) }), null);
		}

		private static void GenerateCallVirt<TType, TArg1, TArg2, TArg3, TArg4>(this ILGenerator ilGen, string methodName)
		{
			ilGen.EmitCall(OpCodes.Callvirt, typeof(TType).GetMethod(methodName, new[] { typeof(TArg1), typeof(TArg2), typeof(TArg3), typeof(TArg4) }), null);
		}

		#endregion ILGenerator extension methods
	}

	#endregion SettingsAdapterFactory class

	#region Supporting interfaces

	/// <summary>
	/// Defines the base interface for application-specific settings interfaces used with the
	/// <see cref="SettingsAdapterFactory"/> class.
	/// </summary>
	public interface ISettings : INotifyPropertyChanged
	{
		/// <summary>
		/// Gets the back-end settings store of the current interface implementation.
		/// </summary>
		ISettingsStore SettingsStore { get; }
	}

	/// <summary>
	/// Defines a data store for setting keys and values.
	/// </summary>
	public interface ISettingsStore : INotifyPropertyChanged, IDisposable
	{
		#region Set methods

		/// <summary>
		/// Sets a setting key to a new value.
		/// </summary>
		/// <param name="key">The setting key to update.</param>
		/// <param name="newValue">The new value for that key. Set null to remove the key.</param>
		void Set(string key, object newValue);

		/// <summary>
		/// Removes a setting key from the settings store.
		/// </summary>
		/// <param name="key">The setting key to remove.</param>
		void Remove(string key);

		/// <summary>
		/// Renames a setting key in the settings store.
		/// </summary>
		/// <param name="oldKey">The old setting key to rename.</param>
		/// <param name="newKey">The new setting key.</param>
		void Rename(string oldKey, string newKey);
	
		#endregion Set methods

		#region Get methods

		/// <summary>
		/// Gets the current bool value of a setting key, or false if the key is unset or has an
		/// incompatible data type.
		/// </summary>
		/// <param name="key">The setting key.</param>
		/// <returns></returns>
		bool GetBool(string key);

		/// <summary>
		/// Gets the current bool value of a setting key, or a fallback value if the key is unset
		/// or has an incompatible data type.
		/// </summary>
		/// <param name="key">The setting key.</param>
		/// <param name="fallbackValue">The fallback value to return if the key is unset.</param>
		/// <returns></returns>
		bool GetBool(string key, bool fallbackValue);

		/// <summary>
		/// Gets the current bool[] value of a setting key, or an empty array if the key is unset
		/// or has an incompatible data type.
		/// </summary>
		/// <param name="key">The setting key.</param>
		/// <returns></returns>
		bool[] GetBoolArray(string key);

		/// <summary>
		/// Gets the current int value of a setting key, or 0 if the key is unset or has an
		/// incompatible data type.
		/// </summary>
		/// <param name="key">The setting key.</param>
		/// <returns></returns>
		int GetInt(string key);

		/// <summary>
		/// Gets the current int value of a setting key, or a fallback value if the key is unset
		/// or has an incompatible data type.
		/// </summary>
		/// <param name="key">The setting key.</param>
		/// <param name="fallbackValue">The fallback value to return if the key is unset.</param>
		/// <returns></returns>
		int GetInt(string key, int fallbackValue);

		/// <summary>
		/// Gets the current int[] value of a setting key, or an empty array if the key is unset
		/// or has an incompatible data type.
		/// </summary>
		/// <param name="key">The setting key.</param>
		/// <returns></returns>
		int[] GetIntArray(string key);

		/// <summary>
		/// Gets the current long value of a setting key, or 0 if the key is unset or has an
		/// incompatible data type.
		/// </summary>
		/// <param name="key">The setting key.</param>
		/// <returns></returns>
		long GetLong(string key);

		/// <summary>
		/// Gets the current long value of a setting key, or a fallback value if the key is unset
		/// or has an incompatible data type.
		/// </summary>
		/// <param name="key">The setting key.</param>
		/// <param name="fallbackValue">The fallback value to return if the key is unset.</param>
		/// <returns></returns>
		long GetLong(string key, long fallbackValue);

		/// <summary>
		/// Gets the current long[] value of a setting key, or an empty array if the key is unset
		/// or has an incompatible data type.
		/// </summary>
		/// <param name="key">The setting key.</param>
		/// <returns></returns>
		long[] GetLongArray(string key);

		/// <summary>
		/// Gets the current double value of a setting key, or NaN if the key is unset or has an
		/// incompatible data type.
		/// </summary>
		/// <param name="key">The setting key.</param>
		/// <returns></returns>
		double GetDouble(string key);

		/// <summary>
		/// Gets the current double value of a setting key, or a fallback value if the key is unset
		/// or has an incompatible data type.
		/// </summary>
		/// <param name="key">The setting key.</param>
		/// <param name="fallbackValue">The fallback value to return if the key is unset.</param>
		/// <returns></returns>
		double GetDouble(string key, double fallbackValue);

		/// <summary>
		/// Gets the current double[] value of a setting key, or an empty array if the key is unset
		/// or has an incompatible data type.
		/// </summary>
		/// <param name="key">The setting key.</param>
		/// <returns></returns>
		double[] GetDoubleArray(string key);

		/// <summary>
		/// Gets the current string value of a setting key, or "" if the key is unset.
		/// </summary>
		/// <param name="key">The setting key.</param>
		/// <returns></returns>
		string GetString(string key);

		/// <summary>
		/// Gets the current string value of a setting key, or a fallback value if the key is unset.
		/// </summary>
		/// <param name="key">The setting key.</param>
		/// <param name="fallbackValue">The fallback value to return if the key is unset.</param>
		/// <returns></returns>
		string GetString(string key, string fallbackValue);

		/// <summary>
		/// Gets the current string[] value of a setting key, or an empty array if the key is unset
		/// or has an incompatible data type.
		/// </summary>
		/// <param name="key">The setting key.</param>
		/// <returns></returns>
		string[] GetStringArray(string key);

		/// <summary>
		/// Gets the current DateTime value of a setting key, or DateTime.MinValue if the key is
		/// unset or has an incompatible data type.
		/// </summary>
		/// <param name="key">The setting key.</param>
		/// <returns></returns>
		DateTime GetDateTime(string key);

		/// <summary>
		/// Gets the current DateTime value of a setting key, or a fallback value if the key is
		/// unset or has an incompatible data type.
		/// </summary>
		/// <param name="key">The setting key.</param>
		/// <param name="fallbackValue">The fallback value to return if the key is unset.</param>
		/// <returns></returns>
		DateTime GetDateTime(string key, DateTime fallbackValue);

		/// <summary>
		/// Gets the current DateTime[] value of a setting key, or an empty array if the key is
		/// unset or has an incompatible data type.
		/// </summary>
		/// <param name="key">The setting key.</param>
		/// <returns></returns>
		DateTime[] GetDateTimeArray(string key);

		/// <summary>
		/// Gets the current TimeSpan value of a setting key, or TimeSpan.Zero if the key is unset
		/// or has an incompatible data type.
		/// </summary>
		/// <param name="key">The setting key.</param>
		/// <returns></returns>
		TimeSpan GetTimeSpan(string key);

		/// <summary>
		/// Gets the current TimeSpan value of a setting key, or a fallback value if the key is
		/// unset or has an incompatible data type.
		/// </summary>
		/// <param name="key">The setting key.</param>
		/// <param name="fallbackValue">The fallback value to return if the key is unset.</param>
		/// <returns></returns>
		TimeSpan GetTimeSpan(string key, TimeSpan fallbackValue);

		/// <summary>
		/// Gets the current TimeSpan[] value of a setting key, or an empty array if the key is
		/// unset or has an incompatible data type.
		/// </summary>
		/// <param name="key">The setting key.</param>
		/// <returns></returns>
		TimeSpan[] GetTimeSpanArray(string key);

		#endregion Get methods
	}

	#endregion Supporting interfaces
}
