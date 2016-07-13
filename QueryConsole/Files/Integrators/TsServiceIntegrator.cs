using Newtonsoft.Json.Linq;
using QueryConsole.Files.BpmEntityHelper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terrasoft.Core;
using Terrasoft.Core.Entities;
using Terrasoft.TsConfiguration;
using CsConstant = QueryConsole.Files.Constants.CsConstant;
using IntegrationInfo = QueryConsole.Files.Constants.CsConstant.IntegrationInfo;

namespace QueryConsole.Files
{
	#region Interface: IServiceIntegrator
	public interface IServiceIntegrator {
		void GetRequest(ServiceRequestInfo info);
		void UpdateRequest(ServiceRequestInfo info);
		void InsertRequest(ServiceRequestInfo info);
		void IntegrateBpmEntity(Entity entity);
	}
	#endregion

	public enum TServiceObject {
		Entity,
		Dict
	}

	public class ServiceRequestInfo {
		public TServiceObject Type;
		public TRequstMethod Method;
		public string FullUrl;
		public string ServiceObjectName;
		public string ServiceObjectId;
		public string Filters;
		public string RequestJson;
		public string ResponseData;
		public Guid LogId;
		public Entity Entity;
		public string Limit;
		public string Skip;
		public static ServiceRequestInfo CreateForUpdateInService(Entity entity, string serviceName, string jsonData) {
			return new ServiceRequestInfo() {
				ServiceObjectId = entity.GetTypedColumnValue<string>(CsConstant.ServiceColumnInBpm.Identifier),
				ServiceObjectName = CsConstant.GetServiceEntityTypeByBmpEntity(entity, serviceName),
				Type = TServiceObject.Entity,
				RequestJson = jsonData,
				Entity = entity
			};
		}

		public static ServiceRequestInfo CreateForExportInBpm(string serviceObjectName, TServiceObject type = TServiceObject.Entity) {
			return new ServiceRequestInfo() {
				ServiceObjectName = serviceObjectName,
				Type = type
			};
		}

	}

	public abstract class BaseServiceIntegrator: IServiceIntegrator {
		public Dictionary<TServiceObject, string> baseUrls;
		public IntegratorHelper integratorHelper;
		public UserConnection userConnection;
		public ServiceUrlMaker UrlMaker;
		public IntegrationEntityHelper entityHelper;
		public string ServiceName;

		public BaseServiceIntegrator(UserConnection userConnection) {
			this.userConnection = userConnection;
			entityHelper = new IntegrationEntityHelper();
		}

		public virtual void GetRequest(ServiceRequestInfo info)
		{
			info.Method = TRequstMethod.GET;
			info.FullUrl = UrlMaker.Make(info);
			MakeRequest(info);
		}

		public virtual void UpdateRequest(ServiceRequestInfo info)
		{
			info.Method = TRequstMethod.PUT;
			info.FullUrl = UrlMaker.Make(info);
			MakeRequest(info);
		}

		public virtual void InsertRequest(ServiceRequestInfo info)
		{
			info.Method = TRequstMethod.POST;;
			info.FullUrl = UrlMaker.Make(info); ;
			MakeRequest(info);
		}

		public virtual void MakeRequest(ServiceRequestInfo info)
		{
			var logId = Guid.NewGuid();
			IntegrationLogger.StartTransaction(logId, new LogTransactionInfo() {
				RequesterName = CsConstant.PersonName.Bpm,
				ResiverName = ServiceName,
				UserConnection = userConnection
			});
			info.LogId = logId;
			integratorHelper.PushRequest(info.Method, info.FullUrl, info.RequestJson, (x, y, requestId) =>
			{
				info.ResponseData = x;
				OnGetResponse(info);
			}, userConnection, info.LogId);
		}

		public virtual void OnGetResponse(ServiceRequestInfo info)
		{
			Console.WriteLine("Catch Response");
			var responseJObj = JObject.Parse(info.ResponseData);
			switch(info.Method) {
				case TRequstMethod.GET:
					IEnumerable<JObject> resultObjects;
					if(string.IsNullOrEmpty(info.ServiceObjectId)) {
						var objArray = responseJObj["data"] as JArray;
						resultObjects = objArray.Select(x => x as JObject);
					} else {
						resultObjects = new List<JObject>() {
							responseJObj.First as JObject
						};
					}
					foreach(var jObj in resultObjects) {
						IntegrateServiceEntity(jObj, info.ServiceObjectName);
					}
				break;
				case TRequstMethod.POST:
				case TRequstMethod.PUT:
					var integrationInfo = IntegrationInfo.CreateForResponse(userConnection, info.Entity);
					integrationInfo.StrData = responseJObj.ToString();
					entityHelper.IntegrateEntity(integrationInfo);
				break;
			}
			
		}

		public virtual void IntegrateBpmEntity(Entity entity) {
			var integrationInfo = IntegrationInfo.CreateForExport(userConnection, entity);
			entityHelper.IntegrateEntity(integrationInfo);
			if (integrationInfo.Result.Type == CsConstant.IntegrationResult.TResultType.Success)
			{
				var json = integrationInfo.Result.Data.ToString();
				var requestInfo = ServiceRequestInfo.CreateForUpdateInService(entity, ServiceName, json);
				if(IntegrationEntityHelper.isEntityAlreadyIntegrated(entity)) {
					UpdateRequest(requestInfo);
				} else {
					InsertRequest(requestInfo);
				}
			}
		}

		public virtual void IntegrateServiceEntity(JObject serviceEntity, string serviceObjectName) {
			var integrationInfo = CsConstant.IntegrationInfo.CreateForImport(userConnection, CsConstant.IntegrationActionName.Create, serviceObjectName, serviceEntity);
			entityHelper.IntegrateEntity(integrationInfo);
			if (integrationInfo.Result.Type == CsConstant.IntegrationResult.TResultType.Exception)
			{
				if (integrationInfo.Result.Exception == CsConstant.IntegrationResult.TResultException.OnCreateEntityExist)
				{
					integrationInfo = CsConstant.IntegrationInfo.CreateForImport(userConnection, CsConstant.IntegrationActionName.Update, serviceObjectName, serviceEntity);
					entityHelper.IntegrateEntity(integrationInfo);
				}
			}
		}
	}
}
