using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terrasoft.TsConfiguration;

namespace MappingCreator.Structure
{
	public class MappingConfiguration
	{
		public static string Name = @"MapingConfiguration";
		public PrepareConfig PrepareConfig;
		public List<ConfigItem> ConfigItems;
		public ConfigItemType ConfigItemType;
		public MappingItem DefaultItem;
	}
}
