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
using System.Web.Configuration;


namespace Terrasoft.TsConfiguration
{
	using IntegrationInfo = QueryConsole.Files.Constants.CsConstant.IntegrationInfo;
	using TIntegrationType = QueryConsole.Files.Constants.CsConstant.TIntegrationType;
	using System.Configuration;
	using System.Collections;
	using System.Threading.Tasks;
	using QueryConsole.Files.BpmEntityHelper;
	using QueryConsole.Files.Constants;


	#region Class: ClientServiceIntegrator
	
	#endregion

	#region Static Class: ExtensionHelper
	public static class ExtensionHelper {
		#region Methods: Public
		/// <summary>
		/// Сериализирует объект
		/// </summary>
		/// <param name="obj">Объект</param>
		/// <returns></returns>
		public static string SerializeToJson(this object obj) {
			return Newtonsoft.Json.JsonConvert.SerializeObject(obj).Replace("ReferenceClientService", "#ref");
		}

		/// <summary>
		/// Десериализирует объект
		/// </summary>
		/// <param name="json">json текс</param>
		/// <returns></returns>
		public static Dictionary<string, object> DeserializeJson(this string json) {
			return Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, object>>(json);
		}
		#endregion
	}
	#endregion

	#region Interface: IMappingMethod
	public interface IMappingMethod {
		//TODO: Вынести методы маппера в отдельные сущности
		void Evaluate(MappingItem mappItem, IntegrationInfo integrationInfo);
	}
	#endregion

	#region Class: MappingMethodAttribute
	public class MappingMethodAttribute : System.Attribute {
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
		/// <summary>
		/// Возвращает xml документ c настройками маппинга
		/// </summary>
		/// <param name="userConnection"></param>
		/// <returns></returns>
		private static XmlDocument GetConfigXmlDocument(UserConnection userConnection) {
			try {
				if(_xDocument != null)
					return _xDocument;

				string confLocation = System.Configuration.ConfigurationManager.AppSettings["XmlConfigurationLocation"] ?? "db";
				if(confLocation == "db") {
					if(string.IsNullOrEmpty(_xmlData)) {
						_xmlData = Terrasoft.Core.Configuration.SysSettings.GetValue(userConnection, CsConstant.SysSettingsCode.ConfigurationData, "<?xml version=\"1.0\" encoding=\"utf-8\"?>");
					}
				} else if(confLocation == "file") {
					if(string.IsNullOrEmpty(_xmlData)) {
						string confPath = System.Configuration.ConfigurationManager.AppSettings["XmlConfigurationFilePath"] ?? "IntegrationConfig.xml";
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
				//IntegrationLogger.Error("Method [GetConfigXmlDocument] throw exception: Message = {0}", e.Message);
				throw;
			}
		}

		/// <summary>
		/// Возвращает ноду с именем name, документа doc
		/// </summary>
		/// <param name="doc">Документ</param>
		/// <param name="name">Имя ноды</param>
		/// <returns></returns>
		private static XmlNode GetXmlNodeByNameAttr(XmlDocument doc, string name, string tag = null) {
			try {
				foreach(XmlNode node in doc.DocumentElement) {
					if(node is XmlComment)
						continue;
					if(
						(
							(node.Attributes["TsName"] != null && node.Attributes["TsName"].Value == name) ||
							(node.Attributes["JName"] != null && node.Attributes["JName"].Value == name)
						) &&
						(
							string.IsNullOrEmpty(tag) || (node.Attributes["Tag"] != null && node.Attributes["Tag"].Value == tag)
						)
					) {
						return node;
					}
				}
				return null;
			} catch(Exception e) {
				//IntegrationLogger.Error("Method [GetXmlNodeByNameAttr] throw exception: Message = {0}", e.Message);
				throw;
			}
		}

		/// <summary>
		/// Возвращает елемент маппинга по ноде из документа конфигурации
		/// </summary>
		/// <param name="userConnection"></param>
		/// <param name="node">Нода</param>
		/// <param name="defItem">Елемент маппинга по умолчанию. Если в ноде не будет какого-то поля, то подставится поле с этого объекта</param>
		/// <returns></returns>
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
				//IntegrationLogger.Error("Method [GetItemByXmlNode] throw exception: Message = {0}", e.Message);
				throw;
			}
		}

		/// <summary>
		/// Возращает конфиг пререндеринга prerenderConfig
		/// </summary>
		/// <param name="userConnection"></param>
		/// <returns></returns>
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
				//IntegrationLogger.Error("Method [GetPrerenderConfig] throw exception: Message = {0}", e.Message);
				throw;
			}
		}

		/// <summary>
		/// Подготавливает конфигурацию маппинга с помощю конфигурации prerenderConfig.
		/// </summary>
		/// <param name="userConnection"></param>
		/// <param name="value"></param>
		/// <returns></returns>
		private static string PrepareValue(UserConnection userConnection, string value) {
			try {
				var configDict = GetPrerenderConfig(userConnection);
				if(configDict.ContainsKey(value))
					return configDict[value];
				return value;
			} catch(Exception e) {
				//IntegrationLogger.Error("Method [PrepareValue] throw exception: Message = {0}", e.Message);
				throw;
			}
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="userConnection"></param>
		/// <returns></returns>
		private static MappingItem GetDefaultItem(UserConnection userConnection) {
			try {
				return _defaultItem == null ? _defaultItem = GetItemByXmlNode(userConnection, GetXmlNodeByNameAttr(GetConfigXmlDocument(userConnection), "Default").ChildNodes[0]) : _defaultItem;
			} catch(Exception e) {
				//IntegrationLogger.Error("Method [GetDefaultItem] throw exception: Message = {0}", e.Message);
				throw;
			}
		}

		private static MappingItem GetDafaultItemByType(UserConnection userConnection, string type, MappingItem defaultItem = null) {
			try
			{
				var document = GetConfigXmlDocument(userConnection);
				foreach(XmlNode node in document.DocumentElement) {
					if(node.Name == "configItemType") {
						foreach(XmlNode childNode in node.ChildNodes) {
							if (childNode.Attributes["MapType"] != null && childNode.Attributes["MapType"].Value == type)
							{
								return GetItemByXmlNode(userConnection, childNode, defaultItem);
							}
						}
					} 
				}
				return defaultItem;
			}
			catch (Exception e)
			{
				//IntegrationLogger.Error("Method [GetDafaultItemByType] throw exception: Message = {0}", e.Message);
				throw;
			}
		}

		#endregion

		#region Methods: Public
		public static List<MappingItem> GetConfigItem(UserConnection userConnection, string Name, string tag = null) {
			try {
				var result = new List<MappingItem>();
				XmlDocument xDoc = GetConfigXmlDocument(userConnection);
				var node = GetXmlNodeByNameAttr(xDoc, Name, tag);
				
				var defItem = GetDefaultItem(userConnection);
				foreach(XmlNode mapItem in node.ChildNodes) {
					if(mapItem is XmlElement) {
						string type = null;
						if (mapItem.Attributes["MapType"] != null)
						{
							type = mapItem.Attributes["MapType"].Value;
						} else {
							type = defItem.MapType.ToString();
						}
						var typeDefItem = GetDafaultItemByType(userConnection, type, defItem);
						result.Add(GetItemByXmlNode(userConnection, mapItem, typeDefItem));
					}
				}
				return result;
			} catch(Exception e) {
				//IntegrationLogger.Error("Method [GetConfigItem] throw exception: Message = {0}", e.Message);
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
}