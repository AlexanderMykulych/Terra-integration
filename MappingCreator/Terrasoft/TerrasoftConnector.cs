using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Terrasoft.Common;
using Terrasoft.Core;

namespace MappingCreator.Terrasoft
{
	public class TerrasoftConsoleClass
	{
		#region Constructors: Public

		public TerrasoftConsoleClass(string workspaceName)
		{
			WorkspaceName = workspaceName;
			SystemUserConnection = AppConnection.SystemUserConnection;
			AppManagerProvider
			= _appManagerProvider ?? (_appManagerProvider = AppConnection.AppManagerProvider);
		}

		#endregion

		#region Properties: Public

		public string WorkspaceName
		{
			get;
			set;
		}

		public UserConnection SystemUserConnection;

		private AppConnection _appConnection;

		public AppConnection AppConnection
		{
			get
			{
				if (_appConnection == null)
				{
					_appConnection = new AppConnection();
				}
				return _appConnection;
			}
			protected set
			{
				_appConnection = value;
			}
		}

		private ManagerProvider _appManagerProvider;

		public ManagerProvider AppManagerProvider;

		#endregion

		#region Methods: Protected

		protected virtual Assembly CurrentDomainAssemblyResolve(object sender, ResolveEventArgs args)
		{
			string requestingAssemblyName = args.Name;
			var appUri = new UriBuilder(Assembly.GetExecutingAssembly().CodeBase);
			string host = appUri.Host;
			if (!string.IsNullOrEmpty(host))
			{
				host = @"\\" + host;
			}
			string appPath = Path.Combine(host, Path.GetDirectoryName(Uri.UnescapeDataString(appUri.Path.TrimStart('/'))));
			var processRunMode = Environment.Is64BitProcess ? "x64" : "x86";
			int index = requestingAssemblyName.IndexOf(',');
			if (index > 0)
			{
				string requestingAssemblyPath = Path.Combine(appPath, processRunMode,
					requestingAssemblyName.Substring(0, index) + ".dll");
				if (System.IO.File.Exists(requestingAssemblyPath))
				{
					return Assembly.LoadFrom(requestingAssemblyPath);
				}
			}
			return null;
		}

		protected AppConfigurationSectionGroup GetAppSettings()
		{
			Configuration configuration = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
			var appSettings = (AppConfigurationSectionGroup)configuration.SectionGroups["terrasoft"];
			appSettings.RootConfiguration = configuration;
			return appSettings;
		}

		protected virtual void Initialize(ConfigurationSectionGroup appConfigurationSectionGroup)
		{
			var appSettings = (AppConfigurationSectionGroup)appConfigurationSectionGroup;
			string appDirectory = Path.GetDirectoryName(this.GetType().Assembly.Location);
			appSettings.Initialize(appDirectory, Path.Combine(appDirectory, "App_Data"), Path.Combine(appDirectory, "Resources"),
				appDirectory);
			AppConnection.Initialize(appSettings);
			AppConnection.InitializeWorkspace(WorkspaceName);
		}

		#endregion

		#region Methods: Public

		public void Run()
		{
			AppDomain.CurrentDomain.AssemblyResolve += CurrentDomainAssemblyResolve;
			AppConfigurationSectionGroup appSettings = GetAppSettings();
			var resources = (ResourceConfigurationSectionGroup)appSettings.SectionGroups["resources"];
			GeneralResourceStorage.Initialize(resources);
			Initialize(appSettings);
		}
		#endregion
	}
}
