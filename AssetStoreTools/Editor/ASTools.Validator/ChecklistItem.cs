using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEditor;
using UnityEngine;

namespace ASTools.Validator
{
    [Serializable]
    public class ChecklistItem : ScriptableObject
    {
        public CheckType Type;

        [SerializeField]
        public List<string> AssetPaths = new List<string>();

        public CheckStatus Status;

        public bool Active = true;

        public bool Foldout;

        public bool FoldoutMessage = true;

        public bool FoldoutPaths = true;

        public bool Failed;

        public ChecklistItem()
        {
        }

        internal void AddPath(string path)
        {
            if (!this.AssetPaths.Contains(path))
            {
                this.AssetPaths.Add(path);
            }
        }

        internal void AddPaths(List<string> paths)
        {
            foreach (string path in paths)
            {
                this.AddPath(path);
            }
        }

        internal void CheckAssetsForDeletion(string[] deletedAssets)
        {
            string[] array = deletedAssets;
            int count = this.AssetPaths.Count;
            array = (
                from d in array
                select Path.GetFullPath(d)).ToArray<string>();
            this.AssetPaths.RemoveAll((string asset) => array.Contains<string>(Path.GetFullPath(asset)));
            if (this.AssetPaths.Count != count)
            {
                this.UpdateState();
            }
        }

        internal void CheckAssetsForMove(string[] movedFromAssetPaths, string[] movedAssets)
        {
            bool flag = false;
            for (int i = 0; i < (int)movedAssets.Length; i++)
            {
                string str = movedFromAssetPaths[i];
                int num = this.AssetPaths.FindIndex((string x) => Path.GetFullPath(x).Equals(Path.GetFullPath(str)));
                if (num > -1)
                {
                    this.AssetPaths[num] = movedAssets[i];
                    flag = true;
                }
            }
            if (flag)
            {
                this.UpdateState();
            }
        }

        internal void Clear()
        {
            this.AssetPaths.Clear();
            this.Status = CheckStatus.Pass;
            this.Failed = false;
        }

        internal void Init(ValidatorData.CheckItemData data)
        {
            this.Type = data.Type;
            EditorUtility.SetDirty(this);
        }

        internal void UpdateState()
        {
            CheckStatus checkStatu;
            DetectionType detection = ValidatorData.ItemData[(int)this.Type].Detection;
            bool flag = (detection != DetectionType.ErrorOnAbsence ? this.AssetPaths.Any<string>() : !this.AssetPaths.Any<string>());
            if (!flag)
            {
                checkStatu = CheckStatus.Pass;
            }
            else
            {
                checkStatu = (detection != DetectionType.WarningOnDetect ? CheckStatus.Error : CheckStatus.Warning);
            }
            this.Status = checkStatu;
            this.Foldout = flag;
        }
    }
}