using System.ComponentModel;
using System.Linq;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using VSLangProj;

namespace ProjectAsCustomTool
{
	internal class SelectedProjectConverter : TypeConverter
	{
		public override bool GetStandardValuesSupported(ITypeDescriptorContext context) => true;

		public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext context)
		{
			var dte = (EnvDTE80.DTE2)Package.GetGlobalService(typeof(SDTE));
			var projects = dte.Solution.Projects.Cast<Project>().ToArray();

			var list = projects.Where(p => p.Kind != EnvDTE.Constants.vsProjectItemKindMisc).Select(p => p.UniqueName).ToList();
			list.Insert(0, null); // so that one can null the setting

			return new StandardValuesCollection(list);
		}
	}
}