using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Terrasoft.TsConfiguration
{
	[AttributeUsage(AttributeTargets.Property | AttributeTargets.Class | AttributeTargets.Method)]
	public class IntegrationHandlerAttribute : System.Attribute
	{
		private string entityName;
		public string EntityName
		{
			get { return entityName; }
		}
		public IntegrationHandlerAttribute(string entityName)
		{
			this.entityName = entityName;
		}
	}

	[AttributeUsage(AttributeTargets.Property | AttributeTargets.Class | AttributeTargets.Method, AllowMultiple=true)]
	public class ImportHandlerAttribute : IntegrationHandlerAttribute
	{
		public ImportHandlerAttribute(string entityName)
			: base(entityName)
		{
		}
	}

	[AttributeUsage(AttributeTargets.Property | AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
	public class ExportHandlerAttribute : IntegrationHandlerAttribute
	{
		public ExportHandlerAttribute(string entityName)
			: base(entityName)
		{
		}
	}
}
