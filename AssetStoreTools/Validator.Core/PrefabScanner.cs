using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ASTools.Validator
{
    public class PrefabScanner : Scanner
    {
        private ChecklistItem colliderCheck;

        private ChecklistItem transformCheck;

        private ChecklistItem emptyCheck;

        private ChecklistItem lodsCheck;

        public override ChecklistItem[] GetChecklistItems
        {
            get
            {
                return new ChecklistItem[] { this.colliderCheck, this.transformCheck, this.emptyCheck, this.lodsCheck };
            }
        }

        public PrefabScanner(ChecklistItem colliderItem, ChecklistItem transformItem, ChecklistItem emptyItem, ChecklistItem lodsItem)
        {
            this.colliderCheck = colliderItem;
            this.transformCheck = transformItem;
            this.emptyCheck = emptyItem;
            this.lodsCheck = lodsItem;
        }

        private static bool HasIncorrectLODs(GameObject go)
        {
            MeshFilter[] componentsInChildren = go.GetComponentsInChildren<MeshFilter>();
            bool component = go.GetComponent<LODGroup>() != null;
            MeshFilter[] meshFilterArray = componentsInChildren;
            for (int i = 0; i < (int)meshFilterArray.Length; i++)
            {
                if (meshFilterArray[i].name.Contains("LOD") && !component)
                {
                    return true;
                }
            }
            return false;
        }

        private static bool IsPrefabEmpty(GameObject go)
        {
            if ((int)go.GetComponents<Component>().Length > 1)
            {
                return false;
            }
            if (go.transform.childCount > 0)
            {
                return false;
            }
            return true;
        }

        private static bool NeedsCollider(GameObject go)
        {
            if (!ValidatorData.GetMeshes(go).Any<Mesh>())
            {
                return false;
            }
            return !go.GetComponentsInChildren<Collider>(true).Any<Collider>();
        }

        private static bool NeedsTransformReset(GameObject go)
        {
            if (!ValidatorData.GetMeshes(go).Any<Mesh>())
            {
                return false;
            }
            return !go.transform.localToWorldMatrix.isIdentity;
        }

        public override void Scan()
        {
            foreach (string pathsWithExtension in ValidatorData.GetPathsWithExtensions(ValidatorData.PREFAB_EXTENSIONS, null))
            {
                GameObject gameObject = ValidatorData.LoadAssetAtPath<GameObject>(pathsWithExtension);
                if (gameObject == null)
                {
                    this.emptyCheck.AddPath(pathsWithExtension);
                }
                else
                {
                    if (PrefabScanner.NeedsCollider(gameObject))
                    {
                        this.colliderCheck.AddPath(pathsWithExtension);
                    }
                    if (PrefabScanner.NeedsTransformReset(gameObject))
                    {
                        this.transformCheck.AddPath(pathsWithExtension);
                    }
                    if (PrefabScanner.IsPrefabEmpty(gameObject))
                    {
                        this.emptyCheck.AddPath(pathsWithExtension);
                    }
                    if (PrefabScanner.HasIncorrectLODs(gameObject))
                    {
                        this.lodsCheck.AddPath(pathsWithExtension);
                    }
                }
            }
        }
    }
}