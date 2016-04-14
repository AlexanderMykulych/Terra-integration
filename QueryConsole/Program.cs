using System;
using System.Configuration;
using System.IO;
using System.Reflection;
using Terrasoft.Common;
using Terrasoft.Configuration;
using Terrasoft.Core;
using Terrasoft.Core.DB;
using Terrasoft.Core.Entities;
using System.Collections.Generic;
using System.Diagnostics;
using Newtonsoft.Json.Linq;

namespace QueryConsole
{
	public class Program
	{
		public static void Main(string[] args) {
			try {
				var consoleApp = new TerrasoftConsoleClass("Default");
				try {
				consoleApp.Run();
				} catch(Exception e) {
					consoleApp.ConsoleColorWrite("Connect to Database: Failed", ConsoleColor.Red);
					Console.WriteLine(e.Message);
				}
				consoleApp.ConsoleColorWrite("Connect to Database: Success");
				Console.WriteLine("Press any button to start integrate");
				Console.ReadKey();
				var integrationEntityNames = new List<string>() {
					"Account",
					//"Contact",
					//"Case",
					//"TsAutomobile",
					//"ContactCareer",
					//"TsAutoTechService",
					//"TsAutoOwnerHistory",
					//"TsAutoOwnerInfo",
					//"TsAccountManagerGroup",
					//"ContactCommunication",
					//"TsAccountNotification",
					//"TsContactNotifications",
					//"Relationship",
					//"SysAdminUnit"
				};
				foreach(var entityName in integrationEntityNames) {
					consoleApp.UpdateEntity(entityName);
				}
			} catch (ReflectionTypeLoadException e1) {
				Console.WriteLine(e1.Message);
			} catch (Exception e) {
				Console.WriteLine(e.Message);
			}
			Console.ForegroundColor = ConsoleColor.Green;
			Console.WriteLine("Press eny key!");
			Console.ReadKey();
		}
	}

	#region Class: TerrasoftConsoleClass

	public class TerrasoftConsoleClass
	{
		#region Constructors: Public

		public TerrasoftConsoleClass(string workspaceName) {
			WorkspaceName = workspaceName;
			SystemUserConnection = AppConnection.SystemUserConnection;
			AppManagerProvider
			= _appManagerProvider ?? (_appManagerProvider = AppConnection.AppManagerProvider);
		}

		#endregion

		#region Properties: Public

		public string WorkspaceName {
			get;
			set;
		}

		private UserConnection SystemUserConnection;

		private AppConnection _appConnection;

		public AppConnection AppConnection {
			get {
				if (_appConnection == null) {
					_appConnection = new AppConnection();
				}
				return _appConnection;
			}
			protected set {
				_appConnection = value;
			}
		}

		private ManagerProvider _appManagerProvider;

		public ManagerProvider AppManagerProvider;

		#endregion

		#region Methods: Protected

		protected virtual Assembly CurrentDomainAssemblyResolve(object sender, ResolveEventArgs args) {
			string requestingAssemblyName = args.Name;
			var appUri = new UriBuilder(Assembly.GetExecutingAssembly().CodeBase);
			string host = appUri.Host;
			if (!string.IsNullOrEmpty(host)) {
				host = @"\\" + host;
			}
			string appPath = Path.Combine(host, Path.GetDirectoryName(Uri.UnescapeDataString(appUri.Path.TrimStart('/'))));
			var processRunMode = Environment.Is64BitProcess ? "x64" : "x86";
			int index = requestingAssemblyName.IndexOf(',');
			if (index > 0) {
				string requestingAssemblyPath = Path.Combine(appPath, processRunMode,
					requestingAssemblyName.Substring(0, index) + ".dll");
				if (File.Exists(requestingAssemblyPath)) {
					return Assembly.LoadFrom(requestingAssemblyPath);
				}
			}
			return null;
		}

		protected AppConfigurationSectionGroup GetAppSettings() {
			Configuration configuration = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
			var appSettings = (AppConfigurationSectionGroup) configuration.SectionGroups["terrasoft"];
			appSettings.RootConfiguration = configuration;
			return appSettings;
		}

		protected virtual void Initialize(ConfigurationSectionGroup appConfigurationSectionGroup) {
			var appSettings = (AppConfigurationSectionGroup) appConfigurationSectionGroup;
			string appDirectory = Path.GetDirectoryName(this.GetType().Assembly.Location);
			appSettings.Initialize(appDirectory, Path.Combine(appDirectory, "App_Data"), Path.Combine(appDirectory, "Resources"),
				appDirectory);
			AppConnection.Initialize(appSettings);
			AppConnection.InitializeWorkspace(WorkspaceName);
		}

		#endregion

		#region Methods: Public

		public void Run() {
			AppDomain.CurrentDomain.AssemblyResolve += CurrentDomainAssemblyResolve;
			AppConfigurationSectionGroup appSettings = GetAppSettings();
			var resources = (ResourceConfigurationSectionGroup) appSettings.SectionGroups["resources"];
			GeneralResourceStorage.Initialize(resources);
			Initialize(appSettings);
		}		
		public EntityCollection GetEntitiesForUpdate(string name, bool onlyNotImportet = false) {
			var esq = new EntitySchemaQuery(SystemUserConnection.EntitySchemaManager, name);
			esq.AddAllSchemaColumns();
			if(onlyNotImportet) {
				//esq.Filters.Add(esq.CreateFilterWithParameters(FilterComparisonType.Equal, "TsExternalId", 0));
			}
			return esq.GetEntityCollection(SystemUserConnection);
		}

		public void UpdateEntity(string entityName) {
			ConsoleColorWrite(string.Format("{0}: Start Integrate", entityName));
			var entities = GetEntitiesForUpdate(entityName);
			int j = 1, count = entities.Count;
			ConsoleColorWrite(string.Format("{0} count: {1}", entityName, count));
			foreach(var entity in entities) {
				Console.WriteLine("{0} with id = '{1}'", entityName, entity.PrimaryColumnValue.ToString());
				var integrator = new ClientServiceIntegrator(SystemUserConnection);
				var sw = new Stopwatch();
				sw.Start();
				integrator.Update(entity);
				sw.Stop();
				ConsoleColorWrite("sec = " + sw.ElapsedMilliseconds / 1000.0 + " ");
				Console.WriteLine(j++ + "/" + count);
			}
			ConsoleColorWrite(entityName + ": end integrate");
		}
		public void Import(string json, string entityName) {
			var integrator = new IntegrationEntityHelper();
			var integrationInfo = new Terrasoft.Configuration.CsConstant.IntegrationInfo(Newtonsoft.Json.JsonConvert.DeserializeObject(json) as JObject, SystemUserConnection, CsConstant.TIntegrationType.Import, null, entityName, "create");
			var sw = new Stopwatch();
			sw.Start();
			integrator.IntegrateEntity(integrationInfo);
			sw.Stop();
			Console.WriteLine("sec = " + sw.ElapsedMilliseconds / 1000.0);
			Console.WriteLine("millisec = " + sw.ElapsedMilliseconds);
			if(integrationInfo.Result != null && integrationInfo.Result.Type == CsConstant.IntegrationResult.TResultType.Success) {
				Console.ForegroundColor = ConsoleColor.Green;
				Console.WriteLine("Success");
			}
		}

		public void ConsoleColorWrite(string text, ConsoleColor color = ConsoleColor.Green) {
			var buff = Console.ForegroundColor;
			Console.ForegroundColor = color;
			Console.WriteLine(text);
			Console.ForegroundColor = buff;
		}
		#endregion

	}

	#endregion

}
