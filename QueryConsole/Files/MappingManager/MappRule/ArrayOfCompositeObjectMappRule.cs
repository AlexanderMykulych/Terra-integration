using Newtonsoft.Json.Linq;
using QueryConsole.Files.BpmEntityHelper;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IntegrationInfo = QueryConsole.Files.Constants.CsConstant.IntegrationInfo;
using Terrasoft.TsConfiguration;

namespace QueryConsole.Files.MappingManager.MappRule
{
	public class ArrayOfCompositeObjectMappRule: BaseMappRule
	{
		public ArrayOfCompositeObjectMappRule()
		{
			_type = "arrayofcompositobject";
		}
		public override void Import(RuleImportInfo info)
		{
			if (info.json is JArray)
			{
				var jArray = (JArray)info.json;
				foreach (JToken jArrayItem in jArray)
				{
					JObject jObj = jArrayItem as JObject;
					var integrator = new IntegrationEntityHelper();
					var objIntegrInfo = new IntegrationInfo(jObj, info.userConnection, info.integrationType, null, jObj.Properties().First().Name, info.action);
					integrator.IntegrateEntity(objIntegrInfo);
				}
			}
		}
		public override void Export(RuleExportInfo info)
		{
			if (JsonEntityHelper.IsAllNotNullAndEmpty(info.entity, info.config.TsSourcePath, info.config.TsDestinationPath, info.config.TsDestinationName))
			{
				var srcEntity = info.entity;
				var dscValue = srcEntity.GetColumnValue(info.config.TsSourcePath);
				string handlerName = JsonEntityHelper.GetFirstNotNull(info.config.HandlerName, info.config.TsDestinationName, info.config.JSourceName);
				var resultJObjs = JsonEntityHelper.GetCompositeJObjects(dscValue, info.config.TsDestinationPath, info.config.TsDestinationName, handlerName, info.userConnection);
				if(resultJObjs.Any()) {
					var jArray = (info.json = new JArray()) as JArray;
					resultJObjs.ForEach(x => jArray.Add(x));
				} else {
					info.json = null;
				}
			}
		}
	}
}
