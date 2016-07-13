using System;
using System.Configuration;
using System.IO;
using System.Reflection;
using Terrasoft.Common;
using Terrasoft.TsConfiguration;
using Terrasoft.Core;
using Terrasoft.Core.DB;
using Terrasoft.Core.Entities;
using System.Collections.Generic;
using System.Diagnostics;
using Newtonsoft.Json.Linq;
using QueryConsole.Files.Integrators;
using QueryConsole.Files.BpmEntityHelper;
using QueryConsole.Files.Constants;
using QueryConsole.Files.IntegratorTester;

namespace QueryConsole
{
	public class Program
	{
		public static void Main(string[] args) {
			try {
				//var consoleApp = new TerrasoftConsoleClass("A.Mykulych");
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
				var testers = new List<BaseIntegratorTester>() {
					new OrderServiceIntegratorTester(consoleApp.SystemUserConnection),
					//new ClientServiceIntegratorTester(consoleApp.SystemUserConnection)
				};
                //	//Account

                //	var esq = new EntitySchemaQuery(consoleApp.SystemUserConnection.EntitySchemaManager, "Account");
                //	esq.AddAllSchemaColumns();
                //	esq.RowCount = 1;
                //	var createdOn = esq.AddColumn("CreatedOn");
                //	createdOn.OrderByDesc();
                //	var entity = esq.GetEntityCollection(consoleApp.SystemUserConnection)[0];
                //	testers[1].ImportBpmEntity(entity);
                ////Contact

                //	var esq2 = new EntitySchemaQuery(consoleApp.SystemUserConnection.EntitySchemaManager, "Contact");
                //	esq2.AddAllSchemaColumns();
                //	esq2.RowCount = 1;
                //	var createdOn2 = esq2.AddColumn("CreatedOn");
                //	createdOn2.OrderByDesc();
                //	var entity2 = esq2.GetEntityCollection(consoleApp.SystemUserConnection)[0];
                //	testers[1].ImportBpmEntity(entity2);
                ////TsAutomobile
                //	var esq3 = new EntitySchemaQuery(consoleApp.SystemUserConnection.EntitySchemaManager, "TsAutomobile");
                //	esq3.AddAllSchemaColumns();
                //	esq3.RowCount = 1;
                //	var createdOn3 = esq3.AddColumn("CreatedOn");
                //	createdOn3.OrderByDesc();
                //	var entity3 = esq3.GetEntityCollection(consoleApp.SystemUserConnection)[0];
                //	testers[1].ImportBpmEntity(entity3);
                //	int limit = 10;
                //	limit = int.Parse(Console.ReadLine());
                foreach (var tester in testers)
                {
                    tester.ExportAllServiceEntitiesByStep(1000, 3000);
                }
                //var test3 = new IntegrationServiceIntegrator(consoleApp.SystemUserConnection);
                //test3.GetBusEventNotification(true);
                //tester.ExportAllServiceEntities(1000);
                //tester.ExportAllServiceEntitiesByStep(20, 3500);
                //
                //tester2.ExportAllServiceEntities(100);
                //tester.ImportAllBpmEntity();
                while (true) {
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

		public UserConnection SystemUserConnection;

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
				if (System.IO.File.Exists(requestingAssemblyPath)) {
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
			var integrationInfo = new CsConstant.IntegrationInfo(Newtonsoft.Json.JsonConvert.DeserializeObject(json) as JObject, SystemUserConnection, CsConstant.TIntegrationType.Import, null, entityName, "create");
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
