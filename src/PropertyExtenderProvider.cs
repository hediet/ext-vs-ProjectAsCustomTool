using System;
using System.Runtime.InteropServices;
using EnvDTE;
using Microsoft.VisualStudio.Shell.Interop;

namespace ProjectAsCustomTool
{
	[ComVisible(true)]
	[Guid(ExtenderGuid)]
	public class PropertyExtenderProvider : IExtenderProvider
	{
		public const string ExtenderGuid = "124D2A83-20C0-4783-AD6B-032929BEC4B1";

		private readonly DTE dte;
		private readonly IServiceProvider serviceProvider;

		public PropertyExtenderProvider(DTE dte, IServiceProvider serviceProvider)
		{
			this.dte = dte;
			this.serviceProvider = serviceProvider;
		}

		public object GetExtender(string extenderCatId, string extenderName, object extendeeObject, IExtenderSite extenderSite, int cookie)
		{
			dynamic extendee = extendeeObject;
			string fullPath = extendee.FullPath;
			var projectItem = dte.Solution.FindProjectItem(fullPath);
			var solution = (IVsSolution)serviceProvider.GetService(typeof(SVsSolution));
			IVsHierarchy projectHierarchy;
			if (solution.GetProjectOfUniqueName(projectItem.ContainingProject.UniqueName, out projectHierarchy) != 0)
				return null;
			uint itemId;
			if (projectHierarchy.ParseCanonicalName(fullPath, out itemId) != 0)
				return null;

			return new PropertyExtender((IVsBuildPropertyStorage)projectHierarchy, itemId, extenderSite, cookie);
		}

		public bool CanExtend(string extenderCatId, string extenderName, object extendeeObject)
		{
			return true;
		}
	}
}