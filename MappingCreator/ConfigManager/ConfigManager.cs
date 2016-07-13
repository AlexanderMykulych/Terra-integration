using MappingCreator.Structure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml;
using Terrasoft.TsConfiguration;

namespace MappingCreator {
	public class ConfigManager {
		private string _text;
		public MappingConfiguration _mappingConfiguration;

		public ConfigManager(string configText) {
			_text = configText;
			ProcessConfigText();
		}

		private void ProcessConfigText() {
			var xmlDocument = new XmlDocument();
			try {
				xmlDocument.LoadXml(_text);
			} catch (Exception e) {
				MessageBox.Show(e.Message);
			}
			var root = xmlDocument[MappingConfiguration.Name];
			if (root != null) {
				var config = new MappingConfiguration();
				config.PrepareConfig = new PrepareConfig() {
					ReplaceConfig = ProcessPrepareConfig(root).ToList()
				};
				var preparePredicate = config.PrepareConfig.GetPreparePredicat();
				config.DefaultItem = PrepareDefaultItem(root, preparePredicate);
				config.ConfigItemType = ProcessConfigItemType(root, config.DefaultItem, preparePredicate);
				config.ConfigItems = ProcessConfigItems(root, config.DefaultItem, preparePredicate);
				_mappingConfiguration = config;
			}
		}

		private IEnumerable<ReplaceConfig> ProcessPrepareConfig(XmlElement root) {
			var prepareRoot = root[PrepareConfig.Name];
			if (prepareRoot != null) {
				foreach (XmlNode node in prepareRoot.ChildNodes) {
					var replaceConfig = DynamicXmlParser.StartMapXmlToObj(node, typeof(ReplaceConfig)) as ReplaceConfig;
					if (replaceConfig != null) {
						yield return replaceConfig;
					}
				}
			}
		}

		private ConfigItemType ProcessConfigItemType(XmlElement root, MappingItem defItem, Func<string, string> preparePredicate = null) {
			var itemType = root[ConfigItemType.Name];
			if (itemType != null) {
				var mappingsItem = new List<MappingItem>();
				foreach (XmlNode node in itemType.ChildNodes) {
					var mappingItem = DynamicXmlParser.StartMapXmlToObj(node, typeof(MappingItem), defItem, preparePredicate) as MappingItem;
					if (mappingItem != null) {
						mappingsItem.Add(mappingItem);
					}
				}
				return new ConfigItemType() {
					MappingItems = mappingsItem
				};
			}
			return new ConfigItemType() {
				MappingItems = new List<MappingItem>()
			};
		}

		private MappingItem PrepareDefaultItem(XmlElement root, Func<string, string> preparePredicate = null) {
			var itemType = root[ConfigItem.Name];
			if (itemType != null) {
				foreach (XmlNode node in itemType.ChildNodes) {
					var mappingItem = DynamicXmlParser.StartMapXmlToObj(node, typeof(MappingItem), null, preparePredicate) as MappingItem;
					if (mappingItem != null) {
						return mappingItem;
					}
				}
			}
			return null;
		}

		private List<ConfigItem> ProcessConfigItems(XmlElement root, MappingItem defItem, Func<string, string> preparePredicate = null) {
			var resultList = new List<ConfigItem>();
			foreach(XmlNode node in root.ChildNodes) {
				if (node.Name == ConfigItem.Name && node.Attributes["TsName"].Value != "Default") {
					var item = new ConfigItem() {
						JName = node.Attributes["JName"].Value,
						TsName = node.Attributes["TsName"].Value,
						MappingItems = new List<MappingItem>()
					};
					foreach(var subNode in node.ChildNodes) {
						var mappingItem = DynamicXmlParser.StartMapXmlToObj(node, typeof(MappingItem), defItem, preparePredicate) as MappingItem;
						if(mappingItem != null) {
							item.MappingItems.Add(mappingItem);
						}
					}
					resultList.Add(item);
				}
			}
			return resultList;
		}
	}
}
