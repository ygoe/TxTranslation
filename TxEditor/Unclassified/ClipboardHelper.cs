using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;

namespace Unclassified
{
	internal static class ClipboardHelper
	{
		/// <summary>
		/// Gets a DataObject that contains all restorable formats of the clipboard.
		/// </summary>
		/// <returns></returns>
		public static IDataObject GetDataObject()
		{
			IDataObject orig = Clipboard.GetDataObject();
			DataObject obj = new DataObject();

			foreach (string format in orig.GetFormats())
			{
				// Only copy supported formats that can be saved and restored
				if (format == "Bitmap" ||
					format == "FileDrop" ||
					format == "FileName" ||
					format == "FileNameW" ||
					format == "HTML Format" ||
					format == "Rich Text Format" ||
					format == "Text" ||
					format == "UnicodeText")
				{
					obj.SetData(format, orig.GetData(format));
				}
			}

			return obj;
		}
	}
}
