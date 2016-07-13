using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QueryConsole.Files.MappingManager.MappRule
{
	public class ComplexFieldMappRule: BaseMappRule
	{
		public ComplexFieldMappRule()
		{
			_type = "firstdestinationfield";
		}
		public override void Import(RuleImportInfo info)
		{
			object resultId = null;
			if (info.json != null)
			{
				var newValue = JsonEntityHelper.GetSimpleTypeValue(info.json);
				resultId = JsonEntityHelper.GetColumnValues(info.userConnection, info.config.TsDestinationName, info.config.TsDestinationResPath, newValue, info.config.TsDestinationPath, 1).FirstOrDefault();
			}
			info.entity.SetColumnValue(info.config.TsSourcePath, resultId);
		}
		public override void Export(RuleExportInfo info)
		{
			object resultObject = null;
			if (JsonEntityHelper.IsAllNotNullAndEmpty(info.entity, info.config.TsDestinationName, info.config.TsSourcePath, info.config.TsDestinationPath, info.config.TsDestinationResPath))
			{
				var sourceValue = info.entity.GetColumnValue(info.config.TsSourcePath);
				resultObject = JsonEntityHelper.GetColumnValues(info.userConnection, info.config.TsDestinationName, info.config.TsDestinationPath, sourceValue, info.config.TsDestinationResPath).FirstOrDefault();
			}
			info.json = resultObject != null ? JToken.FromObject(resultObject) : null;
		}
	}
}
