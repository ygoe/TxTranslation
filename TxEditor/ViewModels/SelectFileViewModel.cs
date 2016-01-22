using System;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Xml;
using Unclassified.FieldLog;
using Unclassified.UI;
using Unclassified.Util;

namespace Unclassified.TxEditor.ViewModels
{
	internal class SelectFileViewModel : ViewModelBase, IViewCommandSource
	{
		#region Private data

		private string baseDir;
		private BackgroundWorker scanBw;
		private ConcurrentQueue<string> foundFilesQueue;
		private ObservableCollection<string> foundFiles;

		#endregion Private data

		#region Constructor

		public SelectFileViewModel(string baseDir)
		{
			if (!baseDir.EndsWith(Path.DirectorySeparatorChar.ToString()))
			{
				baseDir += Path.DirectorySeparatorChar;
			}
			this.baseDir = baseDir;

			foundFiles = new ObservableCollection<string>();
			foundFilesQueue = new ConcurrentQueue<string>();

			scanBw = new BackgroundWorker();
			scanBw.DoWork += scanBw_DoWork;
			scanBw.ProgressChanged += scanBw_ProgressChanged;
			scanBw.RunWorkerCompleted += scanBw_RunWorkerCompleted;
			scanBw.WorkerReportsProgress = true;
			scanBw.WorkerSupportsCancellation = true;
			scanBw.RunWorkerAsync();
		}

		#endregion Constructor

		#region Data properties

		public string BaseDir { get { return baseDir; } }

		public ObservableCollection<string> FoundFiles
		{
			get
			{
				return foundFiles;
			}
		}

		public string[] SelectedFileNames { get; set; }

		public Visibility SpinnerVisibility
		{
			get
			{
				return scanBw.IsBusy ? Visibility.Visible : Visibility.Collapsed;
			}
		}

		public Visibility AllButtonVisibility
		{
			get { return foundFiles.Count > 1 ? Visibility.Visible : Visibility.Collapsed; }
		}

		#endregion Data properties

		#region BackgroundWorker

		private void scanBw_DoWork(object sender, DoWorkEventArgs args)
		{
			ScanDirectory(baseDir);
		}

		private void scanBw_ProgressChanged(object sender, ProgressChangedEventArgs args)
		{
			string str;
			while (foundFilesQueue.TryDequeue(out str))
			{
				if (str.Length > baseDir.Length)
				{
					str = str.Substring(baseDir.Length);
				}
				foundFiles.InsertSorted(str, (a, b) => string.Compare(a, b, StringComparison.InvariantCultureIgnoreCase));
				OnPropertyChanged(() => AllButtonVisibility);
			}
		}

		private void scanBw_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs args)
		{
			OnPropertyChanged(() => SpinnerVisibility);
			if (args.Error != null)
			{
				FL.Error(args.Error);
				// TODO: Show the error in a user message box
			}
			else
			{
				if (FoundFiles.Count == 1)
				{
					// Automatically load the only found file
					ViewCommandManager.Invoke("LoadSingleFile");
				}
			}
		}

		#endregion BackgroundWorker

		#region Directory scanning

		private void ScanDirectory(string startDir)
		{
			foreach (string fileName in Directory.GetFiles(startDir))
			{
				string localFileName = Path.GetFileName(fileName);
				if (Path.GetExtension(localFileName) == ".txd")
				{
					foundFilesQueue.Enqueue(fileName);
					scanBw.ReportProgress(1);
				}
				if (Regex.IsMatch(localFileName, @"[^.]\.[a-z]{2}(?:-[a-z]{2})?\.xml$", RegexOptions.IgnoreCase))
				{
					try
					{
						XmlDocument xdoc = new XmlDocument();
						xdoc.Load(fileName);
						if (xdoc.DocumentElement.Name == "translation")
						{
							foundFilesQueue.Enqueue(fileName);
							scanBw.ReportProgress(1);
						}
					}
					catch
					{
						// Ignore errors, it's just not the file we're looking for
					}
				}
			}
			foreach (string dirName in Directory.GetDirectories(startDir))
			{
				string localDirName = Path.GetFileName(dirName);
				if (localDirName != "bin" &&
					localDirName != "obj" &&
					!localDirName.StartsWith("."))
				{
					ScanDirectory(dirName);
				}
			}
		}

		#endregion Directory scanning

		#region IViewCommandSource members

		private ViewCommandManager viewCommandManager = new ViewCommandManager();
		public ViewCommandManager ViewCommandManager { get { return viewCommandManager; } }

		#endregion IViewCommandSource members
	}
}
