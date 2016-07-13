using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terrasoft.TsConfiguration;

namespace QueryConsole.Files.MappingManager.MappRule
{
	public class ArrayOfReferenceMappRule: BaseMappRule
	{
		public ArrayOfReferenceMappRule()
		{
			_type = "arrayofreference";
		}
		public override void Import(RuleImportInfo info)
		{
			throw new NotImplementedException();
		}
		public override void Export(RuleExportInfo info)
		{
			if (JsonEntityHelper.IsAllNotNullAndEmpty(info.entity, info.config.TsDestinationName, info.config.TsSourcePath, info.config.JSourceName))
			{
				var srcValue = info.entity.GetTypedColumnValue<Guid>(info.config.TsSourcePath);
				var jArray = new JArray();
				var resultList = JsonEntityHelper.GetColumnValues(info.userConnection, info.config.TsDestinationName, info.config.TsDestinationPath, srcValue, info.config.TsDestinationResPath);
				foreach (var resultItem in resultList)
				{
					var extId = int.Parse(resultItem.ToString());
					if (extId != 0)
					{
						jArray.Add(JToken.FromObject(CsReference.Create(extId, info.config.JSourceName)));
					}
				}
				info.json = jArray;
			}
			else
			{
				info.json = null;
			}
		}
	}
}
