using System;
using System.Linq;
using System.Windows.Controls;
using Unclassified.UI;

namespace Unclassified.TxEditor.View
{
	public partial class TextKeyView : UserControl
	{
		#region Static constructor

		static TextKeyView()
		{
			ViewCommandManager.SetupMetadata<TextKeyView>();
		}

		#endregion Static constructor

		#region Constructors

		public TextKeyView()
		{
			InitializeComponent();
		}

		#endregion Constructors

		#region View commands

		#endregion View commands
	}
}
