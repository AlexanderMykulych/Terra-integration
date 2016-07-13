using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace MappingCreator.ObjectExtensions {
	public static class ObjectExtension {
		public static object CloneObject(this object objSource) {
			Type typeSource = objSource.GetType();
			object objTarget = Activator.CreateInstance(typeSource);

			PropertyInfo[] propertyInfo = typeSource.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

			foreach (PropertyInfo property in propertyInfo) {
				if (property.CanWrite) {
					if (property.PropertyType.IsValueType || property.PropertyType.IsEnum || property.PropertyType.Equals(typeof(System.String))) {
						property.SetValue(objTarget, property.GetValue(objSource, null), null);
					}
					else {
						object objPropertyValue = property.GetValue(objSource, null);
						if (objPropertyValue == null) {
							property.SetValue(objTarget, null, null);
						} else {
							property.SetValue(objTarget, objPropertyValue.CloneObject(), null);
						}
					}
				}
			}
			return objTarget;
		}
	}
}
