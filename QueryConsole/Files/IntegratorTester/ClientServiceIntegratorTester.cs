using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terrasoft.Core;
using Terrasoft.Core.Entities;
using Terrasoft.CsConfiguration;

namespace QueryConsole.Files.IntegratorTester
{
	class ClientServiceIntegratorTester : BaseIntegratorTester
	{
		public ClientServiceIntegratorTester(UserConnection userConnection)
			: base(userConnection)
		{

		}
		public override BaseServiceIntegrator CreateIntegrator()
		{
			return new ClientServiceIntegrator(UserConnection);
		}
		public override List<string> InitEntitiesName() {
			return new List<string>() {
				"Account",
				//"Contact",
				//"TsAutomobile",
				//"SysAdminUnit",
				//"Case",
				//"Relationship",
				//"ContactCareer",
				//"TsContactNotifications",
				//"TsAccountNotification",
				//"ContactAddress",
				//"TsAutoTechService",
				//"TsAutoOwnerHistory",
				//"TsAutoOwnerInfo",
				//"TsAutoTechHistory",
				//"TsLocSalMarket"
			};
		}
		public override void ImportBpmEntity(Entity entity)
		{
			Integrator.IntegrateBpmEntity(entity);
		}
		public override List<string> InitServiceEntitiesName()
		{
			return new List<string>() {
				"CompanyProfile",
				"PersonProfile",
				"ContactRecord",
				"VehicleProfile",
				"Manager",
				"ManagerGroup",
				"ClientRequest",
				"Relationship",
				"Employee",
				"NotificationProfile",
				"VehicleRelationship",
				"Market"
			};
		}
	}
}
