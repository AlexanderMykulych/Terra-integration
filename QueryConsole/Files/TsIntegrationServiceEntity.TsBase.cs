using System;
using System.Collections.Generic;
using System.Linq;
using Terrasoft.Core;
using Terrasoft.Core.Entities;
using Newtonsoft.Json.Linq;
using IntegrationInfo = QueryConsole.Files.Constants.CsConstant.IntegrationInfo;
using QueryConsole.Files.Constants;
using QueryConsole.Files.MappingManager;
using Terrasoft.Core.DB;
using TSysAdminUnitType = QueryConsole.Files.Constants.CsConstant.TSysAdminUnitType;
using System.Data;
using Terrasoft.Common;

namespace Terrasoft.TsConfiguration
{
	public abstract class EntityHandler: IIntegrationEntityHandler {
		public MappingHelper Mapper;
		public string EntityName;
		public string JName;
		public virtual string HandlerName { get {
			return EntityName;
		}}
		public virtual string ExternalIdPath
		{
			get
			{
				return CsConstant.ServiceColumnInBpm.Identifier;
			}
		}
		public virtual string ExternalVersionPath
		{
			get
			{
				return CsConstant.ServiceColumnInBpm.Version;
			}
		}

		public virtual void Create(IntegrationInfo integrationInfo) {
			integrationInfo.TsExternalIdPath = ExternalIdPath;
			integrationInfo.TsExternalVersionPath = ExternalVersionPath;
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
				AfterEntitySave(integrationInfo);
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

		public virtual void AfterEntitySave(IntegrationInfo integrationInfo) {
		}

		public virtual void Update(IntegrationInfo integrationInfo)
		{
			integrationInfo.TsExternalIdPath = ExternalIdPath;
			integrationInfo.TsExternalVersionPath = ExternalVersionPath;
			var entity = GetEntityByExternalId(integrationInfo.UserConnection, integrationInfo.Data[JName].Value<int>("id"), integrationInfo.TsExternalIdPath);
			if(entity != null) {
				integrationInfo.IntegratedEntity = entity;
				BeforeMapping(integrationInfo);
				Mapper.StartMappByConfig(integrationInfo, JName, GetMapConfig(integrationInfo.UserConnection));
				AfterMapping(integrationInfo);
				Mapper.SaveEntity(entity);
				AfterEntitySave(integrationInfo);
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

		public virtual bool IsEntityAlreadyExist(IntegrationInfo integrationInfo)
		{
			integrationInfo.TsExternalIdPath = ExternalIdPath;
			integrationInfo.TsExternalVersionPath = ExternalVersionPath;
			Mapper.UserConnection = integrationInfo.UserConnection;
			return Mapper.CheckIsExist(EntityName, integrationInfo.Data[JName].Value<int>("id"), integrationInfo.TsExternalIdPath);
		}

		public virtual void ProcessResponse(IntegrationInfo integrationInfo)
		{
			integrationInfo.TsExternalIdPath = ExternalIdPath;
			integrationInfo.TsExternalVersionPath = ExternalVersionPath;
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
			integrationInfo.TsExternalIdPath = ExternalIdPath;
			integrationInfo.TsExternalVersionPath = ExternalVersionPath;
			BeforeMapping(integrationInfo);
			Mapper.StartMappByConfig(integrationInfo, JName, GetMapConfig(integrationInfo.UserConnection));
			AfterMapping(integrationInfo);
			return integrationInfo.Data;
		}

		public virtual List<MappingItem> GetMapConfig(UserConnection userConnection) {
			return IntegrationConfigurationManager.GetConfigItem(userConnection, HandlerName);
		}

		protected Entity GetEntityByExternalId(UserConnection userConnection, int extId, string externalIdPath = "TsExternalId")
		{
			var esq = new EntitySchemaQuery(userConnection.EntitySchemaManager, EntityName);
			esq.AddAllSchemaColumns();
			esq.RowCount = 1;
			esq.Filters.Add(esq.CreateFilterWithParameters(FilterComparisonType.Equal, externalIdPath, extId));
			return esq.GetEntityCollection(userConnection).FirstOrDefault();
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

	[ImportHandlerAttribute("VehicleRelationship")]
	[ExportHandlerAttribute("TsAutoOwnerInfo")]
	[ExportHandlerAttribute("TsAutoOwnerHistory")]
	[ExportHandlerAttribute("TsAutoTechService")]
	public class TsAutoOwnerInfoHandler : EntityHandler {
		public TsAutoOwnerInfoHandler() {
			Mapper = new MappingHelper();
			EntityName = "";
			JName = "VehicleRelationship";
		}

		public override bool IsEntityAlreadyExist(IntegrationInfo integrationInfo)
		{
			var typeId = integrationInfo.Data[JName]["type"]["#ref"]["id"].Value<int>();
			EntityName = GetEntityNameByTypeId(typeId);
			Mapper.UserConnection = integrationInfo.UserConnection;
			return Mapper.CheckIsExist(EntityName, integrationInfo.Data[JName].Value<int>("id"));
		}

		public string GetEntityNameByTypeId(int typeId) {
			switch(typeId) {
				case 1:
				case 2:
					return "TsAutoOwnerInfo";
				case 3:
				case 4:
					return "TsAutoOwnerHistory";
				case 5:
				case 6:
					return "TsAutoTechService";
				default:
					return "TsAutoTechService";
			}
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

	[ImportHandlerAttribute("Manager")]
	[ImportHandlerAttribute("ManagerGroup")]
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
			if(string.IsNullOrEmpty(JName)) {
				var typeIndex = integrationInfo.IntegratedEntity.GetTypedColumnValue<int>("SysAdminUnitTypeValue");
				if(typeIndex < 4) {
					JName = "ManagerGroup";
				} else {
					JName = "Manager";
				}
			}
			if(integrationInfo.IntegrationType == CsConstant.TIntegrationType.Import) {
				if(JName == "Manager") {
					integrationInfo.IntegratedEntity.SetColumnValue("SysAdminUnitTypeValue", TSysAdminUnitType.User);
				} else {
					integrationInfo.IntegratedEntity.SetColumnValue("SysAdminUnitTypeValue", TSysAdminUnitType.Unit);
				}
			}
		}

		public override bool IsEntityAlreadyExist(IntegrationInfo integrationInfo)
		{
			JName = integrationInfo.EntityName;
			Mapper.UserConnection = integrationInfo.UserConnection;
			return Mapper.CheckIsExist("SysAdminUnit", integrationInfo.Data[JName].Value<int>("id"));
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
	public class ContactCareerHandler : EntityHandler
	{
		public ContactCareerHandler()
		{
			Mapper = new MappingHelper();
			EntityName = "ContactCareer";
			JName = "Employee";
		}
	}

	[ImportHandlerAttribute("Market")]
	[ExportHandlerAttribute("TsLocSalMarket")]
	public class TsLocSalMarketHandler : EntityHandler
	{
		public bool TypeIsLp;
		public static readonly Guid TypeLp = new Guid("f11e685a-060d-43cc-a221-26246317257d");
		public TsLocSalMarketHandler()
		{
			Mapper = new MappingHelper();
			EntityName = "TsLocSalMarket";
			JName = "Market";
		}
		public override List<MappingItem> GetMapConfig(UserConnection userConnection)
		{
			if (TypeIsLp)
			{
				return IntegrationConfigurationManager.GetConfigItem(userConnection, HandlerName, "lp");
			}
			return IntegrationConfigurationManager.GetConfigItem(userConnection, HandlerName, "gp");
		}

		public override void BeforeMapping(IntegrationInfo integrationInfo)
		{
			if(integrationInfo.IntegratedEntity.GetTypedColumnValue<Guid>("TsMarketTypeId") == TypeLp) {
				TypeIsLp = true;
			}
		}
	}

	[ImportHandlerAttribute("Payment")]
	[ExportHandlerAttribute("TsPayment")]
	public class PaymentHandler : EntityHandler
	{
		public PaymentHandler()
		{
			Mapper = new MappingHelper();
			EntityName = "TsPayment";
			JName = "Payment";
		}
	}

	[ImportHandlerAttribute("Order")]
	[ExportHandlerAttribute("Order")]
	public class OrderHandler : EntityHandler
	{
		public OrderHandler()
		{
			Mapper = new MappingHelper();
			EntityName = "Order";
			JName = "Order";
		}

		public override void AfterEntitySave(IntegrationInfo integrationInfo)
		{
			if(integrationInfo.IntegrationType == CsConstant.TIntegrationType.Import) {
				try {
					if(integrationInfo.Data["Order"]["shipmentInfo"].HasValues) {
						var userConnection = integrationInfo.UserConnection;
						var id = integrationInfo.IntegratedEntity.GetTypedColumnValue<Guid>("Id");
						var country = integrationInfo.Data["Order"]["shipmentInfo"]["ShipmentInfo"]["country"].Value<string>();
						var region = integrationInfo.Data["Order"]["shipmentInfo"]["ShipmentInfo"]["region"].Value<string>();
						var place = integrationInfo.Data["Order"]["shipmentInfo"]["ShipmentInfo"]["place"].Value<string>();
						var district = integrationInfo.Data["Order"]["shipmentInfo"]["ShipmentInfo"]["district"].Value<string>();
						var street = integrationInfo.Data["Order"]["shipmentInfo"]["ShipmentInfo"]["street"].Value<string>();
						var building = integrationInfo.Data["Order"]["shipmentInfo"]["ShipmentInfo"]["building"].Value<string>();
						var appartament = integrationInfo.Data["Order"]["shipmentInfo"]["ShipmentInfo"]["appartament"].Value<string>();
						var zipCode = integrationInfo.Data["Order"]["shipmentInfo"]["ShipmentInfo"]["zipCode"].Value<string>();
						var address = integrationInfo.Data["Order"]["shipmentInfo"]["ShipmentInfo"]["address"].Value<string>();
						address = string.Format("{0}, {1}, {2}, {3}", street, building, appartament, address);
						ImportAddress(id, integrationInfo.UserConnection,
								GetGuidByValue("Country", country, userConnection),
								GetGuidByValue("Region", region, userConnection),
								GetGuidByValue("City", place, userConnection),
								GetGuidByValue("TsCounty", district, userConnection),
								IfNullThanEmpty(address),
								IfNullThanEmpty(zipCode),
								CsConstant.TsAddressType.Delivery);
					}
				} catch(Exception e) {
                   
                }

                try
                {
                    var sum = GetOrderItemSum(integrationInfo.IntegratedEntity.GetTypedColumnValue<Guid>("Id"), integrationInfo.UserConnection);
                    integrationInfo.IntegratedEntity.SetColumnValue("Amount", sum);
                    integrationInfo.IntegratedEntity.Save(false);
                    Console.WriteLine(sum);
                } catch(Exception e)
                {
                    Console.WriteLine(e.ToString());
                }

                try
                {
                    if (integrationInfo.IntegratedEntity.GetTypedColumnValue<Guid>("TsContractId") != Guid.Empty) {
                        var account = GetAccountByContract(integrationInfo.IntegratedEntity.GetTypedColumnValue<Guid>("TsContractId"), integrationInfo.UserConnection);
                        if (account != Guid.Empty)
                        {
                            integrationInfo.IntegratedEntity.SetColumnValue("AccountId", account);
                            integrationInfo.IntegratedEntity.Save(false);
                        }
                    }
                } catch(Exception e)
                {
                    Console.WriteLine(e.ToString());
                }

            }
		}

		public string IfNullThanEmpty(string text) {
			return string.IsNullOrEmpty(text) ? string.Empty : text;
		}
		public static Guid GetGuidByValue(string schemaName, string value, UserConnection userConnetion, string columnValue = "Name", string primaryColumn = "Id") {
			if(string.IsNullOrEmpty(value)) {
				return Guid.Empty;
			}
			var select = new Select(userConnetion)
						.Column(primaryColumn).As("Id")
						.From(schemaName)
						.Where(columnValue).IsEqual(Column.Parameter(value)) as Select;
			using (DBExecutor dbExecutor = select.UserConnection.EnsureDBConnection())
			{
				using (IDataReader reader = select.ExecuteReader(dbExecutor))
				{
					while (reader.Read())
					{
						return DBUtilities.GetColumnValue<Guid>(reader, "Id");
					}
				}
			}
			return Guid.Empty;
		}

		public void ImportAddress(Guid orderId, UserConnection userConnection, Guid country, Guid region, Guid city, Guid tsCountry, string address, string zipCode, Guid addressType) {
			var orderAddressId = GetOrderAddres(orderId, userConnection);
			if(orderAddressId == Guid.Empty) {
				InsertOrderAddress(orderId, userConnection, country, tsCountry, city, addressType, region, zipCode, address);
			} else {
				UpdateOrderAddress(orderId, userConnection, country, tsCountry, city, addressType, region, zipCode, address);
			}
		}

		public Guid GetOrderAddres(Guid orderId, UserConnection userConnetion) {
			var select = new Select(userConnetion)
						.Column("Id")
						.From("TsOrderAddress").As("src")
						.Where("TsOrderId").IsEqual(Column.Parameter(orderId)) as Select;
			using (DBExecutor dbExecutor = select.UserConnection.EnsureDBConnection())
			{
				using (IDataReader reader = select.ExecuteReader(dbExecutor))
				{
					while (reader.Read())
					{
						return DBUtilities.GetColumnValue<Guid>(reader, "Id");
					}
				}
			}
			return Guid.Empty;
		}

		public Guid InsertOrderAddress(Guid orderId, UserConnection userConnection, Guid countryId, Guid tsCountryId, Guid cityId, Guid addressTypeId, Guid regionId, string zip, string address)
		{
			var addressId = Guid.NewGuid();
			var columns = new Dictionary<string, Guid>() {
				{ "TsOrderId", orderId },
				{ "TsCountyId", tsCountryId },
				{ "CityId", cityId },
				{ "RegionId", regionId },
				{ "CountryId", countryId },
				{ "AddressTypeId", addressTypeId }
			};
			var insert = new Insert(userConnection)
						.Into("TsOrderAddress")
						.Set("Id", Column.Parameter(addressId))
						.Set("Primary", Column.Parameter(true))
						.Set("Zip", Column.Parameter(zip))
						.Set("Address", Column.Parameter(address)) as Insert;
			foreach(var column in columns) {
				if(column.Value != Guid.Empty) {
					insert.Set(column.Key, Column.Parameter(column.Value));
				}
			}
			insert.Execute();
			return addressId;
		}
		public void UpdateOrderAddress(Guid orderId, UserConnection userConnection, Guid countryId, Guid tsCountryId, Guid cityId, Guid addressTypeId, Guid regionId, string zip, string address)
		{
			var columns = new Dictionary<string, Guid>() {
				{ "TsOrderId", orderId },
				{ "TsCountyId", tsCountryId },
				{ "CityId", cityId },
				{ "RegionId", regionId },
				{ "CountryId", countryId },
				{ "AddressTypeId", addressTypeId }
			};
			var update = new Update(userConnection, "TsOrderAddress")
						.Set("Primary", Column.Parameter(true))
						.Set("Zip", Column.Parameter(zip))
						.Set("Address", Column.Parameter(address))
						.Where("TsOrderId").IsEqual(Column.Parameter(orderId)) as Update;
			foreach (var column in columns)
			{
				if (column.Value != Guid.Empty)
				{
					update.Set(column.Key, Column.Parameter(column.Value));
				}
			}
			update.Execute();
		}

        public Guid GetAccountFromContract(Guid id, UserConnection userConnection)
        {
            var select = new Select(userConnection)
                                .Column("AccountId").As("Id")
                                .From("Contract").As("c")
                                .Where("Id").IsEqual(Column.Parameter(id)) as Select;
            using (DBExecutor dbExecutor = select.UserConnection.EnsureDBConnection())
            {
                using (IDataReader reader = select.ExecuteReader(dbExecutor))
                {
                    while (reader.Read())
                    {
                        return DBUtilities.GetColumnValue<Guid>(reader, "Id");
                    }
                }
            }
            return Guid.Empty;
        }

        public double GetOrderItemSum(Guid orderId, UserConnection userConnection)
        {
            var select = new Select(userConnection)
                                .Column(Func.Sum("TotalAmount")).As("amount")
                                .From("OrderProduct")
                                .Where("OrderId").IsEqual(Column.Parameter(orderId)) as Select;
            using (DBExecutor dbExecutor = select.UserConnection.EnsureDBConnection())
            {
                using (IDataReader reader = select.ExecuteReader(dbExecutor))
                {
                    while (reader.Read())
                    {
                        return DBUtilities.GetColumnValue<double>(reader, "amount");
                    }
                }
            }
            return 0;
        }

        public Guid GetAccountByContract(Guid id, UserConnection userConnection)
        {
            var select = new Select(userConnection)
                            .Column("AccountId")
                            .From("Contract")
                            .Where("Id").IsEqual(Column.Parameter(id)) as Select;

            using (DBExecutor dbExecutor = select.UserConnection.EnsureDBConnection())
            {
                using (IDataReader reader = select.ExecuteReader(dbExecutor))
                {
                    while (reader.Read())
                    {
                        return DBUtilities.GetColumnValue<Guid>(reader, "AccountId");
                    }
                }
            }
            return Guid.Empty;
        }
	}

	[ImportHandlerAttribute("OrderItem")]
	[ExportHandlerAttribute("OrderProduct")]
	public class OrderProductHandler : EntityHandler
	{
		public OrderProductHandler()
		{
			Mapper = new MappingHelper();
			EntityName = "OrderProduct";
			JName = "OrderItem";
		}
		public override void AfterEntitySave(IntegrationInfo integrationInfo)
		{
			if(integrationInfo.IntegrationType == CsConstant.TIntegrationType.Import) {
				var id =		integrationInfo.Data["contextInfo.OrderItemContextInfo.id"];
				var catalog =	integrationInfo.Data["contextInfo.OrderItemContextInfo.catalog"];
				var catalogV =	integrationInfo.Data["contextInfo.OrderItemContextInfo.catalogVehicleId"];
				var ssd =		integrationInfo.Data["contextInfo.OrderItemContextInfo.ssd"];
				var vih = 		integrationInfo.Data["contextInfo.OrderItemContextInfo.vin"];
				var frame =		integrationInfo.Data["contextInfo.OrderItemContextInfo.frame"];
				var catCate =	integrationInfo.Data["contextInfo.OrderItemContextInfo.catalogCategoryId"];
				var unit = 		integrationInfo.Data["contextInfo.OrderItemContextInfo.unitId"];
				var clientData = integrationInfo.Data["contextInfo.OrderItemContextInfo.clientData"];
				var detailCode = integrationInfo.Data["contextInfo.OrderItemContextInfo.detailCode"];
				if(id != null) {
					var auto = JsonEntityHelper.GetEntityByExternalId("TsAutomobile", int.Parse(catalogV.ToString()), integrationInfo.UserConnection, false, "Id");
					if(JsonEntityHelper.isEntityExist("TsOrderAddInfo", integrationInfo.UserConnection, new Dictionary<string,object>() {
						{ "TsExternalId", id }
					})) {
						var update = new Update(integrationInfo.UserConnection, "TsOrderAddInfo")
									.Set("TsCatalog", Column.Parameter(catalog))
									.Set("TsAutomobileId", Column.Parameter(auto != null ? auto.Item2.GetTypedColumnValue<Guid>(auto.Item1["Id"]) : Guid.Empty))
									.Set("TsSSD", Column.Parameter(ssd))
									.Set("TsVIN", Column.Parameter(vih))
									.Set("TsFrame", Column.Parameter(frame))
									.Set("TsCategory", Column.Parameter(catCate))
									.Set("TsQuantity", Column.Parameter(unit))
									.Set("TsClientInfo", Column.Parameter(clientData))
									.Set("TsCode", Column.Parameter(detailCode))
									.Where("TsExternalId").IsEqual(Column.Parameter(id));
						update.Execute();
					} else {
						var insert = new Insert(integrationInfo.UserConnection)
										.Into("TsOrderAddInfo")
										.Set("TsExternalId", Column.Parameter(id))
										.Set("TsCatalog", Column.Parameter(catalog))
										.Set("TsAutomobileId", Column.Parameter(auto != null ? auto.Item2.GetTypedColumnValue<Guid>(auto.Item1["Id"]) : Guid.Empty))
										.Set("TsSSD", Column.Parameter(ssd))
										.Set("TsVIN", Column.Parameter(vih))
										.Set("TsFrame", Column.Parameter(frame))
										.Set("TsCategory", Column.Parameter(catCate))
										.Set("TsQuantity", Column.Parameter(unit))
										.Set("TsClientInfo", Column.Parameter(clientData))
										.Set("TsCode", Column.Parameter(detailCode));
						insert.Execute();
					}
				}
			}
		}

		public override void Create(IntegrationInfo integrationInfo)
		{
			base.Create(integrationInfo);

			CreateProduct(integrationInfo);
		}


		public override void Update(IntegrationInfo integrationInfo)
		{
			base.Update(integrationInfo);
			CreateProduct(integrationInfo);
		}
		public void CreateProduct(IntegrationInfo integrationInfo) {
            var entity = integrationInfo.IntegratedEntity;
            var articul = integrationInfo.Data["OrderItem"]["oem"].Value<string>();
			integrationInfo.IntegratedEntity = GetProductByArticuleOrCreateNew(integrationInfo.UserConnection, articul);
			integrationInfo.IntegratedEntity.SetDefColumnValues();
			Mapper.StartMappByConfig(integrationInfo, JName, IntegrationConfigurationManager.GetConfigItem(integrationInfo.UserConnection, "Product"));
			try
			{
				Mapper.SaveEntity(integrationInfo.IntegratedEntity);
                var productId = integrationInfo.IntegratedEntity.GetTypedColumnValue<Guid>("Id");
                entity.SetColumnValue("ProductId", productId);
                entity.Save(false);
            }
			catch (Exception e)
			{
			}
		}

		public Entity GetProductByArticuleOrCreateNew(UserConnection userConnection, string articule) {
			var productId = GetProductIdByArticule(userConnection, articule);
			if(productId == Guid.Empty) {
				var schema = userConnection.EntitySchemaManager.GetInstanceByName("Product");
				var entity = schema.CreateEntity(userConnection);
				entity.SetDefColumnValues();
				return entity;
			} else {
				var esq = new EntitySchemaQuery(userConnection.EntitySchemaManager, "Product");
				esq.AddAllSchemaColumns();
				return esq.GetEntity(userConnection, productId);
			}
		}

		public Guid GetProductIdByArticule(UserConnection userConnection, string articule) {
			var select = new Select(userConnection)
						.Column("Id")
						.From("Product")
						.Where("Code").IsEqual(Column.Parameter(articule)) as Select;
			using (DBExecutor dbExecutor = select.UserConnection.EnsureDBConnection())
			{
				using (IDataReader reader = select.ExecuteReader(dbExecutor))
				{
					while (reader.Read())
					{
						return DBUtilities.GetColumnValue<Guid>(reader, "Id");
					}
				}
			}
			return Guid.Empty;
		}
	}

	[ImportHandlerAttribute("Return")]
	[ExportHandlerAttribute("TsReturn")]
	public class TsReturnHandler : EntityHandler
	{
		public TsReturnHandler()
		{
			Mapper = new MappingHelper();
			EntityName = "TsReturn";
			JName = "Return";
		}
	}

	[ImportHandlerAttribute("ReturnItem")]
	[ExportHandlerAttribute("TsReturnPosition")]
	public class TsReturnPositionHandler : EntityHandler
	{
		public TsReturnPositionHandler()
		{
			Mapper = new MappingHelper();
			EntityName = "TsReturnPosition";
			JName = "ReturnItem";
		}
	}

	[ImportHandlerAttribute("Shipment")]
	[ExportHandlerAttribute("TsShipment")]
	public class TsShipmentHandler : EntityHandler
	{
		public TsShipmentHandler()
		{
			Mapper = new MappingHelper();
			EntityName = "TsShipment";
			JName = "Shipment";
		}
	}

	[ImportHandlerAttribute("ShipmentItem")]
	[ExportHandlerAttribute("TsShipmentPosition")]
	public class TsShipmentPositionHandler : EntityHandler
	{
		public TsShipmentPositionHandler()
		{
			Mapper = new MappingHelper();
			EntityName = "TsShipmentPosition";
			JName = "ShipmentItem";
		}

		public override void Create(IntegrationInfo integrationInfo)
		{
			base.Create(integrationInfo);

			UpdateProduct(integrationInfo);
		}


		public override void Update(IntegrationInfo integrationInfo)
		{
			base.Update(integrationInfo);
			UpdateProduct(integrationInfo);
		}

		public void UpdateProduct(IntegrationInfo integrationInfo) {
			var eom = integrationInfo.Data["ShipmentItem"]["eom"].Value<string>();
			var productId = GetProductIdByArticule(integrationInfo.UserConnection, eom);
			if(productId != Guid.Empty) {
				var unitName = integrationInfo.Data["ShipmentItem"]["unitName"].Value<string>();
				updateProductUnitName(integrationInfo.UserConnection, productId, unitName);
			}
		}

		public Guid GetProductIdByArticule(UserConnection userConnection, string articule)
		{
			var select = new Select(userConnection)
						.Column("Id")
						.From("Product")
						.Where("Code").IsEqual(Column.Parameter(articule)) as Select;
			using (DBExecutor dbExecutor = select.UserConnection.EnsureDBConnection())
			{
				using (IDataReader reader = select.ExecuteReader(dbExecutor))
				{
					while (reader.Read())
					{
						return DBUtilities.GetColumnValue<Guid>(reader, "Id");
					}
				}
			}
			return Guid.Empty;
		}

		public void updateProductUnitName(UserConnection userConnection, Guid productId, string unitName) {
			var unitId = OrderHandler.GetGuidByValue("Unit", unitName, userConnection);
			var update = new Update(userConnection, "Product")
						.Set("", Column.Parameter(unitId))
						.Where("Id").IsEqual(Column.Parameter(productId)) as Update;
			update.Execute();
		}
	}

	[ImportHandlerAttribute("ContractBalance")]
	[ExportHandlerAttribute("Contract")]
	public class ContractBalanceHandler : EntityHandler
	{
		public ContractBalanceHandler()
		{
			Mapper = new MappingHelper();
			EntityName = "Contract";
			JName = "ContractBalance";
		}
		public override void BeforeMapping(IntegrationInfo integrationInfo)
		{
			integrationInfo.Data["id"] = integrationInfo.Data["contract.#ref.id"];
		}
	}

	[ImportHandlerAttribute("Contract")]
	[ExportHandlerAttribute("Contract")]
	public class ContractHandler : EntityHandler
	{
		public ContractHandler()
		{
			Mapper = new MappingHelper();
			EntityName = "Contract";
			JName = "Contract";
		}
		public override void BeforeMapping(IntegrationInfo integrationInfo)
		{
			integrationInfo.Data["id"] = integrationInfo.Data["contract.#ref.id"];
		}
	}

	[ImportHandlerAttribute("Debt")]
	[ExportHandlerAttribute("TsContractDebt")]
	public class TsContractDebtHandler : EntityHandler
	{
		public TsContractDebtHandler()
		{
			Mapper = new MappingHelper();
			EntityName = "TsContractDebt";
			JName = "Debt";
		}
	}

	[ImportHandlerAttribute("ManagerInfo")]
	[ExportHandlerAttribute("")]
	public class ManagerInfoHandler : EntityHandler
	{
		public override string HandlerName
		{
			get
			{
				return JName;
			}
		}
		public override string ExternalIdPath
		{
			get
			{
				return CsConstant.ServiceColumnInBpm.IdentifierOrder;
			}
		}

		public override string ExternalVersionPath
		{
			get
			{
				return CsConstant.ServiceColumnInBpm.VersionOrder;
			}
		}

		public ManagerInfoHandler()
		{
			Mapper = new MappingHelper();
			EntityName = "Contact";
			JName = "ManagerInfo";
		}
	}
	[ImportHandlerAttribute("CounteragentContactInfo")]
	[ExportHandlerAttribute("")]
	public class CounteragentContactInfoHandler : EntityHandler
	{
		public override string HandlerName
		{
			get
			{
				return JName;
			}
		}
		public CounteragentContactInfoHandler()
		{
			Mapper = new MappingHelper();
			EntityName = "Contact";
			JName = "CounteragentContactInfo";
		}
	}

	[ImportHandlerAttribute("Counteragent")]
	[ExportHandlerAttribute("")]
	public class CounteragentHandler : EntityHandler
	{
		public override string HandlerName
		{
			get
			{
				return JName;
			}
		}
		public override string ExternalIdPath
		{
			get
			{
				return CsConstant.ServiceColumnInBpm.IdentifierOrder;
			}
		}

		public override string ExternalVersionPath
		{
			get
			{
				return CsConstant.ServiceColumnInBpm.VersionOrder;
			}
		}
		public CounteragentHandler()
		{
			Mapper = new MappingHelper();
			EntityName = "Account";
			JName = "Counteragent";
		}
	}
}