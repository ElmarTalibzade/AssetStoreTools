using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEditor;
using UnityEngine;

namespace ASTools.Validator
{
    public static class ValidatorData
    {
        internal static string AS_DIRECTORY;

        internal static string AS_PATH;

        internal static string MANAGER_PATH;

        internal static string SCAN_PATH;

        internal static readonly string[] MODEL_EXTENSIONS;

        internal static readonly string[] JPG_EXTENSIONS;

        internal static readonly string[] DEMO_EXTENSIONS;

        internal static readonly string[] PACKAGE_EXTENSIONS;

        internal static readonly string[] DOC_EXTENSIONS;

        internal static readonly string[] PREFAB_EXTENSIONS;

        internal static readonly string[] JS_EXTENSIONS;

        internal static readonly string[] MP3_EXTENSIONS;

        internal static readonly string[] VIDEO_EXTENSIONS;

        internal static readonly string[] EXECUTABLE_EXTENSIONS;

        internal static readonly string[] TEXTURE_EXTENSIONS;

        internal static readonly string[] SPEEDTREE_EXTENSIONS;

        internal static readonly string[] EXCLUDED_DIRECTORIES;

        internal static List<ValidatorData.CheckItemData> ItemData;

        static ValidatorData()
        {
            ValidatorData.AS_DIRECTORY = "AssetStoreTools";
            ValidatorData.AS_PATH = Path.Combine("Assets", ValidatorData.AS_DIRECTORY);
            ValidatorData.MANAGER_PATH = Path.Combine(ValidatorData.AS_PATH, "AS_Checklist.asset");
            ValidatorData.SCAN_PATH = Application.dataPath;
            ValidatorData.MODEL_EXTENSIONS = new string[] { ".obj", ".fbx" };
            ValidatorData.JPG_EXTENSIONS = new string[] { ".jpg", ".jpeg" };
            ValidatorData.DEMO_EXTENSIONS = new string[] { ".unity" };
            ValidatorData.PACKAGE_EXTENSIONS = new string[] { ".unitypackage" };
            ValidatorData.DOC_EXTENSIONS = new string[] { ".txt", ".pdf", ".html", ".rtf" };
            ValidatorData.PREFAB_EXTENSIONS = new string[] { ".prefab" };
            ValidatorData.JS_EXTENSIONS = new string[] { ".js" };
            ValidatorData.MP3_EXTENSIONS = new string[] { ".mp3" };
            ValidatorData.VIDEO_EXTENSIONS = new string[] { ".mp4", ".webm", ".wmv", ".avi", ".mov" };
            ValidatorData.EXECUTABLE_EXTENSIONS = new string[] { ".exe", ".msi" };
            ValidatorData.TEXTURE_EXTENSIONS = new string[] { ".bmp", ".exr", ".gif", ".hdr", ".iff", ".jpg", ".pict", ".png", ".psd", ".tga", ".tiff" };
            ValidatorData.SPEEDTREE_EXTENSIONS = new string[] { ".spm", ".srt", ".stm", ".scs", ".sfc", ".sme" };
            ValidatorData.EXCLUDED_DIRECTORIES = new string[] { "AssetStoreTools", "AssetStoreChecker", "AssetStoreHelper" };
            List<ValidatorData.CheckItemData> checkItemDatas = new List<ValidatorData.CheckItemData>();
            ValidatorData.CheckItemData checkItemDatum = new ValidatorData.CheckItemData()
            {
                Type = CheckType.Demo,
                Title = "Include demo",
                Message = "If your product has content to show off, it should be displayed in a demo scene. Please provide a practical demo with all of your assets set up in a lighted scene, and if your package has multiple assets, please have an additional demo scene that displays all of your assets in a grid or a continuous line. If your asset is based on scripting, you still have to add a demo scene showcasing the asset or showing setup steps in the scene. Although, if your asset is an Editor extension, which works out of the box and has clear documentation, you may not add a demo scene. (Submission Guidelines, Section 3.3.a)",
                Detection = DetectionType.ErrorOnAbsence
            };
            checkItemDatas.Add(checkItemDatum);
            checkItemDatum = new ValidatorData.CheckItemData()
            {
                Type = CheckType.ModelPrefabs,
                Title = "Include prefabs",
                Message = "Each mesh should have a corresponding prefab set up with all variations of the texture/mesh/material that you are providing. Please create prefabs for all of your imported objects. (Submission Guidelines, Section 4.1.e)",
                Detection = DetectionType.WarningOnDetect
            };
            checkItemDatas.Add(checkItemDatum);
            checkItemDatum = new ValidatorData.CheckItemData()
            {
                Type = CheckType.PrefabTransform,
                Title = "Reset prefabs",
                Message = "Prefabs must have their position/rotation set to zero upon import and should have their scale set to 1.",
                Detection = DetectionType.WarningOnDetect
            };
            checkItemDatas.Add(checkItemDatum);
            checkItemDatum = new ValidatorData.CheckItemData()
            {
                Type = CheckType.PrefabCollider,
                Title = "Include colliders",
                Message = "Prefabs with meshes inside them have to have colliders applied to them. Please make sure you have appropriately sized colliders applied to your prefabs. Please keep in mind that colliders are not always required and we recommend putting colliders on static models.",
                Detection = DetectionType.WarningOnDetect
            };
            checkItemDatas.Add(checkItemDatum);
            checkItemDatum = new ValidatorData.CheckItemData()
            {
                Type = CheckType.PrefabEmpty,
                Title = "Empty Prefab",
                Message = "Prefabs cannot be empty, please make sure that you set up your prefabs correctly.",
                Detection = DetectionType.ErrorOnDetect
            };
            checkItemDatas.Add(checkItemDatum);
            checkItemDatum = new ValidatorData.CheckItemData()
            {
                Type = CheckType.Documentation,
                Title = "Include documentation",
                Message = "If your asset contains any code (scripts, shaders) - we ask that you include offline documentation in the format of pdf or rtf with your submission, as it is mandatory for all packages that include scripts or other components that require set up. Your documentation must be organized with a table of contents and numbered, written in English and have no grammar mistakes. Create a setup guide with a step-by-step tutorial (pdf or video), as well as a script reference if users will need to do any coding. (Submission Guidelines, Section 3.2.a) \nIf your asset contains art (3D models, sprites) and you used code to set up a demo scene, you may skip this step.",
                Detection = DetectionType.ErrorOnAbsence
            };
            checkItemDatas.Add(checkItemDatum);
            checkItemDatum = new ValidatorData.CheckItemData()
            {
                Type = CheckType.Orientation,
                Title = "Fix orientation",
                Message = "Meshes must have the correct orientation. The proper orientation is: Z - Vector is forward, Y - Vector is up, X - Vector is Right. (Submission Guidelines, Section 4.1.j)",
                Detection = DetectionType.WarningOnDetect
            };
            checkItemDatas.Add(checkItemDatum);
            checkItemDatum = new ValidatorData.CheckItemData()
            {
                Type = CheckType.Jpg,
                Title = "Remove .jpg",
                Message = "We do not allow texture images that are saved as .jpg. Please save all of your images as lossless format file types, such as PNG or TGA. (Submission Guidelines, Section 4.1.3.a)",
                Detection = DetectionType.ErrorOnDetect
            };
            checkItemDatas.Add(checkItemDatum);
            checkItemDatum = new ValidatorData.CheckItemData()
            {
                Type = CheckType.Prepackage,
                Title = "Remove .unitypackage",
                Message = "You must not include a .unitypackage file within your submission. If there is content within the .unitypackage file that is important for you to include, please put it in a regular folder in your folder structure. Submissions that include set up preferences, settings or supplemental files for another Asset Store product must be nested in a .unitypackage file. If you want to add different render pipeline (RP) versions to your package, you can add the RP specific files into .unitypackage. (Submission Guidelines, Section 3.4.a)",
                Detection = DetectionType.WarningOnDetect
            };
            checkItemDatas.Add(checkItemDatum);
            checkItemDatum = new ValidatorData.CheckItemData()
            {
                Type = CheckType.StandardAssets,
                Title = "Remove Standard Assets",
                Message = "Due to possible compatibility issues between Unity versions, we would ask that you remove all Standard Assets from your submission. If Standard Assets are required for demo scenes, or to achieve a certain effect, please list which packages the users should import in the description, your README file or other documentation. (Submission Guidelines, Section 3.1.e)",
                Detection = DetectionType.ErrorOnDetect
            };
            checkItemDatas.Add(checkItemDatum);
            checkItemDatum = new ValidatorData.CheckItemData()
            {
                Type = CheckType.MissingReference,
                Title = "Missing reference",
                Message = "We do not allow missing or broken material/texture/prefab/script connections in your package. Before submitting your asset and creating your package, be sure to test it! Create a new project and import your package into it. Check that everything works properly—and make sure that textures are linked to their respective materials. We often receive packages which do not work, throw exceptions, have unlinked textures and problems with surface normals.",
                Detection = DetectionType.ErrorOnDetect
            };
            checkItemDatas.Add(checkItemDatum);
            checkItemDatum = new ValidatorData.CheckItemData()
            {
                Type = CheckType.JavaScript,
                Title = "Remove JavaScript",
                Message = "As of version 2017.2, Unity has deprecated UnityScript and we will no longer be accepting projects that include .js files.",
                Detection = DetectionType.ErrorOnDetect
            };
            checkItemDatas.Add(checkItemDatum);
            checkItemDatum = new ValidatorData.CheckItemData()
            {
                Type = CheckType.Mp3,
                Title = "Remove Mp3",
                Message = "We do not recommend audio files that are saved as .mp3. Please save all of your audio as lossless format file types, such as .wav or .ogg.",
                Detection = DetectionType.ErrorOnDetect
            };
            checkItemDatas.Add(checkItemDatum);
            checkItemDatum = new ValidatorData.CheckItemData()
            {
                Type = CheckType.Video,
                Title = "Remove Videos",
                Message = "You cannot include a video file in your package. Please upload your video file to an online video hosting website (Youtube, Vimeo, etc.) and include the link to the video in your written documentation. This helps keep file sizes smaller and your package easier to download. (Submission Guidelines, Section 3.2.g)",
                Detection = DetectionType.ErrorOnDetect
            };
            checkItemDatas.Add(checkItemDatum);
            checkItemDatum = new ValidatorData.CheckItemData()
            {
                Type = CheckType.Executable,
                Title = "Remove Executables files",
                Message = "Your package must not contain an .exe or installer program or application. If your plugin requires an external program to run, please remove the installer program from your package and write the instructions on how to download and install the installer program in your documentation. (Submission Guidelines, Section 1.2.f)",
                Detection = DetectionType.ErrorOnDetect
            };
            checkItemDatas.Add(checkItemDatum);
            checkItemDatum = new ValidatorData.CheckItemData()
            {
                Type = CheckType.Mixamo,
                Title = "Remove Mixamo files",
                Message = "We do not allow or accept packages files that were made with third-party software, such as Mixamo, Fuse, etc. because these files are under licensing that does not agree with the Asset Store End User License Agreement (EULA). Please read the third-party software’s “Terms of Use” before you submit it to an online store. (Submission Guidelines, Section 1.1.1.a)",
                Detection = DetectionType.ErrorOnDetect
            };
            checkItemDatas.Add(checkItemDatum);
            checkItemDatum = new ValidatorData.CheckItemData()
            {
                Type = CheckType.Texture,
                Title = "Texture Import Settings incorrect",
                Message = "Your asset is not set up to work \"out of the box\". The textures in the package are compressed into a smaller size than actual texture resolution. We recommend changing the individual textures' compression details in Unity before submitting the asset.",
                Detection = DetectionType.WarningOnDetect
            };
            checkItemDatas.Add(checkItemDatum);
            checkItemDatum = new ValidatorData.CheckItemData()
            {
                Type = CheckType.SpeedTree,
                Title = "Remove SpeedTree files",
                Message = "You cannot redistribute SpeedTree files on other marketplaces. Please remove all SpeedTree files that are under this license. (Submission Guidelines, Section 1.1.c)",
                Detection = DetectionType.ErrorOnDetect
            };
            checkItemDatas.Add(checkItemDatum);
            checkItemDatum = new ValidatorData.CheckItemData()
            {
                Type = CheckType.Animation,
                Title = "Remove Duplicate Animation names",
                Message = "Your animation cannot contain duplicate names. The user can either use one animation per prefab or have to rename all animation files in the fbx objects themselves.  Make sure that your animations have a naming convention which is easy to understand. Please use underscores or numbers if needed.",
                Detection = DetectionType.ErrorOnDetect
            };
            checkItemDatas.Add(checkItemDatum);
            checkItemDatum = new ValidatorData.CheckItemData()
            {
                Type = CheckType.LODs,
                Title = "Check LODs on your Prefabs",
                Message = "We do not allow prefabs that contain a mesh with \"LOD\" written in the name but do not contain a \"LOD\" element or more than 1 mesh attached. Please make sure that \"LODs\" are correctly set up in the prefab. Refer to this guide to set up your \"LODs\": https://docs.unity3d.com/Manual/class-LODGroup.html",
                Detection = DetectionType.ErrorOnDetect
            };
            checkItemDatas.Add(checkItemDatum);
            ValidatorData.ItemData = checkItemDatas;
        }

        public static string GenerateUniquePath(string path, string extension)
        {
            string str;
            char[] charArray = Path.GetExtension(path).ToCharArray();
            path = path.TrimEnd(charArray);
            int num = 0;
            do
            {
                object[] objArray = new object[] { path, '\u005F', null, null };
                int num1 = num + 1;
                num = num1;
                objArray[2] = num1;
                objArray[3] = extension;
                str = string.Concat(objArray);
            }
            while (File.Exists(str));
            return str;
        }

        public static Mesh GetMesh(Transform transform)
        {
            MeshFilter component = transform.GetComponent<MeshFilter>();
            SkinnedMeshRenderer skinnedMeshRenderer = transform.GetComponent<SkinnedMeshRenderer>();
            if (component)
            {
                return component.sharedMesh;
            }
            if (!skinnedMeshRenderer)
            {
                return null;
            }
            return skinnedMeshRenderer.sharedMesh;
        }

        public static List<Mesh> GetMeshes(GameObject go)
        {
            List<Mesh> meshes = new List<Mesh>();
            MeshFilter[] componentsInChildren = go.GetComponentsInChildren<MeshFilter>(true);
            SkinnedMeshRenderer[] skinnedMeshRendererArray = go.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            meshes.AddRange(
                from m in (IEnumerable<MeshFilter>)componentsInChildren
                select m.sharedMesh);
            meshes.AddRange(
                from m in (IEnumerable<SkinnedMeshRenderer>)skinnedMeshRendererArray
                select m.sharedMesh);
            meshes = (
                from m in meshes
                where ValidatorData.PathInAssetDir(AssetDatabase.GetAssetPath(m))
                select m).ToList<Mesh>();
            return meshes;
        }

        public static List<string> GetModelPaths()
        {
            List<string> pathsWithExtensions = ValidatorData.GetPathsWithExtensions(ValidatorData.MODEL_EXTENSIONS, null);
            List<string> strs = new List<string>();
            foreach (string pathsWithExtension in pathsWithExtensions)
            {
                if (!ValidatorData.GetMeshes(ValidatorData.LoadAssetAtPath<GameObject>(pathsWithExtension)).Any<Mesh>() || ValidatorData.HasAnimations(pathsWithExtension))
                {
                    continue;
                }
                strs.Add(pathsWithExtension);
            }
            return strs;
        }

        internal static List<string> GetPathsWithExtensions(string[] extensions, string[] exceptions = null)
        {
            string[] strArrays = exceptions;
            strArrays = strArrays ?? new string[0];
            DirectoryInfo directoryInfo = new DirectoryInfo(ValidatorData.SCAN_PATH);
            HashSet<string> strs = new HashSet<string>(extensions, StringComparer.OrdinalIgnoreCase);
            IEnumerable<FileInfo> files =
                from f in directoryInfo.GetFiles("*", SearchOption.AllDirectories)
                where strs.Contains(f.Extension)
                select f;
            IEnumerable<string> projectRelativePath =
                from f in files
                select ValidatorData.ToProjectRelativePath(f.FullName) into p
                where strArrays.All<string>((string e) => !Path.GetDirectoryName(p).Contains(e))
                select p;
            return projectRelativePath.ToList<string>();
        }

        private static bool HasAnimations(string path)
        {
            ModelImporter atPath = (ModelImporter)AssetImporter.GetAtPath(path);
            return atPath.clipAnimations.Count<ModelImporterClipAnimation>() > 0;
        }

        public static T LoadAssetAtPath<T>(string path)
        where T : UnityEngine.Object
        {
            return (T)AssetDatabase.LoadAssetAtPath(path, typeof(T));
        }

        internal static bool PathInAssetDir(string path)
        {
            return (path.StartsWith(string.Concat("Assets", Path.DirectorySeparatorChar)) ? true : path.StartsWith(string.Concat("Assets", Path.AltDirectorySeparatorChar)));
        }

        public static void SetScanPath(string path)
        {
            string str = Application.dataPath.Replace("Assets", string.Empty);
            ValidatorData.SCAN_PATH = Path.Combine(str, path);
        }

        internal static string ToProjectRelativePath(string path)
        {
            path = path.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
            string str = Application.dataPath;
            str = str.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
            if (path.StartsWith(str) && path.Length > str.Length)
            {
                path = path.Substring(str.Length + 1);
            }
            if (!ValidatorData.PathInAssetDir(path))
            {
                path = Path.Combine("Assets", path);
            }
            return path;
        }

        internal class CheckItemData
        {
            public CheckType Type;

            public string Title;

            public string Message;

            public DetectionType Detection;

            public CheckItemData()
            {
            }
        }
    }
}