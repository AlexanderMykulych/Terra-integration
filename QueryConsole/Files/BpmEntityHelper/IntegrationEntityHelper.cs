using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terrasoft.Core.Entities;
using Terrasoft.TsConfiguration;
using IntegrationInfo = QueryConsole.Files.Constants.CsConstant.IntegrationInfo;
using TIntegrationType = QueryConsole.Files.Constants.CsConstant.TIntegrationType;
using CsConstant = QueryConsole.Files.Constants.CsConstant;
using QueryConsole.Files.Integrators;
using Terrasoft.Core.DB;
using System.Data;

namespace QueryConsole.Files.BpmEntityHelper
{
	public class IntegrationEntityHelper
	{
		#region Properties: Private
		private static List<Type> IntegrationEntityTypes { get; set; }
		private static Dictionary<Type, IIntegrationEntityHandler> EntityHandlers { get; set; }
		#endregion

		#region Constructor: Public
		public IntegrationEntityHelper()
		{
			EntityHandlers = new Dictionary<Type, IIntegrationEntityHandler>();
		}
		#endregion

		#region Methods: Public
		/// <summary>
		/// Експортирует или импортирует объекты в зависимости от настроек
		/// </summary>
		/// <param name="integrationInfo">Настройки интеграции</param>
		public void IntegrateEntity(IntegrationInfo integrationInfo)
		{
			ExecuteHandlerMethod(integrationInfo, GetIntegrationHandler(integrationInfo));
		}

		/// <summary>
		/// В зависимости от типа интеграции возвращает соответсвенный атрибут
		/// </summary>
		/// <param name="integrationInfo">Настройки интеграции</param>
		/// <returns></returns>
		public Type GetAttributeType(IntegrationInfo integrationInfo)
		{
			switch (integrationInfo.IntegrationType)
			{
				case TIntegrationType.Import:
					return typeof(ImportHandlerAttribute);
				case TIntegrationType.Export:
				case TIntegrationType.ExportResponseProcess:
					return typeof(ExportHandlerAttribute);
				default:
					return typeof(ExportHandlerAttribute);
			}
		}

		/// <summary>
		/// Возвращает все классы помеченые атрибутами интеграции которые розмещены в пространстве имен Terrasoft.Configuration
		/// </summary>
		/// <param name="integrationInfo">Настройки интеграции</param>
		/// <returns></returns>
		public List<Type> GetIntegrationTypes(IntegrationInfo integrationInfo)
		{
			if (IntegrationEntityTypes != null && IntegrationEntityTypes.Any())
			{
				return IntegrationEntityTypes;
			}
			var attributeType = GetAttributeType(integrationInfo);
			var assembly = typeof(IntegrationServiceIntegrator).Assembly;
			return IntegrationEntityTypes = assembly.GetTypes().Where(x =>
			{
				var attributes = x.GetCustomAttributes(attributeType, true);
				return attributes != null && attributes.Length > 0;
			}).ToList();
		}

		/// <summary>
		/// Возвращает объект который отвечает за интеграцию конкретной сущности
		/// </summary>
		/// <param name="integrationInfo">Настройки интеграции</param>
		/// <returns></returns>
		public IIntegrationEntityHandler GetIntegrationHandler(IntegrationInfo integrationInfo)
		{
			var attributeType = GetAttributeType(integrationInfo);
			var types = GetIntegrationTypes(integrationInfo);
			foreach (var type in types)
			{
				var attributes = type.GetCustomAttributes(attributeType, true);
				foreach(IntegrationHandlerAttribute attribute in attributes) {
					if (attribute != null && attribute.EntityName == integrationInfo.EntityName)
					{
						if (EntityHandlers.ContainsKey(type))
						{
							return EntityHandlers[type];
						}
						var entityHandler = Activator.CreateInstance(type) as IIntegrationEntityHandler;
						EntityHandlers.Add(type, entityHandler);
						return entityHandler;
					}
				}
			}
			return null;
		}

		/// <summary>
		/// В зависимости от настройки интеграции, выполняет соответсвенный метод объкта, который отвечает за интеграцию конкретной сущности
		/// </summary>
		/// <param name="integrationInfo">Настройки интеграции</param>
		/// <param name="handler">объект, который отвечает за интеграцию конкретной сущности</param>
		public void ExecuteHandlerMethod(IntegrationInfo integrationInfo, IIntegrationEntityHandler handler)
		{
			if (handler != null)
			{
				try
				{
					if (integrationInfo.IntegrationType == TIntegrationType.Export)
					{
						var result = new CsConstant.IntegrationResult(CsConstant.IntegrationResult.TResultType.Success, handler.ToJson(integrationInfo));
						integrationInfo.Result = result;
						return;
					}
					else if (integrationInfo.IntegrationType == TIntegrationType.ExportResponseProcess)
					{
						handler.ProcessResponse(integrationInfo);
						return;
					}
					if (integrationInfo.Action == CsConstant.IntegrationActionName.Create)
					{
						if (!handler.IsEntityAlreadyExist(integrationInfo))
						{
							handler.Create(integrationInfo);
						}
						else
						{
							var result = new CsConstant.IntegrationResult(CsConstant.IntegrationResult.TResultException.OnCreateEntityExist);
							integrationInfo.Result = result;
							return;
						}
					}
					else if (integrationInfo.Action == CsConstant.IntegrationActionName.Update)
					{
						if (handler.IsEntityAlreadyExist(integrationInfo))
						{
							handler.Update(integrationInfo);
						}
						else
						{
							handler.Create(integrationInfo);
						}
					}
					else if (integrationInfo.Action == CsConstant.IntegrationActionName.Delete)
					{
						handler.Delete(integrationInfo);
					}
					else
					{
						handler.Unknown(integrationInfo);
					}
				}
				catch (Exception e)
				{
					//IntegrationLogger.Error("Method [ReportEntity] class entity handler throw exception: entityName = {0} id = {1} message = {2}", integrationInfo.EntityName, integrationInfo.EntityIdentifier, e.Message);
				}
			}
		}
		#endregion

		public static bool isEntityAlreadyIntegrated(Entity entity)
		{
			return entity.IsColumnValueLoaded("TsExternalId") && entity.GetTypedColumnValue<int>("TsExternalId") != 0;
		}
	}
}
