using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Threading.Tasks;
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

        public bool IsSlave =>
            this.Pawn.health.hediffSet.HasHediff(The1nk.WorkGroups.WorkGroupsMapComponent.SlaveHediff);

        public bool IsPrisoner => this.Pawn.IsPrisonerOfColony;
        public bool IsWorkingPrisoner => this.Pawn.IsPrisonerOfColony &&
                                         (bool)WorkGroupsMapComponent.PlMethod.Invoke(null,
                                             new object[] { (object)this.Pawn });
        

        public bool IsRjwWorker =>
            (bool) The1nk.WorkGroups.WorkGroupsMapComponent.RjwMethod.Invoke(null, new object[] {(object) this.Pawn});
    }
}
