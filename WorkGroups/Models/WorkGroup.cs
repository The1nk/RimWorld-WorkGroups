using System;
using System.Collections.Generic;
using RimWorld;
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
        public List<StatDef> ImportantStats;
        public List<StatDef> HighStats;
        public List<StatDef> LowStats;
        public List<Trait> TraitsMustHave;
        public List<Trait> TraitsWantToHave;
        public List<Trait> TraitsCantHave;
        public string Badge;

        public WorkGroup(string name) {
            Name = name;
            Items = new List<WorkTypeDef>();
            CanBeAssignedWith = new List<string>();
            DisableTitleForThisWorkGroup = false;
            TargetQuantity = 1;
            AssignToEveryone = false;
            ColonistsAllowed = true;
            SlavesAllowed = false;
            PrisonersAllowed = false;
            RjwWorkersAllowed = false;
            ImportantStats = new List<StatDef>();
            HighStats = new List<StatDef>();
            LowStats = new List<StatDef>();
            TraitsMustHave = new List<Trait>();
            TraitsWantToHave = new List<Trait>();
            TraitsCantHave = new List<Trait>();
        }

        public WorkGroup() {
            
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
            Scribe_Collections.Look(ref ImportantStats, "ImportantStats", LookMode.Def);
            Scribe_Collections.Look(ref HighStats, "HighStats", LookMode.Def);
            Scribe_Collections.Look(ref LowStats, "LowStats", LookMode.Def);
            Scribe_Collections.Look(ref TraitsMustHave, "TraitsMustHave", LookMode.Deep);
            Scribe_Collections.Look(ref TraitsWantToHave, "TraitsWantToHave", LookMode.Deep);
            Scribe_Collections.Look(ref TraitsCantHave, "TraitsCantHave", LookMode.Deep);
            Scribe_Values.Look(ref Badge, "Badge");
        }
    }
}
