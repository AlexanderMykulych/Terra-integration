using MappingCreator.FileReader;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using MappingCreator.FormConnector;

namespace MappingCreator
{
	public partial class Form1 : Form
	{
		private MappingCreator.FormConnector.FormConnector FormConnector;
		public Form1()
		{
			InitializeComponent();
			FormConnector = new FormConnector.FormConnector();
		}

		public void Test() {
			var reader = new ConfigFileReader();
			var text = reader.ReadFile(@"C:\VS_Project\QueryConsole\Terra-integration\QueryConsole\ConfigurationFile.xml");

			var manager = new ConfigManager(text);
		}

		private void OpenConfigFileMenuButton_Click(object sender, EventArgs e) {
			OpenFileDialog openFileDialog1 = new OpenFileDialog();

			openFileDialog1.InitialDirectory = "c:\\";
			openFileDialog1.Filter = "txt files (*.txt)|*.txt|All files (*.*)|*.*";
			openFileDialog1.FilterIndex = 2;
			openFileDialog1.RestoreDirectory = true;

			//if (openFileDialog1.ShowDialog() != DialogResult.OK) {
			//	return;
			//}
			//var path = openFileDialog1.FileName;
			var path = @"C:\VS_Project\QueryConsole\Terra-integration\QueryConsole\ConfigurationFile.xml";
			var reader = new ConfigFileReader();
			var text = reader.ReadFile(path);
			var manager = new ConfigManager(text);
			FormConnector.LoadManager(manager);
			LoadMappingEntityNames();
		}

		private void LoadMappingEntityNames() {
			var entityNames = FormConnector.GenerateEntityList();
			entityNames.ForEach(x => entityNameGridView.Rows.Add(x.Item1, x.Item2, x.Item3));
		}

		private void entityNameGridView_RowHeaderMouseClick(object sender, DataGridViewCellMouseEventArgs e) {
			var index = (sender as DataGridView).SelectedRows[0].Index;
			FormConnector.SetSelectedMappingItem(index);
		}
	}
}
