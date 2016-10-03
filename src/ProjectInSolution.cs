using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using VSLangProj80;
using Task = System.Threading.Tasks.Task;

namespace ProjectAsCustomTool
{
	[ComVisible(true)]
	[Guid("AFD180A2-A1F9-4429-9EE2-4118DD669960")]
	[CodeGeneratorRegistration(typeof(ProjectInSolution), nameof(ProjectInSolution), vsContextGuids.vsContextGuidVCSProject, GeneratesDesignTimeSource = true)]
	[ProvideObject(typeof(ProjectInSolution))]
	public class ProjectInSolution : BaseCodeGeneratorWithSite
	{
		protected override string GetDefaultExtension()
		{
			return ".cs";
		}

		protected override byte[] GenerateCode(string inputFileContent)
		{
			var result = GenerateStringCode(inputFileContent);

			var enc = Encoding.Unicode;
			var preamble = enc.GetPreamble();
			var preambleLength = preamble.Length;
			var body = enc.GetBytes(result);

			Array.Resize(ref preamble, preambleLength + body.Length);
			Array.Copy(body, 0, preamble, preambleLength, body.Length);

			return preamble;
		}

		private string GenerateStringCode(string inputFileContent)
		{
			var dte = (DTE2)Package.GetGlobalService(typeof(SDTE));

			var generatorProjectName = GetGeneratorProjectName(dte);

			var projects = dte.Solution.Projects.Cast<Project>().ToArray();
			var proj = projects.FirstOrDefault(p => p.UniqueName == generatorProjectName);

			if (proj == null)
			{
				return $"Error: Project '{generatorProjectName}' not found.";
			}

			var config = dte.Solution.SolutionBuild.ActiveConfiguration.Name;
			dte.Solution.SolutionBuild.BuildProject(config, generatorProjectName, true);

			var path = GetAssemblyPath(proj);

			if (!File.Exists(path))
			{
				return $"Executable '{path}' does not exist. A compilation error?";
			}

			return StartProcessAndGetGeneratedCode(path, inputFileContent).Result;
		}

		// taken from https://social.msdn.microsoft.com/Forums/vstudio/en-US/03d9d23f-e633-4a27-9b77-9029735cfa8d/how-to-get-the-right-output-path-from-envdteproject-by-code-if-show-advanced-build?forum=vsx
		static string GetAssemblyPath(Project vsProject)
		{
			var fullPath = vsProject.Properties.Item("FullPath").Value.ToString();
			var outputPath = vsProject.ConfigurationManager.ActiveConfiguration.Properties.Item("OutputPath").Value.ToString();
			var outputDir = Path.Combine(fullPath, outputPath);
			var outputFileName = vsProject.Properties.Item("OutputFileName").Value.ToString();
			var assemblyPath = Path.Combine(outputDir, outputFileName);
			return assemblyPath;
		}

		private string GetGeneratorProjectName(DTE2 dte)
		{
			var projectItem = dte.Solution.FindProjectItem(InputFilePath);

			var solution = (IVsSolution)Package.GetGlobalService(typeof(SVsSolution));
			IVsHierarchy projectHierarchy;
			if (solution.GetProjectOfUniqueName(projectItem.ContainingProject.UniqueName, out projectHierarchy) != 0)
			{
				throw new InvalidOperationException("Should not happen.");
			}

			uint itemId;
			if (projectHierarchy.ParseCanonicalName(InputFilePath, out itemId) != 0)
			{
				throw new InvalidOperationException("Should not happen.");
			}

			var storage = (IVsBuildPropertyStorage)projectHierarchy;

			string generatorProjectName;
			if (storage.GetItemAttribute(itemId, nameof(PropertyExtender.GeneratorProject), out generatorProjectName) != 0)
			{
				throw new InvalidOperationException("Should not happen.");
			}
			return generatorProjectName;
		}

		private static async Task<string> StartProcessAndGetGeneratedCode(string exePath, string input)
		{
			try
			{
				var startInfo = new ProcessStartInfo(exePath, "generate")
				{
					RedirectStandardInput = true,
					RedirectStandardOutput = true,
					UseShellExecute = false,
					CreateNoWindow = true
				};

				var p = System.Diagnostics.Process.Start(startInfo);

				if (p == null) return $"Could not run '{exePath}'.";

				var t = Task.Run(() =>
				{
					p.StandardInput.Write(input);
					p.StandardInput.Close();

					return p.StandardOutput.ReadToEnd();
				});

				var timeout = TimeSpan.FromSeconds(3);
				await Task.WhenAny(Task.Delay(timeout), t).ConfigureAwait(false);

				if (!t.IsCompleted)
				{
					p.Kill();

					return $"Timeout after {timeout.TotalSeconds} seconds.";
				}

				return t.Result;

			}
			catch (Exception ex)
			{
				return "An exception was thrown: " + ex;
			}
		}

	}
}