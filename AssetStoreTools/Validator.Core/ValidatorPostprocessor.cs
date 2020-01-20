using System;
using System.Collections.Generic;
using UnityEditor;

namespace ASTools.Validator
{
    internal class ValidatorPostprocessor : AssetPostprocessor
    {
        public ValidatorPostprocessor()
        {
        }

        private static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
        {
            Checklist checklist = ValidatorData.LoadAssetAtPath<Checklist>(ValidatorData.MANAGER_PATH);
            if (checklist == null)
            {
                return;
            }
            foreach (ChecklistItem check in checklist.Checks)
            {
                check.CheckAssetsForDeletion(deletedAssets);
                check.CheckAssetsForMove(movedFromAssetPaths, movedAssets);
            }
        }
    }
}