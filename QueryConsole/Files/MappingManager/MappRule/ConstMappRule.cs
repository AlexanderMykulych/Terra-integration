using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terrasoft.TsConfiguration;

namespace QueryConsole.Files.MappingManager.MappRule
{
	public class ConstMappRule: BaseMappRule
	{
		public ConstMappRule()
		{
			_type = "const";
		}
		public override void Import(RuleImportInfo info)
		{
			//throw new NotImplementedException();
		}
		public override void Export(RuleExportInfo info)
		{
			object resultValue = null;
			switch (info.config.ConstType)
			{
				case TConstType.String:
					resultValue = info.config.ConstValue;
					break;
				case TConstType.Bool:
					resultValue = Convert.ToBoolean(info.config.ConstValue.ToString());
					break;
				case TConstType.Int:
					resultValue = int.Parse(info.config.ConstValue.ToString());
					break;
				case TConstType.Null:
					resultValue = null;
					break;
				case TConstType.EmptyArray:
					resultValue = new ArrayList();
					break;
			}
			info.json = resultValue != null ? JToken.FromObject(resultValue) : null;
		}
	}
}
