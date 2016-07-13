using QueryConsole.Files.Constants;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terrasoft.Core;
using Terrasoft.Core.Entities;

namespace QueryConsole.Files.IntegratorTester
{
	public abstract class BaseIntegratorTester
	{
		public BaseServiceIntegrator Integrator;
		public List<string> BpmEntitiesName;
		public List<string> ServiceEntitiesName;
		public UserConnection UserConnection;
		public int Limit;
		public int Skip;

		public BaseIntegratorTester(UserConnection userConnection) {
			BpmEntitiesName = InitEntitiesName();
			ServiceEntitiesName = InitServiceEntitiesName();
			UserConnection = userConnection;
			Integrator = CreateIntegrator();
		}
		public void ImportAllBpmEntity() {
			foreach(var entityName in BpmEntitiesName) {
				ClonsoleGreen(entityName + " Start:");
				try {
					var collection = GetEntitiesBySchemaNames(entityName, false);
					foreach(var entity in collection) {
						ImportBpmEntity(entity);
					}
				} catch(Exception e) {
					ClonsoleGreen(" Error: " + e.Message);
				}
				ClonsoleGreen(entityName + " End:");
			}
		}
		public EntityCollection GetEntitiesBySchemaNames(string name, bool withoutIntegrated = true) {
			var esq = new EntitySchemaQuery(UserConnection.EntitySchemaManager, name);
			esq.AddAllSchemaColumns();
			if(withoutIntegrated) {
				esq.Filters.Add(esq.CreateFilterWithParameters(FilterComparisonType.Equal, CsConstant.ServiceColumnInBpm.Identifier, 0));
			}
			return esq.GetEntityCollection(UserConnection);
		}
		public void ExportServiceEntity(string name)
		{
			var serviceRequestInfo = ServiceRequestInfo.CreateForExportInBpm(name);
			serviceRequestInfo.Limit = Limit.ToString();
			serviceRequestInfo.Skip = Skip.ToString();
			Integrator.GetRequest(serviceRequestInfo);
		}
		public void ExportAllServiceEntities(int limit = 10, int skip = 0) {
			Limit = limit;
			Skip = skip;
			foreach(var name in ServiceEntitiesName) {
				ClonsoleGreen("Start: " + name);
				try {
					ExportServiceEntity(name);
				} catch(Exception e) {
					ClonsoleGreen("Error: " + e.Message);
				}
				ClonsoleGreen("End: " + name);
			}
		}

		public abstract BaseServiceIntegrator CreateIntegrator();
		public abstract List<string> InitEntitiesName();
		public abstract List<string> InitServiceEntitiesName();
		public abstract void ImportBpmEntity(Entity entity);
		public void ExportAllServiceEntitiesByStep(int stepCount = 10, int rightLimit = 1000) {
			Limit = stepCount;
			Skip = 0;
			foreach (var name in ServiceEntitiesName)
			{
				ClonsoleGreen("Start: " + name);
				try
				{
					for(Skip = 0; Skip - Limit < rightLimit; Skip += stepCount) {
						ExportServiceEntity(name);
					}
				}
				catch (Exception e)
				{
					ClonsoleGreen("Error: " + e.Message);
				}
				ClonsoleGreen("End: " + name);
			}
		}
		private void ClonsoleGreen(string text)
		{
			var buff = Console.ForegroundColor;
			Console.ForegroundColor = ConsoleColor.Green;
			Console.WriteLine(text);
			Console.ForegroundColor = buff;
		}

		private void Clonsole(string text)
		{
			Console.WriteLine(text);
		}
	}
}
