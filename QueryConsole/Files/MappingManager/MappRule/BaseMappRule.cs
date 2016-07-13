using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terrasoft.Core.Entities;

namespace QueryConsole.Files.MappingManager.MappRule
{
	public abstract class BaseMappRule: IMappRule
	{
		protected string _type;
		public string Type
		{
			get
			{
				return _type;
			}
			set
			{
				_type = value;
			}
		}

		protected Entity _entity;
		public Entity Entity
		{
			get
			{
				return _entity;
			}
			set
			{
				_entity = value;
			}
		}

		protected JObject _json;
		public JObject Json
		{
			get
			{
				return _json;
			}
			set
			{
				_json = value;
			}
		}

		public virtual void Import(RuleImportInfo info)
		{
			throw new NotImplementedException();
		}

		public virtual void Export(RuleExportInfo info)
		{
			throw new NotImplementedException();
		}
	}
}
