using Newtonsoft.Json.Linq;
using QueryConsole.Files.BpmEntityHelper;
using QueryConsole.Files.Constants;
using QueryConsole.Files.MappingManager;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terrasoft.Common;
using Terrasoft.Core;
using Terrasoft.Core.Configuration;
using Terrasoft.Core.DB;
using Terrasoft.Core.Entities;
using IntegrationInfo = QueryConsole.Files.Constants.CsConstant.IntegrationInfo;
using TIntegrationType = QueryConsole.Files.Constants.CsConstant.TIntegrationType;


namespace Terrasoft.TsConfiguration
{
	public class MappingHelper
	{

		#region Fields: Public
		public string RefName = @"#ref";
		public bool _isInsertToDB;
		public List<MappingItem> MapConfig;
		public UserConnection UserConnection;
		public Queue<Action> MethodQueue;
		public RulesFactory RulesFactory;
		#endregion

		#region Properties: Public
		public bool IsInsertToDB
		{
			get
			{
				try
				{
					_isInsertToDB = Terrasoft.Core.Configuration.SysSettings.GetValue(UserConnection, CsConstant.SysSettingsCode.IsInsertToDB, _isInsertToDB);
				}
				catch (Exception e)
				{
					//IntegrationLogger.Error("Method [IsInsertToDB get] exception: Message = {0}", e.Message);
					_isInsertToDB = false;
				}
				return _isInsertToDB;
			}
		}
		#endregion

		#region Constructor: Public
		public MappingHelper()
		{
			_isInsertToDB = false;
			MethodQueue = new Queue<Action>();
			RulesFactory = new RulesFactory();
		}
		#endregion

		#region Methods: Public
		public void StartMappByConfig(IntegrationInfo integrationInfo, string jName, List<MappingItem> mapConfig)
		{
			try
			{
				switch (integrationInfo.IntegrationType)
				{
					case TIntegrationType.Import:
						{
							StartMappImportByConfig(integrationInfo, jName, mapConfig);
							break;
						}
					case TIntegrationType.Export:
						{
							StartMappExportByConfig(integrationInfo, jName, mapConfig);
							break;
						}
					case TIntegrationType.ExportResponseProcess:
						{
							StartMappExportResponseProcessByConfig(integrationInfo, jName, mapConfig);
							break;
						}
				}
			}
			catch (Exception e)
			{
				//IntegrationLogger.Error("Method [StartMappByConfig] catch exception Message = {0} jName = {1}", e.Message, jName);
				throw;
			}
		}

		public void StartMappImportByConfig(IntegrationInfo integrationInfo, string jName, List<MappingItem> mapConfig)
		{
			if (integrationInfo.IntegratedEntity == null)
				throw new Exception(string.Format("Integration Entity not exist {0} ({1})", jName));
			var entityJObj = integrationInfo.Data[jName];
			foreach (var item in mapConfig)
			{
				if (item.MapIntegrationType == TIntegrationType.All || item.MapIntegrationType == TIntegrationType.Import)
				{
					try
					{
						var subJObj = GetJTokenByPath(entityJObj, item.JSourcePath);
						MapColumn(item, ref subJObj, integrationInfo);
					}
					catch (Exception e)
					{
						if (CsConstant.IntegrationFlagSetting.AllowErrorOnColumnAssign)
						{
							throw;
						}
					}
				}
			}
		}

		public void StartMappExportByConfig(IntegrationInfo integrationInfo, string jName, List<MappingItem> mapConfig)
		{
			integrationInfo.Data = new JObject();
			if (integrationInfo.Data[jName] == null)
				integrationInfo.Data[jName] = new JObject();
			foreach (var item in mapConfig)
			{
				if (item.MapIntegrationType == TIntegrationType.All || item.MapIntegrationType == TIntegrationType.Export)
				{
					var jObjItem = (new JObject()) as JToken;
					try
					{
						MapColumn(item, ref jObjItem, integrationInfo);
					}
					catch (Exception e)
					{
						if (!item.IgnoreError)
						{
							throw;
						}
						jObjItem = null;
					}
					if (integrationInfo.Data[jName][item.JSourcePath] != null && integrationInfo.Data[jName][item.JSourcePath].HasValues)
					{
						if (integrationInfo.Data[jName][item.JSourcePath] is JArray)
							((JArray)integrationInfo.Data[jName][item.JSourcePath]).Add(jObjItem);
					}
					else
					{
						var resultJ = GetJTokenByPath(integrationInfo.Data[jName], item.JSourcePath);
						if (jObjItem == null && item.EFieldRequier)
							throw new ArgumentNullException("Field " + item.JSourcePath + " required!");
						if(jObjItem == null && !item.SerializeIfNull) {
							resultJ.Parent.Remove();
							return;
						}
						if (jObjItem != null && jObjItem.ToString() == "0" && !item.SerializeIfZero)
						{
							resultJ.Parent.Remove();
							return;
						}
						resultJ.Replace(jObjItem);
					}
				}
			}
		}

		public void StartMappExportResponseProcessByConfig(IntegrationInfo integrationInfo, string jName, List<MappingItem> mapConfig)
		{
			var entityJObj = integrationInfo.Data[jName];
			foreach (var item in mapConfig)
			{
				try
				{
					if (item.SaveOnResponse)
					{
						var subJObj = GetJTokenByPath(entityJObj, item.JSourcePath);
						MapColumn(item, ref subJObj, integrationInfo);
					}
				}
				catch (Exception e)
				{
					if (CsConstant.IntegrationFlagSetting.AllowErrorOnColumnAssign)
					{
						throw;
					}
				}
			}
		}

		public void MapColumn(MappingItem mapItem, ref JToken jToken, IntegrationInfo integrationInfo)
		{
			if (UserConnection == null)
				UserConnection = integrationInfo.UserConnection;
			var entity = integrationInfo.IntegratedEntity;
			var integrationType = integrationInfo.IntegrationType;
			Action executedMethod = new Action(() => { });
			var rule = RulesFactory.GetRule(mapItem.MapType.ToString());
			if(rule != null) {
				RuleInfo ruleInfo = null;
				switch(integrationInfo.IntegrationType) {
					case TIntegrationType.ExportResponseProcess:
					case TIntegrationType.Import:
						ruleInfo = new RuleImportInfo() {
							config = mapItem,
							entity = integrationInfo.IntegratedEntity,
							json = jToken,
							userConnection = UserConnection,
							integrationType = integrationInfo.IntegrationType,
							action = integrationInfo.Action
						};
						executedMethod = () => rule.Import((RuleImportInfo)ruleInfo);
						if (mapItem.MapExecuteType == TMapExecuteType.BeforeEntitySave)
						{
							executedMethod();
						} else {
							MethodQueue.Enqueue(executedMethod);
						}
						break;
					case TIntegrationType.Export:
						ruleInfo = new RuleExportInfo()
						{
							config = mapItem,
							entity = integrationInfo.IntegratedEntity,
							json = jToken,
							userConnection = UserConnection,
							integrationType = integrationInfo.IntegrationType,
							action = integrationInfo.Action
						};
						rule.Export((RuleExportInfo)ruleInfo);
						jToken = ruleInfo.json;
						break;
				}
			}
		}

		public void ArrayOfReference(MappingItem mappingItem, IntegrationInfo integrationInfo, ref JToken jToken)
		{
			try
			{
				switch (integrationInfo.IntegrationType)
				{
					case TIntegrationType.Import:
					case TIntegrationType.ExportResponseProcess:
						{
							//Guid? resultGuid = null;
							//if(jToken != null && jToken.HasValues) {
							//	var refColumns = jToken[RefName];
							//	var externalId = int.Parse(refColumns["id"].ToString());
							//	var type = refColumns["type"];
							//	resultGuid = GetGuidByExternalId(mappingItem.TsSourceName, externalId);
							//}
							//integrationInfo.IntegratedEntity.SetColumnValue(mappingItem.TsSourcePath, resultGuid);
							break;
						}
					case TIntegrationType.Export:
						{
							if (IsAllNotNullAndEmpty(integrationInfo.IntegratedEntity, mappingItem.TsDestinationName, mappingItem.TsSourcePath, mappingItem.JSourceName))
							{
								var srcValue = integrationInfo.IntegratedEntity.GetTypedColumnValue<Guid>(mappingItem.TsSourcePath);
								var jArray = new JArray();
								var resultList = GetColumnValues(mappingItem.TsDestinationName, mappingItem.TsDestinationPath, srcValue, mappingItem.TsDestinationResPath);
								foreach (var resultItem in resultList)
								{
									var extId = int.Parse(resultItem.ToString());
									if (extId != 0)
									{
										jArray.Add(JToken.FromObject(CsReference.Create(extId, mappingItem.JSourceName)));
									}
								}
								jToken = jArray;
							}
							else
							{
								jToken = null;
							}
							break;
						}
				}
			}
			catch (Exception e)
			{
				throw;
			}
		}
		public void Const(MappingItem mappingItem, IntegrationInfo integrationInfo, ref JToken jToken)
		{
			try
			{
				switch (integrationInfo.IntegrationType)
				{
					case TIntegrationType.Import:
					case TIntegrationType.ExportResponseProcess:
						{
							//Guid? resultGuid = null;
							//if(jToken != null && jToken.HasValues) {
							//	var refColumns = jToken[RefName];
							//	var externalId = int.Parse(refColumns["id"].ToString());
							//	var type = refColumns["type"];
							//	resultGuid = GetGuidByExternalId(mappingItem.TsSourceName, externalId);
							//}
							//integrationInfo.IntegratedEntity.SetColumnValue(mappingItem.TsSourcePath, resultGuid);
							break;
						}
					case TIntegrationType.Export:
						{
							object resultValue = null;
							switch (mappingItem.ConstType)
							{
								case TConstType.String:
									resultValue = mappingItem.ConstValue;
									break;
								case TConstType.Bool:
									resultValue = Convert.ToBoolean(mappingItem.ConstValue.ToString());
									break;
								case TConstType.Int:
									resultValue = int.Parse(mappingItem.ConstValue.ToString());
									break;
								case TConstType.Null:
									resultValue = null;
									break;
								case TConstType.EmptyArray:
									resultValue = new ArrayList();
									break;
							}
							jToken = resultValue != null ? JToken.FromObject(resultValue) : null;
							break;
						}
				}
			}
			catch (Exception e)
			{
				throw;
			}
		}

		public void RefToGuid(MappingItem mappingItem, IntegrationInfo integrationInfo, ref JToken jToken)
		{
			switch (integrationInfo.IntegrationType)
			{
				case TIntegrationType.Import:
				case TIntegrationType.ExportResponseProcess:
					{
						Guid? resultGuid = null;
						if (jToken != null && jToken.HasValues)
						{
							var refColumns = jToken[RefName];
							var externalId = int.Parse(refColumns["id"].ToString());
							var type = refColumns["type"];
							resultGuid = GetColumnValues(mappingItem.TsDestinationName, "TsExternalId", externalId, "Id").FirstOrDefault() as Guid?;
						}
						integrationInfo.IntegratedEntity.SetColumnValue(mappingItem.TsSourcePath, resultGuid);
						break;
					}
				case TIntegrationType.Export:
					{
						object resultObj = null;
						if (IsAllNotNullAndEmpty(integrationInfo.IntegratedEntity, mappingItem.TsDestinationName, mappingItem.TsSourcePath, mappingItem.JSourceName, mappingItem.TsDestinationPath))
						{
							var resultValue = GetColumnValues(mappingItem.TsDestinationName, mappingItem.TsDestinationPath, integrationInfo.IntegratedEntity.GetTypedColumnValue<Guid>(mappingItem.TsSourcePath), mappingItem.TsExternalIdPath).FirstOrDefault(x => (int)x > 0);
							if (resultValue != null)
							{
								var resultRef = CsReference.Create(int.Parse(resultValue.ToString()), mappingItem.JSourceName);
								resultObj = resultRef != null ? JToken.FromObject(resultRef) : null;
							}
						}
						jToken = resultObj as JToken;
						break;
					}
			}
		}

		public void Simple(MappingItem mappingItem, IntegrationInfo integrationInfo, ref JToken jToken)
		{
			switch (integrationInfo.IntegrationType)
			{
				case TIntegrationType.Import:
				case TIntegrationType.ExportResponseProcess:
					{
						object value = GetSimpleTypeValue(jToken);
						integrationInfo.IntegratedEntity.SetColumnValue(mappingItem.TsSourcePath, value);
						break;
					}
				case TIntegrationType.Export:
					{
						var value = integrationInfo.IntegratedEntity.GetColumnValue(mappingItem.TsSourcePath);
						var simpleResult = value != null ? GetSimpleTypeValue(value) : null;
						if (!string.IsNullOrEmpty(mappingItem.MacrosName))
						{
							simpleResult = TsMacrosHelper.GetMacrosResultImport(mappingItem.MacrosName, simpleResult);
						}
						jToken = simpleResult != null ? JToken.FromObject(simpleResult) : null;
						break;
					}
			}
		}

		public void FirstDestinationField(MappingItem mappingItem, IntegrationInfo integrationInfo, ref JToken jToken)
		{
			switch (integrationInfo.IntegrationType)
			{
				case TIntegrationType.Import:
				case TIntegrationType.ExportResponseProcess:
					{
						object resultId = null;
						if (jToken != null)
						{
							var newValue = GetSimpleTypeValue(jToken);
							resultId = GetColumnValues(mappingItem.TsDestinationName, mappingItem.TsDestinationResPath, newValue, mappingItem.TsDestinationPath, 1).FirstOrDefault();
						}
						integrationInfo.IntegratedEntity.SetColumnValue(mappingItem.TsSourcePath, resultId);
						break;
					}
				case TIntegrationType.Export:
					{
						object resultObject = null;
						if (IsAllNotNullAndEmpty(integrationInfo.IntegratedEntity, mappingItem.TsDestinationName, mappingItem.TsSourcePath, mappingItem.TsDestinationPath, mappingItem.TsDestinationResPath))
						{
							var sourceValue = integrationInfo.IntegratedEntity.GetColumnValue(mappingItem.TsSourcePath);
							resultObject = GetColumnValues(mappingItem.TsDestinationName, mappingItem.TsDestinationPath, sourceValue, mappingItem.TsDestinationResPath).FirstOrDefault();
						}
						jToken = resultObject != null ? JToken.FromObject(resultObject) : null;
						break;
					}
			}
		}

		public void CompositObject(MappingItem mappingItem, IntegrationInfo integrationInfo, ref JToken jToken)
		{
			try
			{	
				switch (integrationInfo.IntegrationType)
				{
					case TIntegrationType.Import:
					case TIntegrationType.ExportResponseProcess:
						{
							var integrator = new IntegrationEntityHelper();
							//var objIntegrInfo = new IntegrationInfo(jToken, integrationInfo.UserConnection, integrationInfo.IntegrationType, null, null, integrationInfo.Action);
							var jObject = jToken as JObject;
							var objIntegrInfo = new IntegrationInfo(jObject, integrationInfo.UserConnection, integrationInfo.IntegrationType, null, jObject.Properties().First().Name, integrationInfo.Action);
							integrator.IntegrateEntity(objIntegrInfo);
							break;
						}
					case TIntegrationType.Export:
						{
							object resultJObj = null;
							if (IsAllNotNullAndEmpty(integrationInfo.IntegratedEntity, mappingItem.TsSourcePath, mappingItem.TsDestinationPath, mappingItem.TsDestinationName, mappingItem.JSourceName))
							{
								var srcEntity = integrationInfo.IntegratedEntity;
								var dscId = srcEntity.GetColumnValue(mappingItem.TsSourcePath);
								string handlerName = GetFirstNotNull(mappingItem.HandlerName, mappingItem.TsDestinationName, mappingItem.JSourceName);
								resultJObj = GetCompositeJObjects(dscId, mappingItem.TsDestinationPath, mappingItem.TsDestinationName, handlerName, integrationInfo.UserConnection, 1).FirstOrDefault();
							}
							jToken = resultJObj as JToken;
							break;
						}
				}
			}
			catch (Exception e)
			{
				throw;
			}
		}

		public void ArrayOfCompositObject(MappingItem mappingItem, IntegrationInfo integrationInfo, ref JToken jToken)
		{
			try
			{
				switch (integrationInfo.IntegrationType)
				{
					case TIntegrationType.Import:
					case TIntegrationType.ExportResponseProcess:
						{
							if (jToken is JArray)
							{
								var jArray = (JArray)jToken;
								foreach (JToken jArrayItem in jArray)
								{
									JToken jObj = jArrayItem;
									CompositObject(mappingItem, integrationInfo, ref jObj);
								}
							}
							break;
						}
					case TIntegrationType.Export:
						{
							if (IsAllNotNullAndEmpty(integrationInfo.IntegratedEntity, mappingItem.TsSourcePath, mappingItem.TsDestinationPath, mappingItem.TsDestinationName))
							{
								var srcEntity = integrationInfo.IntegratedEntity;
								var dscValue = srcEntity.GetColumnValue(mappingItem.TsSourcePath);
								string handlerName = GetFirstNotNull(mappingItem.HandlerName, mappingItem.TsDestinationName, mappingItem.JSourceName);
								var resultJObjs = GetCompositeJObjects(dscValue, mappingItem.TsDestinationPath, mappingItem.TsDestinationName, handlerName, integrationInfo.UserConnection);
								var jArray = (jToken = new JArray()) as JArray;
								resultJObjs.ForEach(x => jArray.Add(x));
							}
							break;
						}
				}
			}
			catch (Exception e)
			{
				throw;
			}
		}

		public List<JObject> GetCompositeJObjects(object colValue, string colName, string entityName, string handlerName, UserConnection userConnection, int maxCount = -1)
		{
			try
			{
				var jObjectsList = new List<JObject>();
				var esq = new EntitySchemaQuery(userConnection.EntitySchemaManager, entityName);
				esq.AddAllSchemaColumns();
				if (maxCount > 0)
					esq.RowCount = maxCount;
				esq.Filters.Add(esq.CreateFilterWithParameters(FilterComparisonType.Equal, colName, colValue));
				var collection = esq.GetEntityCollection(userConnection);
				foreach (var item in collection)
				{
					try
					{
						var integrationInfo = new IntegrationInfo(new JObject(), userConnection, TIntegrationType.Export, null, handlerName, "", item);
						var handler = (new IntegrationEntityHelper()).GetIntegrationHandler(integrationInfo);
						if (handler != null)
						{
							jObjectsList.Add(handler.ToJson(integrationInfo));
						}
					}
					catch (Exception e)
					{
						//IntegrationLogger.Error("Method [] catch exception message = {0}", e.Message);
						throw;
					}
				}
				return jObjectsList;
			}
			catch (Exception e)
			{
				return new List<JObject>();
			}
		}

		public bool CheckIsExist(string entityName, int externalId, string externalIdPath = "TsExternalId")
		{
			var select = new Select(UserConnection)
							.Column(Func.Count(CsConstant.ServiceColumnInBpm.Identifier)).As("Count")
							.From(entityName)
							.Where(externalIdPath).IsEqual(Column.Parameter(externalId)) as Select;
			using(DBExecutor dbExecutor = UserConnection.EnsureDBConnection()) {
				using (IDataReader reader = select.ExecuteReader(dbExecutor))
				{
					while (reader.Read())
					{
						return DBUtilities.GetColumnValue<int>(reader, "Count") > 0;
					}
				}
			}
			return false;
		}

		public void SaveEntity(Entity entity)
		{
			try
			{
				UserConnection = entity.UserConnection;
				bool result = false;
				if (IsInsertToDB)
				{
					switch (entity.StoringState)
					{
						case StoringObjectState.New:
							result = entity.InsertToDB(false, false);
							break;
						case StoringObjectState.Changed:
							result = entity.UpdateInDB(false);
							break;
					}
				}
				else
				{
					result = entity.Save(false);
				}
				ExecuteMapMethodQueue();
				IntegrationLogger.SuccessSave(entity.GetType().ToString());
			}
			catch (Exception e)
			{
				IntegrationLogger.AfterSaveError(e, entity.GetType().ToString());
			}
		}

		public JObject GetJObject(string json)
		{
			return !string.IsNullOrEmpty(json) ? JObject.Parse(json) : null;
		}
		#endregion

		#region Methods: Private
		private List<object> GetColumnValues(string entityName, string entityPath, object entityPathValue, string resultColumnName, int limit = -1,
			string orderColumnName = "CreatedOn", Common.OrderDirection orderType = Common.OrderDirection.Descending)
		{
			var esq = new EntitySchemaQuery(UserConnection.EntitySchemaManager, entityName);
			if (limit > 0)
			{
				esq.RowCount = limit;
			}
			var resColumn = esq.AddColumn(resultColumnName);
			if (!string.IsNullOrEmpty(orderColumnName))
			{
				var orderColumn = esq.AddColumn(orderColumnName);
				orderColumn.SetForcedQueryColumnValueAlias("orderColumn");
				orderColumn.OrderDirection = orderType;
				orderColumn.OrderPosition = 0;
			}
			esq.Filters.Add(esq.CreateFilterWithParameters(FilterComparisonType.Equal, entityPath, entityPathValue));
			return esq.GetEntityCollection(UserConnection).Select(x =>
				x.GetColumnValue(resColumn.IsLookup ? PrepareColumn(resColumn.Name, true) : resColumn.Name)
			).ToList();
		}

		private string PrepareColumn(string columnName, bool withId = false)
		{
			var endWithId = columnName.EndsWith("Id");
			return withId ? (endWithId ? columnName : columnName + "Id") : (endWithId ? columnName.Substring(0, columnName.Length - 2) : columnName);
		}

		private object GetSimpleTypeValue(JToken jToken)
		{
			try
			{
				switch (jToken.Type)
				{
					case JTokenType.String:
						return jToken.Value<string>();
					case JTokenType.Integer:
						return jToken.Value<int>();
					case JTokenType.Float:
						return jToken.Value<float>();
					case JTokenType.Date:
						return jToken.Value<DateTime>();
					case JTokenType.TimeSpan:
						return jToken.Value<TimeSpan>();
					case JTokenType.Boolean:
						return jToken.Value<bool>();
					default:
						return null;
				}
			}
			catch (Exception e)
			{
				//IntegrationLogger.Error("Method [GetSimpleTypeValue] catch exception: Message = {0}", e.Message);
				throw;
			}
		}

		private object GetSimpleTypeValue(object value)
		{
			try
			{
				if (value is DateTime)
				{
					return ((DateTime)value).ToString("yyyy-MM-dd");
				}

				return value;
			}
			catch (Exception e)
			{
				//IntegrationLogger.Error("Method [GetSimpleTypeValue] catch exception: Message = {0}", e.Message);
				throw;
			}
		}

		private bool IsAllNotNullAndEmpty(params object[] values)
		{
			foreach (var value in values)
			{
				if (value == null || (value is string && string.IsNullOrEmpty(value as string)))
					return false;
			}
			return true;
		}

		private string GetFirstNotNull(params string[] strings)
		{
			return strings.FirstOrDefault(x => !string.IsNullOrEmpty(x));
		}

		private JToken GetJTokenByPath(JToken jToken, string path)
		{
			var pItems = path.Split('.');
			foreach (var pItem in pItems)
			{
				if (!jToken.HasValues || jToken[pItem] == null)
				{
					jToken[pItem] = new JObject();
				}
				jToken = jToken[pItem];
			}
			return jToken;
		}

		private void ExecuteMapMethodQueue()
		{
			while (MethodQueue.Any())
			{
				var method = MethodQueue.Dequeue();
				method();
			}
		}
		#endregion
	}

	#region Enum: TMapType
	public enum TMapType
	{
		RefToGuid = 0,
		Simple = 1,
		FirstDestinationField = 2,
		CompositObject = 3,
		ArrayOfCompositObject = 4,
		Const = 5,
		ArrayOfReference = 6,
		ManyToMany = 8
	}
	#endregion

	#region Enum: TMapExecuteType
	public enum TMapExecuteType
	{
		AfterEntitySave = 0,
		BeforeEntitySave = 1
	}
	#endregion
	#region Enum: TConstType
	public enum TConstType
	{
		String = 0,
		Bool = 1,
		Int = 2,
		Null = 3,
		EmptyArray = 4
	}
	#endregion
}
