using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using HugsLib;
using RimWorld;
using The1nk.WorkGroups.Models;
using UnityEngine;
using Verse;

namespace The1nk.WorkGroups {
    public class WorkGroupsMapComponent : MapComponent {
        public static HediffDef SlaveHediff;
        public static MethodInfo RjwMethod;
        public static MethodInfo PlMethod;
        private Type badgeCompType;

        private long lastUpdateTick = 0;
        private long nextUpdateTick = 0;

        public WorkGroupsSettings Settings;

        private static PropertyInfo badgeTextureProp;
        
        public WorkGroupsMapComponent(Map map) : base(map) {
            Settings = new WorkGroupsSettings();
            WorkGroupsSettings.SetSettings(Settings); // Needed for open game -> new map
            TickThreadQueue.EnqueueItem(Prep);
        }

        public override void ExposeData() {
            base.ExposeData();
            Scribe_Deep.Look(ref Settings, "WorkGroupsSettings", null);

            WorkGroupsSettings.SetSettings(Settings); // Needed for open game -> load save
            TickThreadQueue.EnqueueItem(Prep);
        }

        public override void MapComponentTick() {
            base.MapComponentTick();
            TickThreadQueue.DoOne();

            if (Find.TickManager.CurTimeSpeed == TimeSpeed.Paused)
                return;
            if (Find.TickManager.TicksGame % 60 != 0)
                return;

            var thisTick = GenTicks.TicksGame;
            if (nextUpdateTick > thisTick)
                return;

            RunNow();
        }

        public void RunNow() {
            Settings = WorkGroupsSettings.GetSettings();

            var thisTick = GenTicks.TicksGame;

            lastUpdateTick = thisTick;
            nextUpdateTick = thisTick + (Settings.HoursUpdateInterval * 2500); // 2500 ticks per in-game hour

            if (!Settings.Enabled)
                return;

            LogHelper.Info($"Firing at {lastUpdateTick}. Next at {nextUpdateTick}.");

            var pawns = FetchColonists();
            if (Settings.PlInstalled && Settings.SetPrioritiesForPrisoners)
                (pawns as List<PawnWithWorkgroups>).AddRange(FetchPrisoners());
            ClearWorkGroups(ref pawns);
            while (true) {
                if (!UpdatePriorities(ref pawns))
                    break;
            }
            ApplyPriorities(ref pawns, Settings.SetPawnTitles);

            LogHelper.Info($"Done!");
        }

        private void Prep() {
            LogHelper.Verbose("+Prep()");

            if (Settings.WorkGroups == null)
                Settings.WorkGroups = new List<WorkGroup>();

            SlaveHediff = DefDatabase<HediffDef>.GetNamedSilentFail("Enslaved");
            LogHelper.Verbose("SS Type found? " + (SlaveHediff != null));
            Settings.SsInstalled = SlaveHediff != null;
            
            var rjwType = GenTypes.GetTypeInAnyAssembly("rjw.xxx", "rjw");
            LogHelper.Verbose("RJW Type found? " + (rjwType != null));
            if (rjwType != null)
                RjwMethod = rjwType.GetMethod("is_whore");

            Settings.RjwInstalled = RjwMethod != null;

            var plType = GenTypes.GetTypeInAnyAssembly("PrisonLabor.Core.PrisonLaborUtility", "PrisonLabor.Core");
            LogHelper.Verbose("Prison Labor Type found? " + (plType != null));
            if (plType != null)
                PlMethod = plType.GetMethod("LaborEnabled");
            Settings.PlInstalled = PlMethod != null;

            var badgeDefType = GenTypes.GetTypeInAnyAssembly("RR_PawnBadge.BadgeDef", "RR_PawnBadge");
            LogHelper.Verbose($"Pawn Badge found ? {badgeDefType != null}");

            if (badgeDefType != null) {
                Settings.PbInstalled = true;
                var pawnBadgeComp = GenTypes.GetTypeInAnyAssembly("RR_PawnBadge.CompBadge", "RR_PawnBadge");

                if (pawnBadgeComp != null)
                    badgeCompType = pawnBadgeComp;
            }

            if (Settings.AllBadges == null)
                Settings.AllBadges = new List<PawnBadge>();

            Settings.AllBadges = FetchBadges(ref Settings.AllBadges, badgeDefType);
            Settings.AllWorkTypes = FetchWorkTypes(ref Settings.AllWorkTypes);
            Settings.AllStatDefs = FetchStatDefs(ref Settings.AllStatDefs);
            Settings.AllTraits = FetchTraitDefs(ref Settings.AllTraits);

            if (!Current.Game.playSettings.useWorkPriorities) {
                Current.Game.playSettings.useWorkPriorities = true;
                foreach (var pawn in PawnsFinder.AllMapsWorldAndTemporary_Alive.Where(pawn =>
                    pawn.Faction == Faction.OfPlayer)) {
                    pawn.workSettings?.Notify_UseWorkPrioritiesChanged();
                }
            }

            foreach (var wg in Settings.WorkGroups) {
                if (wg.ImportantStats == null)
                    wg.ImportantStats = new List<StatDef>(); // Upgrade from 1.0 to 1.1

                for (int i = 0; i < wg.Items.Count; i++) {
                    var wt = wg.Items[i]; 
                    if (wt == null) {
                        LogHelper.Warning($"Found null work type on group '{wg.Name}', position {i + 1}. Removing..");
                        wg.Items.RemoveAt(i);
                        i--;
                    }
                    else {
                        if (!Settings.AllWorkTypes.Any(wtD => wtD == wt)) {
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
                        continue;
                    }
                    else {
                        if (!Settings.AllStatDefs.Any(sdD => sdD == sd)) {
                            LogHelper.Warning($"ImportantStat on group '{wg.Name}', position {i + 1}, missing from database. Removing..");
                            wg.Items.RemoveAt(i);
                            i--;
                            continue;
                        }
                    }

                    // Convert ImportantStats to HighStats
                    if (wg.HighStats == null)
                        wg.HighStats = new List<StatDef>();
                    if (wg.LowStats == null)
                        wg.LowStats = new List<StatDef>();
                    if (wg.TraitsMustHave == null)
                        wg.TraitsMustHave = new List<Trait>();
                    if (wg.TraitsWantToHave == null)
                        wg.TraitsWantToHave = new List<Trait>();
                    if (wg.TraitsCantHave == null)
                        wg.TraitsCantHave = new List<Trait>();

                    wg.HighStats.Add(wg.ImportantStats[i]);
                    wg.ImportantStats.RemoveAt(i);
                    i--;
                }
            }

            LogHelper.Verbose("-Prep()");
        }

        private IEnumerable<PawnBadge> FetchBadges(ref IEnumerable<PawnBadge> settingsAllBadges, Type badgeDefType) {
            var ret = new List<PawnBadge>();

            if (badgeDefType == null)
                return ret;

            if (badgeTextureProp == null)
                badgeTextureProp = badgeDefType.GetProperty("Symbol", BindingFlags.Instance | BindingFlags.Public);

            if (badgeTextureProp == null) {
                LogHelper.Error("Failed to get PropertyInfo for Symbol from Pawn Badge! :(");
                return ret;
            }

            foreach (var def in GenDefDatabase.GetAllDefsInDatabaseForDef(badgeDefType)) {
                var texture = (Texture2D) badgeTextureProp.GetValue(def);

                ret.Add(new PawnBadge(def, texture));
            }

            return ret;
        }

        private IEnumerable<Trait> FetchTraitDefs(ref IEnumerable<Trait> allTraitDefs) {
            var tdList = allTraitDefs as List<Trait> ?? new List<Trait>();

            tdList.Clear();

            foreach (var td in DefDatabase<TraitDef>.AllDefs) {
                foreach (var degree in td.degreeDatas) {
                    tdList.Add(new Trait(td, degree.degree));
                }
            }

            tdList = tdList.OrderBy(t => t.CurrentData.label).ToList();

            return tdList;
        }

        private IEnumerable<StatDef> FetchStatDefs(ref IEnumerable<StatDef> allStatDefs) {
            var sdList = allStatDefs as List<StatDef>;

            sdList.Clear();

            sdList.AddRange(DefDatabase<StatDef>.AllDefsListForReading.Where(d => !d.alwaysHide && d.showOnPawns)
                .OrderBy(d => d.category.displayOrder).ThenBy(d => d.displayPriorityInCategory));

            return sdList;
        }

        private IEnumerable<WorkTypeDef> FetchWorkTypes(ref IEnumerable<WorkTypeDef> allWorkTypes) {
            var awtList = allWorkTypes as List<WorkTypeDef>;

            awtList.Clear();

            awtList.AddRange(DefDatabase<WorkTypeDef>.AllDefsListForReading.Where(d => d.visible)
                .OrderByDescending(d => d.naturalPriority));

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

                pawn.Pawn.workSettings.Notify_UseWorkPrioritiesChanged();
                pawn.Pawn.Notify_DisabledWorkTypesChanged();
                pawn.Pawn.workSettings.EnableAndInitialize();
                pawn.Pawn.workSettings.DisableAll();

                Settings = WorkGroupsSettings.GetSettings();

                if (Settings.ForcedBedRestForInjuredPawns && HealthAIUtility.ShouldSeekMedicalRest(pawn.Pawn)) {
                    foreach (var awt in WorkGroupsSettings.GetSettings().AllWorkTypes) {
                        int priority = 0;

                        if (awt.defName == "PatientBedRest" || awt.defName == "Patient")
                            priority = 1;

                        pawn.Pawn.workSettings.SetPriority(awt, priority);
                    }

                    newTitle.Add("Resting");
                }
                else {
                    var disabled = pawn.Pawn.GetDisabledWorkTypes();
                    var seenTypes = new List<WorkTypeDef>();
                    int currentPriority = 0;
                    foreach (var wg in pawn.WorkGroups) {
                        currentPriority++;

                        currentPriority = Math.Min(currentPriority, Settings.MaxPriority);
                        
                        foreach (var wgi in wg.Items) {
                            if (seenTypes.Contains(wgi)) {
                                LogHelper.Verbose($"Already seen {wgi.labelShort}, bailing out");
                                continue; // Only set each WorkType priority *once*. First-come-first-serve!!
                            }

                            if (!disabled.Contains(wgi)) {
                                var was = pawn.Pawn.workSettings.GetPriority(wgi);
                                pawn.Pawn.workSettings.SetPriority(wgi, currentPriority);
                                var isNow = pawn.Pawn.workSettings.GetPriority(wgi);

                                if (isNow != currentPriority)
                                    Log.Warning(
                                        $"Tried to set '{pawn.Pawn.Name.ToStringShort}'.'{wgi.labelShort}' to {currentPriority}, but it's still set to {isNow:0}!");
                            }

                            seenTypes.Add(wgi);
                        }

                        if (!wg.DisableTitleForThisWorkGroup)
                            newTitle.Add(wg.Name);
                    }
                }

                if (Settings.ClearOutSchedules)
                    for (int i = 0; i < 24; i++)
                        pawn.Pawn.timetable.SetAssignment(i, TimeAssignmentDefOf.Anything);

                if (setPawnTitles)
                    pawn.Pawn.story.Title = string.Join(",", newTitle);

                if (Settings.SetBadges) {
                    var targetBadge = pawn.WorkGroups.FirstOrDefault(wg => !string.IsNullOrEmpty(wg.Badge))?.Badge ??
                                      "";
                    SetPawnBadge(pawn, targetBadge);
                }

                LogHelper.Verbose($"{pawn.Pawn.Name.ToStringShort} - {string.Join(",", newTitle)}");

                // Force re-caching of workgivers
                pawn.Pawn.workSettings.Notify_UseWorkPrioritiesChanged();
                pawn.Pawn.Notify_DisabledWorkTypesChanged();
                var tmp = pawn.Pawn.workSettings.WorkGiversInOrderEmergency;
                var tmp2 = pawn.Pawn.workSettings.WorkGiversInOrderNormal;
            }
            LogHelper.Verbose("-ApplyPriorities()");
        }

        private void SetPawnBadge(PawnWithWorkgroups pawn, string badge) {
            var pawnComp = pawn.Pawn.AllComps.FirstOrDefault(c => c.GetType() == badgeCompType);

            if (pawnComp == null)
                return;

            var field = badgeCompType.GetField("badges", BindingFlags.Public | BindingFlags.Instance);

            if (field == null)
                return;

            var currentValue = (string[]) field.GetValue(pawnComp); // So we don't lose the manually selected 2nd badge
            field.SetValue(pawnComp, new[] {badge, currentValue[1]});
        }

        private void ClearWorkGroups(ref IEnumerable<PawnWithWorkgroups> pawns) {
            LogHelper.Verbose("+ClearWorkGroups()");
            foreach (var pawn in pawns) {
                pawn.WorkGroups = new List<WorkGroup>();
                
                if (Settings.SetPawnTitles)
                    pawn.Pawn.story.Title = string.Empty;
            }
            LogHelper.Verbose("-ClearWorkGroups()");
        }

        private bool UpdatePriorities(ref IEnumerable<PawnWithWorkgroups> pawns) {
            LogHelper.Verbose("+UpdatePriorities()");
            var changedSomething = false;

            foreach (var wg in Settings.WorkGroups) {
                if (wg.TargetQuantity < 1)
                    wg.TargetQuantity = 1;

                for (int i = 0; i < wg.TargetQuantity; i++) {
                    PawnWithWorkgroups bestPawn = null;
                    float averageSkill = -1f;
                    LogHelper.Verbose($"- Looking for a {wg.Name}.. " + (wg.AssignToEveryone ? " everyone!" : ""));

                    var filteredPawns = pawns.Where(p => !p.WorkGroups.Contains(wg));
                    if (Settings.ForcedBedRestForInjuredPawns) {
                        var before = filteredPawns.Count();
                        filteredPawns = filteredPawns.Where(p => !HealthAIUtility.ShouldSeekMedicalRest(p.Pawn));
                        var after = filteredPawns.Count();

                        if (before != after)
                            LogHelper.Verbose(
                                $"Filtered out {before - after} pawns due to recovering and ForcedBedRestForInjuredPawns");
                    }

                    if (!wg.ColonistsAllowed) {
                        var before = filteredPawns.Count();
                        filteredPawns = filteredPawns.Where(p =>
                            !p.IsColonist || // Not a colonist .. or 
                            (p.IsColonist && Settings.SsInstalled && p.IsSlave) || // A colonist, but is also a slave
                            (p.IsColonist && Settings.PlInstalled && p.IsPrisoner)); // A colonist, but also a prisoner
                        var after = filteredPawns.Count();

                        if (before != after)
                            LogHelper.Verbose(
                                $"Filtered out {before - after} colonists (not slave colonists or prisoner colonists) due to WorkGroup setting disabled");
                    }

                    if (!Settings.SsInstalled ||
                        (Settings.SsInstalled && !wg.SlavesAllowed)) {
                        var before = filteredPawns.Count();
                        filteredPawns = filteredPawns.Where(p => !p.IsSlave);
                        var after = filteredPawns.Count();

                        if (before != after)
                            LogHelper.Verbose(
                                $"Filtered out {before - after} slaves due to WorkGroup setting disabled");
                    }

                    if (Settings.RjwInstalled && !wg.RjwWorkersAllowed) {
                        var before = filteredPawns.Count();
                        filteredPawns = filteredPawns.Where(p => !p.IsRjwWorker);
                        var after = filteredPawns.Count();

                        if (before != after)
                            LogHelper.Verbose(
                                $"Filtered out {before - after} RJW Workers due to WorkGroup setting disabled");
                    }

                    if (!Settings.PlInstalled ||
                        (Settings.PlInstalled && !wg.PrisonersAllowed)) {
                        var before = filteredPawns.Count();
                        filteredPawns = filteredPawns.Where(p => !p.IsPrisoner);
                        var after = filteredPawns.Count();

                        if (before != after)
                            LogHelper.Verbose(
                                $"Filtered out {before - after} Prisoners due to WorkGroup setting disabled");
                    }

                    filteredPawns = filteredPawns.Where(p =>
                        !p.Pawn.Downed && !p.Pawn.Dead && !p.Pawn.InMentalState);

                    if (wg.AssignToEveryone) {
                        foreach (var pawn in filteredPawns) {
                            LogHelper.Verbose($"-- {pawn.Pawn.Name.ToStringFull} - Yep");
                            (pawn.WorkGroups as List<WorkGroup>).Add(wg);
                        }

                        break;
                    }

                    foreach (var trait in wg.TraitsMustHave) {
                        var newFiltered = filteredPawns.ToList();

                        foreach (var p in filteredPawns.Where(p => !p.Pawn.story.traits.allTraits.Any(t => t.def == trait.def && t.Degree == trait.Degree))) {
                            LogHelper.Verbose(
                                $"Not considering pawn {p.Pawn.Name.ToStringShort} due to missing Must Have Trait {trait.LabelCap}");
                            newFiltered.Remove(p);
                        }

                        filteredPawns = newFiltered;
                    }

                    foreach (var trait in wg.TraitsCantHave) {
                        var newFiltered = filteredPawns.ToList();

                        foreach (var p in filteredPawns.Where(p => p.Pawn.story.traits.allTraits.Any(t => t.def == trait.def && t.Degree == trait.Degree))) {
                            LogHelper.Verbose(
                                $"Not considering pawn {p.Pawn.Name.ToStringShort} due to having Can't Have Trait {trait.LabelCap}");
                            newFiltered.Remove(p);
                        }

                        filteredPawns = newFiltered;
                    }

                    var card = new ScoreCard();
                    foreach (var pawn in filteredPawns) {
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

                        var entry = new ScoreCardEntry() {Pawn = pawn};
                        card.Entries.Add(entry);

                        foreach (var trait in wg.TraitsWantToHave) {
                            if (pawn.Pawn.story.traits.allTraits.Any(
                                t => t.def == trait.def && t.Degree == trait.Degree)) {
                                entry.HasSomeWantedTraits = true;
                                break;
                            }
                        }

                        var thisPawnsSkill = 0f;
                        var cnt = 0;

                        foreach (var wgItem in wg.Items) {
                            if (wgItem.relevantSkills.Any())
                                foreach (var skill in wgItem.relevantSkills) {
                                    var multiplier = 1f;
                                    var pawnSkill = pawn.Pawn.skills.GetSkill(skill);

                                    if (Settings.UseLearningRates)
                                        multiplier = pawnSkill.LearnRateFactor();

                                    thisPawnsSkill +=
                                        multiplier *
                                        (pawnSkill.Level
                                         + 1 // This stupid +1 insures that if a pawn's level is 0, their stats still matter in the ImportantStats section below
                                         + pawnSkill.XpProgressPercent); // This + XpProgressPercent will make it so it doesn't flip-flop between multiple pawns who have the same level, since their XP% is likely different
                                }
                            else
                                thisPawnsSkill += 3f;

                            cnt++;
                        }

                        entry.Skill = thisPawnsSkill / cnt;

                        foreach (var importantStat in wg.HighStats) {
                            entry.Stats.Add(new ScoreCardEntryStat() {
                                Stat =  importantStat,
                                StatValue = GetStatValue(pawn.Pawn, importantStat),
                                IsLowStat = false
                            });
                        }

                        foreach (var importantStat in wg.LowStats) {
                            entry.Stats.Add(new ScoreCardEntryStat() {
                                Stat =  importantStat,
                                StatValue = GetStatValue(pawn.Pawn, importantStat),
                                IsLowStat = true
                            });
                        }
                    }

                    card.CalculateFinalModifiers();
                    // First get the pawn with 1+ of the wanted traits .. fall back to just anyone who's the best
                    bestPawn = card.Entries.Where(e => e.HasSomeWantedTraits).OrderByDescending(e => e.FinalScore).FirstOrDefault()?.Pawn ??
                               card.Entries.OrderByDescending(e => e.FinalScore).FirstOrDefault()?.Pawn;

                    var debug = new StringBuilder();
                    foreach (var scoreCardEntry in card.Entries) {
                        debug.AppendLine($"-{scoreCardEntry.Pawn.Pawn.Name}");
                        debug.AppendLine($"--Skill: {scoreCardEntry.Skill}\tMod: {scoreCardEntry.FinalModifier}\tScore: {scoreCardEntry.FinalScore}");
                        foreach (var scoreCardEntryStat in scoreCardEntry.Stats) {
                            debug.AppendLine(
                                $"---Stat: {scoreCardEntryStat.Stat.defName}\tVal: {scoreCardEntryStat.StatValue}\tLow: {scoreCardEntryStat.IsLowStat}");
                        }
                    }

                    LogHelper.Verbose($"Scores:\r\n{debug}");

                    if (bestPawn != null) {
                        LogHelper.Verbose($"-- {bestPawn.Pawn.Name.ToStringFull} - Yep");
                        (bestPawn.WorkGroups as List<WorkGroup>).Add(wg);
                        changedSomething = true;
                    }
                    else {
                        LogHelper.Verbose("Ooops no-one available..");
                    }
                }
            }

            LogHelper.Verbose($"-UpdatePriorities() -- changedSomething={changedSomething}");
            return changedSomething;
        }

        private float GetStatValue(Pawn pawn, StatDef stat) {
            return CopiedAndModified_GetValueUnfinalized(stat.Worker, StatRequest.For(pawn), stat);
        }

        private float CopiedAndModified_GetValueUnfinalized(StatWorker statWorker, StatRequest req, StatDef stat) {
            if (!stat.supressDisabledError && Prefs.DevMode && statWorker.IsDisabledFor(req.Thing))
                Log.ErrorOnce(
                    string.Format(
                        "Attempted to calculate value for disabled stat {0}; this is meant as a consistency check, either set the stat to neverDisabled or ensure this pawn cannot accidentally use this stat (thing={1})",
                        (object) stat, (object) req.Thing.ToStringSafe<Thing>()),
                    75193282 + (int) stat.index);
            float a = stat.defaultBaseValue;
            Pawn thing = null;
            if (req.Thing is Pawn)
                thing = (Pawn) req.Thing;

            if (req.Thing is Pawn) {
                if (thing.skills != null) {
                    if (stat.skillNeedOffsets != null) {
                        for (int index = 0; index < stat.skillNeedOffsets.Count; ++index)
                            a += stat.skillNeedOffsets[index].ValueFor(thing);
                    }
                }
                else
                    a += stat.noSkillOffset;

                if (stat.capacityOffsets != null) {
                    for (int index = 0; index < stat.capacityOffsets.Count; ++index) {
                        PawnCapacityOffset capacityOffset = stat.capacityOffsets[index];
                        a += capacityOffset.GetOffset(thing.health.capacities.GetLevel(capacityOffset.capacity));
                    }
                }

                if (thing.story != null) {
                    for (int index = 0; index < thing.story.traits.allTraits.Count; ++index)
                        a += thing.story.traits.allTraits[index].OffsetOfStat(stat);
                }

                List<Hediff> hediffs = thing.health.hediffSet.hediffs;
                for (int index = 0; index < hediffs.Count; ++index) {
                    HediffStage curStage = hediffs[index].CurStage;
                    if (curStage != null) {
                        float statOffsetFromList = curStage.statOffsets.GetStatOffsetFromList(stat);
                        if ((double) statOffsetFromList != 0.0 && curStage.statOffsetEffectMultiplier != null)
                            statOffsetFromList *= thing.GetStatValue(curStage.statOffsetEffectMultiplier);
                        a += statOffsetFromList;
                    }
                }

                if (thing.apparel != null) {
                    for (int index = 0; index < thing.apparel.WornApparel.Count; ++index)
                        a += StatWorker.StatOffsetFromGear((Thing) thing.apparel.WornApparel[index], stat);
                }

                if (thing.equipment != null && thing.equipment.Primary != null)
                    a += StatWorker.StatOffsetFromGear((Thing) thing.equipment.Primary, stat);
                if (thing.story != null) {
                    for (int index = 0; index < thing.story.traits.allTraits.Count; ++index)
                        a *= thing.story.traits.allTraits[index].MultiplierOfStat(stat);
                }

                for (int index = 0; index < hediffs.Count; ++index) {
                    HediffStage curStage = hediffs[index].CurStage;
                    if (curStage != null) {
                        float factor = curStage.statFactors.GetStatFactorFromList(stat);
                        if ((double) Math.Abs(factor - 1f) > 1.40129846432482E-45 &&
                            curStage.statFactorEffectMultiplier != null)
                            factor = StatWorker.ScaleFactor(factor,
                                thing.GetStatValue(curStage.statFactorEffectMultiplier));
                        a *= factor;
                    }
                }

                a *= thing.ageTracker.CurLifeStage.statFactors.GetStatFactorFromList(stat);
            }

            if (req.StuffDef != null) {
                if ((double) a > 0.0 || stat.applyFactorsIfNegative)
                    a *= req.StuffDef.stuffProps.statFactors.GetStatFactorFromList(stat);
                a += req.StuffDef.stuffProps.statOffsets.GetStatOffsetFromList(stat);
            }

            //if (req.ForAbility && stat.statFactors != null) {
            //    for (int index = 0; index < stat.statFactors.Count; ++index)
            //        a *= req.AbilityDef.statBases.GetStatValueFromList(stat.statFactors[index], 1f);
            //}

            if (req.HasThing) {
                CompAffectedByFacilities comp = req.Thing.TryGetComp<CompAffectedByFacilities>();
                if (comp != null)
                    a += comp.GetStatOffset(stat);
                //if (stat.statFactors != null) {
                //    for (int index = 0; index < stat.statFactors.Count; ++index)
                //        a *= req.Thing.GetStatValue(stat.statFactors[index]);
                //}

                if (thing != null) {
                    if (thing.skills != null) {
                        if (stat.skillNeedFactors != null) {
                            for (int index = 0; index < stat.skillNeedFactors.Count; ++index)
                                a *= stat.skillNeedFactors[index].ValueFor(thing);
                        }
                    }
                    else
                        a *= stat.noSkillFactor;

                    if (stat.capacityFactors != null) {
                        for (int index = 0; index < stat.capacityFactors.Count; ++index) {
                            PawnCapacityFactor capacityFactor = stat.capacityFactors[index];
                            float factor =
                                capacityFactor.GetFactor(thing.health.capacities.GetLevel(capacityFactor.capacity));
                            a = Mathf.Lerp(a, a * factor, capacityFactor.weight);
                        }
                    }

                    if (thing.Inspired)
                        a = (a + thing.InspirationDef.statOffsets.GetStatOffsetFromList(stat)) *
                            thing.InspirationDef.statFactors.GetStatFactorFromList(stat);
                }
            }

            return a;
        }

        private IEnumerable<PawnWithWorkgroups> FetchColonists() {
            LogHelper.Verbose("+FetchPawns()");
            var ret = new List<PawnWithWorkgroups>();

            ret.AddRange(map.mapPawns.FreeColonists.Select(p => new PawnWithWorkgroups(p)));

            if (Settings.SsInstalled && !Settings.SetPrioritiesForSlaves) {
                var before = ret.Count();
                ret.RemoveAll(p => p.IsSlave);
                var after = ret.Count();

                if (before != after)
                    LogHelper.Verbose($"Filtered out {before - after} slaves due to global setting disabled");
            }

            if (Settings.RjwInstalled && !Settings.SetPrioritiesForRjwWorkers) {
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

            ret.AddRange(map.mapPawns.PrisonersOfColony.Select(p => new PawnWithWorkgroups(p)));

            if (Settings.RjwInstalled && !Settings.SetPrioritiesForRjwWorkers) {
                var before = ret.Count();
                ret.RemoveAll(p => p.IsRjwWorker);
                var after = ret.Count();

                if (before != after)
                    LogHelper.Verbose($"Filtered out {before - after} (Prisoner) RJW Workers due to global setting disabled");
            }

            if (!Settings.PlInstalled) {
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
