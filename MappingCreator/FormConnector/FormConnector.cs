using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MappingCreator.FormConnector {
	public class FormConnector {
		private ConfigManager _manager;
		private List<Tuple<int, string, string>> EntityNames;
		private int SelectedIndex;
		public void LoadManager(ConfigManager manager) {
			_manager = manager;
		}

		public List<Tuple<int, string, string>> GenerateEntityList() {
			EntityNames = _manager._mappingConfiguration.ConfigItems.Select((x, y) => new Tuple<int, string, string>(y, x.TsName, x.JName)).ToList();
			return EntityNames;
		}

		public void SetSelectedMappingItem(int index) {
			SelectedIndex = index;
		}
	}
}
