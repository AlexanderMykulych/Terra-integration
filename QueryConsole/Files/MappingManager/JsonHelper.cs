using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terrasoft.Common;
using Terrasoft.Core;
using Terrasoft.Core.Entities;
using Terrasoft.TsConfiguration;
using TIntegrationType = QueryConsole.Files.Constants.CsConstant.TIntegrationType;
using IntegrationInfo = QueryConsole.Files.Constants.CsConstant.IntegrationInfo;
using QueryConsole.Files.BpmEntityHelper;
using QueryConsole.Files.Constants;
using Terrasoft.Core.DB;
using System.Data;

namespace QueryConsole.Files.MappingManager
{
	public static class JsonEntityHelper
	{
		public static string RefName = @"#ref";
		public static object GetSimpleTypeValue(JToken jToken)
		{
			try
			{
				switch (jToken.Type)
				{
					case JTokenType.String:
						return jToken.Value<string>();
					case JTokenType.Integer:
						return jToken.Value<Int64>();
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

		public static object GetSimpleTypeValue(object value)
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

		public static List<object> GetColumnValues(UserConnection userConnection, string entityName, string entityPath, object entityPathValue, string resultColumnName, int limit = -1,
			string orderColumnName = "CreatedOn", OrderDirection orderType = OrderDirection.Descending)
		{
			var esq = new EntitySchemaQuery(userConnection.EntitySchemaManager, entityName);
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
			return esq.GetEntityCollection(userConnection).Select(x =>
				x.GetColumnValue(resColumn.IsLookup ? PrepareColumn(resColumn.Name, true) : resColumn.Name)
			).ToList();
		}

		public static string PrepareColumn(string columnName, bool withId = false)
		{
			var endWithId = columnName.EndsWith("Id");
			return withId ? (endWithId ? columnName : columnName + "Id") : (endWithId ? columnName.Substring(0, columnName.Length - 2) : columnName);
		}
		public static  bool IsAllNotNullAndEmpty(params object[] values)
		{
			foreach (var value in values)
			{
				if (value == null || (value is string && string.IsNullOrEmpty(value as string)))
					return false;
			}
			return true;
		}
		public static string GetFirstNotNull(params string[] strings)
		{
			return strings.FirstOrDefault(x => !string.IsNullOrEmpty(x));
		}
		public static List<JObject> GetCompositeJObjects(object colValue, string colName, string entityName, string handlerName, UserConnection userConnection, int maxCount = -1)
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
		public static Tuple<Dictionary<string, string>, Entity> GetEntityByExternalId(string schemaName, int externalId, UserConnection userConnection, bool addAllColumn, params string[] columns) {
			var esq = new EntitySchemaQuery(userConnection.EntitySchemaManager, schemaName);
			var columnDict = new Dictionary<string, string>();
			if(addAllColumn) {
				esq.AddAllSchemaColumns();
			} else {
				foreach(var column in columns) {
					var columnSchema = esq.AddColumn(column);
					columnDict.Add(column, columnSchema.Name);
				}
			}
			esq.Filters.Add(esq.CreateFilterWithParameters(FilterComparisonType.Equal, CsConstant.ServiceColumnInBpm.Identifier, externalId));
			var entity = esq.GetEntityCollection(userConnection).FirstOrDefault();
			return new Tuple<Dictionary<string,string>,Entity>(columnDict, entity);
		}
		public static bool isEntityExist(string schemaName, UserConnection userConnection, Dictionary<string, object> filters) {
			var esq = new EntitySchemaQuery(userConnection.EntitySchemaManager, schemaName);
			var schema = userConnection.EntitySchemaManager.GetInstanceByName(schemaName);
			esq.AddColumn(esq.CreateAggregationFunction(AggregationTypeStrict.Count, schema.PrimaryColumn.Name));
			foreach(var filter in filters) {
				esq.Filters.Add(esq.CreateFilterWithParameters(FilterComparisonType.Equal, filter.Key, filter.Value));
			}
			var select = esq.GetSelectQuery(userConnection);
			using(DBExecutor dbExecutor = userConnection.EnsureDBConnection()) {
				using (IDataReader reader = select.ExecuteReader(dbExecutor))
				{
					while (reader.Read())
					{
						return reader.GetColumnValue<int>("Count") > 0;
					}
				}
			}
			return false;
		}
	}
}
