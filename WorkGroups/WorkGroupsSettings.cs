using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using The1nk.WorkGroups.Models;
using Verse;

namespace The1nk.WorkGroups
{
    public class WorkGroupsSettings : IExposable {
        public static WorkGroupsSettings GetSettings { get; private set; }
        public bool SsInstalled  { get; set; }
        public bool PlInstalled { get; set; }

        public IEnumerable<WorkTypeDef> AllWorkTypes = new List<WorkTypeDef>();

        public bool Enabled = false;
        public bool SetPrioritiesForSlaves = true; // Simple Slavery
        public bool SetPrioritiesForPrisoners = true; // Prison Labor
        public bool RjwInstalled = false; // So that we can hide settings
        public bool SetPrioritiesForRjwWorkers = true; // RJW
        public bool SetPawnTitles = true; 
        public int MaxPriority = 4;
        public int HoursUpdateInterval = 2;
        public bool ClearOutSchedules = true;
        public List<WorkGroup> WorkGroups;
        public bool VerboseLogging = false;

        public WorkGroupsSettings() {
            WorkGroups = new List<WorkGroup>();
            GetSettings = this;
        }

        public void ExposeData() {
            Scribe_Values.Look(ref Enabled, "Enabled", false, true);
            Scribe_Values.Look(ref SetPrioritiesForSlaves, "SetPrioritiesForSlaves", true, true);
            Scribe_Values.Look(ref SetPrioritiesForPrisoners, "SetPrioritiesForPrisoners", true, true);
            Scribe_Values.Look(ref SetPrioritiesForRjwWorkers, "SetPrioritiesForRjwWorkers", false, false);
            Scribe_Values.Look(ref SetPawnTitles, "SetPawnTitles", true, true);
            Scribe_Values.Look(ref MaxPriority, "MaxPriority", 4, true);
            Scribe_Values.Look(ref HoursUpdateInterval, "HoursUpdateInterval", 2, true);
            Scribe_Values.Look(ref ClearOutSchedules, "ClearOutSchedules", true, true);
            Scribe_Collections.Look(ref WorkGroups, "WorkGroups", LookMode.Deep);
            Scribe_Values.Look(ref VerboseLogging, "VerboseLogging", false, true);
        }
    }
}
