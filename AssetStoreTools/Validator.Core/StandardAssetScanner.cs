using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace ASTools.Validator
{
    public class StandardAssetScanner : Scanner
    {
        private ChecklistItem checklistItem;

        public override ChecklistItem[] GetChecklistItems
        {
            get
            {
                return new ChecklistItem[] { this.checklistItem };
            }
        }

        public StandardAssetScanner(ChecklistItem check)
        {
            this.checklistItem = check;
        }

        public override void Scan()
        {
            List<string> list = (
                from s in Directory.GetDirectories(Application.dataPath)
                where s.Contains("Standard Assets")
                select s).ToList<string>();
            this.checklistItem.AddPaths(list);
        }
    }
}