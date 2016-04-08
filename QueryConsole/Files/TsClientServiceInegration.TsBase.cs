using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using Terrasoft.Core;
using Terrasoft.Core.Entities;
using Newtonsoft.Json.Linq;
using System.Dynamic;
using System.Data;
using Terrasoft.Core.Configuration;
using System.Xml;
using System.Reflection;
using System.Threading;


namespace Terrasoft.Configuration
{
	using IntegrationInfo = CsConstant.IntegrationInfo;
	using TIntegrationType = CsConstant.TIntegrationType;
	using System.Configuration;
	using System.Collections;
	#region Enum: TRequstMethod
	public enum TRequstMethod {
		GET,
		POST,
		PUT,
		DELETE
	}
	#endregion

	#region Class: IntegratorHelper
	/// <summary>
	/// Клас-helper для отправки запроса и его приема
	/// </summary>
	public class IntegratorHelper {
		#region Methods: public
		/// <summary>
		/// Конструктор
		/// </summary>
		/// <param name="requestMethod">Get, Put, Post</param>
		/// <param name="url"></param>
		/// <param name="jsonText">Данные для отправки в формате json</param>
		/// <param name="callback">callback - для обработки ответа</param>
		/// <param name="userConnection"></param>
		public void PushRequest(TRequstMethod requestMethod, string url, string jsonText, Action<string, UserConnection> callback, UserConnection userConnection = null) {
			if(string.IsNullOrEmpty(url)) {
				return;
			}
			MakeAsyncRequest(requestMethod, url, jsonText, callback, userConnection);
		}
		#endregion
		#region Methods: Private
		/// <summary>
		/// Делает асинхронный запрос
		/// </summary>
		/// <param name="requestMethod"></param>
		/// <param name="url"></param>
		/// <param name="jsonText"></param>
		/// <param name="callback"></param>
		/// <param name="userConnection"></param>
		private static void MakeAsyncRequest(TRequstMethod requestMethod, string url, string jsonText, Action<string, UserConnection> callback, UserConnection userConnection = null) {
			try {
				var _request = WebRequest.Create(new Uri(url)) as HttpWebRequest;
				_request.Method = requestMethod.ToString();
				_request.ContentType = "application/json";
				_request.Headers.Add("authorization", "Basic YnBtb25saW5lMjoxMjM0NTY=");
				_request.Headers.Add("cache-control", "no-cache");
				switch(requestMethod) {
					case TRequstMethod.POST:
					case TRequstMethod.PUT:
					if(string.IsNullOrEmpty(jsonText))
						return;
					AddDataToRequest(_request, jsonText);
					break;
				}
				_request.BeginGetResponse(GetRequestResponse, new ResponceParams(_request, callback, userConnection));
			} catch(Exception e) {
				IntegrationUtilities.Error("Method [MakeAsyncRequest] catch exception: threadId = {0}\nMessage = {1}", Thread.CurrentThread.ManagedThreadId, e.Message);
			}
		}
		/// <summary>
		/// Добавляет данные к запросу
		/// </summary>
		/// <param name="request"></param>
		/// <param name="data"></param>
		private static void AddDataToRequest(HttpWebRequest request, string data) {
			if(string.IsNullOrEmpty(data))
				return;
			var encoding = new UTF8Encoding();
			data = data.Replace("ReferenceClientService", "#ref");
			var bytes = Encoding.UTF8.GetBytes(data);
			request.ContentLength = bytes.Length;

			using(var writeStream = request.GetRequestStream()) {
				writeStream.Write(bytes, 0, bytes.Length);
			}
		}
		/// <summary>
		/// Приймает ответ на запрос
		/// </summary>
		/// <param name="result"></param>
		private static void GetRequestResponse(IAsyncResult result) {
			var responceParams = (ResponceParams)result.AsyncState;
			var responce = responceParams.Request.EndGetResponse(result);
			using(Stream responseStream = responce.GetResponseStream())
			using(StreamReader sr = new StreamReader(responseStream)) {
				if(responceParams.Callback != null) {
					string responceText = sr.ReadToEnd();
					responceParams.Callback(responceText, responceParams.UserConnection);
				}
			}
		}
		private class ResponceParams {
			public ResponceParams(HttpWebRequest request, Action<string, UserConnection> callback, UserConnection userConnection) {
				Request = request;
				Callback = callback;
				UserConnection = userConnection;
			}
			public HttpWebRequest Request;
			public Action<string, UserConnection> Callback;
			public UserConnection UserConnection;
		}
		#endregion
	}
	#endregion

	#region Class: ClientServiceIntegrator
	public class ClientServiceIntegrator {
		#region Constructor: Public
		public ClientServiceIntegrator(UserConnection userConnection) {
			_userConnection = userConnection;
			_isIntegrationActive = SysSettings.GetValue(UserConnection, CsConstant.SysSettingsCode.IsIntegrationActive, _isIntegrationActive);
		}
		#endregion

		#region Properties: Private
		private UserConnection _userConnection;
		private bool _isIntegrationActive = true;
		#endregion

		#region Properties: Public
		public UserConnection UserConnection {
			get { return _userConnection; }
		}
		#endregion
		
		#region Methods: Public
		public bool Update(Entity entity) {
			if(!_isIntegrationActive) {
				return true;
			}
			try {
				if(entity.IsColumnValueLoaded("TsExternalId") && entity.GetTypedColumnValue<int>("TsExternalId") == 0) {
					Insert(entity);
					return true;
				}
				var integratorHelper = new IntegratorHelper();
				var method = TRequstMethod.PUT;
				integratorHelper.PushRequest(method, GetUrlByEntity(entity, method), GetEntityJson(entity), (x, y) => {
					OnRequestResive(x, entity, method);
				});
				return true;
			} catch(Exception e) {
				return true;
			}
		}
		public bool Insert(Entity entity) {
			if(!_isIntegrationActive) {
				return true;
			}
			try {
				var integratorHelper = new IntegratorHelper();
				var method = TRequstMethod.POST;
				if(entity.IsColumnValueLoaded("TsExternalId") && entity.GetTypedColumnValue<int>("TsExternalId") != 0) {
					Update(entity);
					return true;
				}
				integratorHelper.PushRequest(method, GetUrlByEntity(entity, method), GetEntityJson(entity), (x, y) => {
					OnRequestResive(x, entity, method);
				});
				return true;
			} catch(Exception e) {
				return true;
			}
		}
		public bool Delete(Entity entity) {
			if(!_isIntegrationActive) {
				return true;
			}
			try {
				var integratorHelper = new IntegratorHelper();
				var method = TRequstMethod.DELETE;
				if(entity.IsColumnValueLoaded("TsExternalId") && entity.GetTypedColumnValue<int>("TsExternalId") == 0) {
					return true;
				}
				integratorHelper.PushRequest(method, GetUrlByEntity(entity, method), "", null);
				return true;
			} catch(Exception e) {
				return true;
			}
		}
		#endregion
		
		#region Methods: Private
		private string GetEntityJson(Entity entity) {
			var integrationInfo = new IntegrationInfo(null, UserConnection, CsConstant.TIntegrationType.Export, entity.PrimaryColumnValue, entity.SchemaName, CsConstant.IntegrationActionName.Empty, entity);
			var integrationEntityHelper = new IntegrationEntityHelper();
			integrationEntityHelper.IntegrateEntity(integrationInfo);
			if(integrationInfo.Result.Type == CsConstant.IntegrationResult.TResultType.Success) {
				return integrationInfo.Result.Data.ToString();
			}
			return null;
		}
		private string GetUrlByEntity(Entity entity, TRequstMethod method) {
			string url, entityName, additionalParam = "";
			string entityType = entity.SchemaName;
			if(CsConstant.clientserviceEntity.ContainsKey(entityType)) {
				url = CsConstant.clientserviceEntityUrl;
				entityName = CsConstant.clientserviceEntity[entityType];
				if(entity.SchemaName == "SysAdminUnit" && entity.GetTypedColumnValue<int>("SysAdminUnitTypeValue") < (int)CsConstant.TSysAdminUnitType.User)
					entityName = "ManagerGroup";
			} else if(CsConstant.clientserviceDict.ContainsKey(entityType)) {
				url = CsConstant.clientserviceDictUrl;
				entityName = CsConstant.clientserviceDict[entityType];
			} else {
				throw new ArgumentException(string.Format("entity - '{0}' is unrecognize for export to client-service!", entity.GetType()));
			}

			if(method == TRequstMethod.PUT || method == TRequstMethod.DELETE)
				additionalParam = "/" + entity.GetTypedColumnValue<int>("TsExternalId").ToString();
			return url + "/" + entityName + additionalParam;
		}

		private void OnRequestResive(string responseJson, Entity entity, TRequstMethod method) {
			int extId = 0;
			int extVersion = 0;
			var obj = Newtonsoft.Json.JsonConvert.DeserializeObject<ExpandoObject>(responseJson) as IDictionary<string, object>;
			if(obj.Any() && obj.First().Value is ExpandoObject) {
				var dict = (IDictionary<string, object>)obj.First().Value;
				extId = int.Parse(dict["id"].ToString());
				extVersion = int.Parse(dict["version"].ToString());
			}
			if(extId != 0 && entity.GetTypedColumnValue<int>("TsExternalId") == 0) {
				entity.SetColumnValue("TsExternalId", extId);
				entity.SetColumnValue("TsExternalVersion", extVersion);
				entity.Save(false);
			}
		}
		#endregion
	}
	#endregion

	#region Static Class: ExtensionHelper
	public static class ExtensionHelper {
		#region Methods: Public
		public static string SerializeToJson(this object obj) {
			return Newtonsoft.Json.JsonConvert.SerializeObject(obj).Replace("ReferenceClientService", "#ref");
		}
		public static Dictionary<string, object> DeserializeJson(this string json) {
			return Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, object>>(json);
		}
		public static long GetExtIdFromJson(this string json) {
			var obj = Newtonsoft.Json.JsonConvert.DeserializeObject<ExpandoObject>(json) as IDictionary<string, object>;
			if(obj.Any() && obj.First().Value is ExpandoObject && ((IDictionary<string, object>)obj.First().Value).ContainsKey("id")) {
				var resultId = (long)((IDictionary<string, object>)obj.First().Value)["id"];
				return resultId;
			}
			return 0;
		}
		#endregion
	}
	#endregion

	#region Class: IntegrationServiceIntegrator
	public class IntegrationServiceIntegrator {

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
		public UserConnection UserConnection {
			get { return _userConnection; }
		}
		public IntegrationEntityHelper IntegrationEntityHelper {
			get {
				return _integrationEntityHelper;
			}
		}
		#endregion

		#region Constructor: Public
		public IntegrationServiceIntegrator(UserConnection userConnection) {
			_userConnection = userConnection;
			_postboxId = SysSettings.GetValue(UserConnection, CsConstant.SysSettingsCode.TerrasoftPostboxId, _postboxId);
			_basePostboxUrl = SysSettings.GetValue(UserConnection, CsConstant.SysSettingsCode.IntegrationServiceBaseUrl, _basePostboxUrl);
			_baseClientServiceUrl = SysSettings.GetValue(UserConnection, CsConstant.SysSettingsCode.ClientServiceBaseUrl, _basePostboxUrl);
			_notifyLimit = SysSettings.GetValue(UserConnection, CsConstant.SysSettingsCode.NotificationLimit, _notifyLimit);
			_isImportAllow = SysSettings.GetValue(UserConnection, CsConstant.SysSettingsCode.AllowImport, _isImportAllow);
			_integrationEntityHelper = new IntegrationEntityHelper();
		}
		#endregion

		#region Methods: Public
		public void GetBusEventNotification(bool withData = true) {
			var url = GenerateUrl(
				withData == true ? TIntegratorRequest.BusEventNotificationData : TIntegratorRequest.BusEventNotification,
				TRequstMethod.GET,
				"0",
				_notifyLimit.ToString(),
				CsConstant.DefaultBusEventFilters,
				CsConstant.DefaultBusEventSorts
			);

			PushRequestWrapper(TRequstMethod.GET, url, "", (x, y) => {
				var responceObj = x.DeserializeJson();
				var busEventNotifications = (JArray)responceObj["data"];
				OnBusEventNotificationsDataRecived(busEventNotifications, y);
			});
		}

		public void SetNotifyRead() {
			var url = GenerateUrl(
				TIntegratorRequest.BusEventNotification,
				TRequstMethod.PUT
			);

			var json = ReadedNotificationIds.Select(x => new {
				isRead = true,
				id = x
			}).SerializeToJson();

			PushRequestWrapper(TRequstMethod.PUT, url, json, null);
			ReadedNotificationIds.Clear();
		}

		public void AddReadId(string notifyId) {
			ReadedNotificationIds.Add(notifyId);
		}

		public void CreatedOnEntityExist(IntegrationInfo integrationInfo) {
			string jName = integrationInfo.EntityName;
			var data = integrationInfo.Data[jName];
			int version = data.Value<int>("version");
			int jId = data.Value<int>("id");
			string url = string.Format("{0}/{1}/{2}", _baseClientServiceUrl, jName, jId);

			PushRequestWrapper(TRequstMethod.GET, url, "", (x, y) => {
				var responceObj = JObject.Parse(x);
				var csData = responceObj[jName] as JObject;
				var csVersion = csData.Value<int>("version");
				if(csVersion > version) {
					integrationInfo.Data = responceObj;
					integrationInfo.EntityName = jName;
					integrationInfo.Action = CsConstant.IntegrationActionName.Update;
					_integrationEntityHelper.IntegrateEntity(integrationInfo);
				}
			});
		}

		public void OnBusEventNotificationsDataRecived(JArray busEventNotifications, UserConnection userConnection) {
			foreach(JObject busEventNotify in busEventNotifications) {
				var busEvent = busEventNotify[CsConstant.IntegrationEventName.BusEventNotify] as JObject;
				if(busEvent != null) {
					var data = busEvent["data"] as JObject;
					var objectType = busEvent["objectType"].ToString();
					var action = busEvent["action"].ToString();
					var notifyId = busEvent["id"].ToString();
					if(!string.IsNullOrEmpty(objectType) && data != null) {
						var integrationInfo = new IntegrationInfo(data, userConnection, TIntegrationType.Import, null, objectType, action, null);
						_integrationEntityHelper.IntegrateEntity(integrationInfo);
						if(integrationInfo.Result != null && integrationInfo.Result.Exception == CsConstant.IntegrationResult.TResultException.OnCreateEntityExist) {
							CreatedOnEntityExist(integrationInfo);
						}
					}
					AddReadId(notifyId);
				}
			}
			SetNotifyRead();
		}

		public string GenerateUrl(TIntegratorRequest integratorRequestType, TRequstMethod requstMethod, string skip = null, string limit = null, Dictionary<string, string> filters = null, Dictionary<string, string> sorts = null) {
			string result = _basePostboxUrl;
			string filtersStr = "";
			string sortStr = "";
			string skipStr = "";
			string limitStr = "";
			#region Requst Method
			switch(requstMethod) {
				case TRequstMethod.GET:
				case TRequstMethod.PUT:

				break;
				default:
				throw new NotImplementedException();
			}
			#endregion

			#region Integration Request
			switch(integratorRequestType) {
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
			if(!string.IsNullOrEmpty(skip))
				skipStr = string.Format("skip={0}", skip);
			#endregion

			#region limit
			if(!string.IsNullOrEmpty(limit))
				limitStr = string.Format("limit={0}", limit);
			#endregion

			#region filters
			if(filters != null && filters.Any()) {
				foreach(var filter in filters) {
					filtersStr += string.Format("filter[{0}]={1}&", filter.Key, filter.Value);
				}
				filtersStr = filtersStr.Remove(filtersStr.Length - 1);
			}
			#endregion

			#region sort
			if(sorts != null && sorts.Any()) {
				foreach(var sort in sorts) {
					sortStr += string.Format("sort[{0}]={1}&", sort.Key, sort.Value);
				}
				sortStr = sortStr.Remove(sortStr.Length - 1);
			}
			#endregion

			#region Param
			string paramStr = GenerateParamRoRequest(skipStr, limitStr, filtersStr, sortStr);
			if(!string.IsNullOrEmpty(paramStr)) {
				result += string.Format("?{0}", paramStr);
			}
			#endregion
			return result;
		}
		#endregion
		
		#region Methods: Private
		private string GenerateRouteToRequest(params object[] routes) {
			return "/" + routes.Aggregate((cur, next) => cur.ToString() + "/" + next.ToString()).ToString();
		}
		private string GenerateParamRoRequest(params string[] param) {
			var collection = param.Where(x => !string.IsNullOrEmpty(x));
			return collection.Any() ? collection.Aggregate((cur, next) => cur + "&" + next) : "";
		}
		private void PushRequestWrapper(TRequstMethod requestMethod, string url, string jsonText, Action<string, UserConnection> callback) {
			if(_isImportAllow) {
				_integratorHelper.PushRequest(requestMethod, url, jsonText, callback, UserConnection);
			}
		}
		#endregion

		#region Enum: Public
		public enum TIntegratorRequest {
			BusEventNotificationData,
			BusEventNotification,
			Postbox
		}
		#endregion
	}
	#endregion

	#region Class: IntegrationEntityHelper
	public class IntegrationEntityHelper {
		#region Properties: Private
		private static List<Type> IntegrationEntityTypes { get; set; }
		private static Dictionary<Type, IIntegrationEntityHandler> EntityHandlers { get; set; }
		#endregion

		#region Constructor: Public
		public IntegrationEntityHelper() {
		}
		#endregion

		#region Methods: Public
		public void IntegrateEntity(IntegrationInfo integrationInfo) {
			EntityHandlers = new Dictionary<Type,IIntegrationEntityHandler>();
			ExecuteHandlerMethod(integrationInfo, GetIntegrationHandler(integrationInfo));
		}

		public Type GetAttributeType(IntegrationInfo integrationInfo) {
			return integrationInfo.IntegrationType == TIntegrationType.Import ? typeof(ImportHandlerAttribute) : typeof(ExportHandlerAttribute);
		}

		public List<Type> GetIntegrationTypes(IntegrationInfo integrationInfo) {
			if(IntegrationEntityTypes != null && IntegrationEntityTypes.Any()) {
				return IntegrationEntityTypes;
			}
			var attributeType = GetAttributeType(integrationInfo);
			var assembly = Type.GetType("Terrasoft.Configuration.IntegrationServiceIntegrator").Assembly;
			return IntegrationEntityTypes = assembly.GetTypes().Where(x => {
				var attributes = x.GetCustomAttributes(attributeType, true);
				return attributes != null && attributes.Length > 0;
			}).ToList();
		}

		public IIntegrationEntityHandler GetIntegrationHandler(IntegrationInfo integrationInfo) {
			var attributeType = GetAttributeType(integrationInfo);
			var types = GetIntegrationTypes(integrationInfo);
			foreach(var type in types) {
				var attribute = type.GetCustomAttributes(attributeType, true).FirstOrDefault() as IntegrationHandlerAttribute;
				if(attribute != null && attribute.EntityName == integrationInfo.EntityName) {
					if(EntityHandlers.ContainsKey(type)) {
						return EntityHandlers[type];
					}
					var entityHandler = Activator.CreateInstance(type) as IIntegrationEntityHandler;
					EntityHandlers.Add(type, entityHandler);
					return entityHandler;
				}
			}
			return null;
		}

		public void ExecuteHandlerMethod(IntegrationInfo integrationInfo, IIntegrationEntityHandler handler) {
			if(handler != null) {
				try {
					if(integrationInfo.IntegrationType == TIntegrationType.Export) {
						var result = new CsConstant.IntegrationResult(CsConstant.IntegrationResult.TResultType.Success, handler.ToJson(integrationInfo));
						integrationInfo.Result = result;
						return;
					}
					if(integrationInfo.Action == CsConstant.IntegrationActionName.Create) {
						if(!handler.IsEntityAlreadyExist(integrationInfo)) {
							handler.Create(integrationInfo);
						} else {
							var result = new CsConstant.IntegrationResult(CsConstant.IntegrationResult.TResultException.OnCreateEntityExist);
							integrationInfo.Result = result;
							return;
						}
					} else if(integrationInfo.Action == CsConstant.IntegrationActionName.Update) {
						if(handler.IsEntityAlreadyExist(integrationInfo)) {
							handler.Update(integrationInfo);
						} else {
							handler.Create(integrationInfo);
						}
					} else if(integrationInfo.Action == CsConstant.IntegrationActionName.Delete) {
						handler.Delete(integrationInfo);
					} else {
						handler.Unknown(integrationInfo);
					}
				} catch(Exception e) {
					IntegrationUtilities.Error("Method [ReportEntity] class entity handler throw exception: entityName = {0} id = {1} message = {2}", integrationInfo.EntityName, integrationInfo.EntityIdentifier, e.Message);
				}
			}
		}
		#endregion
	}
	#endregion

	#region Class: CsReferenceProperty
	public class CsReferenceProperty {
		public int id;
		public string type;
		public string name;
	}
	#endregion

	#region Class: CsReference
	public class CsReference {
		public CsReferenceProperty ReferenceClientService;
		public static CsReference Create(int pid, string ptype, string pname = "") {
			return pid != 0 ? new CsReference {
				ReferenceClientService = new CsReferenceProperty {
					id = pid,
					type = ptype,
					name = pname
				}
			} : (CsReference)null;
		}
	}
	#endregion

	#region Interface: Public
	public interface IIntegrationEntityHandler {
		void Create(IntegrationInfo integrationInfo);
		void Update(IntegrationInfo integrationInfo);
		void Delete(IntegrationInfo integrationInfo);
		void Unknown(IntegrationInfo integrationInfo);
		JObject ToJson(IntegrationInfo integrationInfo);
		bool IsEntityAlreadyExist(IntegrationInfo integrationInfo);
	}
	#endregion

	#region Interface: IMappingMethod
	public interface IMappingMethod {
		void Evaluate(MappingItem mappItem, IntegrationInfo integrationInfo);
	}
	#endregion

	#region Class: IntegrationHandlerAttribute
	[AttributeUsage(AttributeTargets.Property | AttributeTargets.Class | AttributeTargets.Method)]
	public class IntegrationHandlerAttribute : Attribute {
		private string entityName;
		public string EntityName {
			get { return entityName; }
		}
		public IntegrationHandlerAttribute(string entityName) {
			this.entityName = entityName;
		}
	}
	#endregion

	#region Class: ImportHandlerAttribute
	[AttributeUsage(AttributeTargets.Property | AttributeTargets.Class | AttributeTargets.Method)]
	public class ImportHandlerAttribute : IntegrationHandlerAttribute {
		public ImportHandlerAttribute(string entityName)
			: base(entityName) {
		}
	}
	#endregion

	#region Class: ExportHandlerAttribute
	[AttributeUsage(AttributeTargets.Property | AttributeTargets.Class | AttributeTargets.Method)]
	public class ExportHandlerAttribute : IntegrationHandlerAttribute {
		public ExportHandlerAttribute(string entityName)
			: base(entityName) {
		}
	}
	#endregion

	#region Class: MappingMethodAttribute
	public class MappingMethodAttribute : Attribute {
		private string methodName;
		public string MethodName {
			get {
				return methodName;
			}
		}

		public MappingMethodAttribute(string methodName) {
			this.methodName = methodName;
		}
	}
	#endregion

	#region Static Class: CsConstant
	public static class CsConstant {
		#region Class: IntegrationResult
		public class IntegrationResult {

			#region Properties: Public
			public bool Success {get;set;}
			public JObject Data {get;set;}
			public TResultType Type {get;set;}
			public TResultException Exception {get;set;}
			public string ExceptionMessage {get;set;}
			#endregion

			#region Constructor: Public
			public IntegrationResult() {

			}

			public IntegrationResult(JObject data) {
				Data = data;
			}

			public IntegrationResult(TResultType type, JObject data = null) {
				Type = type;
				Data = data;
			}

			public IntegrationResult(TResultException exception, string message = null, JObject data = null) {
				Type = TResultType.Exception;
				Exception = exception;
				ExceptionMessage = message;
				Data = data;
			}
			#endregion

			#region Enum: Public
			public enum TResultException {
				OnCreateEntityExist
			}
			public enum TResultType {
				Exception,
				Success
			}
			#endregion
		}
		#endregion

		#region Class: IntegrationInfo
		public class IntegrationInfo {
			
			#region Properties: Public
			public JObject Data { get; set; }
			public UserConnection UserConnection { get; set; }
			public TIntegrationType IntegrationType { get; set; }
			public string EntityName { get; set; }
			public string Action { get; set; }
			public Guid? EntityIdentifier { get; set; }
			public IntegrationResult Result { get;set; }
			public Entity IntegratedEntity {get;set;}
			#endregion

			#region Constructor: Public
			public IntegrationInfo(JObject data, UserConnection userConnection, TIntegrationType integrationType = TIntegrationType.Export,
					Guid? entityIdentifier = null, string entityName = "", string action = "Create", Entity integratedEntity = null) {
				Data = data;
				UserConnection = userConnection;
				IntegrationType = integrationType;
				EntityIdentifier = entityIdentifier;
				EntityName = entityName;
				Action = action;
				IntegratedEntity = integratedEntity;
			}
			#endregion

			#region Method Override: public
			public override string ToString() {
				return string.Format("Data = {0}\nIntegrationType={1} EntityIdentifier={2}", Data, IntegrationType.ToString(), EntityIdentifier);
			}
			#endregion

		}
		#endregion

		#region Enum: Public
		public enum TIntegrationType {
			Export = 0,
			Import = 1,
			All = 3
		}
		public enum TSysAdminUnitType {
			Organization = 0,
			Unit = 1,
			Head = 2,
			Team = 3,
			User = 4,
			SelfServicePortalUser = 5,
			FunctionalRole = 6
		}
		#endregion

		#region Properties: Public
		public const string clientserviceEntityUrl = "http://api.client-service.stage2.laximo.ru/v2/entity/AUTO3N";
		public const string clientserviceDictUrl = "http://api.client-service.stage2.laximo.ru/v2/dict/AUTO3N";
		public static Dictionary<string, string> clientserviceEntity = new Dictionary<string, string>() {
			{ "Account", "CompanyProfile" },
			{ "Contact", "PersonProfile" },
			{ "ContactCommunication", "ContactRecord" },
			{ "TsAutomobile", "VehicleProfile" },
			{ "SysAdminUnit", "Manager" },
			{ "SysAdminUnit2", "ManagerGroup" },
			{ "Case", "ClientRequest" },
			{ "Relationship", "Relationship" },
			{ "ContactCareer", "Employee" },
			{ "TsContactNotifications", "NotificationProfile" },
			{ "ContactAddress", "AddressInfo" },
			{ "TsAutoTechService", "VehicleRelationship" },
			{ "TsAutoOwnerHistory", "VehicleRelationship" },
			{ "TsAutoOwnerInfo", "VehicleRelationship" },
			{ "TsAutoTechHistory", "VehicleRelationship" }
		};
		public static Dictionary<string, string> clientserviceDict = new Dictionary<string, string>() {
			//Dictionary
			{ "RelationType", "RelationshipType" },
			{ "CommunicationType", "ContactRecordType" },
			{ "AddressType", "AddressType" },
			{ "TsSto", "VehicleRelationshipType" }
			//AssortmentRequestStatus - unrecognize
		};
		
		public static class VehicleRelationshipType {
			public const int Owner = 1;
			public const int Leasing = 2;
			public const int Driver = 3;
			public const int Service = 4;
			public const int Rent = 5;
			public const int Other = 6;
		}
		
		public const string ContactEntityName = "PersonProfile";
		public const string AccountEntityName = "CompanyProfile";
		public const string RelationshipTypeEntityName = "RelationshipType";
		public const string ManagerEntityName = "Manager";
		public const string RelationshipEntityName = "Relationship";
		public const string ContactRecordEntityName = "ContactRecordType";
		public const string ManagerGroupEntityName = "ManagerGroup";
		public const string AddressTypeEntityName = "AddressType";
		public const string AutomobilePassportEntityName = "VehiclePassport";
		public const string AutomobileRelationshipEntityName = "VehicleRelationship";
		public const string ContactNotificationProfileEntityName = "NotificationProfile";
		public const string AutomobileRelTypeEntityName = "VehicleRelationshipType";

		public static Dictionary<string, string> DefaultBusEventFilters = new Dictionary<string, string>() {
			{"isRead", "false"}
		};
		public static Dictionary<string, string> DefaultBusEventSorts = new Dictionary<string, string>() {
			{"loggedAt", "desc"}
		};
		#endregion

		#region Static Class: IntegrationEventName
		public static class IntegrationEventName {
			public const string BusEventNotify = @"BusEventNotification";
			public const string BusEventNotifyData = @"BusEventNotificationData";
		}
		#endregion

		#region Static Class: IntegrationActionName
		public static class IntegrationActionName {
			public const string Create = @"create";
			public const string Update = @"update";
			public const string Delete = @"delete";
			public const string Empty = @"";
		}
		#endregion

		#region Static Class: SysSettingsCode
		public static class SysSettingsCode {
			public const string AllowImport = @"IntegrServAllowImport";
			public const string IntegrationServiceBaseUrl = @"IntegrServBaseUrl";
			public const string TerrasoftPostboxId = @"IntegrServTerrasoftPostboxId";
			public const string NotificationLimit = @"IntegrServBusEventNotificationLimin";
			public const string IsInsertToDB = @"IntegrServInsertToDbWithoutEntityLogic";
			public const string ClientServiceBaseUrl = @"ClientServiceBaseUrl";
			public const string ConfigurationData = @"IntegrationXmlConfigData";
			public const string IsIntegrationActive = @"IsIntegrationActive";
		}
		#endregion

		#region Static Class: IntegrationFlagSetting
		public static class IntegrationFlagSetting {
			public const bool AllowErrorOnColumnAssign = false;
		}
		#endregion

	}
	#endregion

	#region Static Class: IntegrationUtilities

	public static class IntegrationUtilities {

		#region Fields: Private

		private static global::Common.Logging.ILog _log;

		#endregion

		#region Properties: Public
		/// <summary>
		/// Логгер для интеграции.
		/// </summary>
		public static global::Common.Logging.ILog Log {
			get {
				if(_log == null)
					_log = global::Common.Logging.LogManager.GetLogger("TscIntegration");
				return _log;
			}
		}
		#endregion

		#region Methods: Public
		public static string ToLogString<T1, T2>(this Dictionary<T1, T2> dict) {
			return string.Join(";", dict.Select(x => x.Key.ToString() + " = " + x.Value.ToString()));
		}

		public static void Info(string format, params object[] args) {
			try {
				//Console.ForegroundColor = ConsoleColor.Green;
				//Console.WriteLine(string.Format(format, args.Select(x => x ?? "null").ToArray()));
				//Log.Info(string.Format(format, args.Select(x => x ?? "null").ToArray()));
			} catch(Exception e) {
				Console.ForegroundColor = ConsoleColor.Red;
				Console.WriteLine(e.Message);
				//Log.Error(e.Message);
			}
		}

		public static void Info(string text) {
			try {
				////Log.Info(text);
				//Console.ForegroundColor = ConsoleColor.Green;
				//Console.WriteLine(text);
			} catch(Exception e) {
				Console.ForegroundColor = ConsoleColor.Red;
				Console.WriteLine(e.Message);
				//Log.Error(e.Message);
			}
		}
		public static void Error(string text) {
			try {
				//Console.ForegroundColor = ConsoleColor.Red;
				//Console.WriteLine(text);
				////Log.Error(text);
			} catch(Exception e) {
				Console.ForegroundColor = ConsoleColor.Red;
				Console.WriteLine(e.Message);
				//Log.Error(e.Message);
			}
		}
		public static void Error(string format, params object[] args) {
			try {
				var buff = Console.ForegroundColor;
				Console.ForegroundColor = ConsoleColor.Red;
				Console.WriteLine(string.Format(format, args.Select(x => x ?? "null").ToArray()));
				Console.ForegroundColor = buff;
				//Log.Error(string.Format(format, args.Select(x => x ?? "null").ToArray()));
			} catch(Exception e) {
				Console.ForegroundColor = ConsoleColor.Red;
				Console.WriteLine(e.Message);
				//Log.Error(e.Message);
			}
		}
		#endregion

	}

	#endregion

	#region Class: IntegrationConfigurationManager
	public static class IntegrationConfigurationManager {
		
		#region Static Fields: Private
		private static List<string> _columnNames;
		
		private static string _xmlData;
		private static XmlDocument _xDocument;
		private static MappingItem _defaultItem;
		private static Dictionary<string, string> _prerenderConfigDict;
		#endregion

		#region Methods: Private
		private static XmlDocument GetConfigXmlDocument(UserConnection userConnection) {
			try {
				if(_xDocument != null)
					return _xDocument;

				string confLocation = ConfigurationManager.AppSettings["XmlConfigurationLocation"] ?? "db";
				if(confLocation == "db") {
					if(string.IsNullOrEmpty(_xmlData)) {
						_xmlData = SysSettings.GetValue(userConnection, CsConstant.SysSettingsCode.ConfigurationData, "<?xml version=\"1.0\" encoding=\"utf-8\"?>");
					}
				} else if(confLocation == "file") {
					if(string.IsNullOrEmpty(_xmlData)) {
						string confPath = ConfigurationManager.AppSettings["XmlConfigurationFilePath"] ?? "IntegrationConfig.xml";
						using(var stream = new StreamReader(confPath)) {
							_xmlData = stream.ReadToEnd();
						}
					}
				}
				if(_xDocument == null) {
					_xDocument = new XmlDocument();
					_xDocument.LoadXml(_xmlData);
				}
				return _xDocument;
			} catch(Exception e) {
				IntegrationUtilities.Error("Method [GetConfigXmlDocument] throw exception: Message = {0}", e.Message);
				throw;
			}
		}

		private static XmlNode GetXmlNodeByNameAttr(XmlDocument doc, string name) {
			try {
				foreach(XmlNode node in doc.DocumentElement) {
					if(node is XmlComment)
						continue;
					if((node.Attributes["TsName"] != null && node.Attributes["TsName"].Value == name) || (node.Attributes["JName"] != null && node.Attributes["JName"].Value == name)) {
						return node;
					}
				}
				return null;
			} catch(Exception e) {
				IntegrationUtilities.Error("Method [GetXmlNodeByNameAttr] throw exception: Message = {0}", e.Message);
				throw;
			}
		}

		private static MappingItem GetItemByXmlNode(UserConnection userConnection, XmlNode node, MappingItem defItem = null) {
			try {
				var resultObj = Activator.CreateInstance(_mapItemType) as MappingItem;
				bool isAttrSetting = false;
				foreach(string attributeName in ColumnNames) {
					isAttrSetting = false;
					PropertyInfo propertyInfo = _mapItemType.GetProperty(attributeName);
					var xmlAttribute = node.Attributes[attributeName];
					if(xmlAttribute != null) {
						string xmlValue = PrepareValue(userConnection, xmlAttribute.Value);
						if(propertyInfo != null) {
							Type propertyType = propertyInfo.PropertyType;

							if(propertyType.IsEnum || propertyType == typeof(int)) {
								isAttrSetting = true;
								propertyInfo.SetValue(resultObj, int.Parse(xmlValue));
							} else if(propertyType == typeof(bool)) {
								isAttrSetting = true;
								propertyInfo.SetValue(resultObj, xmlValue != "0");
							} else {
								isAttrSetting = true;
								propertyInfo.SetValue(resultObj, xmlValue);
							}
						}
					}
					if(!isAttrSetting && defItem != null) {
						propertyInfo.SetValue(resultObj, propertyInfo.GetValue(defItem));
					}
				}
				return resultObj;
			} catch(Exception e) {
				IntegrationUtilities.Error("Method [GetItemByXmlNode] throw exception: Message = {0}", e.Message);
				throw;
			}
		}

		private static Dictionary<string, string> GetPrerenderConfig(UserConnection userConnection) {
			try {
				if(_prerenderConfigDict != null && _prerenderConfigDict.Any())
					return _prerenderConfigDict;
				var doc = GetConfigXmlDocument(userConnection);
				var element = doc.DocumentElement["prerenderConfig"];
				_prerenderConfigDict = new Dictionary<string, string>();
				if(element != null) {
					foreach(XmlNode confItem in element.ChildNodes) {
						string from = confItem.Attributes["From"].Value;
						string to = confItem.Attributes["To"].Value;
						if(!string.IsNullOrEmpty(from) && !string.IsNullOrEmpty(to)) {
							_prerenderConfigDict.Add(from, to);
						}
					}
				}
				return _prerenderConfigDict;
			} catch(Exception e) {
				IntegrationUtilities.Error("Method [GetPrerenderConfig] throw exception: Message = {0}", e.Message);
				throw;
			}
		}

		private static string PrepareValue(UserConnection userConnection, string value) {
			try {
				var configDict = GetPrerenderConfig(userConnection);
				if(configDict.ContainsKey(value))
					return configDict[value];
				return value;
			} catch(Exception e) {
				IntegrationUtilities.Error("Method [PrepareValue] throw exception: Message = {0}", e.Message);
				throw;
			}
		}
		private static MappingItem GetDefaultItem(UserConnection userConnection) {
			try {
				return _defaultItem == null ? _defaultItem = GetItemByXmlNode(userConnection, GetXmlNodeByNameAttr(GetConfigXmlDocument(userConnection), "Default").ChildNodes[0]) : _defaultItem;
			} catch(Exception e) {
				IntegrationUtilities.Error("Method [GetDefaultItem] throw exception: Message = {0}", e.Message);
				throw;
			}
		}
		#endregion

		#region Methods: Public
		public static List<MappingItem> GetConfigItem(UserConnection userConnection, string Name) {
			try {
				var result = new List<MappingItem>();
				XmlDocument xDoc = GetConfigXmlDocument(userConnection);
				var node = GetXmlNodeByNameAttr(xDoc, Name);
				var defItem = GetDefaultItem(userConnection);
				foreach(XmlNode mapItem in node.ChildNodes) {
					if(mapItem is XmlElement)
						result.Add(GetItemByXmlNode(userConnection, mapItem, defItem));
				}
				return result;
			} catch(Exception e) {
				IntegrationUtilities.Error("Method [GetConfigItem] throw exception: Message = {0}", e.Message);
				throw;
			}
		}
		#endregion

		#region Static Fields: Public
		public static Type _mapItemType = typeof(MappingItem);
		#endregion

		#region Properties: Public
		public static List<string> ColumnNames {
			get {
				return _columnNames == null || !_columnNames.Any() ? _columnNames = _mapItemType.GetProperties().Where(x => x.MemberType == MemberTypes.Property).Select(x => x.Name).ToList() : _columnNames;
			}
		}
		#endregion
		
	}
	#endregion

	#region Class: MappingHelper
	public class MappingHelper {
		
		#region Fields: Public
		public string RefName = @"#ref";
		public bool _isInsertToDB;
		public List<MappingItem> MapConfig;
		public UserConnection UserConnection;
		public Queue<Action> MethodQueue;
		#endregion

		#region Properties: Public
		public bool IsInsertToDB {
			get {
				try {
					_isInsertToDB = SysSettings.GetValue(UserConnection, CsConstant.SysSettingsCode.IsInsertToDB, _isInsertToDB);
				} catch(Exception e) {
					IntegrationUtilities.Error("Method [IsInsertToDB get] exception: Message = {0}", e.Message);
					_isInsertToDB = false;
				}
				return _isInsertToDB;
			}
		}
		#endregion

		#region Constructor: Public
		public MappingHelper() {
			_isInsertToDB = false;
			MethodQueue = new Queue<Action>();
		}
		#endregion

		#region Methods: Public
		public void StartMappByConfig(IntegrationInfo integrationInfo, string jName, List<MappingItem> mapConfig) {
			try {
				switch(integrationInfo.IntegrationType) {
					case TIntegrationType.Import: {
						if(integrationInfo.IntegratedEntity == null)
							throw new Exception(string.Format("Integration Entity not exist {0} ({1})", jName));
						var entityJObj = integrationInfo.Data[jName];
						integrationInfo.IntegratedEntity.SetDefColumnValues();
						foreach(var item in mapConfig) {
							try {
								if(item.MapIntegrationType != TIntegrationType.All && item.MapIntegrationType != integrationInfo.IntegrationType)
									continue;
								var subJObj = GetJTokenByPath(entityJObj, item.JSourcePath);
								MapColumn(item, ref subJObj, integrationInfo);
							} catch(Exception e) {
								if(CsConstant.IntegrationFlagSetting.AllowErrorOnColumnAssign) {
									throw;
								}
							}
						}
						break;
					}
					case TIntegrationType.Export:
						integrationInfo.Data = new JObject();
						if(integrationInfo.Data[jName] == null)
							integrationInfo.Data[jName] = new JObject(); 
						foreach(var item in mapConfig) {
							
							var jObjItem = (new JObject()) as JToken;
							try {
								MapColumn(item, ref jObjItem, integrationInfo);
							} catch(Exception e) {
								IntegrationUtilities.Error("Method [StartMappByConfig] catch exception by map column {0} Message = {1} IgnoreError = {2}", item.JSourcePath, e.Message, item.IgnoreError);
								if(!item.IgnoreError) {
									throw;
								}
								jObjItem = null;
							}
							if(integrationInfo.Data[jName][item.JSourcePath] != null && integrationInfo.Data[jName][item.JSourcePath].HasValues) {
								if(integrationInfo.Data[jName][item.JSourcePath] is JArray)
									((JArray)integrationInfo.Data[jName][item.JSourcePath]).Add(jObjItem);
							} else {
								var resultJ = GetJTokenByPath(integrationInfo.Data[jName], item.JSourcePath);
								if(jObjItem == null && item.EFieldRequier)
									throw new ArgumentNullException("Field " + item.JSourcePath + " required!");
								resultJ.Replace(jObjItem);
							}
						}
					break;
				}
			} catch(Exception e) {
				IntegrationUtilities.Error("Method [StartMappByConfig] catch exception Message = {0} jName = jName", e.Message, jName);
				throw;
			}
		}

		public void MapColumn(MappingItem mapItem, ref JToken jToken, IntegrationInfo integrationInfo) {
			if(UserConnection == null)
				UserConnection = integrationInfo.UserConnection;
			var entity = integrationInfo.IntegratedEntity;
			var integrationType = integrationInfo.IntegrationType;
			Action executedMethod = new Action(() => { });
			switch(mapItem.MapType) {
				case TMapType.RefToGuid:
				//executedMethod = new Action(() => RefToGuid(mapItem, integrationInfo, jToken));
				RefToGuid(mapItem, integrationInfo, ref jToken);
				break;
				case TMapType.Simple:
				//executedMethod = new Action(() => Simple(mapItem, integrationInfo, jToken));
				Simple(mapItem, integrationInfo, ref jToken);
				break;
				case TMapType.StringToDictionaryGuid:
				//executedMethod = new Action(() => StringToDictionaryGuid(mapItem, integrationInfo, jToken));
				StringToDictionaryGuid(mapItem, integrationInfo, ref jToken);
				break;
				case TMapType.CompositObject:
				//executedMethod = new Action(() => CompositObject(mapItem, integrationInfo, jToken));
				CompositObject(mapItem, integrationInfo, ref jToken);
				break;
				case TMapType.ArrayOfCompositObject:
				//executedMethod = new Action(() => ArrayOfCompositObject(mapItem, integrationInfo, jToken));
				ArrayOfCompositObject(mapItem, integrationInfo, ref jToken);
				break;
				case TMapType.Const:
				Const(mapItem, integrationInfo, ref jToken);
				break;
				case TMapType.ArrayOfReference:
				ArrayOfReference(mapItem, integrationInfo, ref jToken);
				break;
			}
			//switch(mapItem.MapExecuteType) {
			//	case TMapExecuteType.AfterEntitySave:
			//		MethodQueue.Enqueue(executedMethod);
			//	break;
			//	case TMapExecuteType.BeforeEntitySave:
			//		if(executedMethod != null) {
			//			executedMethod();
			//		}
			//	break;
			//}
		}

		public void ArrayOfReference(MappingItem mappingItem, IntegrationInfo integrationInfo, ref JToken jToken) {
			try {
				switch(integrationInfo.IntegrationType) {
					case TIntegrationType.Import: {
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
					case TIntegrationType.Export: {
						if(IsAllNotNullAndEmpty(integrationInfo.IntegratedEntity, mappingItem.TsDestinationName, mappingItem.TsSourcePath, mappingItem.JSourceName)) {
							var srcValue = integrationInfo.IntegratedEntity.GetTypedColumnValue<Guid>(mappingItem.TsSourcePath);
							var jArray = new JArray();
							var resultList = GetColumnValues(mappingItem.TsDestinationName, mappingItem.TsDestinationPath, srcValue, "TsExternalId");
							foreach(var resultItem in resultList) {
								var extId = int.Parse(resultItem.ToString());
								if(extId != 0) {
									jArray.Add(JToken.FromObject(CsReference.Create(extId, mappingItem.JSourceName)));
								}
							}
							jToken = jArray;
						} else {
							jToken = null;
						}
						break;
					}
				}
			} catch(Exception e) {
				throw;
			}
		}
		public void Const(MappingItem mappingItem, IntegrationInfo integrationInfo, ref JToken jToken) {
			try {
				switch(integrationInfo.IntegrationType) {
					case TIntegrationType.Import: {
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
					case TIntegrationType.Export: {
						object resultValue = null;
						switch(mappingItem.ConstType) {
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
			} catch(Exception e) {
				throw;
			}
		}

		public void RefToGuid(MappingItem mappingItem, IntegrationInfo integrationInfo, ref JToken jToken) {
			switch(integrationInfo.IntegrationType) {
				case TIntegrationType.Import: {
					Guid? resultGuid = null;
					if(jToken != null && jToken.HasValues) {
						var refColumns = jToken[RefName];
						var externalId = int.Parse(refColumns["id"].ToString());
						var type = refColumns["type"];
						resultGuid = GetColumnValues(mappingItem.TsDestinationName, "TsExternalId", externalId, "Id").FirstOrDefault() as Guid?;
					}
					integrationInfo.IntegratedEntity.SetColumnValue(mappingItem.TsSourcePath, resultGuid);
					break;
				}
				case TIntegrationType.Export: {
					object resultObj = null;
					if(IsAllNotNullAndEmpty(integrationInfo.IntegratedEntity, mappingItem.TsDestinationName, mappingItem.TsSourcePath, mappingItem.JSourceName, mappingItem.TsDestinationPath)) {
						var resultValue = GetColumnValues(mappingItem.TsDestinationName, mappingItem.TsDestinationPath, integrationInfo.IntegratedEntity.GetTypedColumnValue<Guid>(mappingItem.TsSourcePath), mappingItem.TsExternalIdPath).FirstOrDefault(x => (int)x > 0);
						if(resultValue != null) {
							var resultRef = CsReference.Create(int.Parse(resultValue.ToString()), mappingItem.JSourceName);
							resultObj = resultRef != null ? JToken.FromObject(resultRef) : null;
						}
					}
					jToken = resultObj as JToken;
					break;
				}
			}
		}

		public void Simple(MappingItem mappingItem, IntegrationInfo integrationInfo, ref JToken jToken) {
			switch(integrationInfo.IntegrationType) {
				case TIntegrationType.Import: {
					object value = GetSimpleTypeValue(jToken);
					integrationInfo.IntegratedEntity.SetColumnValue(mappingItem.TsSourcePath, value);
					break;
				}
				case TIntegrationType.Export: {
					var value = integrationInfo.IntegratedEntity.GetColumnValue(mappingItem.TsSourcePath);
					jToken = value != null ? JToken.FromObject(GetSimpleTypeValue(value)) : null;
					break;
				}
			}
		}

		public void StringToDictionaryGuid(MappingItem mappingItem, IntegrationInfo integrationInfo, ref JToken jToken) {
			switch(integrationInfo.IntegrationType) {
				case TIntegrationType.Import: {
					object resultId = null;
					if(jToken != null) {
						var newValue = GetSimpleTypeValue(jToken);
						resultId = GetColumnValues(mappingItem.TsDestinationName, mappingItem.TsDestinationResPath, newValue, mappingItem.TsDestinationPath, 1).FirstOrDefault();
					}
					integrationInfo.IntegratedEntity.SetColumnValue(mappingItem.TsSourcePath, resultId);
					break;
				}
				case TIntegrationType.Export: {
					object resultObject = null;
					if(IsAllNotNullAndEmpty(integrationInfo.IntegratedEntity, mappingItem.TsDestinationName, mappingItem.TsSourcePath, mappingItem.TsDestinationPath, mappingItem.TsDestinationResPath)) {
						var sourceValue = integrationInfo.IntegratedEntity.GetColumnValue(mappingItem.TsSourcePath);
						resultObject = GetColumnValues(mappingItem.TsDestinationName, mappingItem.TsDestinationPath, sourceValue, mappingItem.TsDestinationResPath).FirstOrDefault();
					}
					jToken = resultObject != null ? JToken.FromObject(resultObject) : null;
					break;
				}
			}
		}

		public void CompositObject(MappingItem mappingItem, IntegrationInfo integrationInfo, ref JToken jToken) {
			try {
				switch(integrationInfo.IntegrationType) {
					case TIntegrationType.Import: {
						//if(jObject != null) {
						//	var integrEntityHelper = IntegrationServiceIntegrator.GetInstance(UserConnection).IntegrationEntityHelper;
						//	var integrationInfo = new IntegrationInfo(jObject, UserConnection, integrationType, null, null, "Create", entity);
						//	//integrEntityHelper.IntegrateEntity(jObject, jObject.Properties().FirstOrDefault().Name, CsConstant.IntegrationActionName.Update, UserConnection);
						//	integrEntityHelper.IntegrateEntity(integrationInfo);
						//}
						break;
					}
					case TIntegrationType.Export: {
						object resultJObj = null;
						if(IsAllNotNullAndEmpty(integrationInfo.IntegratedEntity, mappingItem.TsSourcePath, mappingItem.TsDestinationPath, mappingItem.TsDestinationName, mappingItem.JSourceName)) {
							var srcEntity = integrationInfo.IntegratedEntity;
							var dscId = srcEntity.GetColumnValue(mappingItem.TsSourcePath);
							string handlerName = GetFirstNotNull(mappingItem.HandlerName, mappingItem.TsDestinationName, mappingItem.JSourceName);
							resultJObj = GetCompositeJObjects(dscId, mappingItem.TsDestinationPath, mappingItem.TsDestinationName, handlerName, integrationInfo.UserConnection, 1).FirstOrDefault();
						}
						jToken = resultJObj as JToken;
						break;
					}
				}
			} catch(Exception e) {
				throw;
			}
		}

		public void ArrayOfCompositObject(MappingItem mappingItem, IntegrationInfo integrationInfo, ref JToken jToken) {
			try {
				switch(integrationInfo.IntegrationType) {
					case TIntegrationType.Import: {
						//if(jObject == null)
						//	return;
						//foreach(var item in jObject) {
						//	CompositObject("", mappingItem, entity, (item as JObject));
						//}
						break;
					}
					case TIntegrationType.Export: {
						if(IsAllNotNullAndEmpty(integrationInfo.IntegratedEntity, mappingItem.TsSourcePath, mappingItem.TsDestinationPath, mappingItem.TsDestinationName)) {
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
			} catch(Exception e) {
				throw;
			}
		}

		public List<JObject> GetCompositeJObjects(object colValue, string colName, string entityName, string handlerName, UserConnection userConnection, int maxCount = -1) {
			try {
				var jObjectsList = new List<JObject>();
				var esq = new EntitySchemaQuery(userConnection.EntitySchemaManager, entityName);
				esq.AddAllSchemaColumns();
				if(maxCount > 0)
					esq.RowCount = maxCount;
				esq.Filters.Add(esq.CreateFilterWithParameters(FilterComparisonType.Equal, colName, colValue));
				var collection = esq.GetEntityCollection(userConnection);
				foreach(var item in collection) {
					try {
						var integrationInfo = new IntegrationInfo(new JObject(), userConnection, TIntegrationType.Export, null, handlerName, "", item);
						var handler = (new IntegrationEntityHelper()).GetIntegrationHandler(integrationInfo);
						if(handler != null) {
							jObjectsList.Add(handler.ToJson(integrationInfo));
						}
					} catch(Exception e) {
						IntegrationUtilities.Error("Method [] catch exception message = {0}", e.Message);
						throw;
					}
				}
				return jObjectsList;
			} catch(Exception e) {
				return new List<JObject>();
			}
		}

		public bool CheckIsExist(string entityName, int externalId) {
			return false;
		}

		public void SaveEntity(Entity entity) {
			try {
				UserConnection = entity.UserConnection;
				bool result = false;
				if(IsInsertToDB) {
					switch(entity.StoringState) {
						case StoringObjectState.New:
						result = entity.Save(false);
						break;
						case StoringObjectState.Changed:
						result = entity.UpdateInDB(false);
						break;
					}
				} else {
					result = entity.Save(false);
				}
				ExecuteMapMethodQueue();
			} catch(Exception e) {
				IntegrationUtilities.Error("Method [SaveEntity] catch exception: Message = {0}", e.Message);
			}
		}
		#endregion

		#region Methods: Private
		private List<object> GetColumnValues(string entityName, string entityPath, object entityPathValue, string resultColumnName, int limit = -1) {
			var esq = new EntitySchemaQuery(UserConnection.EntitySchemaManager, entityName);
			if(limit > 0)
				esq.RowCount = limit;
			var resColumn = esq.AddColumn(resultColumnName);
			esq.Filters.Add(esq.CreateFilterWithParameters(FilterComparisonType.Equal, entityPath, entityPathValue));
			return esq.GetEntityCollection(UserConnection).Select(x => 
				x.GetColumnValue(resColumn.IsLookup ? PrepareColumn(resColumn.Name, true) : resColumn.Name)
			).ToList();
		}

		private string PrepareColumn(string columnName, bool withId = false) {
			var endWithId = columnName.EndsWith("Id");
			return withId ? (endWithId ? columnName : columnName + "Id") : (endWithId ? columnName.Substring(0, columnName.Length - 2) : columnName);
		}

		private object GetSimpleTypeValue(JToken jToken) {
			try {
				switch(jToken.Type) {
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
			} catch(Exception e) {
				IntegrationUtilities.Error("Method [GetSimpleTypeValue] catch exception: Message = {0}", e.Message);
				throw;
			}
		}

		private object GetSimpleTypeValue(object value) {
			try {
				if(value is DateTime) {
					return ((DateTime)value).ToString("yyyy-MM-dd");
				}
				
				return value; 
			} catch(Exception e) {
				IntegrationUtilities.Error("Method [GetSimpleTypeValue] catch exception: Message = {0}", e.Message);
				throw;
			}
		}

		private bool IsAllNotNullAndEmpty(params object[] values) {
			foreach(var value in values) {
				if(value == null || (value is string && string.IsNullOrEmpty(value as string)))
					return false;
			}
			return true;
		}

		private string GetFirstNotNull(params string[] strings) {
			return strings.FirstOrDefault(x => !string.IsNullOrEmpty(x));
		}

		private JToken GetJTokenByPath(JToken jToken, string path) {
			var pItems = path.Split('.');
			foreach(var pItem in pItems) {
				if(jToken[pItem] == null) {
					jToken[pItem] = new JObject();
				}
				jToken = jToken[pItem];
			}
			return jToken;
		}

		private void ExecuteMapMethodQueue() {
			while(MethodQueue.Any()) {
				var method = MethodQueue.Dequeue();
				method();
			}
		}
		#endregion
	}
	#endregion

	#region Enum: TMapType
	public enum TMapType {
		RefToGuid = 0,
		Simple = 1,
		StringToDictionaryGuid = 2,
		CompositObject = 3,
		ArrayOfCompositObject = 4,
		Const = 5,
		ArrayOfReference = 6
	}
	#endregion

	#region Enum: TMapExecuteType
	public enum TMapExecuteType {
		AfterEntitySave = 0,
		BeforeEntitySave = 1
	}
	#endregion

	#region Enum: TConstType
	public enum TConstType {
		String = 0,
		Bool = 1,
		Int = 2,
		Null = 3,
		EmptyArray = 4
	}
	#endregion

	#region Class: MappingItem
	public class MappingItem {
		
		#region Properties: Public
		public string TsSourcePath { get; set; }
		public string TsSourceName { get; set; }

		public string JSourceName { get; set; }
		public string JSourcePath { get; set; }

		public string TsDestinationPath { get; set; }
		public string TsDestinationName { get; set; }
		public string TsDetinationValueType { get;set; }
		public string TsDestinationResPath {get;set;}

		public TMapType MapType { get; set; }
		public TMapExecuteType MapExecuteType { get; set; }
		public TIntegrationType MapIntegrationType {get;set;}
		public bool IFieldRequier { get; set; }
		public bool EFieldRequier { get; set; }

		public string TsExternalIdPath { get; set; }

		public object ConstValue {get;set;}
		public TConstType ConstType {get;set;}

		public bool IgnoreError {get;set;}

		public string HandlerName {get;set;}
		#endregion

		#region Constructor: MappingItem
		public MappingItem() {

		}
		#endregion

		#region Methods: Public
		public override string ToString() {
			return string.Format("MappingItem: Path = {0} DecPath = {1} EntityName = {2} Type = {3} JObjType = {4}", TsSourcePath ?? "null", TsDestinationPath ?? "null", TsSourceName ?? "null", MapType.ToString() ?? "null", JSourceName ?? "null");
		}
		#endregion
	}
	#endregion
}