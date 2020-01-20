using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

public static class MainAssetsUtil
{
    private static IAssetBundler s_Bundler;

    private static IAssetBundler Bundler
    {
        get
        {
            if (MainAssetsUtil.s_Bundler == null)
            {
                MainAssetsUtil.s_Bundler = AssetBundlerFactory.GetBundler();
            }

            return MainAssetsUtil.s_Bundler;
        }
    }

    public static bool CanGenerateBundles
    {
        get { return MainAssetsUtil.Bundler.CanGenerateBundles(); }
    }

    public static bool CanPreview
    {
        get { return MainAssetsUtil.Bundler.CanPreview(); }
    }

    public static string CreateBundle(string mainAssetPath)
    {
        if (!MainAssetsUtil.CanGenerateBundles)
        {
            DebugUtils.LogWarning("This version os Unity cannot generate Previews");
            return null;
        }

        string str = string.Concat("Temp/AssetBundle_", mainAssetPath.Trim(new char[] {'/'}).Replace('/', '\u005F'), ".unity3d");
        UnityEngine.Object obj = AssetDatabase.LoadMainAssetAtPath(mainAssetPath);
        if (obj == null)
        {
            DebugUtils.LogWarning(string.Format("Unable to find asset at: {0}", mainAssetPath));
            return null;
        }

        Type type = obj.GetType().Module.GetType("UnityEditor.SubstanceArchive");
        Module module = Vector3.zero.GetType().Module;
        Type type1 = module.GetType("UnityEngine.ProceduralMaterial");
        if (type != null && type1 != null && type.IsInstanceOfType(obj))
        {
            UnityEngine.Object[] objArray = AssetDatabase.LoadAllAssetsAtPath(mainAssetPath);
            int num = 0;
            while (num < (int) objArray.Length)
            {
                UnityEngine.Object obj1 = objArray[num];
                if (!type1.IsInstanceOfType(obj1))
                {
                    num++;
                }
                else
                {
                    obj = obj1;
                    break;
                }
            }
        }

        if (obj == null)
        {
            DebugUtils.LogWarning("Unable to find the Asset");
        }

        bool flag = MainAssetsUtil.Bundler.CreateBundle(obj, str);
        if (!flag)
        {
            DebugUtils.Log("bundleResut false");
        }
        else
        {
            DebugUtils.Log("bundleResut true");
        }

        if (!flag)
        {
            return null;
        }

        return str;
    }

    public static List<string> GetMainAssetsByTag(string folder)
    {
        List<string> strs = new List<string>();
        string[] files = Directory.GetFiles(string.Concat(Application.dataPath, folder));
        string[] strArrays = files;
        for (int i = 0; i < (int) strArrays.Length; i++)
        {
            string str = strArrays[i];
            bool flag = false;
            string str1 = str.Substring(Application.dataPath.Length + 1);
            str1 = str1.Replace("\\", "/");
            str1 = string.Concat("Assets/", str1);
            if (!(new Regex(".*[/][.][^/]*")).Match(str1).Success)
            {
                UnityEngine.Object obj = AssetDatabase.LoadMainAssetAtPath(str1);
                if (obj != null)
                {
                    string[] labels = AssetDatabase.GetLabels(obj);
                    int num = 0;
                    while (num < (int) labels.Length)
                    {
                        if (labels[num] != "MainAsset")
                        {
                            num++;
                        }
                        else
                        {
                            flag = true;
                            break;
                        }
                    }

                    if (flag)
                    {
                        strs.Add(str1);
                    }
                }
            }
        }

        string[] directories = Directory.GetDirectories(string.Concat(Application.dataPath, folder));
        string[] strArrays1 = directories;
        for (int j = 0; j < (int) strArrays1.Length; j++)
        {
            string str2 = strArrays1[j];
            string str3 = str2.Substring(Application.dataPath.Length);
            strs.AddRange(MainAssetsUtil.GetMainAssetsByTag(str3));
        }

        return strs;
    }

    public static void Preview(string assetpath)
    {
        MainAssetsUtil.Bundler.Preview(assetpath);
    }

    public static void ShowManager(string rootPath, List<string> mainAssets, FileSelector.DoneCallback onFinishChange)
    {
        FileSelector.Show(rootPath, mainAssets, onFinishChange);
    }
}