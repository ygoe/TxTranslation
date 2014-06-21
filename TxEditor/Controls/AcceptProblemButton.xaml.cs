using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace Unclassified.TxEditor.Controls
{
	public partial class AcceptProblemButton : Button
	{
		#region Dependency properties

		public static DependencyProperty IsAcceptedProperty = DependencyProperty.Register(
			"IsAccepted",
			typeof(bool),
			typeof(AcceptProblemButton));
		public bool IsAccepted
		{
			get { return (bool) GetValue(IsAcceptedProperty); }
			set { SetValue(IsAcceptedProperty, value); }
		}

		#endregion Dependency properties

		#region Constructor

		public AcceptProblemButton()
		{
			InitializeComponent();
		}

		#endregion Constructor
	}
}
