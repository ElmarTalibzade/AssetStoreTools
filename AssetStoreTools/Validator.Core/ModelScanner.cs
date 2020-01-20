using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace ASTools.Validator
{
    public class ModelScanner : Scanner
    {
        private ChecklistItem prefabsCheck;

        private ChecklistItem mixamoCheck;

        private ChecklistItem animationCheck;

        private ChecklistItem orientationCheck;

        public override ChecklistItem[] GetChecklistItems
        {
            get
            {
                return new ChecklistItem[] { this.prefabsCheck, this.mixamoCheck, this.animationCheck, this.orientationCheck };
            }
        }

        public ModelScanner(ChecklistItem prefabsCheck, ChecklistItem mixamoCheck, ChecklistItem animationCheck, ChecklistItem orientationCheck)
        {
            this.prefabsCheck = prefabsCheck;
            this.mixamoCheck = mixamoCheck;
            this.animationCheck = animationCheck;
            this.orientationCheck = orientationCheck;
        }

        private static bool IsUpright(GameObject model)
        {
            Transform[] componentsInChildren = model.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < (int)componentsInChildren.Length; i++)
            {
                Transform transforms = componentsInChildren[i];
                if (transforms.localRotation != Quaternion.identity && ValidatorData.GetMesh(transforms))
                {
                    return false;
                }
            }
            return true;
        }

        public override void Scan()
        {
            this.ScanForPrefabs();
            this.ScanForMixamo();
            this.ScanForAnimations();
            this.ScanForOrientations();
        }

        private void ScanForAnimations()
        {
        Label0:
            foreach (string pathsWithExtension in ValidatorData.GetPathsWithExtensions(ValidatorData.MODEL_EXTENSIONS, null))
            {
                List<ModelImporterClipAnimation> modelImporterClipAnimations = new List<ModelImporterClipAnimation>();
                ModelImporter atPath = (ModelImporter)AssetImporter.GetAtPath(pathsWithExtension);
                modelImporterClipAnimations.AddRange(atPath.clipAnimations);
                modelImporterClipAnimations.AddRange(atPath.defaultClipAnimations);
                HashSet<string> strs = new HashSet<string>();
                int num = 0;
                while (num < modelImporterClipAnimations.Count)
                {
                    if (strs.Add(modelImporterClipAnimations[num].name))
                    {
                        num++;
                    }
                    else
                    {
                        this.animationCheck.AddPath(pathsWithExtension);
                        goto Label0;
                    }
                }
            }
        }

        private void ScanForMixamo()
        {
            foreach (string pathsWithExtension in ValidatorData.GetPathsWithExtensions(new string[] { ".fbx" }, null))
            {
                if (!ValidatorUtils.IsMixamoFbx(pathsWithExtension))
                {
                    continue;
                }
                this.mixamoCheck.AddPath(pathsWithExtension);
            }
        }

        private void ScanForOrientations()
        {
            foreach (string modelPath in ValidatorData.GetModelPaths())
            {
                GameObject gameObject = ValidatorData.LoadAssetAtPath<GameObject>(modelPath);
                if (ModelScanner.IsUpright(gameObject))
                {
                    continue;
                }
                this.orientationCheck.AddPath(AssetDatabase.GetAssetPath(gameObject));
            }
        }

        private void ScanForPrefabs()
        {
            List<string> pathsWithExtensions = ValidatorData.GetPathsWithExtensions(ValidatorData.PREFAB_EXTENSIONS, null);
            HashSet<string> strs = new HashSet<string>();
            foreach (string pathsWithExtension in pathsWithExtensions)
            {
                GameObject gameObject = ValidatorData.LoadAssetAtPath<GameObject>(pathsWithExtension);
                if (gameObject == null)
                {
                    continue;
                }
                foreach (Mesh mesh in ValidatorData.GetMeshes(gameObject))
                {
                    strs.Add(AssetDatabase.GetAssetPath(mesh));
                }
            }
            List<string> modelPaths = ValidatorData.GetModelPaths();
            List<string> list = modelPaths.Except<string>(strs, new CustomPathComparer()).ToList<string>();
            this.prefabsCheck.AddPaths(list);
        }
    }
}