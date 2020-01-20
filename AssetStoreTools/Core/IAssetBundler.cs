using System;
using UnityEngine;

public interface IAssetBundler
{
    bool CanGenerateBundles();

    bool CanPreview();

    bool CreateBundle(UnityEngine.Object asset, string targetPath);

    void Preview(string assetpath);
}