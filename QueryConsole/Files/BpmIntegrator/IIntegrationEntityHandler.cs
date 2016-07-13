using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IntegrationInfo = QueryConsole.Files.Constants.CsConstant.IntegrationInfo;

namespace Terrasoft.TsConfiguration
{
	public interface IIntegrationEntityHandler
	{
		void Create(IntegrationInfo integrationInfo);
		void Update(IntegrationInfo integrationInfo);
		void Delete(IntegrationInfo integrationInfo);
		void Unknown(IntegrationInfo integrationInfo);
		void ProcessResponse(IntegrationInfo integrationInfo);
		JObject ToJson(IntegrationInfo integrationInfo);
		bool IsEntityAlreadyExist(IntegrationInfo integrationInfo);
	}
}
