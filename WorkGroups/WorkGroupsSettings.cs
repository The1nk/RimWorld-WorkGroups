using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using RimWorld;
using The1nk.WorkGroups.Models;
using Verse;

namespace The1nk.WorkGroups {
    public class WorkGroupsSettings : IExposable {
        public static WorkGroupsSettings GetSettings { get; private set; }

        private bool _ssInstalled;
        private bool _plInstalled;
        private bool _rjwInstalled;

        public bool SsInstalled {
            get => _ssInstalled;
            set {
                if (value == false)
                    SetPrioritiesForSlaves = false;

                _ssInstalled = value;
            }
        }

        public bool PlInstalled {
            get => _plInstalled;
            set {
                if (value == false)
                    SetPrioritiesForPrisoners = false;

                _plInstalled = value;
            }
        }

        public bool RjwInstalled {
            get => _rjwInstalled;
            set {
                if (value == false)
                    SetPrioritiesForRjwWorkers = false;

                _rjwInstalled = value;
            }
        }

        public WorkGroupsMapComponent Component { get; set; }

        public IEnumerable<WorkTypeDef> AllWorkTypes = new List<WorkTypeDef>();

        public bool Enabled = false;
        public bool SetPrioritiesForSlaves = true; // Simple Slavery
        public bool SetPrioritiesForPrisoners = true; // Prison Labor
        public bool SetPrioritiesForRjwWorkers = true; // RJW
        public bool SetPawnTitles = true;
        public int MaxPriority = 4;
        public int HoursUpdateInterval = 2;
        public bool ClearOutSchedules = true;
        public List<WorkGroup> WorkGroups;
        public bool VerboseLogging = false;
        public bool ForcedBedRestForInjuredPawns = true;

        private string _baseDir = "";

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
            Scribe_Values.Look(ref ForcedBedRestForInjuredPawns, nameof(ForcedBedRestForInjuredPawns), true, false);
            Scribe_Collections.Look(ref WorkGroups, "WorkGroups", LookMode.Deep);
            Scribe_Values.Look(ref VerboseLogging, "VerboseLogging", false, true);

        }

        private string GetSaveDir() {
            try {
                var path = System.IO.Path.Combine(GenFilePaths.SaveDataFolderPath, "WorkGroups");
                var dirInfo = new DirectoryInfo(path);
                if (!dirInfo.Exists)
                    dirInfo.Create();

                return path;
            }
            catch (Exception ex) {
                Log.Error("Failed to get folder name - " + ex?.ToString());
                LogHelper.Error($"Unable to get save directory: {ex?.ToString()}");
                return (string) null;
            }
        }

        public IEnumerable<string> GetAllPresetSaves() {
            var ret = new List<string>();

            foreach (var preset in Directory.GetFiles(GetSaveDir(), "*.wg")) {
                ret.Add(Path.GetFileNameWithoutExtension(preset));
            }

            return ret;
        }

        public void SaveToPreset(string save) {
            var asmV = typeof(WorkGroup).Assembly.GetName().Version;
            var lines = new List<string>();
            lines.Add($"{asmV.Major}.{asmV.Minor}.{asmV.Revision}");
            lines.Add(Enabled.ToString());
            lines.Add(SetPrioritiesForSlaves.ToString());
            lines.Add(SetPrioritiesForPrisoners.ToString());
            lines.Add(SetPrioritiesForRjwWorkers.ToString());
            lines.Add(SetPawnTitles.ToString());
            lines.Add(MaxPriority.ToString());
            lines.Add(HoursUpdateInterval.ToString());
            lines.Add(ClearOutSchedules.ToString());
            lines.Add(VerboseLogging.ToString());
            lines.Add(ForcedBedRestForInjuredPawns.ToString());

            int grpCounter = 1;
            foreach (var grp in WorkGroups) {
                lines.Add(grp.Name);

                var grpLines = new List<string>();
                grpLines.Add(grp.DisableTitleForThisWorkGroup.ToString());
                grpLines.Add(grp.TargetQuantity.ToString());
                grpLines.Add(grp.AssignToEveryone.ToString());
                grpLines.Add(grp.ColonistsAllowed.ToString());
                grpLines.Add(grp.SlavesAllowed.ToString());
                grpLines.Add(grp.PrisonersAllowed.ToString());
                grpLines.Add(grp.RjwWorkersAllowed.ToString());

                foreach (var wt in grp.Items) {
                    grpLines.Add(wt.defName);
                }

                grpLines.Add("---");

                foreach (var cbaw in grp.CanBeAssignedWith) {
                    grpLines.Add(cbaw);
                }

                DeleteIfExists(System.IO.Path.Combine(GetSaveDir(), save + ".wg" + grpCounter));
                System.IO.File.WriteAllLines(System.IO.Path.Combine(GetSaveDir(), save + ".wg." + grpCounter), grpLines);
                grpCounter++;
            }

            DeleteIfExists(Path.Combine(GetSaveDir(), save + ".wg"));
            File.WriteAllLines(Path.Combine(GetSaveDir(), save + ".wg"), lines);
        }

        private void DeleteIfExists(string path) {
            if (File.Exists(path))
                File.Delete(path);
        }

        public void LoadFromPreset(string save) {
            var lines = System.IO.File.ReadAllLines(System.IO.Path.Combine(GetSaveDir(), save + ".wg"));
            var version = lines[0];
            switch (version) {
                case "1.1.0":
                    Enabled = bool.Parse(lines[1]);
                    SetPrioritiesForSlaves = bool.Parse(lines[2]);
                    SetPrioritiesForPrisoners = bool.Parse(lines[3]);
                    SetPrioritiesForRjwWorkers = bool.Parse(lines[4]);
                    SetPawnTitles = bool.Parse(lines[5]);
                    MaxPriority = int.Parse(lines[6]);
                    HoursUpdateInterval = int.Parse(lines[7]);
                    ClearOutSchedules = bool.Parse(lines[8]);
                    VerboseLogging = bool.Parse(lines[9]);
                    ForcedBedRestForInjuredPawns = bool.Parse(lines[10]);

                    WorkGroups.Clear();

                    for (int i = 11; i < lines.Length; i++) {
                        WorkGroups.Add(GetWorkGroupFromSaveLine(lines[i], save, i-10));
                    }

                    break;
                default:
                    LogHelper.Error("Unknown version number of preset attempted to be loaded: " + version);
                    break;
            }
        }

        private WorkGroup GetWorkGroupFromSaveLine(string line, string save, int lineNum) {
            var lines = System.IO.File.ReadAllLines(System.IO.Path.Combine(GetSaveDir(), save + ".wg." + lineNum));
            var grp = new WorkGroup {
                Name = line,
                DisableTitleForThisWorkGroup = bool.Parse(lines[0]),
                TargetQuantity = int.Parse(lines[1]),
                AssignToEveryone = bool.Parse(lines[2]),
                ColonistsAllowed = bool.Parse(lines[3]),
                SlavesAllowed = bool.Parse(lines[4]),
                PrisonersAllowed = bool.Parse(lines[5]),
                RjwWorkersAllowed = bool.Parse(lines[6])
            };

            bool settingAllowedGroups = false;
            for (int i = 7; i < lines.Length; i++) {
                if (lines[i] == "---") {
                    settingAllowedGroups = true;
                    continue;
                }

                if (!settingAllowedGroups) {
                    var wt = GetSettings.AllWorkTypes.FirstOrDefault(t => t.defName == lines[i]);
                    if (wt != null)
                        grp.Items.Add(wt);
                }
                else {
                    grp.CanBeAssignedWith.Add(lines[i]);
                }
            }

            return grp;
        }
    }
}
