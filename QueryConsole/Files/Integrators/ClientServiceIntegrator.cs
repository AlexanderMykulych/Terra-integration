using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terrasoft.Core;
using Terrasoft.Core.Entities;
using IntegrationInfo = QueryConsole.Files.Constants.CsConstant.IntegrationInfo;
using TIntegrationType = QueryConsole.Files.Constants.CsConstant.TIntegrationType;
using IntegrationResult =  QueryConsole.Files.Constants.CsConstant.IntegrationResult;

using Terrasoft.TsConfiguration;
using QueryConsole.Files.Constants;
using QueryConsole.Files.BpmEntityHelper;
namespace QueryConsole.Files.Integrators
{
	public class ClientServiceIntegrator
	{
		#region Constructor: Public
		public ClientServiceIntegrator(UserConnection userConnection)
		{
			_userConnection = userConnection;
			_isIntegrationActive = Terrasoft.Core.Configuration.SysSettings.GetValue(UserConnection, CsConstant.SysSettingsCode.IsIntegrationActive, _isIntegrationActive);
		}
		#endregion

		#region Properties: Private
		private UserConnection _userConnection;
		private bool _isIntegrationActive = true;
		#endregion

		#region Properties: Public
		public UserConnection UserConnection
		{
			get { return _userConnection; }
		}
		#endregion

		#region Methods: Public
		/// <summary>
		/// Обновляет объект в clientservice. Если объект еще не создан в clientservice, то создает его.
		/// </summary>
		/// <param name="entity">Сущность которую обновляем</param>
		/// <returns></returns>
		public bool Update(Entity entity)
		{
			var logId = Guid.NewGuid();
			if (!_isIntegrationActive)
			{
				return true;
			}
			try
			{
				if (entity.IsColumnValueLoaded("TsExternalId") && entity.GetTypedColumnValue<int>("TsExternalId") == 0)
				{
					Insert(entity);
					return true;
				}
				IntegrationLogger.StartTransaction(logId, new LogTransactionInfo()
				{
					RequesterName = CsConstant.PersonName.Bpm,
					ResiverName = CsConstant.PersonName.ClientService,
					UserConnection = UserConnection
				});
				var integratorHelper = new IntegratorHelper();
				var method = TRequstMethod.PUT;
				
				integratorHelper.PushRequest(method, GetUrlByEntity(entity, method), GetEntityJson(entity), (x, y, requestId) =>
				{
					ProcessResponse(x, entity, method);
				}, UserConnection, logId);
				return true;
			}
			catch (Exception e)
			{
				IntegrationLogger.BeforeRequestError(logId, e);
				return true;
			}
		}

		/// <summary>
		/// Создает объект в clientservice. Если объект уже создан в clientservice, то обновляет его.
		/// </summary>
		/// <param name="entity">Сущность которую создаем</param>
		/// <returns></returns>
		public bool Insert(Entity entity)
		{
			var logId = Guid.NewGuid();
			if (!_isIntegrationActive)
			{
				return true;
			}
			try
			{
				var integratorHelper = new IntegratorHelper();
				var method = TRequstMethod.POST;
				if (entity.IsColumnValueLoaded("TsExternalId") && entity.GetTypedColumnValue<int>("TsExternalId") != 0)
				{
					Update(entity);
					return true;
				}
				IntegrationLogger.StartTransaction(logId, new LogTransactionInfo()
				{
					RequesterName = CsConstant.PersonName.Bpm,
					ResiverName = CsConstant.PersonName.ClientService,
					UserConnection = UserConnection
				});
				//IntegrationLogger.Info("Insert: Start. schemaName = {0} id = {1}", entity.SchemaName, entity.PrimaryColumnValue);
				integratorHelper.PushRequest(method, GetUrlByEntity(entity, method), GetEntityJson(entity), (x, y, requestId) =>
				{
					ProcessResponse(x, entity, method);
				});
				//IntegrationLogger.Info("Insert: Finish. schemaName = {0} id = {1}", entity.SchemaName, entity.PrimaryColumnValue);
				return true;
			}
			catch (Exception e)
			{
				IntegrationLogger.BeforeRequestError(logId, e);
				return true;
			}
		}

		/// <summary>
		/// Удаляет объкт в clientservice. Не должен использоватся.
		/// </summary>
		/// <param name="entity">Сущность которую удаляем</param>
		/// <returns></returns>
		public bool Delete(Entity entity)
		{
			var logId = Guid.NewGuid();
			if (!_isIntegrationActive)
			{
				return true;
			}
			try
			{
				var integratorHelper = new IntegratorHelper();
				var method = TRequstMethod.DELETE;
				if (entity.IsColumnValueLoaded("TsExternalId") && entity.GetTypedColumnValue<int>("TsExternalId") == 0)
				{
					return true;
				}

				IntegrationLogger.StartTransaction(logId, new LogTransactionInfo()
				{
					RequesterName = CsConstant.PersonName.Bpm,
					ResiverName = CsConstant.PersonName.ClientService,
					UserConnection = UserConnection
				});
				integratorHelper.PushRequest(method, GetUrlByEntity(entity, method), "", null, UserConnection, logId);
				return true;
			}
			catch (Exception e)
			{
				IntegrationLogger.BeforeRequestError(logId, e);
				return true;
			}
		}
		#endregion

		#region Methods: Private
		/// <summary>
		/// Формирует JSON из объекта
		/// </summary>
		/// <param name="entity">Сущность из которой формируем JSON</param>
		/// <returns></returns>
		private string GetEntityJson(Entity entity)
		{
			var integrationInfo = new IntegrationInfo(null, UserConnection, TIntegrationType.Export, entity.PrimaryColumnValue, entity.SchemaName, CsConstant.IntegrationActionName.Empty, entity);
			var integrationEntityHelper = new IntegrationEntityHelper();
			integrationEntityHelper.IntegrateEntity(integrationInfo);
			if (integrationInfo.Result.Type == IntegrationResult.TResultType.Success)
			{
				return integrationInfo.Result.Data.ToString();
			}
			return null;
		}

		/// <summary>
		/// Возвращает url по которому делаем запрос в clientservice
		/// </summary>
		/// <param name="entity"></param>
		/// <param name="method"></param>
		/// <returns></returns>
		private string GetUrlByEntity(Entity entity, TRequstMethod method)
		{
			string url, entityName, additionalParam = "";
			string entityType = entity.SchemaName;
			if (CsConstant.clientserviceEntity.ContainsKey(entityType))
			{
				url = CsConstant.clientserviceEntityUrl;
				entityName = CsConstant.clientserviceEntity[entityType];
				if (entity.SchemaName == "SysAdminUnit" && entity.GetTypedColumnValue<int>("SysAdminUnitTypeValue") < (int)CsConstant.TSysAdminUnitType.User)
					entityName = "ManagerGroup";
			}
			else if (CsConstant.clientserviceDict.ContainsKey(entityType))
			{
				url = CsConstant.clientserviceDictUrl;
				entityName = CsConstant.clientserviceDict[entityType];
			}
			else
			{
				throw new ArgumentException(string.Format("entity - '{0}' is unrecognize for export to client-service!", entity.GetType()));
			}

			if (method == TRequstMethod.PUT || method == TRequstMethod.DELETE)
				additionalParam = "/" + entity.GetTypedColumnValue<int>("TsExternalId").ToString();
			return url + "/" + entityName + additionalParam;
		}

		/// <summary>
		/// После операций Update, Insert обрабатывает ответ от clientservice
		/// </summary>
		/// <param name="responseJson"></param>
		/// <param name="entity"></param>
		/// <param name="method"></param>
		private void ProcessResponse(string responseJson, Entity entity, TRequstMethod method)
		{
			var integrationEntityHelper = new IntegrationEntityHelper();
			var integrationInfo = new IntegrationInfo(null, entity.UserConnection, TIntegrationType.ExportResponseProcess, entity.PrimaryColumnValue, entity.SchemaName, CsConstant.IntegrationActionName.UpdateFromResponse, entity);
			integrationInfo.StrData = responseJson;
			integrationEntityHelper.IntegrateEntity(integrationInfo);
		}
		#endregion
	}
}
