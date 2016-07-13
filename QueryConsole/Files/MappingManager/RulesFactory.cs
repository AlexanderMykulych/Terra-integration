using QueryConsole.Files.MappingManager.MappRule;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QueryConsole.Files.MappingManager
{
	public class RulesFactory
	{
		public List<IMappRule> Rules;
		public RulesFactory() {
			Rules = new List<IMappRule>() {
				new SimpleMappRule(),
				new ReferensToEntityMappRule(),
				new CompositMappRule(),
				new ConstMappRule(),
				new ArrayOfCompositeObjectMappRule(),
				new ArrayOfReferenceMappRule(),
				new ComplexFieldMappRule(),
				new ManyToManyMappRule()
			};
		}
		public IMappRule GetRule(string type) {
			type = type.ToLower();
			return Rules.FirstOrDefault(x => x.Type == type);
		}
	}
}
