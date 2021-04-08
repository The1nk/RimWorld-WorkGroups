using System.Collections.Generic;
using Verse;

namespace The1nk.WorkGroups.Models
{
    public class WorkGroup : IExposable {
        public string Name;
        public List<WorkTypeDef> Items;
        public List<string> CanBeAssignedWith;
        public bool DisableTitleForThisWorkGroup;
        public int TargetQuantity;
        public bool AssignToEveryone;
        public bool ColonistsAllowed;
        public bool SlavesAllowed;
        public bool PrisonersAllowed;
        public bool RjwWorkersAllowed;

        public WorkGroup() {
            Name = "New WorkGroup";
            Items = new List<WorkTypeDef>();
            CanBeAssignedWith = new List<string>();
            DisableTitleForThisWorkGroup = false;
            TargetQuantity = 1;
            AssignToEveryone = false;
            ColonistsAllowed = true;
            SlavesAllowed = false;
            PrisonersAllowed = false;
            RjwWorkersAllowed = false;
        }

        public void ExposeData() {
            Scribe_Values.Look(ref Name, "Name");
            Scribe_Collections.Look(ref Items, "Items", LookMode.Def);
            Scribe_Collections.Look(ref CanBeAssignedWith, "CanBeAssignedWith", LookMode.Value);
            Scribe_Values.Look(ref DisableTitleForThisWorkGroup, "DisableTitleForThisWorkGroup");
            Scribe_Values.Look(ref TargetQuantity, "TargetQuantity");
            Scribe_Values.Look(ref AssignToEveryone, "AssignToEveryone");
            Scribe_Values.Look(ref ColonistsAllowed, "ColonistsAllowed");
            Scribe_Values.Look(ref SlavesAllowed, "SlavesAllowed");
            Scribe_Values.Look(ref PrisonersAllowed, "PrisonersAllowed");
            Scribe_Values.Look(ref RjwWorkersAllowed, "RjwWorkersAllowed");
        }
    }
}
