using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
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
	/// Generates a dynamic implementation of an interface with properties that binds to a settings
	/// store and implements INotifyPropertyChanged.
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
				MyClass + "Asm.dll");
			// If the module defines a different file name than where the assembly will be written,
			// there a two files written: one with the assembly manifest and another one with the
			// module. So the module should have the same file name as the assembly name.
			// NOTE: For better analysing the generated code in the written DLL file, call the
			// DefineDynamicModule method with a third parameter 'true' to emit symbols.
		}

		#endregion Static constructor

		#region Public methods

		/// <summary>
		/// Returns an object for the specified interface type.
		/// </summary>
		/// <typeparam name="TInterface">The interface type to implement.</typeparam>
		/// <param name="settingsStore">The <see cref="ISettingsStore"/> instance to bind the generated properties to.</param>
		/// <param name="prefix">The settings path prefix for the new object. Should be null for the initial instance.</param>
		/// <returns>An object that implements the <typeparamref name="TInterface"/> interface.</returns>
		public static TInterface New<TInterface>(ISettingsStore settingsStore, string prefix = null)
			where TInterface : class
		{
			Type interfaceType = typeof(TInterface);
			if (!generatedTypes.ContainsKey(interfaceType))
			{
				CreateType(interfaceType);
			}

			// DEBUG: Enable the following code to verify the assembly with peverify.exe from the .NET command prompt.
			//string fileName = assemblyBuilder.GetName().Name + ".dll";
			//System.IO.File.Delete(fileName);
			//assemblyBuilder.Save(fileName);

			// Adjust prefix to include the trailing dot as used internally
			if (prefix != null)
			{
				prefix = prefix.Trim().TrimEnd('.', ' ') + ".";
			}
			else if (prefix == "")
			{
				prefix = null;
			}

			return (TInterface) Activator.CreateInstance(generatedTypes[interfaceType], settingsStore, prefix);
		}

		#endregion Public methods

		#region Type creation methods

		/// <summary>
		/// Creates a class type that implements the specified interface and adds it to the
		/// <see cref="generatedTypes"/> dictionary.
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

					if (propertyInfo.GetSetMethod() != null)
					{
						throw new NotSupportedException("The ISettingsStore-type property " + propertyInfo.Name + " must not have a setter.");
					}
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

					if (propertyInfo.GetSetMethod() != null)
					{
						throw new NotSupportedException("The interface-type property " + propertyInfo.Name + " must not have a setter.");
					}
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

			// DEBUG:
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

				if (IsListType(field.FieldType))
				{
					// This is a list type that should be implemented by an observable list that
					// initially loads from the store and automatically passes changes back into the
					// store. Write code that calls settingsStore.CreateList().

					// To support renaming on obfuscation, an expression must be specified for the
					// CreateDictionary method. This expression must already contain type arguments.
					// After reading the method from the expression, the type arguments are stripped
					// and then the actual runtime type arguments are added again. (Is this a hack?)

#pragma warning disable 1720   // Expression will always cause a System.NullReferenceException because the default value of 'generic type' is null

					MethodInfo method = MethodOf(() => default(ISettingsStore).CreateList<object>(default(string)));

#pragma warning restore 1720

					method = method.GetGenericMethodDefinition();
					method = method.MakeGenericMethod(field.FieldType.GetGenericArguments());

					ilGen.Emit(OpCodes.Ldarg_0);
					ilGen.Emit(OpCodes.Ldarg_1);   // settingsStore
					// -> prefix + [propertyName]
					ilGen.Emit(OpCodes.Ldarg_2);   // prefix
					ilGen.Emit(OpCodes.Ldstr, propertyName);
					ilGen.GenerateCall<string, string, string>("Concat");
					// -> settingsStore.CreateList<>(prefix + [propertyName]);
					ilGen.EmitCall(OpCodes.Call, method, null);
					// -> [field] = ...
					ilGen.Emit(OpCodes.Stfld, field);
				}
				else if (IsDictionaryType(field.FieldType))
				{
					// This is a dictionary type that should be implemented by a dictionary that
					// initially loads from the store and automatically passes changes back into the
					// store. Write code that calls settingsStore.CreateDictionary<,>().

#pragma warning disable 1720   // Expression will always cause a System.NullReferenceException because the default value of 'generic type' is null

					MethodInfo method = MethodOf(() => default(ISettingsStore).CreateDictionary<object, object>(default(string)));

#pragma warning restore 1720

					// See comment about generic list handling above
					method = method.GetGenericMethodDefinition();
					method = method.MakeGenericMethod(field.FieldType.GetGenericArguments());

					ilGen.Emit(OpCodes.Ldarg_0);
					ilGen.Emit(OpCodes.Ldarg_1);   // settingsStore
					// -> prefix + [propertyName]
					ilGen.Emit(OpCodes.Ldarg_2);   // prefix
					ilGen.Emit(OpCodes.Ldstr, propertyName);
					ilGen.GenerateCall<string, string, string>("Concat");
					// -> settingsStore.CreateDictionary<,>(prefix + [propertyName]);
					ilGen.EmitCall(OpCodes.Call, method, null);
					// -> [field] = ...
					ilGen.Emit(OpCodes.Stfld, field);
				}
				else if (field.FieldType.IsInterface)
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
		/// Creates the getter method for a property that returns a field.
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

#pragma warning disable 1720   // Expression will always cause a System.NullReferenceException because the default value of 'generic type' is null

		/// <summary>
		/// Creates the getter method for a property that fetches a value from the
		/// <see cref="ISettingsStore"/> instance.
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
			// TODO: This doesn't handle arrays of enums
			Type propType = propertyInfo.PropertyType;
			if (propType.IsEnum)
			{
				propType = propType.GetEnumUnderlyingType();
			}

			MethodInfo storeGetMethod;
			MethodInfo storeGetMethodWithDefault = null;
			if (propType == typeof(bool))
			{
				storeGetMethod = MethodOf(() => default(ISettingsStore).GetBool(default(string)));
				storeGetMethodWithDefault = MethodOf(() => default(ISettingsStore).GetBool(default(string), default(bool)));
			}
			else if (propType == typeof(bool[]))
			{
				storeGetMethod = MethodOf(() => default(ISettingsStore).GetBoolArray(default(string)));
			}
			else if (propType == typeof(int))
			{
				storeGetMethod = MethodOf(() => default(ISettingsStore).GetInt(default(string)));
				storeGetMethodWithDefault = MethodOf(() => default(ISettingsStore).GetInt(default(string), default(int)));
			}
			else if (propType == typeof(int[]))
			{
				storeGetMethod = MethodOf(() => default(ISettingsStore).GetIntArray(default(string)));
			}
			else if (propType == typeof(long))
			{
				storeGetMethod = MethodOf(() => default(ISettingsStore).GetLong(default(string)));
				storeGetMethodWithDefault = MethodOf(() => default(ISettingsStore).GetLong(default(string), default(long)));
			}
			else if (propType == typeof(long[]))
			{
				storeGetMethod = MethodOf(() => default(ISettingsStore).GetLongArray(default(string)));
			}
			else if (propType == typeof(double))
			{
				storeGetMethod = MethodOf(() => default(ISettingsStore).GetDouble(default(string)));
				storeGetMethodWithDefault = MethodOf(() => default(ISettingsStore).GetDouble(default(string), default(double)));
			}
			else if (propType == typeof(double[]))
			{
				storeGetMethod = MethodOf(() => default(ISettingsStore).GetDoubleArray(default(string)));
			}
			else if (propType == typeof(string))
			{
				storeGetMethod = MethodOf(() => default(ISettingsStore).GetString(default(string)));
				storeGetMethodWithDefault = MethodOf(() => default(ISettingsStore).GetString(default(string), default(string)));
			}
			else if (propType == typeof(string[]))
			{
				storeGetMethod = MethodOf(() => default(ISettingsStore).GetStringArray(default(string)));
			}
			else if (propType == typeof(DateTime))
			{
				storeGetMethod = MethodOf(() => default(ISettingsStore).GetDateTime(default(string)));
			}
			else if (propType == typeof(DateTime[]))
			{
				storeGetMethod = MethodOf(() => default(ISettingsStore).GetDateTimeArray(default(string)));
			}
			else if (propType == typeof(TimeSpan))
			{
				storeGetMethod = MethodOf(() => default(ISettingsStore).GetTimeSpan(default(string)));
			}
			else if (propType == typeof(TimeSpan[]))
			{
				storeGetMethod = MethodOf(() => default(ISettingsStore).GetTimeSpanArray(default(string)));
			}
			else if (propType == typeof(NameValueCollection))
			{
				storeGetMethod = MethodOf(() => default(ISettingsStore).GetNameValueCollection(default(string)));
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
				}
				else if (propType == typeof(int))
				{
					ilGen.Emit(OpCodes.Ldc_I4, Convert.ToInt32(defAttr.Value, CultureInfo.InvariantCulture));
				}
				else if (propType == typeof(long))
				{
					ilGen.Emit(OpCodes.Ldc_I8, Convert.ToInt64(defAttr.Value, CultureInfo.InvariantCulture));
				}
				else if (propType == typeof(double))
				{
					ilGen.Emit(OpCodes.Ldc_R8, Convert.ToDouble(defAttr.Value, CultureInfo.InvariantCulture));
				}
				else if (propType == typeof(string))
				{
					ilGen.Emit(OpCodes.Ldstr, Convert.ToString(defAttr.Value, CultureInfo.InvariantCulture));
				}
				else
				{
					throw new NotSupportedException(
						"The property type does not support default values: " +
						propertyInfo.DeclaringType.Name + "." + propertyInfo.Name);
				}
				ilGen.GenerateCallVirt(storeGetMethodWithDefault);
			}
			else
			{
				ilGen.GenerateCallVirt(storeGetMethod);
			}
			ilGen.Emit(OpCodes.Ret);

			// Associate new method with the getter method in the interface
			typeBuilder.DefineMethodOverride(methodBuilder, getMethod);

			return methodBuilder;
		}

		/// <summary>
		/// Creates the setter method for a property that passes the value to an
		/// <see cref="ISettingsStore"/> instance.
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
			ilGen.GenerateCallVirt(MethodOf(() => default(ISettingsStore).Set(default(string), default(object))));

			// return;
			ilGen.MarkLabel(exitLabel);
			ilGen.Emit(OpCodes.Ret);

			// Associate new method with the setter method in the interface
			typeBuilder.DefineMethodOverride(methodBuilder, setMethod);

			return methodBuilder;
		}

#pragma warning restore 1720

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

		private static void GenerateCall(this ILGenerator ilGen, MethodInfo methodInfo)
		{
			ilGen.EmitCall(OpCodes.Call, methodInfo, null);
		}

		private static void GenerateCallVirt(this ILGenerator ilGen, MethodInfo methodInfo)
		{
			ilGen.EmitCall(OpCodes.Callvirt, methodInfo, null);
		}

		// Source: http://stackoverflow.com/q/1213862/143684
		private static MethodInfo MethodOf(Expression<Action> expression)
		{
			MethodCallExpression body = (MethodCallExpression) expression.Body;
			return body.Method;
		}

		private static MethodInfo MethodOf<T>(Expression<Action<T>> expression)
		{
			MethodCallExpression body = (MethodCallExpression) expression.Body;
			return body.Method;
		}

		private static MethodInfo MethodOf<T1, T2>(Expression<Action<T1, T2>> expression)
		{
			MethodCallExpression body = (MethodCallExpression) expression.Body;
			return body.Method;
		}

		private static MethodInfo MethodOf<TResult>(Expression<Func<TResult>> expression)
		{
			MethodCallExpression body = (MethodCallExpression) expression.Body;
			return body.Method;
		}

		private static MethodInfo MethodOf<T, TResult>(Expression<Func<T, TResult>> expression)
		{
			MethodCallExpression body = (MethodCallExpression) expression.Body;
			return body.Method;
		}

		private static MethodInfo MethodOf<T1, T2, TResult>(Expression<Func<T1, T2, TResult>> expression)
		{
			MethodCallExpression body = (MethodCallExpression) expression.Body;
			return body.Method;
		}

		#endregion ILGenerator extension methods

		#region Collection property type helpers

		/// <summary>
		/// Determines whether a type is <see cref="IList{T}"/>.
		/// </summary>
		/// <param name="type">The type to check.</param>
		/// <returns></returns>
		private static bool IsListType(Type type)
		{
			if (type.IsGenericType)
			{
				if (typeof(IList<>) == type.GetGenericTypeDefinition()) return true;
			}
			// Alternative:
			//if (type.IsGenericType &&
			//    type.GetGenericArguments().Length == 1)
			//{
			//    if (type == typeof(IList<>).MakeGenericType(type.GetGenericArguments()[0])) return true;
			//}
			return false;
		}

		/// <summary>
		/// Determines whether a type is <see cref="IDictionary{TKey, TValue}"/>.
		/// </summary>
		/// <param name="type">The type to check.</param>
		/// <returns></returns>
		private static bool IsDictionaryType(Type type)
		{
			if (type.IsGenericType)
			{
				if (typeof(IDictionary<,>) == type.GetGenericTypeDefinition()) return true;
			}
			return false;
		}

		#endregion Collection property type helpers
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
		/// <returns>true if the value was deleted, false if it did not exist.</returns>
		bool Remove(string key);

		/// <summary>
		/// Renames a setting key in the settings store.
		/// </summary>
		/// <param name="oldKey">The old setting key to rename.</param>
		/// <param name="newKey">The new setting key.</param>
		/// <returns>true if the value was renamed, false if it did not exist.</returns>
		bool Rename(string oldKey, string newKey);

		#endregion Set methods

		#region Get methods

		/// <summary>
		/// Gets all setting keys that are currently set in this settings store.
		/// </summary>
		/// <returns></returns>
		string[] GetKeys();

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

		/// <summary>
		/// Gets the current NameValueCollection of a setting key, or an empty collection if the key
		/// is unset or has an incompatible data type.
		/// </summary>
		/// <param name="key">The setting key.</param>
		/// <returns></returns>
		NameValueCollection GetNameValueCollection(string key);

		/// <summary>
		/// Creates a list wrapper for an array-typed key. Changes to the list are written back to
		/// the settings store.
		/// </summary>
		/// <typeparam name="T">The type of list items.</typeparam>
		/// <param name="key">The setting key.</param>
		/// <returns></returns>
		IList<T> CreateList<T>(string key);

		/// <summary>
		/// Creates a dictionary wrapper for a NameValueCollection-typed key. Changes to the
		/// dictionary are written back to the settings store.
		/// </summary>
		/// <typeparam name="TKey">The type of dictionary keys.</typeparam>
		/// <typeparam name="TValue">The type of dictionary values.</typeparam>
		/// <param name="key">The setting key.</param>
		/// <returns></returns>
		IDictionary<TKey, TValue> CreateDictionary<TKey, TValue>(string key);

		#endregion Get methods
	}

	#endregion Supporting interfaces

	#region Bound collection classes

	/// <summary>
	/// Implements an <see cref="ObservableCollection{T}"/> that is bound to an
	/// <see cref="ISettingsStore"/> instance. Changes to the list are written back to the settings
	/// store.
	/// </summary>
	/// <typeparam name="T">The type of the elements of the list.</typeparam>
	/// <remarks>
	/// <para>
	///   This class supports all basic types as type parameter that are supported in
	///   <see cref="ISettingsStore"/> for scalar values as well.
	/// </para>
	/// <para>
	///   Creating an instance of the class with an unsupported type parameter will throw a
	///   <see cref="NotSupportedException"/> in the constructor. This happens when creating an
	///   instance of an adapter class. As the list is statically typed, no further exceptions can
	///   be thrown for using incompatible values.
	/// </para>
	/// </remarks>
	public class SettingsStoreBoundList<T> : ObservableCollection<T>
	{
		private ISettingsStore store;
		private string key;

		/// <summary>
		/// Initialises a new instance of the <see cref="SettingsStoreBoundList{T}"/> class and
		/// loads all array items from the entry <paramref name="key"/> in <paramref name="store"/>.
		/// </summary>
		/// <param name="store">The settings store to bind the data to.</param>
		/// <param name="key">The setting key to bind the data to.</param>
		public SettingsStoreBoundList(ISettingsStore store, string key)
			: base(GetItems(store, key))
		{
			this.store = store;
			this.key = key;
		}

		/// <summary>
		/// Reads an array from the settings store and casts its items for the IEnumerable.
		/// </summary>
		private static IEnumerable<T> GetItems(ISettingsStore store, string key)
		{
			if (typeof(T) == typeof(string)) return store.GetStringArray(key).Cast<T>();
			if (typeof(T) == typeof(int)) return store.GetIntArray(key).Cast<T>();
			if (typeof(T) == typeof(long)) return store.GetLongArray(key).Cast<T>();
			if (typeof(T) == typeof(double)) return store.GetDoubleArray(key).Cast<T>();
			if (typeof(T) == typeof(bool)) return store.GetBoolArray(key).Cast<T>();
			if (typeof(T) == typeof(DateTime)) return store.GetDateTimeArray(key).Cast<T>();
			if (typeof(T) == typeof(TimeSpan)) return store.GetTimeSpanArray(key).Cast<T>();
			throw new NotSupportedException("The list item type " + typeof(T).Name + " is not supported.");
		}

		#region Overridden ObservableCollection methods

		/// <inheritdoc/>
		protected override void ClearItems()
		{
			base.ClearItems();
			store.Set(key, this.ToArray());
		}

		/// <inheritdoc/>
		protected override void InsertItem(int index, T item)
		{
			base.InsertItem(index, item);
			store.Set(key, this.ToArray());
		}

		/// <inheritdoc/>
		protected override void MoveItem(int oldIndex, int newIndex)
		{
			base.MoveItem(oldIndex, newIndex);
			store.Set(key, this.ToArray());
		}

		/// <inheritdoc/>
		protected override void RemoveItem(int index)
		{
			base.RemoveItem(index);
			store.Set(key, this.ToArray());
		}

		/// <inheritdoc/>
		protected override void SetItem(int index, T item)
		{
			base.SetItem(index, item);
			store.Set(key, this.ToArray());
		}

		#endregion Overridden ObservableCollection methods
	}

	/// <summary>
	/// Implements a dictionary that is bound to an <see cref="ISettingsStore"/> instance. Changes
	/// to the dictionary are written back to the settings store.
	/// </summary>
	/// <typeparam name="TKey">The type of the keys of the dictionary.</typeparam>
	/// <typeparam name="TValue">The type of the values of the dictionary.</typeparam>
	/// <para>
	///   This class supports all types as key and value type parameter that can be converted to and
	///   from a string with the <see cref="Convert.ToString(object)"/> and
	///   <see cref="Convert.ChangeType(object, Type)"/> methods, using the invariant culture. This
	///   includes all types that are supported in <see cref="ISettingsStore"/> for scalar values as
	///   well, with their respective string representation and parsing method.
	/// </para>
	/// <para>
	///   Incompatible data entries are skipped, similar to the behaviour of the Get methods of
	///   <see cref="ISettingsStore"/>.
	/// </para>
	public class SettingsStoreBoundDictionary<TKey, TValue> : IDictionary<TKey, TValue>
	{
		private ISettingsStore store;
		private string key;
		private Dictionary<TKey, TValue> dictionary;

		/// <summary>
		/// Initialises a new instance of the <see cref="SettingsStoreBoundDictionary{TKey, TValue}"/>
		/// class and loads all <see cref="NameValueCollection"/> items from the entry
		/// <paramref name="key"/> in <paramref name="store"/>.
		/// </summary>
		/// <param name="store">The settings store to bind the data to.</param>
		/// <param name="key">The setting key to bind the data to.</param>
		public SettingsStoreBoundDictionary(ISettingsStore store, string key)
		{
			this.store = store;
			this.key = key;
			ReadFromStore();
		}

		/// <summary>
		/// Reads a <see cref="NameValueCollection"/> instance from the settings store and converts
		/// its items for the dictionary. Incompatible data entries are skipped, similar to the
		/// behaviour of the Get methods of <see cref="ISettingsStore"/>.
		/// </summary>
		private void ReadFromStore()
		{
			NameValueCollection collection = store.GetNameValueCollection(key);
			dictionary = new Dictionary<TKey, TValue>(collection.Count);
			for (int i = 0; i < collection.Count; i++)
			{
				try
				{
					string keyStr = collection.GetKey(i);
					string valueStr = collection[i];

					TKey key2;
					TValue value;

					// Special type conversions
					if (typeof(TKey) == typeof(bool))
					{
						if (keyStr.Trim() == "1" ||
							keyStr.Trim().ToLower() == "true")
							key2 = (TKey) (object) true;
						else if (keyStr.Trim() == "0" ||
							keyStr.Trim().ToLower() == "false")
							key2 = (TKey) (object) false;
						else throw new FormatException("Invalid bool value");
					}
					else if (typeof(TKey) == typeof(DateTime))
					{
						key2 = (TKey) (object) DateTime.Parse(keyStr, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);
					}
					else if (typeof(TKey) == typeof(TimeSpan))
					{
						key2 = (TKey) (object) new TimeSpan(long.Parse(keyStr, CultureInfo.InvariantCulture));
					}
					else
					{
						key2 = (TKey) Convert.ChangeType(keyStr, typeof(TKey), CultureInfo.InvariantCulture);
					}

					if (typeof(TValue) == typeof(bool))
					{
						if (valueStr.Trim() == "1" ||
							valueStr.Trim().ToLower() == "true")
							value = (TValue) (object) true;
						else if (valueStr.Trim() == "0" ||
							valueStr.Trim().ToLower() == "false")
							value = (TValue) (object) false;
						else throw new FormatException("Invalid bool value");
					}
					else if (typeof(TValue) == typeof(DateTime))
					{
						value = (TValue) (object) DateTime.Parse(valueStr, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);
					}
					else if (typeof(TValue) == typeof(TimeSpan))
					{
						value = (TValue) (object) new TimeSpan(long.Parse(valueStr, CultureInfo.InvariantCulture));
					}
					else
					{
						value = (TValue) Convert.ChangeType(valueStr, typeof(TValue), CultureInfo.InvariantCulture);
					}

					dictionary.Add(key2, value);
				}
#if WITH_FIELDLOG
				catch (FormatException ex)
				{
					// Ignore entries that cannot be converted to the requested type (for key and value)
					FL.Warning(ex, "Converting entry from NameValueCollection");
				}
#else
				catch (FormatException)
				{
					// Ignore entries that cannot be converted to the requested type (for key and value)
				}
#endif
			}
		}

		/// <summary>
		/// Converts the contents of the dictionary into strings and sets a new
		/// <see cref="NameValueCollection"/> instance in the settings store.
		/// </summary>
		private void WriteToStore()
		{
			NameValueCollection collection = new NameValueCollection(dictionary.Count);
			foreach (var kvp in dictionary)
			{
				string keyStr;
				string valueStr;

				// Special type conversions
				if (typeof(TKey) == typeof(bool))
				{
					keyStr = (bool) (object) kvp.Key ? "true" : "false";
				}
				else if (typeof(TKey) == typeof(DateTime))
				{
					keyStr = ((DateTime) (object) kvp.Key).ToString("o", CultureInfo.InvariantCulture);
				}
				else if (typeof(TKey) == typeof(TimeSpan))
				{
					keyStr = ((TimeSpan) (object) kvp.Key).Ticks.ToString(CultureInfo.InvariantCulture);
				}
				else
				{
					keyStr = Convert.ToString(kvp.Key, CultureInfo.InvariantCulture);
				}

				if (typeof(TValue) == typeof(bool))
				{
					valueStr = (bool) (object) kvp.Value ? "true" : "false";
				}
				else if (typeof(TValue) == typeof(DateTime))
				{
					valueStr = ((DateTime) (object) kvp.Value).ToString("o", CultureInfo.InvariantCulture);
				}
				else if (typeof(TValue) == typeof(TimeSpan))
				{
					valueStr = ((TimeSpan) (object) kvp.Value).Ticks.ToString(CultureInfo.InvariantCulture);
				}
				else
				{
					valueStr = Convert.ToString(kvp.Value, CultureInfo.InvariantCulture);
				}

				collection.Add(keyStr, valueStr);
			}
			store.Set(key, collection);
		}

		#region IDictionary members

		public void Add(TKey key, TValue value)
		{
			dictionary.Add(key, value);
			WriteToStore();
		}

		public bool ContainsKey(TKey key)
		{
			return dictionary.ContainsKey(key);
		}

		public ICollection<TKey> Keys
		{
			get { return dictionary.Keys; }
		}

		public bool Remove(TKey key)
		{
			bool res = dictionary.Remove(key);
			WriteToStore();
			return res;
		}

		public bool TryGetValue(TKey key, out TValue value)
		{
			return dictionary.TryGetValue(key, out value);
		}

		public ICollection<TValue> Values
		{
			get { return dictionary.Values; }
		}

		public TValue this[TKey key]
		{
			get
			{
				return dictionary[key];
			}
			set
			{
				dictionary[key] = value;
				WriteToStore();
			}
		}

		void ICollection<KeyValuePair<TKey, TValue>>.Add(KeyValuePair<TKey, TValue> item)
		{
			((ICollection<KeyValuePair<TKey, TValue>>) dictionary).Add(item);
			WriteToStore();
		}

		public void Clear()
		{
			dictionary.Clear();
			WriteToStore();
		}

		bool ICollection<KeyValuePair<TKey, TValue>>.Contains(KeyValuePair<TKey, TValue> item)
		{
			return ((ICollection<KeyValuePair<TKey, TValue>>) dictionary).Contains(item);
		}

		void ICollection<KeyValuePair<TKey, TValue>>.CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
		{
			((ICollection<KeyValuePair<TKey, TValue>>) dictionary).CopyTo(array, arrayIndex);
		}

		public int Count
		{
			get { return dictionary.Count; }
		}

		public bool IsReadOnly
		{
			get { return false; }
		}

		bool ICollection<KeyValuePair<TKey, TValue>>.Remove(KeyValuePair<TKey, TValue> item)
		{
			bool res = ((ICollection<KeyValuePair<TKey, TValue>>) dictionary).Remove(item);
			WriteToStore();
			return res;
		}

		public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
		{
			return ((IEnumerable<KeyValuePair<TKey, TValue>>) dictionary).GetEnumerator();
		}

		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
		{
			return ((System.Collections.IEnumerable) dictionary).GetEnumerator();
		}

		#endregion IDictionary members
	}

	#endregion Bound collection classes
}
