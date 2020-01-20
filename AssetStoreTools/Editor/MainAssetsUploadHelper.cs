using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;

internal class MainAssetsUploadHelper
{
    private readonly AssetStorePackageController m_AssetStorePackageController;

    private readonly Action<string> m_OnUploadAssetBundlesFinished;

    private readonly List<string> m_MainAssets;

    private Dictionary<string, string> m_AssetBundleFiles;

    private List<string> m_PendingUploadsAssetPath;

    private List<double> m_MainAssetsProgress;

    private string m_ErrorMessages;

    private double m_CachedProgressValue;

    private bool m_ProgressDirty;

    public MainAssetsUploadHelper(AssetStorePackageController assetStorePackageController, List<string> mainAssets, Action<string> onUploadAssetBundlesFinished)
    {
        this.m_AssetStorePackageController = assetStorePackageController;
        this.m_OnUploadAssetBundlesFinished = onUploadAssetBundlesFinished;
        this.m_MainAssets = mainAssets;
    }

    public void GenerateAssetBundles()
    {
        this.m_AssetBundleFiles = new Dictionary<string, string>();
        foreach (string mMainAsset in this.m_MainAssets)
        {
            DebugUtils.Log(string.Concat("processing: ", mMainAsset));
            string str = MainAssetsUtil.CreateBundle(mMainAsset);
            if (str != null)
            {
                this.m_AssetBundleFiles.Add(mMainAsset, str);
            }
            else
            {
                DebugUtils.LogWarning(string.Format("Unable to Create Preview for: {0}", mMainAsset));
            }
        }
    }

    public double GetProgress()
    {
        if (this.m_ProgressDirty)
        {
            double num = this.m_MainAssetsProgress.Sum();
            this.m_CachedProgressValue = num / (double) this.m_MainAssetsProgress.Count;
        }

        return this.m_CachedProgressValue;
    }

    private AssetStoreClient.ProgressCallback OnBundleProgress(string assetPath)
    {
        int num = this.m_MainAssets.IndexOf(assetPath);
        return (double pctup, double pctdown) =>
        {
            this.m_MainAssetsProgress[num] = pctup;
            this.m_ProgressDirty = true;
        };
    }

    private void OnFinishUploadBundle(string filepath, string errorMessage)
    {
        string str = (
            from kv in this.m_AssetBundleFiles
            where kv.Value == filepath
            select kv.Key).Single<string>();
        this.m_PendingUploadsAssetPath.Remove(str);
        int num = this.m_MainAssets.IndexOf(str);
        this.m_MainAssetsProgress[num] = 100;
        this.m_ProgressDirty = true;
        if (errorMessage != null)
        {
            this.m_ErrorMessages = (this.m_ErrorMessages != null ? string.Concat(this.m_ErrorMessages, "\n", errorMessage) : errorMessage);
        }

        File.Delete(filepath);
        if (this.m_PendingUploadsAssetPath.Count == 0)
        {
            this.m_OnUploadAssetBundlesFinished(this.m_ErrorMessages);
        }
    }

    public void UploadAllAssetBundles()
    {
        this.m_PendingUploadsAssetPath = this.m_AssetBundleFiles.Keys.ToList<string>();
        this.m_MainAssetsProgress = new List<double>(this.m_MainAssets.Count);
        this.m_ErrorMessages = null;
        this.m_ProgressDirty = false;
        this.m_CachedProgressValue = 0;
        foreach (string mPendingUploadsAssetPath in this.m_PendingUploadsAssetPath)
        {
            string item = this.m_AssetBundleFiles[mPendingUploadsAssetPath];
            string uploadBundlePath = AssetStoreAPI.GetUploadBundlePath(this.m_AssetStorePackageController.SelectedPackage, mPendingUploadsAssetPath);
            AssetStoreAPI.UploadBundle(uploadBundlePath, item, new AssetStoreAPI.UploadBundleCallback(this.OnFinishUploadBundle), this.OnBundleProgress(mPendingUploadsAssetPath));
            this.m_MainAssetsProgress.Add(0);
        }
    }
}