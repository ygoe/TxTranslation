using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;

namespace TxEditor.ViewModel
{
	class DetailsMessageViewModel : ViewModelBase
	{
		public string Title { get; set; }
		public string Message { get; set; }
		public string IconName { get; set; }

		public DetailsMessageViewModel(string title, string message, string iconName)
		{
			Title = title;
			Message = message;
			IconName = iconName;
		}

		public DetailsMessageViewModel(string title, string message)
		{
			Title = title;
			Message = message;
		}

		public DetailsMessageViewModel(string title)
		{
			Title = title;
		}

		public Visibility ArrowIconVisibility { get { return IconName == "Arrow" ? Visibility.Visible : Visibility.Collapsed; } }
		public Visibility FlashIconVisibility { get { return IconName == "Flash" ? Visibility.Visible : Visibility.Collapsed; } }
	}
}
