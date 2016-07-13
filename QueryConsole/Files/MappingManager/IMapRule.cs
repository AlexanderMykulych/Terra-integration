using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terrasoft.Core.Entities;

namespace QueryConsole.Files.MappingManager
{
	public interface IMappRule
	{
		string Type { get; set; }
		Entity Entity { get; set; }
		JObject Json { get; set; }
		void Import(RuleImportInfo info);
		void Export(RuleExportInfo info);
	}
}
