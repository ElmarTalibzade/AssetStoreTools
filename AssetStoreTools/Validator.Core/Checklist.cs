using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace ASTools.Validator
{
    [Serializable]
    public class Checklist : ScriptableObject
    {
        private static Checklist _checklist;

        [SerializeField]
        internal List<ChecklistItem> Checks = new List<ChecklistItem>();

        public Checklist()
        {
        }

        private void AddCheck(ValidatorData.CheckItemData data)
        {
            ChecklistItem checklistItem = ScriptableObject.CreateInstance<ChecklistItem>();
            checklistItem.Init(data);
            AssetDatabase.AddObjectToAsset(checklistItem, ValidatorData.MANAGER_PATH);
            this.Checks.Add(checklistItem);
        }

        private static void CreateChecklist()
        {
            Checklist._checklist = ScriptableObject.CreateInstance<Checklist>();
            AssetDatabase.CreateAsset(Checklist._checklist, ValidatorData.MANAGER_PATH);
            foreach (ValidatorData.CheckItemData itemDatum in ValidatorData.ItemData)
            {
                Checklist._checklist.AddCheck(itemDatum);
            }
            EditorUtility.SetDirty(Checklist._checklist);
            AssetDatabase.ImportAsset(ValidatorData.MANAGER_PATH);
            AssetDatabase.SaveAssets();
        }

        internal static ChecklistItem GetCheck(CheckType check)
        {
            return Checklist._checklist.Checks[(int)check];
        }

        internal static Checklist GetCheckList()
        {
            Checklist._checklist = ValidatorData.LoadAssetAtPath<Checklist>(ValidatorData.MANAGER_PATH);
            if (Checklist._checklist == null)
            {
                Checklist.CreateChecklist();
            }
            return Checklist._checklist;
        }

        public void Scan()
        {
            foreach (ChecklistItem check in this.Checks)
            {
                check.Clear();
            }
            List<Scanner> scanners = new List<Scanner>()
            {
                new ExtensionScanner(Checklist.GetCheck(CheckType.Demo), ValidatorData.DEMO_EXTENSIONS, null),
                new ExtensionScanner(Checklist.GetCheck(CheckType.Jpg), ValidatorData.JPG_EXTENSIONS, null),
                new ExtensionScanner(Checklist.GetCheck(CheckType.Prepackage), ValidatorData.PACKAGE_EXTENSIONS, null),
                new ExtensionScanner(Checklist.GetCheck(CheckType.Documentation), ValidatorData.DOC_EXTENSIONS, ValidatorData.EXCLUDED_DIRECTORIES),
                new ExtensionScanner(Checklist.GetCheck(CheckType.JavaScript), ValidatorData.JS_EXTENSIONS, null),
                new ExtensionScanner(Checklist.GetCheck(CheckType.Mp3), ValidatorData.MP3_EXTENSIONS, null),
                new ExtensionScanner(Checklist.GetCheck(CheckType.Video), ValidatorData.VIDEO_EXTENSIONS, null),
                new ExtensionScanner(Checklist.GetCheck(CheckType.Executable), ValidatorData.EXECUTABLE_EXTENSIONS, null),
                new ExtensionScanner(Checklist.GetCheck(CheckType.SpeedTree), ValidatorData.SPEEDTREE_EXTENSIONS, null),
                new PrefabScanner(Checklist.GetCheck(CheckType.PrefabCollider), Checklist.GetCheck(CheckType.PrefabTransform), Checklist.GetCheck(CheckType.PrefabEmpty), Checklist.GetCheck(CheckType.LODs)),
                new StandardAssetScanner(Checklist.GetCheck(CheckType.StandardAssets)),
                new ReferenceScanner(Checklist.GetCheck(CheckType.MissingReference)),
                new ModelScanner(Checklist.GetCheck(CheckType.ModelPrefabs), Checklist.GetCheck(CheckType.Mixamo), Checklist.GetCheck(CheckType.Animation), Checklist.GetCheck(CheckType.Orientation)),
                new TextureScanner(Checklist.GetCheck(CheckType.Texture))
            };
            foreach (Scanner scanner in scanners)
            {
                try
                {
                    scanner.Scan();
                }
                catch (Exception exception1)
                {
                    Exception exception = exception1;
                    Debug.LogError(string.Concat("Validator check failed with ", scanner.GetType().ToString(), "\n\n", exception.ToString()));
                    ChecklistItem[] getChecklistItems = scanner.GetChecklistItems;
                    for (int i = 0; i < (int)getChecklistItems.Length; i++)
                    {
                        getChecklistItems[i].Failed = true;
                    }
                }
            }
            foreach (ChecklistItem checklistItem in this.Checks)
            {
                checklistItem.UpdateState();
            }
        }
    }
}