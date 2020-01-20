using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEditor;
using UnityEngine;

namespace ASTools.Validator
{
    public class ReferenceScanner : Scanner
    {
        private ChecklistItem checklistItem;

        public override ChecklistItem[] GetChecklistItems
        {
            get
            {
                return new ChecklistItem[] { this.checklistItem };
            }
        }

        public ReferenceScanner(ChecklistItem check)
        {
            this.checklistItem = check;
        }

        private static bool IsMissingReference(GameObject asset)
        {
            Component[] components = asset.GetComponents<Component>();
            for (int i = 0; i < (int)components.Length; i++)
            {
                if (!components[i])
                {
                    return true;
                }
            }
            return false;
        }

        public override void Scan()
        {
            IEnumerable<string> allAssetPaths =
                from p in AssetDatabase.GetAllAssetPaths()
                where ValidatorData.PathInAssetDir(p)
                select p;
            IEnumerator<string> enumerator = allAssetPaths.GetEnumerator();
            try
            {
                while (enumerator.MoveNext())
                {
                    string current = enumerator.Current;
                    GameObject gameObject = ValidatorData.LoadAssetAtPath<GameObject>(current);
                    if (!(gameObject != null) || !ReferenceScanner.IsMissingReference(gameObject))
                    {
                        continue;
                    }
                    this.checklistItem.AddPath(current);
                }
            }
            finally
            {
                if (enumerator == null)
                {
                }
                enumerator.Dispose();
            }
        }
    }
}