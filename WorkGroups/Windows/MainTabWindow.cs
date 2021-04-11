using System;
using System.Collections.Generic;
using System.Linq;
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

        public override void DoWindowContents(UnityEngine.Rect inRect) {
            base.DoWindowContents(inRect);
            string buffer = WorkGroupsSettings.GetSettings.HoursUpdateInterval.ToString("0");
            string buffer2 = WorkGroupsSettings.GetSettings.MaxPriority.ToString("0");

            var cbLocation = new Rect(inRect);
            cbLocation.x += horizontalPadding;
            cbLocation.y += verticalPadding;
            cbLocation.width -= 2 * horizontalPadding;
            cbLocation.height = textHeight;

            Widgets.CheckboxLabeled(cbLocation, "Enabled", ref WorkGroupsSettings.GetSettings.Enabled);

            cbLocation.y += textHeight + verticalPadding;
            Widgets.CheckboxLabeled(cbLocation, "Set Priorities for Slaves? (Requires Simple Slavery)", ref WorkGroupsSettings.GetSettings.SetPrioritiesForSlaves, !WorkGroupsSettings.GetSettings.SsInstalled);

            cbLocation.y += textHeight + verticalPadding;
            Widgets.CheckboxLabeled(cbLocation, "Set Priorities for Prisoners? (Requires Prison Labor)", ref WorkGroupsSettings.GetSettings.SetPrioritiesForPrisoners, !WorkGroupsSettings.GetSettings.PlInstalled);

            if (WorkGroupsSettings.GetSettings.RjwInstalled) {
                cbLocation.y += textHeight + verticalPadding;
                Widgets.CheckboxLabeled(cbLocation, "Set Priorities for Whores? (Requires RimJobWorld)", ref WorkGroupsSettings.GetSettings.SetPrioritiesForRjwWorkers, !WorkGroupsSettings.GetSettings.RjwInstalled);
            }

            cbLocation.y += textHeight + verticalPadding;
            Widgets.CheckboxLabeled(cbLocation, "Set Pawn Titles? (Useful with 'Show Titles' feature of Guards For Me)", ref WorkGroupsSettings.GetSettings.SetPawnTitles);

            cbLocation.y += textHeight + verticalPadding;
            Widgets.CheckboxLabeled(cbLocation, "Clear Schedules?", ref WorkGroupsSettings.GetSettings.ClearOutSchedules);

            cbLocation.y += textHeight + verticalPadding;
            Widgets.CheckboxLabeled(cbLocation, "Forced Best Rest for injured pawns?",
                ref WorkGroupsSettings.GetSettings.ForcedBedRestForInjuredPawns);

            cbLocation.y += textHeight + verticalPadding;
            Widgets.CheckboxLabeled(cbLocation, "Consider Learning Rate when selecting best pawn?",
                ref WorkGroupsSettings.GetSettings.UseLearningRates);

            cbLocation.y += textHeight + verticalPadding;
            Widgets.CheckboxLabeled(cbLocation, "Verbose Logging?", ref WorkGroupsSettings.GetSettings.VerboseLogging);
            
            cbLocation.y += textHeight + verticalPadding;
            Widgets.TextFieldNumericLabeled(cbLocation, "Update Priorities Every x Hours",
                ref WorkGroupsSettings.GetSettings.HoursUpdateInterval,
                ref buffer, 1, 1000);

            cbLocation.y += textHeight + verticalPadding;
            Widgets.TextFieldNumericLabeled(cbLocation, "Max Priority",
                ref WorkGroupsSettings.GetSettings.MaxPriority,
                ref buffer2, 4, 9);
            cbLocation.y += textHeight + verticalPadding;

            var btnAdd = new Rect(cbLocation);
            btnAdd.width = 120;

            var btnDel = new Rect(btnAdd);
            btnDel.x += btnAdd.width + horizontalPadding;

            if (Widgets.ButtonText(btnAdd, "Add")) {
                WorkGroupsSettings.GetSettings.WorkGroups.Add(
                    new WorkGroup($"New WorkGroup {WorkGroupsSettings.GetSettings.WorkGroups.Count + 1}"));
            }

            if (Widgets.ButtonText(btnDel, "Del")) {
                var lst = WorkGroups.WorkGroupsSettings.GetSettings.WorkGroups.Select(w =>
                    new FloatMenuOption($"Delete {w.Name}",
                        () => { WorkGroups.WorkGroupsSettings.GetSettings.WorkGroups.Remove(w); })).ToList();
                if (lst.Any())
                    Find.WindowStack.Add(new FloatMenu(lst));
            }
            cbLocation.y += textHeight + verticalPadding;

            var btnSave = new Rect(cbLocation);
            btnSave.width = 120;

            var btnLoad = new Rect(btnSave);
            btnLoad.x += btnSave.width + horizontalPadding;

            if (Widgets.ButtonText(btnSave, "Save")) {
                var lst = GetSavePresetList();
                if (lst.Any())
                    Find.WindowStack.Add(new FloatMenu(GetSavePresetList()));
            }

            if (Widgets.ButtonText(btnLoad, "Load")) {
                var lst = GetLoadPresetList();
                if (lst.Any())
                    Find.WindowStack.Add(new FloatMenu(GetLoadPresetList()));
            }
            cbLocation.y += textHeight + verticalPadding;

            DrawHeader(cbLocation);
            cbLocation.y += textHeight + verticalPadding;

            var outRect = new Rect(cbLocation.x, cbLocation.y, cbLocation.width, 300);
            var viewRect = new Rect(0, 0, outRect.width - 18f,
                (textHeight + verticalPadding) * WorkGroupsSettings.GetSettings.WorkGroups.Count + 100);
            Widgets.BeginScrollView(outRect, ref this.scrollPosition, viewRect);

            var groupRect = inRect.AtZero();
            groupRect.height = textHeight;

            for (int i = 0; i < WorkGroupsSettings.GetSettings.WorkGroups.Count; i++) {
                DrawGroup(WorkGroupsSettings.GetSettings.WorkGroups[i], groupRect);
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
            if (Widgets.ButtonText(txtRec, "Jobs")) {
                Find.WindowStack.Add(new FloatMenu(GetWorkTypesList(group)));
            }

            TooltipHandler.TipRegion(txtRec,
                "What Jobs this WorkGroup contains. All Jobs in a given WorkGroup will have the same Work Priority.");
            newLoc.x += 60;

            txtRec = new Rect(newLoc);
            txtRec.width = 40;
            if (Widgets.ButtonText(txtRec, "And..")) {
                var lst = GetWorkGroupsList(group);
                if (lst.Any())
                    Find.WindowStack.Add(new FloatMenu(lst));
            }
            TooltipHandler.TipRegion(txtRec,
                "What other WorkGroups can be assigned to the same pawn, who has been assigned this WorkGroup. This is useful for having your Doctors not planting, but your Growers being back-up Haulers.");
            newLoc.x += 60;

            string qtyBuffer = @group.TargetQuantity.ToString("0");
            txtRec = new Rect(newLoc);
            txtRec.width = 50;
            Widgets.TextFieldNumeric(txtRec, ref @group.TargetQuantity, ref qtyBuffer, 0, 999);
            newLoc.x += 70;

            Widgets.Checkbox(new Vector2(newLoc.x, newLoc.y), ref @group.DisableTitleForThisWorkGroup, newLoc.height, !WorkGroupsSettings.GetSettings.SetPawnTitles);
            newLoc.x += 90;
            
            Widgets.Checkbox(new Vector2(newLoc.x, newLoc.y), ref @group.AssignToEveryone, newLoc.height);
            newLoc.x += 70;
            
            Widgets.Checkbox(new Vector2(newLoc.x, newLoc.y), ref @group.ColonistsAllowed, newLoc.height);
            newLoc.x += 70;
            
            Widgets.Checkbox(new Vector2(newLoc.x, newLoc.y), ref @group.SlavesAllowed, newLoc.height, !WorkGroupsSettings.GetSettings.SsInstalled);
            newLoc.x += 50;
            
            Widgets.Checkbox(new Vector2(newLoc.x, newLoc.y), ref @group.PrisonersAllowed, newLoc.height, !WorkGroupsSettings.GetSettings.PlInstalled);
            newLoc.x += 70;
            
            if (WorkGroupsSettings.GetSettings.RjwInstalled) {
                Widgets.Checkbox(new Vector2(newLoc.x, newLoc.y), ref @group.RjwWorkersAllowed, newLoc.height, !WorkGroupsSettings.GetSettings.RjwInstalled);
                newLoc.x += 70;
            }

            txtRec = new Rect(newLoc);
            txtRec.width = 15;
            if (Widgets.ButtonImage(txtRec, ReorderUp)) {
                var newIndex = Math.Max(WorkGroupsSettings.GetSettings.WorkGroups.IndexOf(group) - 1, 0);
                WorkGroupsSettings.GetSettings.WorkGroups.Remove(group);
                WorkGroupsSettings.GetSettings.WorkGroups.Insert(newIndex, group);
            }

            newLoc.x += 20;
            txtRec = new Rect(newLoc);
            txtRec.width = 15;
            if (Widgets.ButtonImage(txtRec, ReorderDown)) {
                var newIndex = Math.Min(WorkGroupsSettings.GetSettings.WorkGroups.IndexOf(group) + 1, WorkGroupsSettings.GetSettings.WorkGroups.Count - 1);
                WorkGroupsSettings.GetSettings.WorkGroups.Remove(group);
                WorkGroupsSettings.GetSettings.WorkGroups.Insert(newIndex, group);
            }
        }

        private void DrawHeader(Rect cbLocation) {
            var newLoc = new Rect(cbLocation);

            Widgets.Label(newLoc, "Group Name");
            newLoc.x += 200;

            Widgets.Label(newLoc, "Edit");
            newLoc.x += 110;

            Widgets.Label(newLoc, "Target Qty");
            newLoc.x += 70;

            Widgets.Label(newLoc, "Hide Titles");
            newLoc.x += 90;
            
            Widgets.Label(newLoc, "Everyone");
            newLoc.x += 70;
            
            Widgets.Label(newLoc, "Colonists");
            newLoc.x += 70;
            
            Widgets.Label(newLoc, "Slaves");
            newLoc.x += 50;
            
            Widgets.Label(newLoc, "Prisoners");
            newLoc.x += 70;
            
            if (WorkGroupsSettings.GetSettings.RjwInstalled) {
                Widgets.Label(newLoc, "Whores");
                newLoc.x += 50;
            }
        }

        public override void PostClose() {
            WorkGroupsSettings.GetSettings.Component?.RunNow();

            base.PostClose();
        }

        public override void PreOpen() {
            WorkGroupsSettings.GetSettings.Component?.RunNow();

            base.PreOpen();
        }

        private List<FloatMenuOption> GetWorkTypesList(WorkGroup group) {
            var ret = new List<FloatMenuOption>();
            
            // Enabled ones
            ret.AddRange(group.Items.OrderByDescending(wt => wt.naturalPriority).Select(wt =>
                new FloatMenuOption($"ENABLED | {wt.labelShort}", () => group.Items.Remove(wt))));

            ret.Add(new FloatMenuOption("---", () => LogHelper.Verbose("Clicked the divider -_-;;")));

            // Disabled ones
            ret.AddRange(WorkGroupsSettings.GetSettings.AllWorkTypes
                .Where(wt => wt.visible && !group.Items.Contains(wt)).OrderByDescending(wt => wt.naturalPriority)
                .Select(wt => new FloatMenuOption($"DISABLED | {wt.labelShort}", () => group.Items.Add(wt))));

            return ret;
        }

        private List<FloatMenuOption> GetWorkGroupsList(WorkGroup group) {
            var ret = new List<FloatMenuOption>();
            
            // Enabled ones
            ret.AddRange(group.CanBeAssignedWith.OrderBy(g => g).Select(g =>
                new FloatMenuOption($"ENABLED | {g}", () => group.CanBeAssignedWith.Remove(g))));

            ret.Add(new FloatMenuOption("---", () => LogHelper.Verbose("Clicked the divider -_-;;")));

            // Disabled ones
            ret.AddRange(WorkGroupsSettings.GetSettings.WorkGroups.Select(g => g.Name)
                .Where(g => !group.CanBeAssignedWith.Contains(g) && group.Name != g)
                .OrderBy(g => g)
                .Select(g => new FloatMenuOption($"DISABLED | {g}", () => group.CanBeAssignedWith.Add(g))));

            return ret;
        }

        private List<FloatMenuOption> GetLoadPresetList() {
            var ret = new List<FloatMenuOption>();

            var saves = WorkGroupsSettings.GetSettings.GetAllPresetSaves();

            foreach (var save in saves) {
                ret.Add(new FloatMenuOption(save, () => WorkGroupsSettings.GetSettings.LoadFromPreset(save)));
            }

            return ret;
        }

        private List<FloatMenuOption> GetSavePresetList() {
            var ret = GetLoadPresetList();

            ret.ForEach(r => r.action = () => WorkGroupsSettings.GetSettings.SaveToPreset(r.Label));

            ret.Add(new FloatMenuOption("---", () => LogHelper.Verbose("Clicked the divider -_-;;")));
            
            ret.Add(new FloatMenuOption("New", () => {
                var newTextWindow = new RenameWindow("New");
                Action<string> saveAction = name => {
                    WorkGroupsSettings.GetSettings.SaveToPreset(name);
                };
                newTextWindow.Action = saveAction;
                Find.WindowStack.Add(newTextWindow);
            }));

            return ret;
        }
    }
}
