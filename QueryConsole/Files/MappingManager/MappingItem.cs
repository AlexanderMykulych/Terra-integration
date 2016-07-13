using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TIntegrationType = QueryConsole.Files.Constants.CsConstant.TIntegrationType;

namespace Terrasoft.TsConfiguration
{
	public class MappingItem
	{

		#region Properties: Public
		public string TsSourcePath { get; set; }
		public string TsSourceName { get; set; }

		public string JSourceName { get; set; }
		public string JSourcePath { get; set; }

		public string TsDestinationPath { get; set; }
		public string TsDestinationName { get; set; }
		public string TsDetinationValueType { get; set; }
		public string TsDestinationResPath { get; set; }

		public TMapType MapType { get; set; }
		public TMapExecuteType MapExecuteType { get; set; }
		public TIntegrationType MapIntegrationType { get; set; }
		public bool IFieldRequier { get; set; }
		public bool EFieldRequier { get; set; }

		public string TsExternalIdPath { get; set; }

		public object ConstValue { get; set; }
		public TConstType ConstType { get; set; }

		public bool IgnoreError { get; set; }
		public bool SaveOnResponse { get; set; }

		public string OrderColumn { get; set; }
		public Common.OrderDirection OrderType { get; set; }

		public string HandlerName { get; set; }

		public string MacrosName { get; set; }

		public string TsExternalSource {get;set;}
		public string TsExternalPath {get;set;}
		public string TsDestinationPathToSource {get;set;}
		public string TsDestinationPathToExternal {get;set;}
		public bool SerializeIfNull {get;set;}
		public bool SerializeIfZero {get;set;}

		public bool Key {get;set;}
		#endregion

		#region Constructor: MappingItem
		public MappingItem()
		{

		}
		#endregion

		#region Methods: Public
		public override string ToString()
		{
			return string.Format("MappingItem: Path = {0} DecPath = {1} EntityName = {2} Type = {3} JObjType = {4}", TsSourcePath ?? "null", TsDestinationPath ?? "null", TsSourceName ?? "null", MapType.ToString() ?? "null", JSourceName ?? "null");
		}
		#endregion
	}
}
