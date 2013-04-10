using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Collections.Specialized;

using log4net;
using Exortech.NetReflector;
using ThoughtWorks.CruiseControl.Core;
using ThoughtWorks.CruiseControl.Remote;
using ThoughtWorks.CruiseControl.Core.Tasks;

namespace CCNet.ExecLabeller.Plugin
{
	[ReflectorType("execLabeller")]
	public class ExecLabeller : ILabeller
	{
		#region ILabeller Members

		public string Generate(IIntegrationResult integrationResult)
		{
			log.Debug("Generate label");

			string label = GetExecLabel(integrationResult);

			log.DebugFormat("Label generated [{0}]", label);
			return label;
		}

		#endregion

		private string GetExecLabel(IIntegrationResult integrationResult)
		{
			ProcessStartInfo executableStartInfo = CreateExecutableProcessStartInfo(integrationResult);

			string label = RunExecutableAndGetOutput(executableStartInfo);

			return label;
		}

		private ProcessStartInfo CreateExecutableProcessStartInfo(IIntegrationResult integrationResult)
		{
			string baseDir = integrationResult.WorkingDirectory;
			if (!String.IsNullOrEmpty(baseDirectory))
			{
				baseDir = baseDirectory;
			}

			ProcessStartInfo startInfo = new ProcessStartInfo(executable, buildArgs);
			startInfo.UseShellExecute = false;
			startInfo.RedirectStandardOutput = true;
			startInfo.RedirectStandardError = true;
			startInfo.WorkingDirectory = baseDir;

			FillEnvironmentVariables(startInfo, integrationResult);

			return startInfo;
		}

		private void FillEnvironmentVariables(ProcessStartInfo startInfo, IIntegrationResult integrationResult)
		{
			integrationResult.Parameters.ForEach(pair =>
			{
				string envName = pair.Name.Substring(1); //$CCNetBuildDate -> CCNetBuildDate
				startInfo.EnvironmentVariables.Add(envName, pair.Value);
			});

// 			foreach(string key in integrationResult.IntegrationProperties.Keys)
// 			{
// 				startInfo.EnvironmentVariables.Add(key, integrationResult.IntegrationProperties[key].ToString());
// 			}
		}

		private string RunExecutableAndGetOutput(ProcessStartInfo startInfo)
		{
			log.DebugFormat("Starting process [{0}] in working directory [{1}] with arguments [{2}] "
				, startInfo.FileName, startInfo.WorkingDirectory, startInfo.Arguments);

// 			log.Debug("Environment variables:");
// 
// 			foreach (string key in startInfo.EnvironmentVariables.Keys)
// 			{
// 				log.DebugFormat("{0}={1}", key, startInfo.EnvironmentVariables[key]);
// 			}

			Process process = Process.Start(startInfo);
			string output = process.StandardOutput.ReadToEnd().Trim(new char[] { '\r', '\n' });
			string errorMessage = process.StandardError.ReadToEnd().Trim(new char[] { '\r', '\n' });
			process.WaitForExit();

			if (0 != process.ExitCode)
			{
				string formattedErrorMessage = String.Format("Process ended with error [{0}], stderr [{1}]", process.ExitCode, errorMessage);
				
				log.Error(formattedErrorMessage);
				throw new BuilderException(this, formattedErrorMessage);
			}

			log.DebugFormat("Process output:{0}", output);

			return output;
		}

		static readonly private ILog log = LogManager.GetLogger(typeof(ExecLabeller));

		#region ITask Members

		public void Run(IIntegrationResult result)
		{
			result.Label = Generate(result);
		}

		#endregion


		[ReflectorProperty("executable", Required = true)]
		public string executable;

		[ReflectorProperty("buildArgs", Required = false)]
		public string buildArgs;

		[ReflectorProperty("baseDirectory", Required = false)]
		public string baseDirectory;
	}
}
