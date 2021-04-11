using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using RimWorld;
using The1nk.WorkGroups.Models;
using Verse;

namespace The1nk.WorkGroups {
    public class WorkGroupsMapComponent : MapComponent {
        
        private WorkGroupsSettings _settings;

        public static HediffDef SlaveHediff;
        public static MethodInfo RjwMethod;
        public static MethodInfo PlMethod;

        private long lastUpdateTick = 0;
        private long nextUpdateTick = 0;
        private bool prepped = false;
        

        public WorkGroupsMapComponent(Map map) : base(map) {
            var crp = new WorkGroupsSettings();
            _settings = WorkGroupsSettings.GetSettings;
            _settings.Component = this;
        }

        public override void ExposeData() {
            base.ExposeData();
            Scribe_Deep.Look(ref _settings, "WorkGroupsSettings", null);
        }

        public override void MapComponentTick() {
            base.MapComponentTick();
            Prep();

            if (Find.TickManager.CurTimeSpeed == TimeSpeed.Paused)
                return;
            if (Find.TickManager.TicksGame % 60 != 0)
                return;

            var thisTick = GenTicks.TicksGame;
            if (nextUpdateTick > thisTick)
                return;

            lastUpdateTick = thisTick;
            nextUpdateTick = thisTick + (_settings.HoursUpdateInterval * 2500); // 2500 ticks per in-game hour
         
            RunNow();
        }

        public void RunNow() {
            if (!_settings.Enabled)
                return;

            LogHelper.Info($"Fired at {lastUpdateTick}. Next at {nextUpdateTick}.");

            var pawns = FetchColonists();
            if (_settings.PlInstalled && _settings.SetPrioritiesForPrisoners)
                (pawns as List<PawnWithWorkgroups>).AddRange(FetchPrisoners());
            ClearWorkGroups(ref pawns);
            var madeChanges = false;
            while (UpdatePriorities(ref pawns))
                madeChanges = true;
            if (madeChanges) ApplyPriorities(ref pawns, _settings.SetPawnTitles);
        }

        private void Prep() {
            if (prepped)
                return;

            LogHelper.Verbose("+Prep()");

            if (_settings.WorkGroups == null)
                _settings.WorkGroups = new List<WorkGroup>();

            SlaveHediff = DefDatabase<HediffDef>.GetNamedSilentFail("Enslaved");
            LogHelper.Verbose("SS Type found? " + (SlaveHediff != null));
            _settings.SsInstalled = SlaveHediff != null;
            
            var rjwType = GenTypes.GetTypeInAnyAssembly("rjw.xxx", "rjw");
            LogHelper.Verbose("RJW Type found? " + (rjwType != null));
            if (rjwType != null)
                RjwMethod = rjwType.GetMethod("is_whore");

            _settings.RjwInstalled = RjwMethod != null;

            var plType = GenTypes.GetTypeInAnyAssembly("PrisonLabor.Core.PrisonLaborUtility", "PrisonLabor.Core");
            LogHelper.Verbose("Prison Labor Type found? " + (plType != null));
            if (plType != null)
                PlMethod = plType.GetMethod("LaborEnabled");
            _settings.PlInstalled = PlMethod != null;

            _settings.AllWorkTypes = FetchWorkTypes(ref _settings.AllWorkTypes);
            _settings.AllStatDefs = FetchStatDefs(ref _settings.AllStatDefs);

            if (!Current.Game.playSettings.useWorkPriorities) {
                Current.Game.playSettings.useWorkPriorities = true;
                foreach (var pawn in PawnsFinder.AllMapsWorldAndTemporary_Alive.Where(pawn =>
                    pawn.Faction == Faction.OfPlayer)) {
                    pawn.workSettings?.Notify_UseWorkPrioritiesChanged();
                }
            }

            foreach (var wg in _settings.WorkGroups) {
                for (int i = 0; i < wg.Items.Count; i++) {
                    var wt = wg.Items[i]; 
                    if (wt == null) {
                        LogHelper.Warning($"Found null work type on group '{wg.Name}', position {i + 1}. Removing..");
                        wg.Items.RemoveAt(i);
                        i--;
                    }
                    else {
                        if (!_settings.AllWorkTypes.Any(wtD => wtD == wt)) {
                            LogHelper.Warning($"Work type on group '{wg.Name}', position {i + 1}, missing from database. Removing..");
                            wg.Items.RemoveAt(i);
                            i--;
                        }
                    }
                }

                for (int i = 0; i < wg.ImportantStats.Count; i++) {
                    var sd = wg.ImportantStats[i];
                    if (sd == null) {
                        LogHelper.Warning($"Found null ImportantStat on group '{wg.Name}', position {i + 1}. Removing..");
                        wg.ImportantStats.RemoveAt(i);
                        i--;
                    }
                    else {
                        if (!_settings.AllStatDefs.Any(sdD => sdD == sd)) {
                            LogHelper.Warning($"ImportantStat on group '{wg.Name}', position {i + 1}, missing from database. Removing..");
                            wg.Items.RemoveAt(i);
                            i--;
                        }
                    }
                }
            }

            prepped = true;
            LogHelper.Verbose("-Prep()");
        }

        private IEnumerable<StatDef> FetchStatDefs(ref IEnumerable<StatDef> allStatDefs) {
            if (allStatDefs.Any())
                return allStatDefs;

            var sdList = allStatDefs as List<StatDef>;

            sdList.AddRange(DefDatabase<StatDef>.AllDefsListForReading.Where(d => !d.alwaysHide && d.showOnPawns)
                .OrderBy(d => d.category.displayOrder).ThenBy(d => d.displayPriorityInCategory));

            sdList.ForEach(d => LogHelper.Verbose($"--{d.LabelForFullStatListCap}, defName = '{d.defName}'"));

            return sdList;
        }

        private IEnumerable<WorkTypeDef> FetchWorkTypes(ref IEnumerable<WorkTypeDef> allWorkTypes) {
            if (allWorkTypes.Any())
                return allWorkTypes;

            var awtList = allWorkTypes as List<WorkTypeDef>;

            awtList.AddRange(DefDatabase<WorkTypeDef>.AllDefsListForReading.Where(d => d.visible)
                .OrderByDescending(d => d.naturalPriority));

            awtList.ForEach(w => LogHelper.Verbose($"--{w.labelShort}, defName = '{w.defName}'"));

            return allWorkTypes;
        }

        private void ApplyPriorities(ref IEnumerable<PawnWithWorkgroups> pawns, bool setPawnTitles) {
            LogHelper.Verbose("+ApplyPriorities()");
            foreach (var pawn in pawns) {
                if (pawn.Pawn.workSettings == null) {
                    LogHelper.Verbose($"--{pawn.Pawn.Name.ToStringShort} has null workSettings .. oops");
                    continue;
                }

                if (!pawn.Pawn.workSettings.EverWork) {
                    LogHelper.Verbose($"--{pawn.Pawn.Name.ToStringShort} has false EverWork");
                    continue;
                }

                var newTitle = new List<string>();

                if (_settings.ForcedBedRestForInjuredPawns && HealthAIUtility.ShouldSeekMedicalRest(pawn.Pawn)) {
                    foreach (var awt in _settings.AllWorkTypes) {
                        pawn.Pawn.workSettings.SetPriority(awt,
                            (awt.defName == "PatientBedRest" || awt.defName == "Patient") ? 1 : 0);
                    }
                }
                else {
                    var disabled = pawn.Pawn.GetDisabledWorkTypes();

                    // Clear out no-longer-assigned works
                    foreach (var wt in _settings.AllWorkTypes) {
                        if (!pawn.WorkGroups.Any(g => g.Items.Contains(wt)))
                            pawn.Pawn.workSettings.SetPriority(wt, 0);
                    }

                    var seenTypes = new List<WorkTypeDef>();

                    int currentPriority = 0;
                    foreach (var wg in pawn.WorkGroups) {
                        currentPriority++;

                        currentPriority = Math.Min(currentPriority, _settings.MaxPriority);
                        foreach (var wgi in wg.Items) {
                            if (seenTypes.Contains(wgi))
                                continue; // Only set each WorkType priority *once*. First-come-first-serve!!

                            if (!disabled.Contains(wgi)) {
                                pawn.Pawn.workSettings.SetPriority(wgi, currentPriority);
                                pawn.Pawn.workSettings.Notify_UseWorkPrioritiesChanged();
                                var priorityAfter = pawn.Pawn.workSettings.GetPriority(wgi);

                                if (priorityAfter != currentPriority)
                                    Log.Warning(
                                        $"Tried to set '{pawn.Pawn.Name.ToStringShort}'.'{wgi.labelShort}' to {currentPriority}, but it's still set to {priorityAfter}!");
                            }
                            seenTypes.Add(wgi);
                        }

                        if (!wg.DisableTitleForThisWorkGroup)
                            newTitle.Add(wg.Name);
                    }    
                }

                if (_settings.ClearOutSchedules)
                    for (int i = 0; i < 24; i++)
                        pawn.Pawn.timetable.SetAssignment(i, TimeAssignmentDefOf.Anything);

                if (setPawnTitles)
                    pawn.Pawn.story.Title = string.Join(",", newTitle);
            }
            LogHelper.Verbose("-ApplyPriorities()");
        }

        private void ClearWorkGroups(ref IEnumerable<PawnWithWorkgroups> pawns) {
            LogHelper.Verbose("+ClearWorkGroups()");
            foreach (var pawn in pawns) {
                pawn.WorkGroups = new List<WorkGroup>();
                
                if (_settings.SetPawnTitles)
                    pawn.Pawn.story.Title = string.Empty;
            }
            LogHelper.Verbose("-ClearWorkGroups()");
        }

        private bool UpdatePriorities(ref IEnumerable<PawnWithWorkgroups> pawns) {
            LogHelper.Verbose("+UpdatePriorities()");
            var changedSomething = false;

            foreach (var wg in _settings.WorkGroups) {
                if (wg.TargetQuantity < 1)
                    wg.TargetQuantity = 1;

                if (wg.AssignToEveryone)
                {
                    LogHelper.Verbose($"- {wg.Name} - for everyone..");
                    foreach (var pawn in pawns.Where(p => !p.WorkGroups.Contains(wg))) {
                        LogHelper.Verbose($"-- {pawn.Pawn.Name.ToStringFull} - Yep");
                        (pawn.WorkGroups as List<WorkGroup>).Add(wg);
                        changedSomething = true;
                    }

                    continue;
                }

                for (int i = 0; i < wg.TargetQuantity; i++) {
                    PawnWithWorkgroups bestPawn = null;
                    float averageSkill = -1f;
                    LogHelper.Verbose($"- Looking for a {wg.Name}..");

                    var filteredPawns = pawns.Where(p => !p.WorkGroups.Contains(wg));
                    if (_settings.ForcedBedRestForInjuredPawns) {
                        var before = filteredPawns.Count();
                        filteredPawns = filteredPawns.Where(p => !HealthAIUtility.ShouldSeekMedicalRest(p.Pawn));
                        var after = filteredPawns.Count();

                        if (before != after)
                            LogHelper.Verbose(
                                $"Filtered out {before - after} pawns due to recovering and ForcedBedRestForInjuredPawns");
                    }

                    if (!_settings.SsInstalled ||
                        (_settings.SsInstalled && !wg.SlavesAllowed)) {
                        var before = filteredPawns.Count();
                        filteredPawns = filteredPawns.Where(p => !p.IsSlave);
                        var after = filteredPawns.Count();

                        if (before != after)
                            LogHelper.Verbose(
                                $"Filtered out {before - after} slaves due to WorkGroup setting disabled");
                    }

                    if (_settings.RjwInstalled && !wg.RjwWorkersAllowed) {
                        var before = filteredPawns.Count();
                        filteredPawns = filteredPawns.Where(p => !p.IsRjwWorker);
                        var after = filteredPawns.Count();

                        if (before != after)
                            LogHelper.Verbose(
                                $"Filtered out {before - after} RJW Workers due to WorkGroup setting disabled");
                    }

                    if (!_settings.PlInstalled ||
                        (_settings.PlInstalled && !wg.PrisonersAllowed)) {
                        var before = filteredPawns.Count();
                        filteredPawns = filteredPawns.Where(p => !p.IsPrisoner);
                        var after = filteredPawns.Count();

                        if (before != after)
                            LogHelper.Verbose(
                                $"Filtered out {before - after} Prisoners due to WorkGroup setting disabled");
                    }

                    foreach (var pawn in filteredPawns) {
                        if (pawn.Pawn.Downed || pawn.Pawn.Dead || pawn.Pawn.InMentalState)
                            continue;

                        var disabled = pawn.Pawn.GetDisabledWorkTypes();
                        if (wg.Items.All(w => disabled.Contains(w))) {
                            LogHelper.Verbose($"-- {pawn.Pawn.Name.ToStringFull} - Nope - All WorkTypes disabled for this dude");
                            continue;
                        }

                        if (pawn.WorkGroups.Any()) {
                            if (pawn.WorkGroups.Any(wg3 =>
                                !wg3.AssignToEveryone && !wg3.CanBeAssignedWith.Contains(wg.Name))) {
                                LogHelper.Verbose($"-- {pawn.Pawn.Name.ToStringFull} - Nope - WorkGroups already assigned that don't mesh with this");
                                continue;
                            }
                        }

                        float thisPawnsSkill = 0f;
                        int cnt = 0;

                        foreach (var wgItem in wg.Items) {
                            if (wgItem.relevantSkills.Any())
                                foreach (var skill in wgItem.relevantSkills) {
                                    var multiplier = 1f;
                                    var pawnSkill = pawn.Pawn.skills.GetSkill(skill);

                                    if (_settings.UseLearningRates)
                                        multiplier = pawnSkill.LearnRateFactor();

                                    thisPawnsSkill += multiplier * (pawnSkill.Level + 1); // This stupid +1 insures that if a pawn's level is 0, their stats still matter in the ImportantStats section below
                                }
                            else
                                thisPawnsSkill += 3f;

                            cnt++;
                        }

                        thisPawnsSkill /= cnt;

                        foreach (var importantStat in wg.ImportantStats) {
                            thisPawnsSkill *= pawn.Pawn.GetStatValue(importantStat);
                        }

                        if (!(thisPawnsSkill > averageSkill)) continue;
                        bestPawn = pawn;
                        averageSkill = thisPawnsSkill;
                    }

                    if (bestPawn != null) {
                        LogHelper.Verbose($"-- {bestPawn.Pawn.Name.ToStringFull} - Yep");
                        (bestPawn.WorkGroups as List<WorkGroup>).Add(wg);
                        changedSomething = true;
                    }
                }
            }

            LogHelper.Verbose($"-UpdatePriorities() -- changedSomething={changedSomething}");
            return changedSomething;
        }

        private IEnumerable<PawnWithWorkgroups> FetchColonists() {
            LogHelper.Verbose("+FetchPawns()");
            var ret = new List<PawnWithWorkgroups>();

            ret.AddRange(map.mapPawns.FreeColonistsSpawned.Select(p => new PawnWithWorkgroups(p)));

            if (_settings.SsInstalled && !_settings.SetPrioritiesForSlaves) {
                var before = ret.Count();
                ret.RemoveAll(p => p.IsSlave);
                var after = ret.Count();

                if (before != after)
                    LogHelper.Verbose($"Filtered out {before - after} slaves due to global setting disabled");
            }

            if (_settings.RjwInstalled && !_settings.SetPrioritiesForRjwWorkers) {
                var before = ret.Count();
                ret.RemoveAll(p => p.IsRjwWorker);
                var after = ret.Count();

                if (before != after)
                    LogHelper.Verbose($"Filtered out {before - after} RJW Workers due to global setting disabled");
            }

            LogHelper.Verbose("-FetchPawns()");
            return ret;
        }

        private IEnumerable<PawnWithWorkgroups> FetchPrisoners() {
            LogHelper.Verbose("+FetchPrisoners()");
            var ret = new List<PawnWithWorkgroups>();

            ret.AddRange(map.mapPawns.PrisonersOfColonySpawned.Select(p => new PawnWithWorkgroups(p)));

            if (_settings.RjwInstalled && !_settings.SetPrioritiesForRjwWorkers) {
                var before = ret.Count();
                ret.RemoveAll(p => p.IsRjwWorker);
                var after = ret.Count();

                if (before != after)
                    LogHelper.Verbose($"Filtered out {before - after} (Prisoner) RJW Workers due to global setting disabled");
            }

            if (!_settings.PlInstalled) {
                var before = ret.Count();
                ret.RemoveAll(p => p.IsPrisoner);
                var after = ret.Count();

                if (before != after)
                    LogHelper.Verbose($"Filtered out {before - after} Prisoners due to no Prison Labor");
            } else {
                var removed = ret.RemoveAll(p => !p.IsWorkingPrisoner);
                if (removed > 0)
                    LogHelper.Verbose($"Filtered out {removed} Prisoners due to not being set to Work");
            }

            LogHelper.Verbose("-FetchPrisoners()");
            return ret;
        }
    }
}
