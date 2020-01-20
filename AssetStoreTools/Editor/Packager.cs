using System;
using System.Collections.Generic;
using System.Reflection;

internal static class Packager
{
    internal static string[] BuildExportPackageAssetListGuids(string[] guids, bool dependencies)
    {
        List<string> strs = new List<string>()
        {
            "UnityEditor.PackageUtility.BuildExportPackageItemsList",
            "UnityEditor.AssetServer.BuildExportPackageAssetListAssetsItems"
        };
        MethodInfo methodInfo = BackwardsCompatibilityUtility.GetMethodInfo(strs, new Type[] {typeof(string[]), typeof(bool)});
        object[] objArray = (object[]) methodInfo.Invoke(null, new object[] {guids, dependencies});
        string[] strArrays = new string[(int) objArray.Length];
        FieldInfo field = methodInfo.ReturnType.GetElementType().GetField("guid");
        for (int i = 0; i < (int) objArray.Length; i++)
        {
            string value = (string) field.GetValue(objArray[i]);
            strArrays[i] = value;
        }

        return strArrays;
    }

    internal static string[] CollectAllChildren(string guid, string[] collection)
    {
        List<string> strs = new List<string>()
        {
            "UnityEditor.AssetDatabase.CollectAllChildren",
            "UnityEditor.AssetServer.CollectAllChildren"
        };
        return (string[]) BackwardsCompatibilityUtility.TryStaticInvoke(strs, new object[] {guid, collection});
    }

    internal static void ExportPackage(string[] guids, string fileName, bool needsPackageManagerManifest)
    {
        List<string> strs = new List<string>();
        if (needsPackageManagerManifest)
        {
            strs.Add("UnityEditor.PackageUtility.ExportPackageAndPackageManagerManifest");
        }

        strs.Add("UnityEditor.PackageUtility.ExportPackage");
        strs.Add("UnityEditor.AssetServer.ExportPackage");
        BackwardsCompatibilityUtility.TryStaticInvoke(strs, new object[] {guids, fileName});
    }
}