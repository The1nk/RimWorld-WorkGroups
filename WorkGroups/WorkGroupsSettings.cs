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
        private static WorkGroupsSettings _instance;

        private bool _ssInstalled;
        private bool _plInstalled;
        private bool _rjwInstalled;
        private bool _pbInstalled;

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

        public bool PbInstalled {
            get => _pbInstalled;
            set {
                if (value == false)
                    SetBadges = false;

                _pbInstalled = value;
            }
        }

        

        public IEnumerable<StatDef> AllStatDefs = new List<StatDef>();
        public IEnumerable<WorkTypeDef> AllWorkTypes = new List<WorkTypeDef>();
        public IEnumerable<Trait> AllTraits = new List<Trait>();
        public IEnumerable<PawnBadge> AllBadges = new List<PawnBadge>();
        
        public bool Enabled = false;
        public bool SetPrioritiesForSlaves = true; // Simple Slavery
        public bool SetPrioritiesForPrisoners = true; // Prison Labor
        public bool SetPrioritiesForRjwWorkers = true; // RJW
        public bool SetBadges; // Pawn Badges
        public bool SetPawnTitles = true;
        public int MaxPriority = 4;
        public int HoursUpdateInterval = 2;
        public bool ClearOutSchedules = true;
        public List<WorkGroup> WorkGroups;
        public bool VerboseLogging = false;
        public bool ForcedBedRestForInjuredPawns = true;
        public bool UseLearningRates;

        private string _baseDir = "";

        public WorkGroupsSettings() {
            WorkGroups = new List<WorkGroup>();
        }

        public void ExposeData() {
            Scribe_Values.Look(ref Enabled, "Enabled", false, true);
            Scribe_Values.Look(ref SetPrioritiesForSlaves, "SetPrioritiesForSlaves", true, true);
            Scribe_Values.Look(ref SetPrioritiesForPrisoners, "SetPrioritiesForPrisoners", true, true);
            Scribe_Values.Look(ref SetPrioritiesForRjwWorkers, "SetPrioritiesForRjwWorkers", false, false);
            Scribe_Values.Look(ref SetBadges, "SetBadges", false, false);
            Scribe_Values.Look(ref SetPawnTitles, "SetPawnTitles", true, true);
            Scribe_Values.Look(ref MaxPriority, "MaxPriority", 4, true);
            Scribe_Values.Look(ref HoursUpdateInterval, "HoursUpdateInterval", 2, true);
            Scribe_Values.Look(ref ClearOutSchedules, "ClearOutSchedules", true, true);
            Scribe_Values.Look(ref ForcedBedRestForInjuredPawns, "ForcedBedRestForInjuredPawns", true, false);
            Scribe_Collections.Look(ref WorkGroups, "WorkGroups", LookMode.Deep);
            Scribe_Values.Look(ref VerboseLogging, "VerboseLogging", false, true);
            Scribe_Values.Look(ref UseLearningRates, "UseLearningRates", false, true);

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
            lines.Add(UseLearningRates.ToString());
            lines.Add(SetBadges.ToString());

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

                grpLines.Add("---");

                foreach (var stat in grp.HighStats) {
                    grpLines.Add(stat.defName);
                }

                grpLines.Add("---");

                foreach (var stat in grp.LowStats) {
                    grpLines.Add(stat.defName);
                }

                grpLines.Add("---");

                foreach (var trait in grp.TraitsMustHave) {
                    grpLines.Add(trait.def.defName + "___" + trait.Degree);
                }

                grpLines.Add("---");

                foreach (var trait in grp.TraitsWantToHave) {
                    grpLines.Add(trait.def.defName + "___" + trait.Degree);
                }

                grpLines.Add("---");

                foreach (var trait in grp.TraitsCantHave) {
                    grpLines.Add(trait.def.defName + "___" + trait.Degree);
                }

                grpLines.Add("---");
                grpLines.Add(grp.Badge);

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
                    UseLearningRates = bool.Parse(lines[11]);

                    WorkGroups.Clear();

                    for (int i = 12; i < lines.Length; i++) {
                        WorkGroups.Add(GetWorkGroupFromSaveLine1dot1(lines[i], save, i-11));
                    }

                    break;
                case "1.2.0":
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
                    UseLearningRates = bool.Parse(lines[11]);

                    WorkGroups.Clear();

                    for (int i = 12; i < lines.Length; i++) {
                        WorkGroups.Add(GetWorkGroupFromSaveLine1dot2(lines[i], save, i-11));
                    }

                    break;
                case "1.3.0":
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
                    UseLearningRates = bool.Parse(lines[11]);
                    SetBadges = bool.Parse(lines[12]);

                    WorkGroups.Clear();

                    for (int i = 13; i < lines.Length; i++) {
                        WorkGroups.Add(GetWorkGroupFromSaveLine1dot3(lines[i], save, i-12));
                    }

                    break;
                default:
                    LogHelper.Error("Unknown version number of preset attempted to be loaded: " + version);
                    break;
            }
        }

        private WorkGroup GetWorkGroupFromSaveLine1dot1(string line, string save, int lineNum) {
            var lines = System.IO.File.ReadAllLines(System.IO.Path.Combine(GetSaveDir(), save + ".wg." + lineNum));
            var grp = new WorkGroup (line) {
                DisableTitleForThisWorkGroup = bool.Parse(lines[0]),
                TargetQuantity = int.Parse(lines[1]),
                AssignToEveryone = bool.Parse(lines[2]),
                ColonistsAllowed = bool.Parse(lines[3]),
                SlavesAllowed = bool.Parse(lines[4]),
                PrisonersAllowed = bool.Parse(lines[5]),
                RjwWorkersAllowed = bool.Parse(lines[6])
            };

            var mode = 1;
            for (int i = 7; i < lines.Length; i++) {
                if (lines[i] == "---") {
                    mode++;
                    continue;
                }

                switch (mode) {
                    case 1:
                        var wt = GetSettings().AllWorkTypes.FirstOrDefault(t => t.defName == lines[i]);
                        if (wt != null)
                            grp.Items.Add(wt);
                        break;

                    case 2:
                        grp.CanBeAssignedWith.Add(lines[i]);
                        break;

                    case 3:
                        var sd = GetSettings().AllStatDefs.FirstOrDefault(s => s.defName == lines[i]);
                        if (sd != null)
                            grp.HighStats.Add(sd); // ImportantStats turn into HighStats
                        break;
                }
            }

            return grp;
        }

        private WorkGroup GetWorkGroupFromSaveLine1dot2(string line, string save, int lineNum) {
            var lines = System.IO.File.ReadAllLines(System.IO.Path.Combine(GetSaveDir(), save + ".wg." + lineNum));
            var grp = new WorkGroup (line) {
                DisableTitleForThisWorkGroup = bool.Parse(lines[0]),
                TargetQuantity = int.Parse(lines[1]),
                AssignToEveryone = bool.Parse(lines[2]),
                ColonistsAllowed = bool.Parse(lines[3]),
                SlavesAllowed = bool.Parse(lines[4]),
                PrisonersAllowed = bool.Parse(lines[5]),
                RjwWorkersAllowed = bool.Parse(lines[6]),
                TraitsMustHave = new List<Trait>(),
                TraitsWantToHave = new List<Trait>(),
                TraitsCantHave = new List<Trait>()
            };

            var mode = 1;
            for (int i = 7; i < lines.Length; i++) {
                if (lines[i] == "---") {
                    mode++;
                    continue;
                }

                switch (mode) {
                    case 1:
                        var wt = GetSettings().AllWorkTypes.FirstOrDefault(t => t.defName == lines[i]);
                        if (wt != null)
                            grp.Items.Add(wt);
                        break;

                    case 2:
                        grp.CanBeAssignedWith.Add(lines[i]);
                        break;

                    case 3:
                        var sd = GetSettings().AllStatDefs.FirstOrDefault(s => s.defName == lines[i]);
                        if (sd != null)
                            grp.HighStats.Add(sd);
                        break;

                    case 4:
                        var sdd = GetSettings().AllStatDefs.FirstOrDefault(s => s.defName == lines[i]);
                        if (sdd != null)
                            grp.LowStats.Add(sdd);
                        break;

                    case 5:
                        var lineParts = lines[i].Split(new[] {"___"}, StringSplitOptions.None);
                        var traitDef = AllTraits.FirstOrDefault(t => t.def.defName == lineParts[0]).def;
                        if (traitDef != null) {
                            var t = new Trait(traitDef, int.Parse(lineParts[1]));
                            grp.TraitsMustHave.Add(t);
                        }

                        break;

                    case 6:
                        var lineParts2 = lines[i].Split(new[] {"___"}, StringSplitOptions.None);
                        var traitDef2 = AllTraits.FirstOrDefault(t => t.def.defName == lineParts2[0]).def;
                        if (traitDef2 != null) {
                            var t = new Trait(traitDef2, int.Parse(lineParts2[1]));
                            grp.TraitsWantToHave.Add(t);
                        }

                        break;

                    case 7:
                        var lineParts3 = lines[i].Split(new[] {"___"}, StringSplitOptions.None);
                        var traitDef3 = AllTraits.FirstOrDefault(t => t.def.defName == lineParts3[0]).def;
                        if (traitDef3 != null) {
                            var t = new Trait(traitDef3, int.Parse(lineParts3[1]));
                            grp.TraitsCantHave.Add(t);
                        }

                        break;
                }
            }

            return grp;
        }

        private WorkGroup GetWorkGroupFromSaveLine1dot3(string line, string save, int lineNum) {
            var lines = System.IO.File.ReadAllLines(System.IO.Path.Combine(GetSaveDir(), save + ".wg." + lineNum));
            var grp = new WorkGroup (line) {
                DisableTitleForThisWorkGroup = bool.Parse(lines[0]),
                TargetQuantity = int.Parse(lines[1]),
                AssignToEveryone = bool.Parse(lines[2]),
                ColonistsAllowed = bool.Parse(lines[3]),
                SlavesAllowed = bool.Parse(lines[4]),
                PrisonersAllowed = bool.Parse(lines[5]),
                RjwWorkersAllowed = bool.Parse(lines[6]),
                TraitsMustHave = new List<Trait>(),
                TraitsWantToHave = new List<Trait>(),
                TraitsCantHave = new List<Trait>(),
                Badge = ""
            };

            var mode = 1;
            for (int i = 7; i < lines.Length; i++) {
                if (lines[i] == "---") {
                    mode++;
                    continue;
                }

                switch (mode) {
                    case 1:
                        var wt = GetSettings().AllWorkTypes.FirstOrDefault(t => t.defName == lines[i]);
                        if (wt != null)
                            grp.Items.Add(wt);
                        break;

                    case 2:
                        grp.CanBeAssignedWith.Add(lines[i]);
                        break;

                    case 3:
                        var sd = GetSettings().AllStatDefs.FirstOrDefault(s => s.defName == lines[i]);
                        if (sd != null)
                            grp.HighStats.Add(sd);
                        break;

                    case 4:
                        var sdd = GetSettings().AllStatDefs.FirstOrDefault(s => s.defName == lines[i]);
                        if (sdd != null)
                            grp.LowStats.Add(sdd);
                        break;

                    case 5:
                        var lineParts = lines[i].Split(new[] {"___"}, StringSplitOptions.None);
                        var traitDef = AllTraits.FirstOrDefault(t => t.def.defName == lineParts[0]).def;
                        if (traitDef != null) {
                            var t = new Trait(traitDef, int.Parse(lineParts[1]));
                            grp.TraitsMustHave.Add(t);
                        }

                        break;

                    case 6:
                        var lineParts2 = lines[i].Split(new[] {"___"}, StringSplitOptions.None);
                        var traitDef2 = AllTraits.FirstOrDefault(t => t.def.defName == lineParts2[0]).def;
                        if (traitDef2 != null) {
                            var t = new Trait(traitDef2, int.Parse(lineParts2[1]));
                            grp.TraitsWantToHave.Add(t);
                        }

                        break;

                    case 7:
                        var lineParts3 = lines[i].Split(new[] {"___"}, StringSplitOptions.None);
                        var traitDef3 = AllTraits.FirstOrDefault(t => t.def.defName == lineParts3[0]).def;
                        if (traitDef3 != null) {
                            var t = new Trait(traitDef3, int.Parse(lineParts3[1]));
                            grp.TraitsCantHave.Add(t);
                        }

                        break;

                    case 8:
                        grp.Badge = lines[i];
                        break;
                }
            }

            return grp;
        }


        public static WorkGroupsSettings GetSettings() {
            return _instance;
        }

        public static void SetSettings(WorkGroupsSettings settings) {
            _instance = settings;
        }
    }
}
