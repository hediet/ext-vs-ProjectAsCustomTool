using System.Runtime.InteropServices;
using EnvDTE;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace ProjectAsCustomTool
{
	[PackageRegistration(UseManagedResourcesOnly = true)]
	[InstalledProductRegistration("#110", "#112", "1.0", IconResourceID = 400)]
	[Guid(VsPackage.PackageGuidString)]
	[ProvideAutoLoad(UIContextGuids.SolutionExists)]
	public sealed class VsPackage : Package
	{
		/// <summary>
		/// VsPackage GUID string.
		/// </summary>
		public const string PackageGuidString = "619b75de-a4b7-454c-8fa8-442d649fb245";

		protected override void Initialize()
		{
			base.Initialize();

			var dte = (DTE)GetService(typeof(DTE));

			var provider = new PropertyExtenderProvider(dte, this);

			dte.ObjectExtenders.RegisterExtenderProvider(VSConstants.CATID.CSharpFileProperties_string, typeof(PropertyExtenderProvider).FullName, provider);
		}
	}
}