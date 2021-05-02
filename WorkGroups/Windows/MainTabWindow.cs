using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.QuestGen;
using The1nk.WorkGroups.Models;
using UnityEngine;
using Verse;

namespace The1nk.WorkGroups.Windows
{
    [StaticConstructorOnStartup]
    public class MainTabWindow : RimWorld.MainTabWindow {
        private int verticalPadding = 10;
        private int horizontalPadding = 10;
        private float textHeight = Verse.Text.LineHeight;
        private Vector2 scrollPosition;

        private static readonly Texture2D ReorderUp = ContentFinder<Texture2D>.Get("UI/Buttons/ReorderUp");
        private static readonly Texture2D ReorderDown = ContentFinder<Texture2D>.Get("UI/Buttons/ReorderDown");
        private WorkGroupsSettings _settings;

        public override void DoWindowContents(UnityEngine.Rect inRect) {
            base.DoWindowContents(inRect);
            string buffer = _settings.HoursUpdateInterval.ToString("0");
            string buffer2 = _settings.MaxPriority.ToString("0");

            var cbLocation = new Rect(inRect);
            cbLocation.x += horizontalPadding;
            cbLocation.y += verticalPadding;
            cbLocation.width -= 2 * horizontalPadding;
            cbLocation.height = textHeight;

            Widgets.CheckboxLabeled(cbLocation, "cbEnabled".Translate(), ref _settings.Enabled);

            cbLocation.y += textHeight + verticalPadding;
            Widgets.CheckboxLabeled(cbLocation, "cbSlavePriorities".Translate(), ref _settings.SetPrioritiesForSlaves, !_settings.SsInstalled);

            cbLocation.y += textHeight + verticalPadding;
            Widgets.CheckboxLabeled(cbLocation, "cbPrisonerPriorities".Translate(), ref _settings.SetPrioritiesForPrisoners, !_settings.PlInstalled);

            if (_settings.RjwInstalled) {
                cbLocation.y += textHeight + verticalPadding;
                Widgets.CheckboxLabeled(cbLocation, "cbRjwPriorities".Translate(), ref _settings.SetPrioritiesForRjwWorkers, !_settings.RjwInstalled);
            }

            cbLocation.y += textHeight + verticalPadding;
            Widgets.CheckboxLabeled(cbLocation, "cbSetPawnTitles".Translate(), ref _settings.SetPawnTitles);

            cbLocation.y += textHeight + verticalPadding;
            Widgets.CheckboxLabeled(cbLocation, "cbSetPawnBadges".Translate(), ref _settings.SetBadges);

            cbLocation.y += textHeight + verticalPadding;
            Widgets.CheckboxLabeled(cbLocation, "cbClearSchedules".Translate(), ref _settings.ClearOutSchedules);

            cbLocation.y += textHeight + verticalPadding;
            Widgets.CheckboxLabeled(cbLocation, "cbForceBedRest".Translate(),
                ref _settings.ForcedBedRestForInjuredPawns);

            cbLocation.y += textHeight + verticalPadding;
            Widgets.CheckboxLabeled(cbLocation, "cbUseLearningRates".Translate(),
                ref _settings.UseLearningRates);

            cbLocation.y += textHeight + verticalPadding;
            Widgets.CheckboxLabeled(cbLocation, "cbVerboseLogging".Translate(), ref _settings.VerboseLogging);
            
            cbLocation.y += textHeight + verticalPadding;
            Widgets.TextFieldNumericLabeled(cbLocation, "UpdateInterval".Translate(),
                ref _settings.HoursUpdateInterval,
                ref buffer, 1, 1000);

            cbLocation.y += textHeight + verticalPadding;
            Widgets.TextFieldNumericLabeled(cbLocation, "MaxPriority".Translate(),
                ref _settings.MaxPriority,
                ref buffer2);
            cbLocation.y += textHeight + verticalPadding;

            var btnAdd = new Rect(cbLocation);
            btnAdd.width = 120;

            var btnDel = new Rect(btnAdd);
            btnDel.x += btnAdd.width + horizontalPadding;

            if (Widgets.ButtonText(btnAdd, "btnAdd".Translate())) {
                _settings.WorkGroups.Add(
                    new WorkGroup($"NewWorkGroup".Translate(_settings.WorkGroups.Count + 1)));
            }

            if (Widgets.ButtonText(btnDel, "btnDel".Translate())) {
                var lst = _settings.WorkGroups.Select(w =>
                    new FloatMenuOption($"DeleteWorkGroup".Translate(w.Name),
                        () => { _settings.WorkGroups.Remove(w); })).ToList();
                if (lst.Any())
                    Find.WindowStack.Add(new FloatMenu(lst));
            }
            cbLocation.y += textHeight + verticalPadding;

            var btnSave = new Rect(cbLocation);
            btnSave.width = 120;

            var btnLoad = new Rect(btnSave);
            btnLoad.x += btnSave.width + horizontalPadding;

            if (Widgets.ButtonText(btnSave, "btnSave".Translate())) {
                var lst = GetSavePresetList();
                if (lst.Any())
                    Find.WindowStack.Add(new FloatMenu(GetSavePresetList()));
            }

            if (Widgets.ButtonText(btnLoad, "btnLoad".Translate())) {
                var lst = GetLoadPresetList();
                if (lst.Any())
                    Find.WindowStack.Add(new FloatMenu(GetLoadPresetList()));
            }
            cbLocation.y += textHeight + verticalPadding;

            DrawHeader(cbLocation);
            cbLocation.y += textHeight + verticalPadding;

            var outRect = new Rect(cbLocation.x, cbLocation.y, cbLocation.width,
                inRect.height - cbLocation.y - verticalPadding);
            var viewRect = new Rect(0, 0, outRect.width - 18f,
                (textHeight + verticalPadding) * _settings.WorkGroups.Count + 100);
            Widgets.BeginScrollView(outRect, ref this.scrollPosition, viewRect);

            var groupRect = inRect.AtZero();
            groupRect.height = textHeight;

            for (int i = 0; i < _settings.WorkGroups.Count; i++) {
                DrawGroup(_settings.WorkGroups[i], groupRect);
                groupRect.y += textHeight + verticalPadding;
            }
            Widgets.EndScrollView();
        }

        private void DrawGroup(WorkGroup @group, Rect cbLocation) {
            var newLoc = new Rect(cbLocation);

            var txtRec = new Rect(newLoc);
            txtRec.width = 175;
            @group.Name = Widgets.TextField(txtRec, @group.Name);
            newLoc.x += 200;

            txtRec = new Rect(newLoc);
            txtRec.width = 40;
            if (Widgets.ButtonText(txtRec, "btnJobs".Translate())) {
                Find.WindowStack.Add(new FloatMenu(GetWorkTypesList(group)));
            }

            TooltipHandler.TipRegion(txtRec,
                "ttJobs".Translate());
            newLoc.x += 45;

            txtRec = new Rect(newLoc);
            txtRec.width = 40;
            if (Widgets.ButtonText(txtRec, "btnAnd".Translate())) {
                var lst = GetWorkGroupsList(group);
                if (lst.Any())
                    Find.WindowStack.Add(new FloatMenu(lst));
            }
            TooltipHandler.TipRegion(txtRec,
                "ttAnd".Translate());
            newLoc.x += 45;

            txtRec = new Rect(newLoc);
            txtRec.width = 40;
            if (Widgets.ButtonText(txtRec, "btnStats".Translate())) {
                if (group.HighStats == null)
                    group.HighStats = new List<StatDef>();
                if (group.LowStats == null)
                    group.LowStats = new List<StatDef>();

                Find.WindowStack.Add(new StatsPicker(group.HighStats, group.LowStats));
            }

            TooltipHandler.TipRegion(txtRec,
                "ttStats".Translate());
            newLoc.x += 45;

            txtRec = new Rect(newLoc);
            txtRec.width = 40;
            if (Widgets.ButtonText(txtRec, "btnTraits".Translate())) {
                if (group.TraitsMustHave == null)
                    group.TraitsMustHave = new List<Trait>();
                if (group.TraitsWantToHave == null)
                    group.TraitsWantToHave = new List<Trait>();
                if (group.TraitsCantHave == null)
                    group.TraitsCantHave = new List<Trait>();

                Find.WindowStack.Add(new TraitsPicker(group.TraitsMustHave, group.TraitsWantToHave,
                    group.TraitsCantHave));
            }

            TooltipHandler.TipRegion(txtRec,
                "ttTraits".Translate());
            newLoc.x += 45;

            string qtyBuffer = @group.TargetQuantity.ToString("0");
            txtRec = new Rect(newLoc);
            txtRec.width = 50;
            Widgets.TextFieldNumeric(txtRec, ref @group.TargetQuantity, ref qtyBuffer, 0, 999);
            newLoc.x += 70;

            Widgets.Checkbox(new Vector2(newLoc.x, newLoc.y), ref @group.DisableTitleForThisWorkGroup, newLoc.height, !_settings.SetPawnTitles);
            newLoc.x += 80;

            txtRec = new Rect(newLoc);
            txtRec.width = 45;
            if (Widgets.ButtonText(txtRec, "btnBadge".Translate())) {
                var lst = GetBadgeList(group);
                if (lst.Any())
                    Find.WindowStack.Add(new FloatMenu(lst));
            }
            newLoc.x += 50;
            
            Widgets.Checkbox(new Vector2(newLoc.x, newLoc.y), ref @group.AssignToEveryone, newLoc.height);
            newLoc.x += 70;
            
            Widgets.Checkbox(new Vector2(newLoc.x, newLoc.y), ref @group.ColonistsAllowed, newLoc.height);
            newLoc.x += 70;
            
            Widgets.Checkbox(new Vector2(newLoc.x, newLoc.y), ref @group.SlavesAllowed, newLoc.height, !_settings.SsInstalled);
            newLoc.x += 50;
            
            Widgets.Checkbox(new Vector2(newLoc.x, newLoc.y), ref @group.PrisonersAllowed, newLoc.height, !_settings.PlInstalled);
            newLoc.x += 70;
            
            if (_settings.RjwInstalled) {
                Widgets.Checkbox(new Vector2(newLoc.x, newLoc.y), ref @group.RjwWorkersAllowed, newLoc.height, !_settings.RjwInstalled);
                newLoc.x += 55;
            }

            txtRec = new Rect(newLoc);
            txtRec.width = 15;
            if (Widgets.ButtonImage(txtRec, ReorderUp)) {
                var newIndex = Math.Max(_settings.WorkGroups.IndexOf(group) - 1, 0);
                _settings.WorkGroups.Remove(group);
                _settings.WorkGroups.Insert(newIndex, group);
            }

            newLoc.x += 20;
            txtRec = new Rect(newLoc);
            txtRec.width = 15;
            if (Widgets.ButtonImage(txtRec, ReorderDown)) {
                var newIndex = Math.Min(_settings.WorkGroups.IndexOf(group) + 1, _settings.WorkGroups.Count - 1);
                _settings.WorkGroups.Remove(group);
                _settings.WorkGroups.Insert(newIndex, group);
            }
        }

        private List<FloatMenuOption> GetBadgeList(WorkGroup group) {
            var ret = new List<FloatMenuOption>();

            if (!WorkGroupsSettings.GetSettings().AllBadges.Any())
                return ret;

            if (WorkGroupsSettings.GetSettings().AllBadges.All(b => b.Def.defName != @group.Badge)) {
                group.Badge = "";
            }

            if (!string.IsNullOrEmpty(group.Badge)) {
                ret.Add(new FloatMenuOption("ActiveBadge".Translate(), () => group.Badge = "",
                    WorkGroupsSettings.GetSettings().AllBadges.First(b => b.Def.defName == group.Badge).Texture,
                    Color.white));
            }

            ret.AddRange(WorkGroupsSettings.GetSettings().AllBadges.Where(b => group.Badge != b.Def.defName)
                .Select(b =>
                    new FloatMenuOption(b.Def.defName, () => group.Badge = b.Def.defName, b.Texture, Color.white)));

            return ret;
        }

        private void DrawHeader(Rect cbLocation) {
            var newLoc = new Rect(cbLocation);

            Widgets.Label(newLoc, "gpName".Translate());
            newLoc.x += 200;

            Widgets.Label(newLoc, "gpEdit".Translate());
            newLoc.x += 180;

            Widgets.Label(newLoc, "gpTargetQty".Translate());
            newLoc.x += 70;

            Widgets.Label(newLoc, "gpHideTitles".Translate());
            newLoc.x += 80;

            Widgets.Label(newLoc, "gpBadge".Translate());
            newLoc.x += 50;
            
            Widgets.Label(newLoc, "gpEveryone".Translate());
            newLoc.x += 70;
            
            Widgets.Label(newLoc, "gpColonists".Translate());
            newLoc.x += 70;
            
            Widgets.Label(newLoc, "gpSlaves".Translate());
            newLoc.x += 50;
            
            Widgets.Label(newLoc, "gpPrisoners".Translate());
            newLoc.x += 70;
            
            if (_settings.RjwInstalled) {
                Widgets.Label(newLoc, "gpWhores".Translate());
                newLoc.x += 50;
            }
        }

        public override void PostClose() {
            Find.CurrentMap.GetComponent<WorkGroupsMapComponent>().RunNow();
            base.PostClose();
        }

        public override void PreOpen() {
            _settings = Find.CurrentMap.GetComponent<WorkGroupsMapComponent>().Settings;

            base.PreOpen();
        }

        private List<FloatMenuOption> GetWorkTypesList(WorkGroup group) {
            var ret = new List<FloatMenuOption>();
            
            // Enabled ones
            ret.AddRange(group.Items.OrderByDescending(wt => wt.naturalPriority).Select(wt =>
                new FloatMenuOption($"ddEnabled".Translate(wt.labelShort), () => group.Items.Remove(wt))));

            ret.Add(new FloatMenuOption("---", () => LogHelper.Verbose("Clicked the divider -_-;;")));

            // Disabled ones
            ret.AddRange(WorkGroupsSettings.GetSettings().AllWorkTypes
                .Where(wt => wt.visible && !group.Items.Contains(wt)).OrderByDescending(wt => wt.naturalPriority)
                .Select(wt => new FloatMenuOption($"ddDisabled".Translate(wt.labelShort), () => group.Items.Add(wt))));

            return ret;
        }

        private List<FloatMenuOption> GetWorkGroupsList(WorkGroup group) {
            var ret = new List<FloatMenuOption>();
            
            // Enabled ones
            ret.AddRange(group.CanBeAssignedWith.OrderBy(g => g).Select(g =>
                new FloatMenuOption("ddEnabled".Translate(g), () => group.CanBeAssignedWith.Remove(g))));

            ret.Add(new FloatMenuOption("---", () => LogHelper.Verbose("Clicked the divider -_-;;")));

            // Disabled ones
            ret.AddRange(_settings.WorkGroups.Select(g => g.Name)
                .Where(g => !group.CanBeAssignedWith.Contains(g) && group.Name != g)
                .OrderBy(g => g)
                .Select(g => new FloatMenuOption($"ddDisabled".Translate(g), () => group.CanBeAssignedWith.Add(g))));

            return ret;
        }

        private List<FloatMenuOption> GetLoadPresetList() {
            var ret = new List<FloatMenuOption>();

            var saves = _settings.GetAllPresetSaves();

            foreach (var save in saves) {
                ret.Add(new FloatMenuOption(save, () => _settings.LoadFromPreset(save)));
            }

            return ret;
        }

        private List<FloatMenuOption> GetSavePresetList() {
            var ret = GetLoadPresetList();

            ret.ForEach(r => r.action = () => _settings.SaveToPreset(r.Label));

            ret.Add(new FloatMenuOption("---", () => LogHelper.Verbose("Clicked the divider -_-;;")));
            
            ret.Add(new FloatMenuOption("ddNew".Translate(), () => {
                var newTextWindow = new RenameWindow("ddNew".Translate());
                Action<string> saveAction = name => {
                    _settings.SaveToPreset(name);
                };
                newTextWindow.Action = saveAction;
                Find.WindowStack.Add(newTextWindow);
            }));

            return ret;
        }
    }
}
