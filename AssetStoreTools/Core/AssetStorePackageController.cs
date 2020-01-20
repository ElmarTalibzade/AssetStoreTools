using ASTools.Validator;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEditor;
using UnityEngine;

internal class AssetStorePackageController
{
    private Package m_Package;

    private Vector2 m_Scroll;

    private PackageSelector m_PkgSelectionCtrl;

    private List<string> m_MainAssets;

    private AssetStorePackageController.AssetsState m_AssetsState;

    private float m_DraftAssetsUploadProgress;

    private string m_DraftAssetsPath;

    private long m_DraftAssetsSize;

    private FileInfo m_DraftAssetsFileInfo;

    private double m_DraftAssetsLastCheckTime;

    private bool m_Dirty;

    private bool m_UnsavedChanges;

    private string m_LocalProjectPath;

    private string m_LocalRootPath;

    private string m_LocalRootGUID;

    private MainAssetsUploadHelper m_MainAssetsUploadHelper;

    private bool m_IncludePackageManagerDependencies;

    private static string[] kForbiddenExtensions;

    public bool CanUpload
    {
        get
        {
            if (this.m_Package == null)
            {
                return false;
            }

            if (string.IsNullOrEmpty(this.m_Package.Name))
                return false;

            if (string.IsNullOrEmpty(this.m_Package.VersionName))
                return false;

            if (this.m_AssetsState != AssetStorePackageController.AssetsState.None)
                return false;

            if (string.IsNullOrEmpty(this.m_LocalRootPath))
                return false;

            if (!this.IsValidRelativeProjectFolder(this.m_LocalRootPath))
                return false;

            if (this.m_Package.Status != Package.PublishedStatus.Draft)
                return false;

            if (this.m_LocalRootPath.StartsWith("/AssetStoreTools"))
                return false;

            return true;

        }
    }

    public bool Dirty
    {
        get
        {
            return this.m_Dirty;
        }
    }

    public AssetStorePackageController.AssetsState GetAssetState
    {
        get
        {
            return this.m_AssetsState;
        }
    }

    public float GetUploadProgress
    {
        get
        {
            return this.m_DraftAssetsUploadProgress;
        }
    }

    public bool IsUploading
    {
        get
        {
            return (this.m_AssetsState != AssetStorePackageController.AssetsState.None && this.m_AssetsState != AssetStorePackageController.AssetsState.AllUploadsFinished);
        }
    }

    public Package SelectedPackage
    {
        get
        {
            return this.m_Package;
        }
        set
        {
            this.m_Package = value;
            this.m_PkgSelectionCtrl.Selected = this.m_Package;
            this.ClearLocalState();
        }
    }

    static AssetStorePackageController()
    {
        AssetStorePackageController.kForbiddenExtensions = new string[] { ".mb", ".ma", ".max", ".c4d", ".blend", ".3ds", ".jas", ".dds", ".pvr" };
    }

    internal AssetStorePackageController(PackageDataSource packageDataSource)
    {
        this.m_PkgSelectionCtrl = new PackageSelector(packageDataSource, new ListView<Package>.SelectionCallback(this.OnPackageSelected));
        this.ClearLocalState();
    }

    public void AutoSetSelected(AssetStoreManager assetStoreManager)
    {
        if (this.Dirty)
        {
            return;
        }
        IList<Package> allPackages = assetStoreManager.packageDataSource.GetAllPackages();
        if (allPackages.Count == 0)
        {
            return;
        }
        if (this.SelectedPackage == null)
        {
            this.SelectedPackage = allPackages[0];
            return;
        }
        Package package = allPackages.FirstOrDefault<Package>((Package x) => x.Id == this.SelectedPackage.Id);
        if (package == null)
        {
            return;
        }
        this.SelectedPackage = package;
    }

    private static bool CancelableProgressBar(float progress, string message, string buttonText)
    {
        EditorGUI.ProgressBar(GUILayoutUtility.GetRect(200f, 19f), progress, message);
        bool flag = GUILayout.Button(buttonText, new GUILayoutOption[0]);
        GUILayout.FlexibleSpace();
        return flag;
    }

    private void ChangeRootPathDialog()
    {
        string str = EditorUtility.OpenFolderPanel("Select root folder of package", Application.dataPath, string.Empty);
        if (!string.IsNullOrEmpty(str))
        {
            if (!this.IsValidProjectFolder(str))
            {
                EditorUtility.DisplayDialog("Wrong project path", "The path selected must be inside the currently active project. Note that the AssetStoreTools folder is removed automatically before the package enters the asset store", "Ok");
                return;
            }
            this.SetRootPath(str);
        }
    }

    private string CheckContent()
    {
        string empty = string.Empty;
        string[] gUIDS = this.GetGUIDS(this.NeedProjectSettings());
        for (int i = 0; i < (int)gUIDS.Length; i++)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(gUIDS[i]);
            string[] strArrays = AssetStorePackageController.kForbiddenExtensions;
            for (int j = 0; j < (int)strArrays.Length; j++)
            {
                if (assetPath.EndsWith(strArrays[j]))
                {
                    if (empty != string.Empty)
                    {
                        empty = string.Concat(empty, "\n");
                    }
                    empty = string.Concat(empty, "Unallowed file type: ", assetPath);
                }
            }
        }
        return empty;
    }

    private void CheckForPackageBuild()
    {
        if (this.m_AssetsState != AssetStorePackageController.AssetsState.BuildingPackage)
        {
            return;
        }
        if (this.m_DraftAssetsFileInfo == null)
        {
            this.m_DraftAssetsFileInfo = new FileInfo(this.m_DraftAssetsPath);
        }
        if (this.m_DraftAssetsLastCheckTime + 2 <= EditorApplication.timeSinceStartup)
        {
            this.m_DraftAssetsLastCheckTime = EditorApplication.timeSinceStartup;
            this.m_DraftAssetsFileInfo.Refresh();
            if (this.m_DraftAssetsFileInfo.Exists && this.m_DraftAssetsFileInfo.Length == this.m_DraftAssetsSize && this.m_DraftAssetsFileInfo.Length != 0)
            {
                this.UploadPackage();
            }
            else if (this.m_DraftAssetsFileInfo.Exists)
            {
                this.m_DraftAssetsSize = this.m_DraftAssetsFileInfo.Length;
            }
        }
    }

    private void ClearLocalState()
    {
        this.m_AssetsState = AssetStorePackageController.AssetsState.None;
        this.m_DraftAssetsUploadProgress = 0f;
        this.m_DraftAssetsPath = string.Empty;
        this.m_DraftAssetsSize = (long)0;
        this.m_DraftAssetsFileInfo = null;
        this.m_UnsavedChanges = false;
        if (this.m_Package != null)
        {
            this.m_MainAssets = new List<string>(this.m_Package.MainAssets);
            this.m_LocalProjectPath = this.m_Package.ProjectPath;
            this.m_LocalRootPath = this.m_Package.RootPath;
            this.m_LocalRootGUID = this.m_Package.RootGUID;
            this.m_IncludePackageManagerDependencies = this.m_Package.IsCompleteProjects;
        }
        else
        {
            this.m_MainAssets = new List<string>();
            this.m_LocalProjectPath = string.Empty;
            this.m_LocalRootPath = string.Empty;
            this.m_LocalRootGUID = string.Empty;
            this.m_IncludePackageManagerDependencies = false;
        }
    }

    private void Export(string toPath)
    {
        File.Delete(toPath);
        this.m_AssetsState = AssetStorePackageController.AssetsState.BuildingPackage;
        Packager.ExportPackage(this.GetGUIDS(this.NeedProjectSettings()), toPath, this.m_IncludePackageManagerDependencies);
    }

    private string[] GetGUIDS(bool includeProjectSettings)
    {
        string[] strArrays = new string[0];
        string str = string.Concat("Assets", this.m_LocalRootPath ?? string.Empty).Trim(new char[] { '/' });
        string[] strArrays1 = Packager.CollectAllChildren(AssetDatabase.AssetPathToGUID(str), strArrays);
        string[] strArrays2 = Packager.BuildExportPackageAssetListGuids(strArrays1, true);
        List<string> strs = new List<string>();
        string lower = str.ToLower();
        string[] strArrays3 = strArrays2;
        for (int i = 0; i < (int)strArrays3.Length; i++)
        {
            string str1 = strArrays3[i];
            string lower1 = AssetDatabase.GUIDToAssetPath(str1).ToLower();
            if (lower1.StartsWith("assets/plugins") || lower1.Contains("standard assets") || lower1.StartsWith(lower))
            {
                strs.Add(str1);
            }
        }
        if (includeProjectSettings)
        {
            string[] files = Directory.GetFiles("ProjectSettings");
            for (int j = 0; j < (int)files.Length; j++)
            {
                string gUID = AssetDatabase.AssetPathToGUID(files[j]);
                if (gUID.Length > 0)
                {
                    strs.Add(gUID);
                }
            }
        }
        string[] strArrays4 = new string[strs.Count];
        strs.CopyTo(strArrays4);
        return strArrays4;
    }

    private static string GetLocalRootGUID(Package package)
    {
        return AssetDatabase.AssetPathToGUID(string.Concat("Assets", package.RootPath ?? string.Empty).Trim(new char[] { '/' }));
    }

    private bool HasUnsavedChanges()
    {
        return this.m_UnsavedChanges;
    }

    private bool IsValidProjectFolder(string directory)
    {
        if (Application.dataPath.Length > directory.Length || directory.Substring(0, Application.dataPath.Length) != Application.dataPath)
        {
            return false;
        }
        if (!Directory.Exists(directory))
        {
            return false;
        }
        return true;
    }

    private bool IsValidRelativeProjectFolder(string relativeDirectory)
    {
        return this.IsValidProjectFolder(string.Concat(Application.dataPath, relativeDirectory));
    }

    private bool NeedProjectSettings()
    {
        return this.m_Package.IsCompleteProjects;
    }

    private void OnAssetsUploaded(string errorMessage)
    {
        DebugUtils.Log(string.Concat("OnAssetsUploaded ", errorMessage ?? string.Empty));
        this.m_AssetsState = AssetStorePackageController.AssetsState.None;
        this.m_DraftAssetsPath = string.Empty;
        this.m_DraftAssetsFileInfo = null;
        this.m_Dirty = true;
        if (errorMessage != null)
        {
            if (errorMessage != "aborted")
            {
                EditorUtility.DisplayDialog("Error uploading assets", string.Concat("An error occurred during assets upload\nPlease retry. ", errorMessage), "Close");
            }
            this.OnSubmitionFail();
        }
        else if (!MainAssetsUtil.CanGenerateBundles)
        {
            this.OnUploadSuccessfull();
        }
        else
        {
            this.UploadAssetBundles();
        }
    }

    private void OnAssetsUploading(double pctUp, double pctDown)
    {
        this.m_DraftAssetsUploadProgress = (float)(pctUp / 100);
        this.m_Dirty = true;
    }

    public void OnClickUpload()
    {
        if (this.m_AssetsState != AssetStorePackageController.AssetsState.UploadingPackage)
        {
            this.Upload();
            GUIUtility.ExitGUI();
        }
        else
        {
            AssetStoreClient.AbortLargeFilesUpload();
            this.m_AssetsState = AssetStorePackageController.AssetsState.None;
        }
    }

    private void OnPackageSelected(Package pkg)
    {
        Event @event = Event.current;
        if (@event.isMouse && @event.type == EventType.MouseDown && @event.clickCount == 2 && this.SelectedPackage == pkg)
        {
            Application.OpenURL(string.Concat("https://publisher.assetstore.unity3d.com/package.html?id=", pkg.versionId));
        }
        if (this.HasUnsavedChanges() && !EditorUtility.DisplayDialog("Change working package", "The package you currently have open has unsaved changes, would you like to discard the changes and view another package?", "Ok", "Cancel"))
        {
            this.m_PkgSelectionCtrl.Selected = this.SelectedPackage;
            return;
        }
        this.SelectedPackage = pkg;
        this.m_Dirty = true;
    }

    private void OnSubmitionFail()
    {
        this.m_AssetsState = AssetStorePackageController.AssetsState.None;
        this.m_Dirty = true;
        EditorApplication.UnlockReloadAssemblies();
    }

    private void OnUploadAssetBundlesFinished(string errorMessage)
    {
        this.m_AssetsState = AssetStorePackageController.AssetsState.None;
        this.m_MainAssetsUploadHelper = null;
        DebugUtils.Log("OnUploadAssetBundlesFinished");
        if (errorMessage == null)
        {
            this.OnUploadSuccessfull();
            return;
        }
        EditorUtility.DisplayDialog("Error uploading previews", errorMessage, "Ok");
    }

    private void OnUploadSuccessfull()
    {
        this.m_AssetsState = AssetStorePackageController.AssetsState.AllUploadsFinished;
        this.m_Dirty = true;
        EditorApplication.UnlockReloadAssemblies();
    }

    internal void Render()
    {
        this.m_Dirty = false;
        GUILayout.BeginVertical(new GUILayoutOption[0]);
        this.m_Scroll = GUILayout.BeginScrollView(this.m_Scroll, new GUILayoutOption[0]);
        bool flag = GUI.enabled;
        if (this.m_AssetsState != AssetStorePackageController.AssetsState.None)
        {
            GUI.enabled = false;
        }
        this.RenderPackageSelection();
        GUI.enabled = flag;
        EditorGUILayout.Space();
        if (this.m_Package != null)
        {
            this.RenderSettings();
        }
        GUILayout.EndScrollView();
        GUILayout.EndVertical();
    }

    private void RenderAssetsFolderStatus()
    {
        if (this.m_AssetsState == AssetStorePackageController.AssetsState.UploadingPackage)
        {
            int num = (int)Mathf.Ceil(this.m_DraftAssetsUploadProgress * 100f);
            string str = num.ToString();
            if (AssetStorePackageController.CancelableProgressBar(this.m_DraftAssetsUploadProgress, string.Concat("Uploading ", str, " %"), "Cancel"))
            {
                this.m_DraftAssetsUploadProgress = 0f;
                this.m_AssetsState = AssetStorePackageController.AssetsState.None;
                this.m_DraftAssetsPath = string.Empty;
                this.m_DraftAssetsSize = (long)0;
                AssetStoreClient.AbortLargeFilesUpload();
            }
        }
        else if (this.m_AssetsState != AssetStorePackageController.AssetsState.BuildingPackage)
        {
            Color color = GUI.color;
            string str1 = "No assets selected";
            if (this.m_LocalRootPath != null)
            {
                if (!this.IsValidRelativeProjectFolder(this.m_LocalRootPath))
                {
                    GUI.color = GUIUtil.ErrorColor;
                    str1 = string.Format("The path \"{0}\" is not valid within this Project", this.m_LocalRootPath);
                }
                else if (!this.m_LocalRootPath.StartsWith("/AssetStoreTools"))
                {
                    str1 = string.Concat(" ", this.m_LocalRootPath);
                }
                else
                {
                    GUI.color = GUIUtil.ErrorColor;
                    str1 = string.Format("The selected path cannot be part of \"/AssetStoreTools\" folder", this.m_LocalRootPath);
                }
            }
            GUILayout.Label(str1, new GUILayoutOption[0]);
            GUI.color = color;
            GUILayout.FlexibleSpace();
        }
        else
        {
            GUILayout.Label(GUIUtil.StatusWheel, new GUILayoutOption[0]);
            GUILayout.Label("Please wait - building package", new GUILayoutOption[0]);
            GUILayout.FlexibleSpace();
        }
    }

    private void RenderPackageSelection()
    {
        this.m_PkgSelectionCtrl.Render(200);
    }

    private void RenderSettings()
    {
        GUIStyle gUIStyle = new GUIStyle(GUI.skin.label)
        {
            richText = true
        };
        EditorGUILayout.Space();
        GUILayout.Label(new GUIContent("2. Select a folder that contains your assets", "You should select one folder that contains all the assets that you want to include to the package."), new GUILayoutOption[0]);
        GUILayout.BeginHorizontal(new GUILayoutOption[0]);
        bool flag = GUI.enabled;
        if (this.m_Package == null || this.m_Package.Status != Package.PublishedStatus.Draft || this.m_AssetsState != AssetStorePackageController.AssetsState.None)
        {
            GUI.enabled = false;
        }
        if (GUILayout.Button("Select...", new GUILayoutOption[] { GUILayout.Width(90f) }))
        {
            this.ChangeRootPathDialog();
        }
        GUI.enabled = flag;
        this.RenderAssetsFolderStatus();
        GUILayout.EndHorizontal();
        EditorGUILayout.Space();
        GUILayout.Label(new GUIContent("3. Tick the box if your content uses \"Package Manager\" dependencies", "If your assets package has dependencies on \"Package Manager\" packages (e.g. \"Ads\", \"TextMesh Pro\", \"Shader Graph\", or others), tick this checkbox to include those dependencies."), gUIStyle, new GUILayoutOption[0]);
        bool mIncludePackageManagerDependencies = this.m_IncludePackageManagerDependencies;
        this.m_IncludePackageManagerDependencies = GUILayout.Toggle(this.m_IncludePackageManagerDependencies, "Include dependencies", new GUILayoutOption[0]);
        if (mIncludePackageManagerDependencies != this.m_IncludePackageManagerDependencies)
        {
            this.m_UnsavedChanges = true;
        }
        EditorGUILayout.Space();
        GUILayout.Label(new GUIContent("4. Validate Package <i>(Optional)</i>", "Click 'Validate' to check if your package meets the basic package validation criteria. Keep in mind that passing this validation does not guarantee that your package will be accepted."), gUIStyle, new GUILayoutOption[0]);
        if (GUILayout.Button("Validate", new GUILayoutOption[] { GUILayout.Width(80f) }))
        {
            ValidatorWindow andShowWindow = ValidatorWindow.GetAndShowWindow();
            andShowWindow.PackagePath = string.Concat("Assets", this.m_LocalRootPath);
        }
        EditorGUILayout.Space();
    }

    private void SetRootPath(string path)
    {
        this.m_UnsavedChanges = true;
        this.m_LocalProjectPath = Application.dataPath;
        this.m_LocalRootPath = path.Substring(Application.dataPath.Length);
        if (this.m_LocalRootPath == string.Empty)
        {
            this.m_LocalRootPath = "/";
        }
        if (this.m_Package.RootPath != this.m_LocalRootPath)
        {
            this.m_MainAssets.Clear();
        }
    }

    private void ShowUploadSucessfull()
    {
        EditorUtility.DisplayDialog("Upload successful!", "The package content has been successfully uploaded. To finish the submission, visit the Publisher Portal and confirm that all information about your package is accurate.", "Ok");
        this.ClearLocalState();
        AssetStoreManager window = (AssetStoreManager)EditorWindow.GetWindow(typeof(AssetStoreManager), false, "Package Upload");
        window.SendEvent(EditorGUIUtility.CommandEvent("refresh"));
    }

    public void Update()
    {
        if (this.m_AssetsState != AssetStorePackageController.AssetsState.None && this.m_AssetsState != AssetStorePackageController.AssetsState.AllUploadsFinished)
        {
            this.m_Dirty = true;
        }
        switch (this.m_AssetsState)
        {
            case AssetStorePackageController.AssetsState.InitiateBuilding:
                {
                    this.Export(this.m_DraftAssetsPath);
                    return;
                }
            case AssetStorePackageController.AssetsState.BuildingPackage:
                {
                    this.CheckForPackageBuild();
                    return;
                }
            case AssetStorePackageController.AssetsState.UploadingPackage:
            case AssetStorePackageController.AssetsState.BuildingMainAssets:
            case AssetStorePackageController.AssetsState.UploadingMainAssets:
                {
                    return;
                }
            case AssetStorePackageController.AssetsState.AllUploadsFinished:
                {
                    if (!this.m_Dirty)
                    {
                        this.ShowUploadSucessfull();
                    }
                    return;
                }
            default:
                {
                    return;
                }
        }
    }

    private void Upload()
    {
        DebugUtils.Log("Upload");
        if (this.m_LocalRootPath == null)
        {
            EditorUtility.DisplayDialog("Package Assets folder not set", "You haven't set the Asset Folder yet. ", "Ok");
            return;
        }
        DebugUtils.Log(string.Concat(Application.dataPath, this.m_LocalRootPath));
        if (!Directory.Exists(string.Concat(Application.dataPath, this.m_LocalRootPath)))
        {
            EditorUtility.DisplayDialog("Project not found!", "The root folder you selected does not exist in the current project.\nPlease make sure you have the correct project open or you have selected the right root folder", "Ok");
            return;
        }
        if ((int)Directory.GetFileSystemEntries(string.Concat(Application.dataPath, this.m_LocalRootPath)).Length == 0)
        {
            EditorUtility.DisplayDialog("Empty folder!", "The root folder you have selected is empty.\nPlease make sure you have the correct project open or you have selected the right root folder", "Ok");
            return;
        }
        this.m_DraftAssetsUploadProgress = 0f;
        this.m_LocalProjectPath = Application.dataPath;
        this.m_LocalRootGUID = AssetStorePackageController.GetLocalRootGUID(this.m_Package);
        string str = this.CheckContent();
        if (string.IsNullOrEmpty(str))
        {
            this.m_DraftAssetsPath = string.Concat("Temp/uploadtool_", this.m_LocalRootPath.Trim(new char[] { '/' }).Replace('/', '\u005F'), ".unitypackage");
            this.m_DraftAssetsSize = (long)0;
            this.m_DraftAssetsLastCheckTime = EditorApplication.timeSinceStartup;
            this.m_AssetsState = AssetStorePackageController.AssetsState.InitiateBuilding;
            this.m_Dirty = true;
            EditorApplication.LockReloadAssemblies();
            return;
        }
        string str1 = AssetStorePackageController.kForbiddenExtensions[0];
        for (int i = 1; i < (int)AssetStorePackageController.kForbiddenExtensions.Length; i++)
        {
            str1 = string.Concat(str1, ", ", AssetStorePackageController.kForbiddenExtensions[i]);
        }
        Debug.LogWarning(str);
        EditorUtility.DisplayDialog("Invalid files", string.Concat("Your project contains file types that are not allowed in the AssetStore.\nPlease remove files with the following extensions:\n", str1, "\nYou can find more details in the console."), "Ok");
    }

    private void UploadAssetBundles()
    {
        this.m_AssetsState = AssetStorePackageController.AssetsState.BuildingMainAssets;
        if (this.m_MainAssets.Count == 0)
        {
            this.OnUploadAssetBundlesFinished(null);
            return;
        }
        this.m_MainAssetsUploadHelper = new MainAssetsUploadHelper(this, this.m_MainAssets, new Action<string>(this.OnUploadAssetBundlesFinished));
        this.m_MainAssetsUploadHelper.GenerateAssetBundles();
        this.m_AssetsState = AssetStorePackageController.AssetsState.UploadingMainAssets;
        this.m_MainAssetsUploadHelper.UploadAllAssetBundles();
        this.m_Dirty = true;
    }

    private void UploadPackage()
    {
        DebugUtils.Log("UploadPackage");
        this.m_AssetsState = AssetStorePackageController.AssetsState.UploadingPackage;
        if (string.IsNullOrEmpty(this.m_DraftAssetsPath))
        {
            DebugUtils.LogError("No assets to upload has been selected");
            this.m_AssetsState = AssetStorePackageController.AssetsState.None;
            return;
        }
        AssetStoreAPI.UploadAssets(this.m_Package, this.m_LocalRootGUID, this.m_LocalRootPath, this.m_LocalProjectPath, this.m_DraftAssetsPath, new AssetStoreAPI.DoneCallback(this.OnAssetsUploaded), new AssetStoreClient.ProgressCallback(this.OnAssetsUploading));
    }

    public enum AssetsState
    {
        None,
        InitiateBuilding,
        BuildingPackage,
        UploadingPackage,
        BuildingMainAssets,
        UploadingMainAssets,
        AllUploadsFinished
    }
}