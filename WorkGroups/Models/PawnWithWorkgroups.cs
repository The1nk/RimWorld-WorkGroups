using System.Collections.Generic;
using System.Reflection;
using RimWorld;
using Verse;

namespace The1nk.WorkGroups.Models
{
    public class PawnWithWorkgroups
    {
        public Pawn Pawn { get; set; }
        public IEnumerable<WorkGroup> WorkGroups { get; set; }

        public PawnWithWorkgroups(Pawn pawn) {
            this.Pawn = pawn;
            this.WorkGroups = new List<WorkGroup>();
        }

        public bool IsColonist => this.Pawn.Faction != null && this.Pawn.Faction.IsPlayer;

        public bool IsSlave => this.Pawn.IsSlave;

        public bool IsRjwWorker =>
            (bool) The1nk.WorkGroups.WorkGroupsMapComponent.RjwMethod.Invoke(null, new object[] {(object) this.Pawn});

        public void SetWorkPriority(WorkTypeDef workTypeDef, int priority)
        {
            var oldPriority = this.Pawn.workSettings.GetPriority(workTypeDef);
            LogHelper.Verbose(
                $"Pawn '{this.Pawn.NameShortColored}'s priority for '{workTypeDef.defName}' is {oldPriority}, setting to {priority} with workSettings.SetPriority.");
            
            this.Pawn.workSettings.SetPriority(workTypeDef, priority);

            LogHelper.Verbose($"Now set to {this.Pawn.workSettings.GetPriority(workTypeDef)} (allegedly)");
        }
    }
}
