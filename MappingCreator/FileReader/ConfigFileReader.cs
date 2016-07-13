using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terrasoft.TsConfiguration;

namespace MappingCreator.FileReader
{
	public class ConfigFileReader
	{
		public string ReadFile(string path) {
			try {
				using(var stream = new StreamReader(new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))) {
					return stream.ReadToEnd();
				}
			} catch(Exception e) {
				throw e;
			}
		}
	}
}
