using System;

namespace ASTools.Validator
{
    public abstract class Scanner
    {
        public abstract ChecklistItem[] GetChecklistItems
        {
            get;
        }

        protected Scanner()
        {
        }

        public abstract void Scan();
    }
}