using System;
using UnityEditor;

internal class UploadDialog
{
    private static AssetStorePackageController packageController;

    public static bool IsUploading
    {
        get { return UploadDialog.packageController != null; }
    }

    public UploadDialog()
    {
    }

    public static void CreateInstance(AssetStorePackageController packageController)
    {
        if (UploadDialog.packageController != null)
        {
            DebugUtils.LogError("New UploadDialog instance being created before an old one has finished");
        }

        UploadDialog.packageController = packageController;
        EditorApplication.update += new EditorApplication.CallbackFunction(UploadDialog.PackageControllerUpdatePump);
    }

    private static void FinishInstance()
    {
        EditorUtility.ClearProgressBar();
        UploadDialog.packageController = null;
        EditorApplication.update -= new EditorApplication.CallbackFunction(UploadDialog.PackageControllerUpdatePump);
        DebugUtils.Log("Upload progress dialog finished it's job");
    }

    private static void PackageControllerUpdatePump()
    {
        UploadDialog.packageController.Update();
        if (!UploadDialog.packageController.IsUploading)
        {
            UploadDialog.FinishInstance();
        }
        else
        {
            float getUploadProgress = UploadDialog.packageController.GetUploadProgress;
            if (EditorUtility.DisplayCancelableProgressBar(string.Format("Uploading {1}... {0}%", (getUploadProgress * 100f).ToString("N0"), UploadDialog.packageController.SelectedPackage.Name), "Closing this window will stop the ongoing upload process", getUploadProgress))
            {
                UploadDialog.packageController.OnClickUpload();
                UploadDialog.FinishInstance();
            }
        }
    }
}