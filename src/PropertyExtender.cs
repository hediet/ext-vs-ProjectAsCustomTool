using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using EnvDTE;
using Microsoft.VisualStudio.Shell.Interop;

namespace ProjectAsCustomTool
{
	[ComVisible(true)]
	public class PropertyExtender
	{
		private readonly IVsBuildPropertyStorage storage;
		private readonly uint itemId;
		private readonly IExtenderSite extenderSite;
		private readonly int cookie;

		public PropertyExtender(IVsBuildPropertyStorage storage, uint itemId, IExtenderSite extenderSite, int cookie)
		{
			this.storage = storage;
			this.itemId = itemId;
			this.extenderSite = extenderSite;
			this.cookie = cookie;
		}

		~PropertyExtender()
		{
			try
			{
				extenderSite?.NotifyDelete(cookie);
			}
			catch (Exception ex)
			{
				Debug.WriteLine("Error in PropertyExtender finalizer: {0}", ex);
			}
		}

		[Category("GeneratorProject")]
		[DisplayName("Generator Project")]
		[Description("Specifies the project within the current solution that is used as generator.")]
		[TypeConverter(typeof(SelectedProjectConverter))]
		public string GeneratorProject
		{
			get
			{
				string s;
				storage.GetItemAttribute(itemId, nameof(GeneratorProject), out s);
				return s;
			}
			set
			{
				storage.SetItemAttribute(itemId, nameof(GeneratorProject), value);
			}
		}

	}
}