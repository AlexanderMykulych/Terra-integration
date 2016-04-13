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
				var consoleApp = new TerrasoftConsoleClass("A.Mykulych");
				consoleApp.Run();
				var integrationType = 3;
				Console.WriteLine("Start");
				var integrationEntityNames = new List<string>() {
					"Account",
					"Contact",
					"Case",
					"TsAutomobile",
					"ContactCareer",
					"TsAutoTechService",
					"TsAutoOwnerHistory",
					"TsAutoOwnerInfo",
					"TsAccountManagerGroup",
					"ContactCommunication",
					"TsAccountNotification",
					"TsContactNotifications",
					"Relationship",
					"Manager",
					"ManagerGroup"
				};
				if(integrationType == 1) {
					foreach(var entityName in integrationEntityNames) {
						consoleApp.ExportEntity(entityName);
					}
				} else if(integrationType == 2) {
					var files = new List<Tuple<string, string>>() {
						new Tuple<string, string>("CompanyProfile", "../../IntegrationJson/CompanyProfile.txt"),
						new Tuple<string, string>("PersonProfile", "../../IntegrationJson/PersonProfile.txt")
					};
					int fileIndex = 0;
					string data = String.Empty;
					using(var stream = new StreamReader(files[fileIndex].Item2)) {
						data = stream.ReadToEnd();
					}
					consoleApp.Import(data, files[fileIndex].Item1);
				} else if(integrationType == 3) {
					foreach(var entityName in integrationEntityNames) {
						consoleApp.UpdateEntity(entityName);
					}
				} else if(integrationType == 4) {
					consoleApp.ExecuteSql();
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

		public EntityCollection GetAccounts() {
			var esq = new EntitySchemaQuery(SystemUserConnection.EntitySchemaManager, "Account");
			esq.AddAllSchemaColumns();
			esq.Filters.Add(esq.CreateFilterWithParameters(FilterComparisonType.Equal, "TsExternalId", 0));
			return esq.GetEntityCollection(SystemUserConnection);
		}

		public EntityCollection GetContacts() {
			var esq = new EntitySchemaQuery(SystemUserConnection.EntitySchemaManager, "Contact");
			esq.AddAllSchemaColumns();
			esq.Filters.Add(esq.CreateFilterWithParameters(FilterComparisonType.Equal, "TsExternalId", 0));
			return esq.GetEntityCollection(SystemUserConnection);
		}
		public EntityCollection GetCase() {
			var esq = new EntitySchemaQuery(SystemUserConnection.EntitySchemaManager, "Case");
			esq.AddAllSchemaColumns();
			esq.Filters.Add(esq.CreateFilterWithParameters(FilterComparisonType.Equal, "TsExternalId", 0));
			return esq.GetEntityCollection(SystemUserConnection);
		}

		public EntityCollection GetTsAutomobile() {
			var esq = new EntitySchemaQuery(SystemUserConnection.EntitySchemaManager, "TsAutomobile");
			esq.AddAllSchemaColumns();
			esq.Filters.Add(esq.CreateFilterWithParameters(FilterComparisonType.Equal, "TsExternalId", 0));
			return esq.GetEntityCollection(SystemUserConnection);
		}

		public EntityCollection GetContactCareer() {
			var esq = new EntitySchemaQuery(SystemUserConnection.EntitySchemaManager, "ContactCareer");
			esq.AddAllSchemaColumns();
			esq.Filters.Add(esq.CreateFilterWithParameters(FilterComparisonType.Equal, "TsExternalId", 0));
			return esq.GetEntityCollection(SystemUserConnection);
		}

		public EntityCollection GetTsAutoTechService() {
			var esq = new EntitySchemaQuery(SystemUserConnection.EntitySchemaManager, "TsAutoTechService");
			esq.AddAllSchemaColumns();
			esq.Filters.Add(esq.CreateFilterWithParameters(FilterComparisonType.Equal, "TsExternalId", 0));
			return esq.GetEntityCollection(SystemUserConnection);
		}
		public EntityCollection GetTsAutoOwnerHistory() {
			var esq = new EntitySchemaQuery(SystemUserConnection.EntitySchemaManager, "TsAutoOwnerHistory");
			esq.AddAllSchemaColumns();
			esq.Filters.Add(esq.CreateFilterWithParameters(FilterComparisonType.Equal, "TsExternalId", 0));
			return esq.GetEntityCollection(SystemUserConnection);
		}
		public EntityCollection GetTsAutoOwnerInfo() {
			var esq = new EntitySchemaQuery(SystemUserConnection.EntitySchemaManager, "TsAutoOwnerInfo");
			esq.AddAllSchemaColumns();
			esq.Filters.Add(esq.CreateFilterWithParameters(FilterComparisonType.Equal, "TsExternalId", 0));
			return esq.GetEntityCollection(SystemUserConnection);
		}
		public EntityCollection GetTsAccountNotification() {
			var esq = new EntitySchemaQuery(SystemUserConnection.EntitySchemaManager, "TsAccountNotification");
			esq.AddAllSchemaColumns();
			esq.Filters.Add(esq.CreateFilterWithParameters(FilterComparisonType.Equal, "TsExternalId", 0));
			return esq.GetEntityCollection(SystemUserConnection);
		}
		public EntityCollection GetSysAdminUnit() {
			var esq = new EntitySchemaQuery(SystemUserConnection.EntitySchemaManager, "SysAdminUnit");
			esq.AddAllSchemaColumns();
			esq.Filters.Add(esq.CreateFilterWithParameters(FilterComparisonType.Equal, "TsExternalId", 0));
			esq.Filters.Add(esq.CreateFilterWithParameters(FilterComparisonType.Less, "SysAdminUnitTypeValue", 4));
			//esq.Filters.Add(esq.CreateFilterWithParameters(FilterComparisonType.NotEqual, "[SysUserInRole:SysUser:Id].SysRole.TsExternalId", 0));
			return esq.GetEntityCollection(SystemUserConnection);
		}
		public EntityCollection GetRelationship() {
			var esq = new EntitySchemaQuery(SystemUserConnection.EntitySchemaManager, "Relationship");
			esq.AddAllSchemaColumns();
			esq.Filters.Add(esq.CreateFilterWithParameters(FilterComparisonType.Equal, "TsExternalId", 0));
			return esq.GetEntityCollection(SystemUserConnection);
		}
		public EntityCollection GetEntities(string name) {
			var esq = new EntitySchemaQuery(SystemUserConnection.EntitySchemaManager, name);
			esq.AddAllSchemaColumns();
			esq.Filters.Add(esq.CreateFilterWithParameters(FilterComparisonType.Equal, "TsExternalId", 0));
			return esq.GetEntityCollection(SystemUserConnection);
		}
		public EntityCollection GetEntitiesForUpdate(string name) {
			var esq = new EntitySchemaQuery(SystemUserConnection.EntitySchemaManager, name);
			esq.AddAllSchemaColumns();
			//esq.Filters.Add(esq.CreateFilterWithParameters(FilterComparisonType.Equal, "TsExternalId", 0));
			return esq.GetEntityCollection(SystemUserConnection);
		}
		public void ExportContacts(int i, List<Tuple<string, Guid>> entityList) {
			var entityName = entityList[i].Item1;
			var entityId = entityList[i].Item2;
			var contacts = GetContacts();
			int j = 1;
			foreach(var entity in contacts) {
				Console.WriteLine(entity.GetColumnValue("Id") + " " + entity.GetColumnValue("Name"));
				var integrator = new ClientServiceIntegrator(SystemUserConnection);

				var sw = new Stopwatch();
				sw.Start();
				integrator.Update(entity);
				sw.Stop();
				Console.Write("sec = " + sw.ElapsedMilliseconds / 1000.0 + " ");
				Console.WriteLine("millisec = " + sw.ElapsedMilliseconds);
				Console.WriteLine(j++ + "/" + contacts.Count);
			}
		}
		public void ExportAccounts(int i, List<Tuple<string, Guid>> entityList) {
			var entityName = entityList[i].Item1;
			var entityId = entityList[i].Item2;
			var accounts = GetAccounts();
			int j = 1;
			foreach(var entity in accounts) {
				Console.WriteLine(entity.GetColumnValue("Id") + " " + entity.GetColumnValue("Name"));
				var integrator = new ClientServiceIntegrator(SystemUserConnection);

				var sw = new Stopwatch();
				sw.Start();
				integrator.Update(entity);
				sw.Stop();
				Console.Write("sec = " + sw.ElapsedMilliseconds / 1000.0 + " ");
				Console.WriteLine("millisec = " + sw.ElapsedMilliseconds);
				Console.WriteLine(j++ + "/" + accounts.Count);
			}
		}
		public void ExportCase(int i, List<Tuple<string, Guid>> entityList) {
			var entityName = entityList[i].Item1;
			var entityId = entityList[i].Item2;
			var cases = GetCase();
			int j = 1;
			foreach(var entity in cases) {
				Console.WriteLine(entity.GetColumnValue("Id") + " " + entity.GetColumnValue("Number"));
				var integrator = new ClientServiceIntegrator(SystemUserConnection);

				var sw = new Stopwatch();
				sw.Start();
				integrator.Update(entity);
				sw.Stop();
				Console.Write("sec = " + sw.ElapsedMilliseconds / 1000.0 + " ");
				Console.WriteLine("millisec = " + sw.ElapsedMilliseconds);
				Console.WriteLine(j++ + "/" + cases.Count);
			}
		}
		public void ExportTsAutomobile(int i, List<Tuple<string, Guid>> entityList) {
			var entityName = entityList[i].Item1;
			var entityId = entityList[i].Item2;
			var automobile = GetTsAutomobile();
			int j = 1;
			foreach(var entity in automobile) {
				Console.WriteLine(entity.GetColumnValue("Id") + " " + entity.GetColumnValue("TsName"));
				var integrator = new ClientServiceIntegrator(SystemUserConnection);

				var sw = new Stopwatch();
				sw.Start();
				integrator.Update(entity);
				sw.Stop();
				Console.Write("sec = " + sw.ElapsedMilliseconds / 1000.0 + " ");
				Console.WriteLine("millisec = " + sw.ElapsedMilliseconds);
				Console.WriteLine(j++ + "/" + automobile.Count);
			}
		}
		public void ExportContactCareer(int i, List<Tuple<string, Guid>> entityList) {
			var entityName = entityList[i].Item1;
			var entityId = entityList[i].Item2;
			var contactCareer = GetContactCareer();
			int j = 1;
			foreach(var entity in contactCareer) {
				Console.WriteLine(entity.GetColumnValue("Id") + " " + entity.GetColumnValue("JobTitle"));
				var integrator = new ClientServiceIntegrator(SystemUserConnection);

				var sw = new Stopwatch();
				sw.Start();
				integrator.Update(entity);
				sw.Stop();
				Console.Write("sec = " + sw.ElapsedMilliseconds / 1000.0 + " ");
				Console.WriteLine("millisec = " + sw.ElapsedMilliseconds);
				Console.WriteLine(j++ + "/" + contactCareer.Count);
			}
		}
		public void ExportTsAutoTechService(int i, List<Tuple<string, Guid>> entityList) {
			var entityName = entityList[i].Item1;
			var entityId = entityList[i].Item2;
			var tsAutoTechService = GetTsAutoTechService();
			int j = 1;
			foreach(var entity in tsAutoTechService) {
				Console.WriteLine(entity.GetColumnValue("Id"));
				var integrator = new ClientServiceIntegrator(SystemUserConnection);

				var sw = new Stopwatch();
				sw.Start();
				integrator.Update(entity);
				sw.Stop();
				Console.Write("sec = " + sw.ElapsedMilliseconds / 1000.0 + " ");
				Console.WriteLine("millisec = " + sw.ElapsedMilliseconds);
				Console.WriteLine(j++ + "/" + tsAutoTechService.Count);
			}
		}
		public void ExportTsAutoOwnerInfo(int i, List<Tuple<string, Guid>> entityList) {
			var entityName = entityList[i].Item1;
			var entityId = entityList[i].Item2;
			var entites = GetTsAutoOwnerInfo();
			int j = 1;
			foreach(var entity in entites) {
				Console.WriteLine(entity.GetColumnValue("Id"));
				var integrator = new ClientServiceIntegrator(SystemUserConnection);

				var sw = new Stopwatch();
				sw.Start();
				integrator.Update(entity);
				sw.Stop();
				Console.Write("sec = " + sw.ElapsedMilliseconds / 1000.0 + " ");
				Console.WriteLine("millisec = " + sw.ElapsedMilliseconds);
				Console.WriteLine(j++ + "/" + entites.Count);
			}
		}
		public void ExportTsAccountNotification(int i, List<Tuple<string, Guid>> entityList) {
			var entityName = entityList[i].Item1;
			var entityId = entityList[i].Item2;
			var entites = GetTsAccountNotification();
			int j = 1;
			foreach(var entity in entites) {
				Console.WriteLine(entity.GetColumnValue("Id"));
				var integrator = new ClientServiceIntegrator(SystemUserConnection);

				var sw = new Stopwatch();
				sw.Start();
				integrator.Update(entity);
				sw.Stop();
				Console.Write("sec = " + sw.ElapsedMilliseconds / 1000.0 + " ");
				Console.WriteLine("millisec = " + sw.ElapsedMilliseconds);
				Console.WriteLine(j++ + "/" + entites.Count);
			}
		}
		public void ExportSysAdminUnit(int i, List<Tuple<string, Guid>> entityList) {
			var entityName = entityList[i].Item1;
			var entityId = entityList[i].Item2;
			var entites = GetSysAdminUnit();
			int j = 1;
			foreach(var entity in entites) {
				Console.WriteLine(entity.GetColumnValue("Id") + " " + entity.GetColumnValue("Name"));
				var integrator = new ClientServiceIntegrator(SystemUserConnection);

				var sw = new Stopwatch();
				sw.Start();
				integrator.Update(entity);
				sw.Stop();
				Console.Write("sec = " + sw.ElapsedMilliseconds / 1000.0 + " ");
				Console.WriteLine("millisec = " + sw.ElapsedMilliseconds);
				Console.WriteLine(j++ + "/" + entites.Count);
			}
		}
		public void ExportRelationship(int i, List<Tuple<string, Guid>> entityList) {
			var entityName = entityList[i].Item1;
			var entityId = entityList[i].Item2;
			var entites = GetRelationship();
			int j = 1;
			foreach(var entity in entites) {
				Console.WriteLine(entity.GetColumnValue("Id"));
				var integrator = new ClientServiceIntegrator(SystemUserConnection);

				var sw = new Stopwatch();
				sw.Start();
				integrator.Update(entity);
				sw.Stop();
				Console.Write("sec = " + sw.ElapsedMilliseconds / 1000.0 + " ");
				Console.WriteLine("millisec = " + sw.ElapsedMilliseconds);
				Console.WriteLine(j++ + "/" + entites.Count);
			}
		}

		public void ExportTsAutoOwnerHistory(int i, List<Tuple<string, Guid>> entityList) {
			var entityName = entityList[i].Item1;
			var entityId = entityList[i].Item2;
			var tsAutoOwnerHistory = GetTsAutoOwnerHistory();
			int j = 1;
			foreach(var entity in tsAutoOwnerHistory) {
				Console.WriteLine(entity.GetColumnValue("Id"));
				var integrator = new ClientServiceIntegrator(SystemUserConnection);

				var sw = new Stopwatch();
				sw.Start();
				integrator.Update(entity);
				sw.Stop();
				Console.Write("sec = " + sw.ElapsedMilliseconds / 1000.0 + " ");
				Console.WriteLine("millisec = " + sw.ElapsedMilliseconds);
				Console.WriteLine(j++ + "/" + tsAutoOwnerHistory.Count);
			}
		}

		public void ExportEntity(string entityName) {
			var entities = GetEntities(entityName);
			int j = 1, count = entities.Count;
			ConsoleColorWrite(entityName + ": start integrate");
			foreach(var entity in entities) {
				Console.WriteLine(entity.GetColumnValue("Id"));
				var integrator = new ClientServiceIntegrator(SystemUserConnection);
				var sw = new Stopwatch();
				sw.Start();
				integrator.Update(entity);
				sw.Stop();
				Console.Write("sec = " + sw.ElapsedMilliseconds / 1000.0 + " ");
				Console.WriteLine("millisec = " + sw.ElapsedMilliseconds);
				Console.WriteLine(j++ + "/" + count);
			}
			ConsoleColorWrite(entityName + ": end integrate");
		}

		public void UpdateEntity(string entityName) {
			var entities = GetEntitiesForUpdate(entityName);
			int j = 1, count = entities.Count;
			ConsoleColorWrite(entityName + ": start integrate");
			foreach(var entity in entities) {
				Console.WriteLine(entity.GetColumnValue("Id"));
				var integrator = new ClientServiceIntegrator(SystemUserConnection);
				var sw = new Stopwatch();
				sw.Start();
				integrator.Update(entity);
				sw.Stop();
				Console.Write("sec = " + sw.ElapsedMilliseconds / 1000.0 + " ");
				Console.WriteLine("millisec = " + sw.ElapsedMilliseconds);
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

		public void ExecuteSql() {
			var select = new Select(SystemUserConnection)
				.Column(Func.Count("Reminding", "Id"))
					.Distinct()
				.From("Reminding")
					.LeftOuterJoin("SysAdminUnit")
						.On("SysAdminUnit", "ContactId").IsEqual("Reminding", "ContactId")
					.InnerJoin("Case")
						.On("Case", "Id").IsEqual("Reminding", "SubjectId")
				.Where("RemindTime").IsLessOrEqual(Column.Const((new DateTime()).ToUniversalTime()))
				.And("IsRead").IsEqual(Column.Parameter(0))
				.And(Column.SqlText("[SysAdminUnit].[Id]")).IsEqual(Column.Parameter(new Guid("117D32F9-8275-4534-8411-1C66115CE9CD"))) as Select;

		}
		#endregion

	}

	#endregion

}
