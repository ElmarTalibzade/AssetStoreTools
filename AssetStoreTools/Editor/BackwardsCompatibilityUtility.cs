using System;
using System.Collections.Generic;
using System.Reflection;

public class BackwardsCompatibilityUtility
{
    public BackwardsCompatibilityUtility()
    {
    }

    public static MethodInfo GetMethodInfo(List<string> methods, Type[] parametersType = null)
    {
        MethodInfo methodInfo = null;
        foreach (string method in methods)
        {
            string[] strArrays = method.Split(new char[] {'.'});
            Assembly assembly = Assembly.Load(strArrays[0]);
            string str = string.Format("{0}.{1}", strArrays[0], strArrays[1]);
            string str1 = strArrays[2];
            Type type = assembly.GetType(str);
            if (type != null)
            {
                methodInfo = (parametersType != null ? type.GetMethod(str1, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic, null, parametersType, null) : type.GetMethod(str1, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic));
            }

            if (methodInfo == null)
            {
                continue;
            }

            break;
        }

        if (methodInfo == null)
        {
            throw new MissingMethodException(methods[0]);
        }

        return methodInfo;
    }

    public static object TryStaticInvoke(List<string> methods, object[] parameters)
    {
        return BackwardsCompatibilityUtility.GetMethodInfo(methods, null).Invoke(null, parameters);
    }
}