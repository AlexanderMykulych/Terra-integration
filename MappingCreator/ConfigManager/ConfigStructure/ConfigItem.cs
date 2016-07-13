using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terrasoft.TsConfiguration;

namespace MappingCreator.Structure {
	public class ConfigItem {
		public static string Name = @"configItem";
		public List<MappingItem> MappingItems;
		public string JName {
			get;
			set;
		}
		public string TsName {
			get;
			set;
		}
	}
}
