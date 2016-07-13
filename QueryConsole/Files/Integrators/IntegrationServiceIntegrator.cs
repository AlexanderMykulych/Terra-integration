using Newtonsoft.Json.Linq;
using QueryConsole.Files.BpmEntityHelper;
using QueryConsole.Files.Constants;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terrasoft.Core;
using Terrasoft.TsConfiguration;
using IntegrationInfo = QueryConsole.Files.Constants.CsConstant.IntegrationInfo;
using TIntegrationType = QueryConsole.Files.Constants.CsConstant.TIntegrationType;

namespace QueryConsole.Files.Integrators
{
	public class IntegrationServiceIntegrator
	{
		#region Properties: Private
		private UserConnection _userConnection;
		private List<string> ReadedNotificationIds = new List<string>();
		private IntegratorHelper _integratorHelper = new IntegratorHelper();
		private IntegrationEntityHelper _integrationEntityHelper;
		private string _basePostboxUrl = @"http://api.integration.stage2.laximo.ru/v2/entity";
		private string _baseClientServiceUrl = @"http://api.client-service.stage2.laximo.ru/v2/entity/AUTO3N";
		private int _postboxId = 1009;
		private int _notifyLimit = 10;
		private bool _isImportAllow = true;
		#endregion

		#region Properties: Public
		public UserConnection UserConnection
		{
			get { return _userConnection; }
		}
		public IntegrationEntityHelper IntegrationEntityHelper
		{
			get
			{
				return _integrationEntityHelper;
			}
		}
		#endregion

		#region Constructor: Public
		public IntegrationServiceIntegrator(UserConnection userConnection)
		{
			_userConnection = userConnection;
			_postboxId = Terrasoft.Core.Configuration.SysSettings.GetValue(UserConnection, CsConstant.SysSettingsCode.TerrasoftPostboxId, _postboxId);
			_basePostboxUrl = Terrasoft.Core.Configuration.SysSettings.GetValue(UserConnection, CsConstant.SysSettingsCode.IntegrationServiceBaseUrl, _basePostboxUrl);
			_baseClientServiceUrl = Terrasoft.Core.Configuration.SysSettings.GetValue(UserConnection, CsConstant.SysSettingsCode.ClientServiceBaseUrl, _basePostboxUrl);
			_notifyLimit = Terrasoft.Core.Configuration.SysSettings.GetValue(UserConnection, CsConstant.SysSettingsCode.NotificationLimit, _notifyLimit);
			_isImportAllow = Terrasoft.Core.Configuration.SysSettings.GetValue(UserConnection, CsConstant.SysSettingsCode.AllowImport, _isImportAllow);
			_integrationEntityHelper = new IntegrationEntityHelper();
		}
		#endregion

		#region Methods: Public
		/// <summary>
		/// Получает BusEventNotification, после чего вызывает OnBusEventNotificationsDataRecived
		/// </summary>
		/// <param name="withData"></param>
		public void GetBusEventNotification(bool withData = true)
		{
			var url = GenerateUrl(
				withData == true ? TIntegratorRequest.BusEventNotificationData : TIntegratorRequest.BusEventNotification,
				TRequstMethod.GET,
				"0",
				_notifyLimit.ToString(),
				CsConstant.DefaultBusEventFilters,
				CsConstant.DefaultBusEventSorts
			);

			PushRequestWrapper(TRequstMethod.GET, url, "", (x, y, requestId) =>
			{
				var responceObj = x.DeserializeJson();
				var busEventNotifications = (JArray)responceObj["data"];
				OnBusEventNotificationsDataRecived(busEventNotifications, y);
			});
		}

		/// <summary>
		/// Всем нотификейшенам в ReadedNotificationIds ставит статус "Прочитано"
		/// </summary>
		public void SetNotifyRead()
		{
			var url = GenerateUrl(
				TIntegratorRequest.BusEventNotification,
				TRequstMethod.PUT
			);

			var json = ReadedNotificationIds.Select(x => new
			{
				isRead = true,
				id = x
			}).SerializeToJson();

			PushRequestWrapper(TRequstMethod.PUT, url, json, null);
			ReadedNotificationIds.Clear();
		}

		/// <summary>
		/// Сохраняет нотификейшен, чтобы потом скопом поставить признак прочитано
		/// </summary>
		/// <param name="notifyId"></param>
		public void AddReadId(string notifyId)
		{
			ReadedNotificationIds.Add(notifyId);
		}

		/// <summary>
		/// Делает запрос в clientservice и если версия объекта в нем больше за версию в integrationservice, то обновляет объектом из clientservice
		/// </summary>
		/// <param name="integrationInfo"></param>
		public void CreatedOnEntityExist(IntegrationInfo integrationInfo)
		{
			string jName = integrationInfo.EntityName;
			var data = integrationInfo.Data[jName];
			int version = data.Value<int>("version");
			int jId = data.Value<int>("id");
			string url = string.Format("{0}/{1}/{2}", _baseClientServiceUrl, jName, jId);

			PushRequestWrapper(TRequstMethod.GET, url, "", (x, y, requestId) =>
			{
				var responceObj = JObject.Parse(x);
				var csData = responceObj[jName] as JObject;
				var csVersion = csData.Value<int>("version");
				if (csVersion > version)
				{
					integrationInfo.Data = responceObj;
					integrationInfo.EntityName = jName;
					integrationInfo.Action = CsConstant.IntegrationActionName.Update;
					_integrationEntityHelper.IntegrateEntity(integrationInfo);
				}
			});
		}

		/// <summary>
		/// Срабатывает на получение нотификейшенов из integrationservice
		/// </summary>
		/// <param name="busEventNotifications"></param>
		/// <param name="userConnection"></param>
		public void OnBusEventNotificationsDataRecived(JArray busEventNotifications, UserConnection userConnection)
		{
			foreach (JObject busEventNotify in busEventNotifications)
			{
				var busEvent = busEventNotify[CsConstant.IntegrationEventName.BusEventNotify] as JObject;
				if (busEvent != null)
				{
					var data = busEvent["data"] as JObject;
					var objectType = busEvent["objectType"].ToString();
					var action = busEvent["action"].ToString();
					var notifyId = busEvent["id"].ToString();
					if (!string.IsNullOrEmpty(objectType) && data != null)
					{
						var integrationInfo = new IntegrationInfo(data, userConnection, TIntegrationType.Import, null, objectType, action, null);
						_integrationEntityHelper.IntegrateEntity(integrationInfo);
						if (integrationInfo.Result != null && integrationInfo.Result.Exception == CsConstant.IntegrationResult.TResultException.OnCreateEntityExist)
						{
							CreatedOnEntityExist(integrationInfo);
						}
					}
					AddReadId(notifyId);
				}
			}
			SetNotifyRead();
		}

		/// <summary>
		/// Генерирует Url в integrationservice
		/// </summary>
		/// <param name="integratorRequestType">Тип возращаемой сущности</param>
		/// <param name="requstMethod">Тип запроса</param>
		/// <param name="skip">Сколько данныех пропустить от начала</param>
		/// <param name="limit">Сколько данных взять</param>
		/// <param name="filters">Фильтры</param>
		/// <param name="sorts">Сортировки</param>
		/// <returns></returns>
		public string GenerateUrl(TIntegratorRequest integratorRequestType, TRequstMethod requstMethod, string skip = null, string limit = null, Dictionary<string, string> filters = null, Dictionary<string, string> sorts = null)
		{
			string result = _basePostboxUrl;
			string filtersStr = "";
			string sortStr = "";
			string skipStr = "";
			string limitStr = "";
			#region Requst Method
			switch (requstMethod)
			{
				case TRequstMethod.GET:
				case TRequstMethod.PUT:

					break;
				default:
					throw new NotImplementedException();
			}
			#endregion

			#region Integration Request
			switch (integratorRequestType)
			{
				case TIntegratorRequest.BusEventNotification:
					result += GenerateRouteToRequest("Postbox", _postboxId, "BusEventNotification");
					break;
				case TIntegratorRequest.BusEventNotificationData:
					result += GenerateRouteToRequest("Postbox", _postboxId, "BusEventNotificationData");
					break;
				case TIntegratorRequest.Postbox:
					result += GenerateRouteToRequest("Postbox");
					break;
			}
			#endregion

			#region skip
			if (!string.IsNullOrEmpty(skip))
				skipStr = string.Format("skip={0}", skip);
			#endregion

			#region limit
			if (!string.IsNullOrEmpty(limit))
				limitStr = string.Format("limit={0}", limit);
			#endregion

			#region filters
			if (filters != null && filters.Any())
			{
				foreach (var filter in filters)
				{
					filtersStr += string.Format("filter[{0}]={1}&", filter.Key, filter.Value);
				}
				filtersStr = filtersStr.Remove(filtersStr.Length - 1);
			}
			#endregion

			#region sort
			if (sorts != null && sorts.Any())
			{
				foreach (var sort in sorts)
				{
					sortStr += string.Format("sort[{0}]={1}&", sort.Key, sort.Value);
				}
				sortStr = sortStr.Remove(sortStr.Length - 1);
			}
			#endregion

			#region Param
			string paramStr = GenerateParamRoRequest(skipStr, limitStr, filtersStr, sortStr);
			if (!string.IsNullOrEmpty(paramStr))
			{
				result += string.Format("?{0}", paramStr);
			}
			#endregion
			return result;
		}
		#endregion

		#region Methods: Private
		private string GenerateRouteToRequest(params object[] routes)
		{
			return "/" + routes.Aggregate((cur, next) => cur.ToString() + "/" + next.ToString()).ToString();
		}
		private string GenerateParamRoRequest(params string[] param)
		{
			var collection = param.Where(x => !string.IsNullOrEmpty(x));
			return collection.Any() ? collection.Aggregate((cur, next) => cur + "&" + next) : "";
		}
		private void PushRequestWrapper(TRequstMethod requestMethod, string url, string jsonText, Action<string, UserConnection, Guid?> callback)
		{
			if (_isImportAllow)
			{
				_integratorHelper.PushRequest(requestMethod, url, jsonText, callback, UserConnection);
			}
		}
		#endregion

		#region Enum: Public
		public enum TIntegratorRequest
		{
			BusEventNotificationData,
			BusEventNotification,
			Postbox
		}
		#endregion
	}
}
