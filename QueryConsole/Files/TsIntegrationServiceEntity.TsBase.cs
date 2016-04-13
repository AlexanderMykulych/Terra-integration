using System;
using System.Collections.Generic;
using System.Linq;
using Terrasoft.Core;
using Terrasoft.Core.Entities;
using Newtonsoft.Json.Linq;
using IntegrationInfo = Terrasoft.Configuration.CsConstant.IntegrationInfo;

namespace Terrasoft.Configuration
{
	public abstract class EntityHandler: IIntegrationEntityHandler {
		public MappingHelper Mapper;
		public string EntityName;
		public string JName;
		public virtual string HandlerName { get {
			return EntityName;
		}}

		public virtual void Create(IntegrationInfo integrationInfo) {
			var entitySchema = integrationInfo.UserConnection.EntitySchemaManager.GetInstanceByName(EntityName);
			integrationInfo.IntegratedEntity = entitySchema.CreateEntity(integrationInfo.UserConnection);
			integrationInfo.IntegratedEntity.SetDefColumnValues();
			BeforeMapping(integrationInfo);
			Mapper.StartMappByConfig(integrationInfo, JName, GetMapConfig(integrationInfo.UserConnection));
			AfterMapping(integrationInfo);
			try {
				Mapper.SaveEntity(integrationInfo.IntegratedEntity);
				integrationInfo.Result = new CsConstant.IntegrationResult() {
					Type = CsConstant.IntegrationResult.TResultType.Success
				};
			} catch(Exception e) {
				integrationInfo.Result = new CsConstant.IntegrationResult() {
					Type = CsConstant.IntegrationResult.TResultType.Exception,
					ExceptionMessage = e.Message
				};
			}
		}
		public virtual void BeforeMapping(IntegrationInfo integrationInfo) {
		}

		public virtual void AfterMapping(IntegrationInfo integrationInfo) {
		}

		public virtual void Update(IntegrationInfo integrationInfo) {
			var entity = GetEntityByExternalId(integrationInfo.UserConnection, integrationInfo.Data[JName].Value<int>("id"));
			if(entity != null) {
				BeforeMapping(integrationInfo);
				Mapper.StartMappByConfig(integrationInfo, JName, GetMapConfig(integrationInfo.UserConnection));
				AfterMapping(integrationInfo);
				Mapper.SaveEntity(entity);
			} else {
				throw new Exception(string.Format("Can not create entity {0}", EntityName));
			}
		}

		public virtual void Delete(IntegrationInfo integrationInfo) {
			//var entity = Mapper.GetEntityByExternalId(EntityName, integrationInfo.Data[JName].Value<int>("id"));
			//entity.SetColumnValue("TsDeleteInIntegrate", true);
			//Mapper.SaveEntity(entity);
		}

		public virtual void Unknown(IntegrationInfo integrationInfo) {
			Update(integrationInfo);
		}

		public virtual bool IsEntityAlreadyExist(IntegrationInfo integrationInfo) {
			Mapper.UserConnection = integrationInfo.UserConnection;
			return Mapper.CheckIsExist(EntityName, integrationInfo.Data[JName].Value<int>("id"));
		}

		public virtual void ProcessResponse(IntegrationInfo integrationInfo) {
			integrationInfo.Data = Mapper.GetJObject(integrationInfo.StrData);
			BeforeMapping(integrationInfo);
			Mapper.StartMappByConfig(integrationInfo, JName, GetMapConfig(integrationInfo.UserConnection));
			AfterMapping(integrationInfo);
			try {
				Mapper.SaveEntity(integrationInfo.IntegratedEntity);
				integrationInfo.Result = new CsConstant.IntegrationResult() {
					Type = CsConstant.IntegrationResult.TResultType.Success
				};
			} catch(Exception e) {
				integrationInfo.Result = new CsConstant.IntegrationResult() {
					Type = CsConstant.IntegrationResult.TResultType.Exception,
					ExceptionMessage = e.Message
				};
			}
		}

		public virtual JObject ToJson(IntegrationInfo integrationInfo) {
			BeforeMapping(integrationInfo);
			Mapper.StartMappByConfig(integrationInfo, JName, GetMapConfig(integrationInfo.UserConnection));
			AfterMapping(integrationInfo);
			return integrationInfo.Data;
		}

		public virtual List<MappingItem> GetMapConfig(UserConnection userConnection) {
			return IntegrationConfigurationManager.GetConfigItem(userConnection, HandlerName);
		}

		private Entity GetEntityByExternalId(UserConnection userConnection, int extId) {
			var esq = new EntitySchemaQuery(userConnection.EntitySchemaManager, EntityName);
			esq.RowCount = 1;
			esq.Filters.Add(esq.CreateFilterWithParameters(FilterComparisonType.Equal, "TsExternalId", extId));
			return esq.GetEntityCollection(userConnection).First();
		}
	}

	[ImportHandlerAttribute("CompanyProfile")]
	[ExportHandlerAttribute("Account")]
	public class AccountHandler : EntityHandler {
		public AccountHandler() {
			Mapper = new MappingHelper();
			EntityName = "Account";
			JName = "CompanyProfile";
		}
	}

	[ImportHandlerAttribute("PersonProfile")]
	[ExportHandlerAttribute("Contact")]
	public class ContactHandler : EntityHandler {
		public ContactHandler() {
			Mapper = new MappingHelper();
			EntityName = "Contact";
			JName = "PersonProfile";
		}
	}

	[ImportHandlerAttribute("")]
	[ExportHandlerAttribute("TsAutoTechService")]
	public class TsAutoTechServiceHandler : EntityHandler {
		public TsAutoTechServiceHandler() {
			Mapper = new MappingHelper();
			EntityName = "TsAutoTechService";
			JName = "VehicleRelationship";
		}
	}

	[ImportHandlerAttribute("")]
	[ExportHandlerAttribute("TsAutoOwnerHistory")]
	public class TsAutoOwnerHistoryHandler : EntityHandler {
		public TsAutoOwnerHistoryHandler() {
			Mapper = new MappingHelper();
			EntityName = "TsAutoOwnerHistory";
			JName = "VehicleRelationship";
		}
	}

	[ImportHandlerAttribute("")]
	[ExportHandlerAttribute("TsAutoOwnerInfo")]
	public class TsAutoOwnerInfoHandler : EntityHandler {
		public TsAutoOwnerInfoHandler() {
			Mapper = new MappingHelper();
			EntityName = "TsAutoOwnerInfo";
			JName = "VehicleRelationship";
		}
	}

	[ImportHandlerAttribute("Relationship")]
	[ExportHandlerAttribute("Relationship")]
	public class RelationshipHandler : EntityHandler {
		public RelationshipHandler() {
			Mapper = new MappingHelper();
			EntityName = "Relationship";
			JName = "Relationship";
		}
	}

	[ImportHandlerAttribute("NotificationProfile")]
	[ExportHandlerAttribute("TsAccountNotification")]
	public class TsAccountNotificationHandler : EntityHandler {
		public TsAccountNotificationHandler() {
			Mapper = new MappingHelper();
			EntityName = "TsAccountNotification";
			JName = "NotificationProfile";
		}
	}

	[ImportHandlerAttribute("NotificationProfile")]
	[ExportHandlerAttribute("TsContactNotifications")]
	public class TsContactNotificationsHandler : EntityHandler {
		public TsContactNotificationsHandler() {
			Mapper = new MappingHelper();
			EntityName = "TsContactNotifications";
			JName = "NotificationProfile";
		}
	}

	[ImportHandlerAttribute("")]
	[ExportHandlerAttribute("SysAdminUnit")]
	public class SysAdminUnitHandler : EntityHandler {
		public override string HandlerName {
			get {
				return JName;
			}
		}
		public SysAdminUnitHandler() {
			Mapper = new MappingHelper();
			EntityName = "SysAdminUnit";
			JName = "";
		}

		public override void BeforeMapping(IntegrationInfo integrationInfo) {
			base.BeforeMapping(integrationInfo);
			var typeIndex = integrationInfo.IntegratedEntity.GetTypedColumnValue<int>("SysAdminUnitTypeValue");
			if(typeIndex < 4) {
				JName = "ManagerGroup";
			} else {
				JName = "Manager";
			}
		}
	}

	[ImportHandlerAttribute("CompanyProfileAssignment")]
	[ExportHandlerAttribute("TsAccountManagerGroup")]
	public class TsAccountManagerGroupHandler : EntityHandler {
		public TsAccountManagerGroupHandler() {
			Mapper = new MappingHelper();
			EntityName = "TsAccountManagerGroup";
			JName = "CompanyProfileAssignment";
		}
	}

	[ImportHandlerAttribute("ClientRequest")]
	[ExportHandlerAttribute("Case")]
	public class CaseHandler : EntityHandler {
		public CaseHandler() {
			Mapper = new MappingHelper();
			EntityName = "Case";
			JName = "ClientRequest";
		}
	}

	[ImportHandlerAttribute("VehicleProfile")]
	[ExportHandlerAttribute("TsAutomobile")]
	public class TsAutomobileHandler : EntityHandler {
		public TsAutomobileHandler() {
			Mapper = new MappingHelper();
			EntityName = "TsAutomobile";
			JName = "VehicleProfile";
		}
	}

	[ImportHandlerAttribute("ContactInfo")]
	[ExportHandlerAttribute("ContactInfo")]
	public class ContactInfoHandler : EntityHandler {
		public ContactInfoHandler() {
			Mapper = new MappingHelper();
			EntityName = "Contact";
			JName = "ContactInfo";
		}

		public override string HandlerName {
			get {
				return JName;
			}
		}
	}

	[ImportHandlerAttribute("AddressInfo")]
	[ExportHandlerAttribute("ContactAddress")]
	public class AddressInfoHandler : EntityHandler {
		public AddressInfoHandler() {
			Mapper = new MappingHelper();
			EntityName = "ContactAddress";
			JName = "AddressInfo";
		}

		public override string HandlerName {
			get {
				return JName;
			}
		}
	}

	[ImportHandlerAttribute("ContactRecord")]
	[ExportHandlerAttribute("ContactCommunication")]
	public class ContactRecordHandler : EntityHandler {
		public ContactRecordHandler() {
			Mapper = new MappingHelper();
			EntityName = "ContactCommunication";
			JName = "ContactRecord";
		}

		public override string HandlerName {
			get {
				return JName;
			}
		}
	}

	[ImportHandlerAttribute("Employee")]
	[ExportHandlerAttribute("ContactCareer")]
	public class ContactCareerHandler : EntityHandler {
		public ContactCareerHandler() {
			Mapper = new MappingHelper();
			EntityName = "ContactCareer";
			JName = "Employee";
		}
	}
}