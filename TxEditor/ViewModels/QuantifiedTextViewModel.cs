using System;
using System.Collections.Specialized;
using System.Linq;
using Unclassified.UI;

namespace Unclassified.TxEditor.ViewModels
{
	internal class QuantifiedTextViewModel : ViewModelBase, IViewCommandSource
	{
		#region Constructor

		public QuantifiedTextViewModel(CultureTextViewModel cultureTextVM)
		{
			CultureTextVM = cultureTextVM;
		}

		#endregion Constructor

		#region Public properties

		public CultureTextViewModel CultureTextVM { get; private set; }

		public int Count
		{
			get { return GetValue<int>("Count"); }
			set
			{
				if (SetValue(value, "Count"))
				{
					CultureTextVM.TextKeyVM.MainWindowVM.ValidateTextKeysDelayed();
					CultureTextVM.TextKeyVM.MainWindowVM.FileModified = true;
				}
			}
		}

		public int Modulo
		{
			get { return GetValue<int>("Modulo"); }
			set
			{
				if (SetValue(value, "Modulo"))
				{
					CultureTextVM.TextKeyVM.MainWindowVM.ValidateTextKeysDelayed();
					CultureTextVM.TextKeyVM.MainWindowVM.FileModified = true;
				}
			}
		}

		public string Text
		{
			get { return GetValue<string>("Text"); }
			set
			{
				if (SetValue(value, "Text"))
				{
					CultureTextVM.TextKeyVM.MainWindowVM.ValidateTextKeysDelayed();
					CultureTextVM.TextKeyVM.MainWindowVM.FileModified = true;
				}
			}
		}

		public bool IsMissing
		{
			get { return GetValue<bool>("IsMissing"); }
			set { SetValue(BooleanBoxes.Box(value), "IsMissing"); }
		}

		public bool IsPlaceholdersProblem
		{
			get { return GetValue<bool>("IsPlaceholdersProblem"); }
			set { SetValue(BooleanBoxes.Box(value), "IsPlaceholdersProblem"); }
		}

		public bool IsPunctuationProblem
		{
			get { return GetValue<bool>("IsPunctuationProblem"); }
			set { SetValue(BooleanBoxes.Box(value), "IsPunctuationProblem"); }
		}

		public bool AcceptMissing
		{
			get { return GetValue<bool>("AcceptMissing"); }
			set
			{
				if (SetValue(BooleanBoxes.Box(value), "AcceptMissing"))
				{
					CultureTextVM.TextKeyVM.MainWindowVM.ValidateTextKeysDelayed();
					CultureTextVM.TextKeyVM.MainWindowVM.FileModified = true;
				}
			}
		}

		public bool AcceptPlaceholders
		{
			get { return GetValue<bool>("AcceptPlaceholders"); }
			set
			{
				if (SetValue(BooleanBoxes.Box(value), "AcceptPlaceholders"))
				{
					CultureTextVM.TextKeyVM.MainWindowVM.ValidateTextKeysDelayed();
					CultureTextVM.TextKeyVM.MainWindowVM.FileModified = true;
				}
			}
		}

		public bool AcceptPunctuation
		{
			get { return GetValue<bool>("AcceptPunctuation"); }
			set
			{
				if (SetValue(BooleanBoxes.Box(value), "AcceptPunctuation"))
				{
					CultureTextVM.TextKeyVM.MainWindowVM.ValidateTextKeysDelayed();
					CultureTextVM.TextKeyVM.MainWindowVM.FileModified = true;
				}
			}
		}

		public string CursorChar
		{
			get { return null; }
			set
			{
				CultureTextVM.TextKeyVM.MainWindowVM.CursorChar = value;
			}
		}

		public StringCollection TextKeyReferences
		{
			get { return GetValue<StringCollection>("TextKeyReferences"); }
			set
			{
				if (SetValue(value, "TextKeyReferences"))
				{
					CultureTextVM.TextKeyVM.MainWindowVM.ValidateTextKeysDelayed();
				}
			}
		}

		#endregion Public properties

		#region Commands

		#region Definition and initialisation

		public DelegateCommand DeleteCommand { get; private set; }
		public DelegateCommand ToggleAcceptMissingCommand { get; private set; }
		public DelegateCommand ToggleAcceptPlaceholdersCommand { get; private set; }
		public DelegateCommand ToggleAcceptPunctuationCommand { get; private set; }

		protected override void InitializeCommands()
		{
			DeleteCommand = new DelegateCommand(OnDelete);
			ToggleAcceptMissingCommand = new DelegateCommand(() => { AcceptMissing = !AcceptMissing; });
			ToggleAcceptPlaceholdersCommand = new DelegateCommand(() => { AcceptPlaceholders = !AcceptPlaceholders; });
			ToggleAcceptPunctuationCommand = new DelegateCommand(() => { AcceptPunctuation = !AcceptPunctuation; });
		}

		#endregion Definition and initialisation

		#region Command handlers

		private void OnDelete()
		{
			int myIndex = CultureTextVM.QuantifiedTextVMs.IndexOf(this);

			CultureTextVM.QuantifiedTextVMs.Remove(this);
			CultureTextVM.TextKeyVM.MainWindowVM.ValidateTextKeysDelayed();
			CultureTextVM.TextKeyVM.MainWindowVM.FileModified = true;

			if (CultureTextVM.QuantifiedTextVMs.Count == 0)
			{
				CultureTextVM.ViewCommandManager.InvokeLoaded("FocusText");
			}
			else if (myIndex < CultureTextVM.QuantifiedTextVMs.Count)
			{
				CultureTextVM.QuantifiedTextVMs[myIndex].ViewCommandManager.InvokeLoaded("FocusText");
			}
			else
			{
				CultureTextVM.QuantifiedTextVMs[myIndex - 1].ViewCommandManager.InvokeLoaded("FocusText");
			}
		}

		#endregion Command handlers

		#endregion Commands

		#region Public methods

		/// <summary>
		/// Returns a value indicating whether any data was entered for this text item.
		/// </summary>
		/// <returns></returns>
		public bool IsEmpty()
		{
			return string.IsNullOrEmpty(Text);
		}

		/// <summary>
		/// Creates a new QuantifiedTextViewModel instance with all contents of this instance.
		/// </summary>
		/// <param name="ctVM">New CultureTextViewModel instance to connect the clone with.</param>
		/// <returns></returns>
		public QuantifiedTextViewModel Clone(CultureTextViewModel ctVM)
		{
			QuantifiedTextViewModel clone = new QuantifiedTextViewModel(ctVM);
			clone.Count = Count;
			clone.Modulo = Modulo;
			clone.Text = Text;
			return clone;
		}

		public static int Compare(object a, object b)
		{
			QuantifiedTextViewModel qa = a as QuantifiedTextViewModel;
			QuantifiedTextViewModel qb = b as QuantifiedTextViewModel;
			if (qa == null || qb == null) return 0;

			int cmp;
			cmp = qa.Count - qb.Count;
			if (cmp != 0) return cmp;

			cmp = qa.Modulo - qb.Modulo;
			return cmp;
		}

		#endregion Public methods

		#region IViewCommandSource members

		private ViewCommandManager viewCommandManager = new ViewCommandManager();
		public ViewCommandManager ViewCommandManager { get { return viewCommandManager; } }

		#endregion IViewCommandSource members
	}
}
