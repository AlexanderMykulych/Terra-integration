using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Terrasoft.TsConfiguration
{
	public class CsReference
	{
		public CsReferenceProperty ReferenceClientService;
		/// <summary>
		/// Cоздает объект, который обозначает ссылку (#ref) в Clientservice
		/// </summary>
		/// <param name="pid">id</param>
		/// <param name="ptype">type</param>
		/// <param name="pname">name</param>
		/// <returns></returns>
		public static CsReference Create(int pid, string ptype, string pname = "")
		{
			return pid != 0 ? new CsReference
			{
				ReferenceClientService = new CsReferenceProperty
				{
					id = pid,
					type = ptype,
					name = pname
				}
			} : (CsReference)null;
		}
	}
}
