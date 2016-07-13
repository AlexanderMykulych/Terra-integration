using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terrasoft.TsConfiguration;

namespace QueryConsole.Files.MappingManager.MappRule
{
	public class SimpleMappRule : BaseMappRule
	{
		public SimpleMappRule() {
			_type = "simple";
		}
		public override void Import(RuleImportInfo info)
		{
			object value = JsonEntityHelper.GetSimpleTypeValue(info.json);
			if (!string.IsNullOrEmpty(info.config.MacrosName))
			{
				value = TsMacrosHelper.GetMacrosResultImport(info.config.MacrosName, value);
			}
			info.entity.SetColumnValue(info.config.TsSourcePath, value);
		}
		public override void Export(RuleExportInfo info)
		{
			var value = info.entity.GetColumnValue(info.config.TsSourcePath);
			var simpleResult = value != null ? JsonEntityHelper.GetSimpleTypeValue(value) : null;
			if (!string.IsNullOrEmpty(info.config.MacrosName))
			{
				simpleResult = TsMacrosHelper.GetMacrosResultExport(info.config.MacrosName, simpleResult);
			}
			if(simpleResult is DateTime) {
				simpleResult = ((DateTime)simpleResult).ToString("yyyy-MM-dd");
			}
			info.json = simpleResult != null ? JToken.FromObject(simpleResult) : null;
		}
	}
}
