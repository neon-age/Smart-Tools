using System;
using System.Reflection;
using Unity.Scripting.LifecycleManagement;
using UnityEditor;
using UnityEngine;

namespace Neonage.SmartTools
{
    [NoAutoStaticsCleanup]
    public static class SerializedPropertyUtil
    {
        static MethodInfo R_GetFieldInfoFromProperty;
        static object[] R_Args2 = new object[2];

        public static T GetAttribute<T>(SerializedProperty property) where T : Attribute
        {
            var fieldInfo = GetFieldInfo(property);
            if (fieldInfo == null)
                return null;
            return fieldInfo.GetCustomAttribute<T>();
        }

        public static FieldInfo GetFieldInfo(SerializedProperty property)
        {
            if (R_GetFieldInfoFromProperty == null)
            {
                var utilityType = typeof(Editor).Assembly.GetType("UnityEditor.ScriptAttributeUtility");
                R_GetFieldInfoFromProperty = utilityType.GetMethod("GetFieldInfoFromProperty", BindingFlags.Static | BindingFlags.NonPublic);
            }
            R_Args2[0] = property;
            R_Args2[1] = null;
            var fieldInfo = R_GetFieldInfoFromProperty.Invoke(null, R_Args2) as FieldInfo;
            //var fieldType = (Type)R_Args2[1];
            return fieldInfo;
        }
    }
}