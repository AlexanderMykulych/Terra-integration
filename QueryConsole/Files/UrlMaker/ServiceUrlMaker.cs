using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terrasoft.TsConfiguration;

namespace QueryConsole.Files
{
	public class ServiceUrlMaker
	{
		public Dictionary<TServiceObject, string> baseUrls;
		public ServiceUrlMaker(Dictionary<TServiceObject, string> baseUrls)
		{
			this.baseUrls = baseUrls;
		}
		public string Make(TServiceObject type, string objectName, string objectId, string filters, TRequstMethod method, string limit, string skip)
		{
			string resultUrl = baseUrls[type];
			resultUrl += "/" + objectName;
			if ((method == TRequstMethod.PUT || method == TRequstMethod.DELETE) && !string.IsNullOrEmpty(objectId))
			{
				resultUrl += "/" + objectId;
				return resultUrl;
			}
			if(!string.IsNullOrEmpty(limit)) {
				resultUrl += (resultUrl.IndexOf("?") > -1 ? "&" : "?") + "limit=" + limit;
			}
			if (!string.IsNullOrEmpty(skip) && int.Parse(skip) > 0)
			{
				resultUrl += (resultUrl.IndexOf("?") > -1 ? "&" : "?") + "skip=" + skip;
			}
			if (!string.IsNullOrEmpty(filters))
			{
				resultUrl += "?" + filters;
			}
			return resultUrl;
		}
		public string Make(ServiceRequestInfo info)
		{
			return Make(info.Type, info.ServiceObjectName, info.ServiceObjectId, info.Filters, info.Method, info.Limit, info.Skip); ;
		}
	}
}
