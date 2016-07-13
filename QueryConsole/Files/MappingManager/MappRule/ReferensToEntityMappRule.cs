using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terrasoft.TsConfiguration;

namespace QueryConsole.Files.MappingManager.MappRule
{
	public class ReferensToEntityMappRule: BaseMappRule
	{
		public ReferensToEntityMappRule()
		{
			_type = "reftoguid";
		}
		public override void Import(RuleImportInfo info)
		{
			Guid? resultGuid = null;
			if (info.json != null && info.json.HasValues)
			{
				var refColumns = info.json[JsonEntityHelper.RefName];
				var externalId = int.Parse(refColumns["id"].ToString());
				var type = refColumns["type"];
				resultGuid = JsonEntityHelper.GetColumnValues(info.userConnection, info.config.TsDestinationName, info.config.TsExternalIdPath, externalId, info.config.TsDestinationPath).FirstOrDefault() as Guid?;
			}
			info.entity.SetColumnValue(info.config.TsSourcePath, resultGuid);
		}
		public override void Export(RuleExportInfo info)
		{
			object resultObj = null;
			if (JsonEntityHelper.IsAllNotNullAndEmpty(info.entity, info.config.TsDestinationName, info.config.TsSourcePath, info.config.JSourceName, info.config.TsDestinationPath))
			{
				var resultValue = JsonEntityHelper.GetColumnValues(info.userConnection, info.config.TsDestinationName, info.config.TsDestinationPath, info.entity.GetTypedColumnValue<Guid>(info.config.TsSourcePath), info.config.TsExternalIdPath).FirstOrDefault(x => (int)x > 0);
				if (resultValue != null)
				{
					var resultRef = CsReference.Create(int.Parse(resultValue.ToString()), info.config.JSourceName);
					resultObj = resultRef != null ? JToken.FromObject(resultRef) : null;
				}
			}
			info.json = resultObj as JToken;
		}
	}
}
