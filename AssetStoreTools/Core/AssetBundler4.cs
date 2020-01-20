using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

public class AssetBundler4 : IAssetBundler
{
    public AssetBundler4()
    {
    }

    public bool CanGenerateBundles()
    {
        return true;
    }

    public bool CanPreview()
    {
        return true;
    }

    public bool CreateBundle(UnityEngine.Object asset, string targetPath)
    {
        return AssetStoreToolUtils.BuildAssetStoreAssetBundle(asset, targetPath);
    }

    public AssetBundleCreateRequest LoadFromMemoryAsync(byte[] bytes)
    {
        List<string> strs = new List<string>()
        {
            "UnityEngine.AssetBundle.LoadFromMemoryAsync",
            "UnityEngine.AssetBundle.CreateFromMemory"
        };
        MethodInfo methodInfo = BackwardsCompatibilityUtility.GetMethodInfo(strs, new Type[] { typeof(byte[]) });
        return (AssetBundleCreateRequest)methodInfo.Invoke(null, new object[] { bytes });
    }

    public void Preview(string assetpath)
    {
        string str = MainAssetsUtil.CreateBundle(assetpath);
        AssetStoreAsset assetStoreAsset = new AssetStoreAsset()
        {
            name = "Preview"
        };
        AssetBundleCreateRequest assetBundleCreateRequest = this.LoadFromMemoryAsync(File.ReadAllBytes(str));
        if (assetBundleCreateRequest == null)
        {
            DebugUtils.Log("Unable to generate preview");
        }
        while (!assetBundleCreateRequest.isDone)
        {
            AssetStoreToolUtils.UpdatePreloadingInternal();
        }
        AssetBundle assetBundle = assetBundleCreateRequest.assetBundle;
        if (assetBundle == null)
        {
            DebugUtils.Log("No bundle at path");
            assetBundleCreateRequest = null;
            return;
        }
        AssetStoreToolUtils.PreviewAssetStoreAssetBundleInInspector(assetBundle, assetStoreAsset);
        assetBundle.Unload(false);
        assetBundleCreateRequest = null;
        File.Delete(str);
    }
}