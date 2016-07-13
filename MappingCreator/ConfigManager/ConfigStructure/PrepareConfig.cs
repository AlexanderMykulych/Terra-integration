using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MappingCreator.Structure {
	public class PrepareConfig {
		public static string Name = @"prerenderConfig";
		public List<ReplaceConfig> ReplaceConfig;

		public Func<string, string> GetPreparePredicat() {
			if(ReplaceConfig != null && ReplaceConfig.Any()) {
				Func<string, string> resultPredicate = x => {
					var result = ReplaceConfig.FirstOrDefault(y => y.From == x);
					if(result != null) {
						return result.To;
					}
					return x;
				};
				return resultPredicate;
			}
			return null;
		}
	}

	public class ReplaceConfig {
		public static string Name = @"replace";
		public string From {
			get;
			set;
		}
		public string To {
			get;
			set;
		}
	}

}
