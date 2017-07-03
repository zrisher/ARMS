using System;
using System.Reflection;
using VRage.Game;

namespace SEPC.Extensions
{
	class MyFinalBuildConstantsExtensions
	{
		public static bool GetBool(string fieldName)
		{
			FieldInfo field = typeof(MyFinalBuildConstants).GetField(fieldName);
			if (field == null)
				throw new NullReferenceException("MyFinalBuildConstants does not have a field named " + fieldName + " or it has unexpected binding");
			return (bool)field.GetValue(null);
		}
	}
}
