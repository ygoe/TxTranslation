using System;
using System.Collections.Specialized;
using System.Linq;
using Unclassified.UI;

namespace Unclassified.TxEditor.ViewModel
{
	internal class QuantifiedTextViewModel : ViewModelBase, IViewCommandSource
	{
		#region Constructor

		public QuantifiedTextViewModel(CultureTextViewModel cultureTextVM)
		{
			CultureTextVM = cultureTextVM;

			InitializeCommands();
		}

		#endregion Constructor

		#region Public properties

		public CultureTextViewModel CultureTextVM { get; private set; }

		private int count;
		public int Count
		{
			get { return count; }
			set
			{
				if (CheckUpdate(value, ref count, "Count"))
				{
					CultureTextVM.TextKeyVM.MainWindowVM.ValidateTextKeysDelayed();
					CultureTextVM.TextKeyVM.MainWindowVM.FileModified = true;
				}
			}
		}

		private int modulo;
		public int Modulo
		{
			get { return modulo; }
			set
			{
				if (CheckUpdate(value, ref modulo, "Modulo"))
				{
					CultureTextVM.TextKeyVM.MainWindowVM.ValidateTextKeysDelayed();
					CultureTextVM.TextKeyVM.MainWindowVM.FileModified = true;
				}
			}
		}

		private string text;
		public string Text
		{
			get { return text; }
			set
			{
				if (CheckUpdate(value, ref text, "Text"))
				{
					CultureTextVM.TextKeyVM.MainWindowVM.ValidateTextKeysDelayed();
					CultureTextVM.TextKeyVM.MainWindowVM.FileModified = true;
				}
			}
		}

		private bool isMissing;
		public bool IsMissing
		{
			get { return isMissing; }
			set { CheckUpdate(value, ref isMissing, "IsMissing"); }
		}

		private bool isPlaceholdersProblem;
		public bool IsPlaceholdersProblem
		{
			get { return isPlaceholdersProblem; }
			set { CheckUpdate(value, ref isPlaceholdersProblem, "IsPlaceholdersProblem"); }
		}

		private bool isPunctuationProblem;
		public bool IsPunctuationProblem
		{
			get { return isPunctuationProblem; }
			set { CheckUpdate(value, ref isPunctuationProblem, "IsPunctuationProblem"); }
		}

		private bool acceptMissing;
		public bool AcceptMissing
		{
			get { return acceptMissing; }
			set
			{
				if (CheckUpdate(value, ref acceptMissing, "AcceptMissing"))
				{
					CultureTextVM.TextKeyVM.MainWindowVM.ValidateTextKeysDelayed();
					CultureTextVM.TextKeyVM.MainWindowVM.FileModified = true;
				}
			}
		}

		private bool acceptPlaceholders;
		public bool AcceptPlaceholders
		{
			get { return acceptPlaceholders; }
			set
			{
				if (CheckUpdate(value, ref acceptPlaceholders, "AcceptPlaceholders"))
				{
					CultureTextVM.TextKeyVM.MainWindowVM.ValidateTextKeysDelayed();
					CultureTextVM.TextKeyVM.MainWindowVM.FileModified = true;
				}
			}
		}

		private bool acceptPunctuation;
		public bool AcceptPunctuation
		{
			get { return acceptPunctuation; }
			set
			{
				if (CheckUpdate(value, ref acceptPunctuation, "AcceptPunctuation"))
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

		private StringCollection textKeyReferences;
		public StringCollection TextKeyReferences
		{
			get { return textKeyReferences; }
			set
			{
				if (CheckUpdate(value, ref textKeyReferences, "TextKeyReferences"))
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

		private void InitializeCommands()
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
