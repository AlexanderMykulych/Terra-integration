using QueryConsole.Files;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terrasoft.Core;

namespace Terrasoft.CsConfiguration
{
	public class ClientServiceIntegrator : BaseServiceIntegrator
	{
		public ClientServiceIntegrator(UserConnection userConnection)
			: base(userConnection)
		{
			baseUrls = new Dictionary<TServiceObject, string>() {
				{ TServiceObject.Dict, "http://api.client-service.stage2.laximo.ru/v2/dict/AUTO3N" },
				{ TServiceObject.Entity, "http://api.client-service.stage2.laximo.ru/v2/entity/AUTO3N" }
			};
			integratorHelper = new Terrasoft.TsConfiguration.IntegratorHelper();
			UrlMaker = new ServiceUrlMaker(baseUrls);
			ServiceName = "ClientService";
		}
	}
}
