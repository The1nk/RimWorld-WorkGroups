using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
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
        }

        public override void ExposeData() {
            base.ExposeData();
            Scribe_Deep.Look(ref _settings, "WorkGroupsSettings", null);
        }

        public override void MapComponentTick() {
            base.MapComponentTick();
            Prep();

            if (!_settings.Enabled)
                return;
            if (Find.TickManager.CurTimeSpeed == TimeSpeed.Paused)
                return;
            if (Find.TickManager.TicksGame % 60 != 0)
                return;

            var thisTick = GenTicks.TicksGame;
            if (nextUpdateTick > thisTick)
                return;

            lastUpdateTick = thisTick;
            nextUpdateTick = thisTick + (_settings.HoursUpdateInterval * 2500); // 2500 ticks per in-game hour
            LogHelper.Info($"Fired at {lastUpdateTick}. Next at {nextUpdateTick}.");

            var pawns = FetchColonists();
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
            
            var rjwType = GenTypes.GetTypeInAnyAssembly("rjw.xxx", "rjw");
            LogHelper.Verbose("RJW Type found? " + (rjwType != null));
            if (rjwType != null)
                RjwMethod = rjwType.GetMethod("is_whore");

            var plType = GenTypes.GetTypeInAnyAssembly("PrisonLabor.Core.PrisonLaborUtility", "PrisonLabor.Core");
            LogHelper.Verbose("Prison Labor Type found? " + (plType != null));
            if (plType != null)
                PlMethod = plType.GetMethod("LaborEnabled");

            _settings.AllWorkTypes = FetchWorkTypes(ref _settings.AllWorkTypes);

            if (!Current.Game.playSettings.useWorkPriorities) {
                Current.Game.playSettings.useWorkPriorities = true;
                foreach (var pawn in PawnsFinder.AllMapsWorldAndTemporary_Alive.Where(pawn =>
                    pawn.Faction == Faction.OfPlayer)) {
                    pawn.workSettings?.Notify_UseWorkPrioritiesChanged();
                }
            }

            if (rjwType != null)
                _settings.RjwInstalled = true;

            prepped = true;
            LogHelper.Verbose("-Prep()");
        }

        private IEnumerable<WorkTypeDef> FetchWorkTypes(ref IEnumerable<WorkTypeDef> allWorkTypes) {
            if (!allWorkTypes.Any()) {
                var awtList = allWorkTypes as List<WorkTypeDef>;

                awtList.AddRange(DefDatabase<WorkTypeDef>.AllDefsListForReading.Where(d => d.visible)
                    .OrderByDescending(d => d.naturalPriority));

                awtList.ForEach(w => LogHelper.Verbose($"--{w.labelShort}"));
            }

            return allWorkTypes;
        }

        private void ApplyPriorities(ref IEnumerable<PawnWithWorkgroups> pawns, bool setPawnTitles) {
            LogHelper.Verbose("+ApplyPriorities()");
            foreach (var pawn in pawns) {
                if (!pawn.Pawn.workSettings.EverWork) continue;

                var newTitle = new List<string>();
                var disabled = pawn.Pawn.GetDisabledWorkTypes();

                foreach (var wt in _settings.AllWorkTypes) {
                    pawn.Pawn.workSettings.SetPriority(wt, 0);
                }

                var seenTypes = new System.Collections.Generic.List<WorkTypeDef>();

                int currentPriority = 0;
                foreach (var wg in pawn.WorkGroups) {
                    currentPriority++;

                    currentPriority = Math.Min(currentPriority, _settings.MaxPriority);
                    foreach (var wgi in wg.Items) {
                        if (seenTypes.Contains(wgi))
                            continue; // Only set each WorkType priority *once*. First-come-first-serve!!

                        if (!disabled.Contains(wgi))
                            pawn.Pawn.workSettings.SetPriority(wgi, currentPriority);
                        seenTypes.Add(wgi);
                    }

                    if (!wg.DisableTitleForThisWorkGroup)
                        newTitle.Add(wg.Name);
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
                if (!(pawn.WorkGroups is IList<WorkGroup> wgList)) {
                    wgList = new List<WorkGroup>();
                    pawn.WorkGroups = wgList;
                    pawn.Pawn.story.Title = string.Empty;
                }

                wgList.Clear();
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
                    foreach (var pawn in pawns.Where(p => !p.WorkGroups.Contains(wg))) {
                        (pawn.WorkGroups as List<WorkGroup>).Add(wg);
                        changedSomething = true;
                    }

                    continue;
                }

                for (int i = 0; i < wg.TargetQuantity; i++) {
                    PawnWithWorkgroups bestPawn = null;
                    float averageSkill = -1f;
                    LogHelper.Verbose($"Looking for a {wg.Name}..");

                    var filteredPawns = pawns.Where(p => !p.WorkGroups.Contains(wg));
                    if (SlaveHediff != null && !wg.SlavesAllowed) {
                        var before = filteredPawns.Count();
                        filteredPawns = filteredPawns.Where(p => !p.IsSlave);
                        var after = filteredPawns.Count();

                        if (before != after)
                            LogHelper.Verbose(
                                $"Filtered out {before - after} slaves due to WorkGroup setting disabled");
                    }

                    if (RjwMethod != null && !wg.RjwWorkersAllowed) {
                        var before = filteredPawns.Count();
                        filteredPawns = filteredPawns.Where(p => !p.IsRjwWorker);
                        var after = filteredPawns.Count();

                        if (before != after)
                            LogHelper.Verbose(
                                $"Filtered out {before - after} RJW Workers due to WorkGroup setting disabled");
                    }

                    if (PlMethod !=null && !wg.PrisonersAllowed) {
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

                        LogHelper.Verbose($"Considering {pawn.Pawn.Name.ToStringFull}");

                        var disabled = pawn.Pawn.GetDisabledWorkTypes();
                        if (wg.Items.All(w => disabled.Contains(w))) {
                            LogHelper.Verbose($"Nope - All WorkTypes disabled for this dude");
                            continue;
                        }

                        if (pawn.WorkGroups.Any()) {
                            if (pawn.WorkGroups.Any(wg3 =>
                                !wg3.AssignToEveryone && !wg3.CanBeAssignedWith.Contains(wg.Name))) {
                                LogHelper.Verbose($"Nope - WorkGroups already assigned that don't mesh with this");
                                continue;
                            }
                        }

                        float thisPawnsSkill = 0f;
                        int cnt = 0;

                        foreach (var wgItem in wg.Items) {
                            thisPawnsSkill += pawn.Pawn.skills.AverageOfRelevantSkillsFor(wgItem);
                            cnt++;
                        }

                        thisPawnsSkill /= cnt;

                        if (!(thisPawnsSkill > averageSkill)) continue;
                        bestPawn = pawn;
                        averageSkill = thisPawnsSkill;
                    }

                    if (bestPawn != null) {
                        LogHelper.Verbose($"Selected {bestPawn.Pawn.Name.ToStringFull}");
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

            if (SlaveHediff != null && !_settings.SetPrioritiesForSlaves) {
                var before = ret.Count();
                ret.RemoveAll(p => p.IsSlave);
                var after = ret.Count();

                if (before != after)
                    LogHelper.Verbose($"Filtered out {before - after} slaves due to global setting disabled");
            }

            if (RjwMethod != null && !_settings.SetPrioritiesForRjwWorkers) {
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

            if (RjwMethod != null && !_settings.SetPrioritiesForRjwWorkers) {
                var before = ret.Count();
                ret.RemoveAll(p => p.IsRjwWorker);
                var after = ret.Count();

                if (before != after)
                    LogHelper.Verbose($"Filtered out {before - after} (Prisoner) RJW Workers due to global setting disabled");
            }

            if (PlMethod != null && !_settings.SetPrioritiesForPrisoners) {
                var before = ret.Count();
                ret.RemoveAll(p => p.IsPrisoner);
                var after = ret.Count();

                if (before != after)
                    LogHelper.Verbose($"Filtered out {before - after} Prisoners due to global setting disabled");
            }else {
                var removed = ret.RemoveAll(p => !p.IsWorkingPrisoner);
                if (removed > 0)
                    LogHelper.Verbose($"Filtered out {removed} Prisoners due to not being set to Work");
            }

            LogHelper.Verbose("-FetchPrisoners()");
            return ret;
        }
    }
}
