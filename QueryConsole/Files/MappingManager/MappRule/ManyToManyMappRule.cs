using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terrasoft.Core.Entities;

namespace QueryConsole.Files.MappingManager.MappRule
{
	public class ManyToManyMappRule: BaseMappRule
	{
		public ManyToManyMappRule()
		{
			_type = "manytomany";
		}
		public override void Import(RuleImportInfo info)
		{
			if (info.json != null && info.json.HasValues)
			{
				var jArray = info.json as JArray;
				foreach (var refItem in jArray)
				{
					var item = refItem[JsonEntityHelper.RefName];
					var externalId = int.Parse(item["id"].ToString());
					var type = item["type"];
					Tuple<Dictionary<string, string>, Entity> tuple = JsonEntityHelper.GetEntityByExternalId(info.config.TsExternalSource, externalId, info.userConnection, false, info.config.TsExternalPath);
					Dictionary<string, string> columnDict = tuple.Item1;
					Entity entity = tuple.Item2;
					if(entity != null) {
						if(!JsonEntityHelper.isEntityExist(info.config.TsDestinationName, info.userConnection, new Dictionary<string,object>() {
							{ info.config.TsDestinationPathToSource, info.entity.GetTypedColumnValue<Guid>(info.config.TsSourcePath) },
							{ info.config.TsDestinationPathToExternal, entity.GetTypedColumnValue<Guid>(columnDict[info.config.TsExternalPath]) }
						})) {
							var schema = info.userConnection.EntitySchemaManager.GetInstanceByName(info.config.TsDestinationName);
							var destEntity = schema.CreateEntity(info.userConnection);
							var firstColumn = schema.Columns.GetByName(info.config.TsDestinationPathToExternal).ColumnValueName;
							var secondColumn = schema.Columns.GetByName(info.config.TsDestinationPathToSource).ColumnValueName;
							destEntity.SetColumnValue(firstColumn, entity.GetTypedColumnValue<Guid>(columnDict[info.config.TsExternalPath]));
							destEntity.SetColumnValue(secondColumn, info.entity.GetTypedColumnValue<Guid>(info.config.TsSourcePath));
							destEntity.Save(false);
						}
					}
				}
			}
		}
		public override void Export(RuleExportInfo info)
		{
			
		}
	}
}
