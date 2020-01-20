using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace ASTools.Validator
{
    public class TextureScanner : Scanner
    {
        private ChecklistItem checklistItem;

        public override ChecklistItem[] GetChecklistItems
        {
            get
            {
                return new ChecklistItem[] { this.checklistItem };
            }
        }

        public TextureScanner(ChecklistItem check)
        {
            this.checklistItem = check;
        }

        public override void Scan()
        {
            foreach (string pathsWithExtension in ValidatorData.GetPathsWithExtensions(ValidatorData.TEXTURE_EXTENSIONS, null))
            {
                Texture2D texture2D = new Texture2D(1, 1);
                texture2D.LoadImage(File.ReadAllBytes(pathsWithExtension));
                TextureImporter atPath = (TextureImporter)AssetImporter.GetAtPath(pathsWithExtension);
                if (texture2D.height <= atPath.maxTextureSize && texture2D.width <= atPath.maxTextureSize)
                {
                    continue;
                }
                this.checklistItem.AddPath(pathsWithExtension);
            }
        }
    }
}