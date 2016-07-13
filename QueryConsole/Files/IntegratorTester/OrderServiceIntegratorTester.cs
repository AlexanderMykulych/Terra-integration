using QueryConsole.Files.Integrators;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terrasoft.Core;
using Terrasoft.Core.Entities;

namespace QueryConsole.Files.IntegratorTester
{
	public class OrderServiceIntegratorTester: BaseIntegratorTester
	{
		public OrderServiceIntegratorTester(UserConnection userConnection): base(userConnection) {

		}
		public override BaseServiceIntegrator CreateIntegrator()
		{
			return new OrderServiceIntegrator(UserConnection);
		}
		public override List<string> InitEntitiesName() {
			return new List<string>() {
				//"TsPayment",
				//"Order",
				//"OrderProduct",
				//"TsReturn",
				//"TsShipment",
				//"TsShipmentPosition",
				"TsContractDebt",
				"Contract"
			};
		}
		public override void ImportBpmEntity(Entity entity)
		{
			Integrator.IntegrateBpmEntity(entity);
		}
		public override List<string> InitServiceEntitiesName()
		{
			return new List<string>() {
               // "Payment",
               "Order",
                //"OrderItem",
                //"Return",
                //"Shipment",
               //Debt",
               // "Contract",
                //"ContractBalance",
                //"ManagerInfo",
               // "CounteragentContactInfo",
             // "Counteragent"
			};
		}
	}
}
