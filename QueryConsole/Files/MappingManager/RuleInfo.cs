using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terrasoft.Core;
using Terrasoft.Core.Entities;
using Terrasoft.TsConfiguration;
using TIntegrationType = QueryConsole.Files.Constants.CsConstant.TIntegrationType;

namespace QueryConsole.Files.MappingManager
{
	public class RuleInfo
	{
		public MappingItem config;
		public Entity entity;
		public JToken json;
		public UserConnection userConnection;
		public TIntegrationType integrationType;
		public string action;
	}
}
