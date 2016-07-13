using MappingCreator.Terrasoft;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MappingCreator
{
	static class Program
	{
		/// <summary>
		/// The main entry point for the application.
		/// </summary>
		[STAThread]
		static void Main()
		{
			Application.EnableVisualStyles();
			Application.SetCompatibleTextRenderingDefault(false);
			//GlobalConnector.Connector = new TerrasoftConsoleClass("A.Mykulych");
			//GlobalConnector.Connector.Run();
			Application.Run(new Form1());
		}
	}
}
