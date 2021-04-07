using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using The1nk.WorkGroups.Models;
using UnityEngine;
using Verse;

namespace The1nk.WorkGroups
{
    public class MainTabWindow : RimWorld.MainTabWindow {
        private int verticalPadding = 10;
        private int horizontalPadding = 10;
        private float textHeight = 0;
        private Vector2 scrollPosition;

        private static readonly Texture2D ReorderUp = ContentFinder<Texture2D>.Get("UI/Buttons/ReorderUp");
        private static readonly Texture2D ReorderDown = ContentFinder<Texture2D>.Get("UI/Buttons/ReorderDown");

        public override void DoWindowContents(UnityEngine.Rect inRect) {
            base.DoWindowContents(inRect);
            textHeight = Text.CalcSize("The quick brown fox jumps over the lazy dog").y + (verticalPadding / 2f);
            string buffer = WorkGroupsSettings.GetSettings.HoursUpdateInterval.ToString("0");
            string buffer2 = WorkGroupsSettings.GetSettings.MaxPriority.ToString("0");

            var cbLocation = new Rect(inRect);
            cbLocation.x += horizontalPadding;
            cbLocation.y += verticalPadding;
            cbLocation.width -= 2 * horizontalPadding;
            cbLocation.height = textHeight;

            Widgets.CheckboxLabeled(cbLocation, "Enabled", ref WorkGroupsSettings.GetSettings.Enabled);

            cbLocation.y += textHeight + verticalPadding;
            Widgets.CheckboxLabeled(cbLocation, "Set Priorities for Slaves? (Simple Slavery support)", ref WorkGroupsSettings.GetSettings.SetPrioritiesForSlaves);

            cbLocation.y += textHeight + verticalPadding;
            Widgets.CheckboxLabeled(cbLocation, "Set Priorities for Prisoners? (Prison Labor support)", ref WorkGroupsSettings.GetSettings.SetPrioritiesForPrisoners);

            if (WorkGroupsSettings.GetSettings.RjwInstalled) {
                cbLocation.y += textHeight + verticalPadding;
                Widgets.CheckboxLabeled(cbLocation, "Set Priorities for Whores? (RimJobWorld support)", ref WorkGroupsSettings.GetSettings.SetPrioritiesForRjwWorkers);
            }

            cbLocation.y += textHeight + verticalPadding;
            Widgets.CheckboxLabeled(cbLocation, "Set Pawn Titles? (Show titles under names with Guards For Me)", ref WorkGroupsSettings.GetSettings.SetPawnTitles);

            cbLocation.y += textHeight + verticalPadding;
            Widgets.CheckboxLabeled(cbLocation, "Clear Schedules?", ref WorkGroupsSettings.GetSettings.ClearOutSchedules);

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
                WorkGroupsSettings.GetSettings.WorkGroups.Add(new WorkGroup());
            }

            if (Widgets.ButtonText(btnDel, "Del")) {
                Find.WindowStack.Add(new FloatMenu(WorkGroups.WorkGroupsSettings.GetSettings.WorkGroups.Select(w =>
                    new FloatMenuOption($"Delete {w.Name}",
                        () => { WorkGroups.WorkGroupsSettings.GetSettings.WorkGroups.Remove(w); })).ToList()));
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
            txtRec.width = 100;
            if (Widgets.ButtonText(txtRec, "Edit")) {
                Find.WindowStack.Add(new FloatMenu(GetWorkTypesList(group)));
            }
            newLoc.x += 120;

            string qtyBuffer = @group.TargetQuantity.ToString("0");
            txtRec = new Rect(newLoc);
            txtRec.width = 80;
            Widgets.TextFieldNumeric(txtRec, ref @group.TargetQuantity, ref qtyBuffer, 0, 999);
            newLoc.x += 100;

            Widgets.Checkbox(new Vector2(newLoc.x, newLoc.y), ref @group.DisableTitleForThisWorkGroup);
            newLoc.x += 70;
            
            Widgets.Checkbox(new Vector2(newLoc.x, newLoc.y), ref @group.AssignToEveryone);
            newLoc.x += 70;
            
            Widgets.Checkbox(new Vector2(newLoc.x, newLoc.y), ref @group.ColonistsAllowed);
            newLoc.x += 70;
            
            Widgets.Checkbox(new Vector2(newLoc.x, newLoc.y), ref @group.SlavesAllowed);
            newLoc.x += 50;
            
            Widgets.Checkbox(new Vector2(newLoc.x, newLoc.y), ref @group.PrisonersAllowed);
            newLoc.x += 70;
            
            if (WorkGroupsSettings.GetSettings.RjwInstalled) {
                Widgets.Checkbox(new Vector2(newLoc.x, newLoc.y), ref @group.RjwWorkersAllowed);
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
            newLoc.x += 120;

            Widgets.Label(newLoc, "Target Qty");
            newLoc.x += 100;

            Widgets.Label(newLoc, "Hide Titles");
            newLoc.x += 70;
            
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


            base.PostClose();
        }

        public override void PreOpen() {
            base.PreOpen();



        }

        private List<FloatMenuOption> GetWorkTypesList(WorkGroup group) {
            var ret = new List<FloatMenuOption>();
            
            // Enabled ones
            ret.AddRange(group.Items.OrderByDescending(wt => wt.naturalPriority).Select(wt =>
                new FloatMenuOption($"ENABLED | {wt.labelShort}", () => group.Items.Remove(wt))));

            // Disabled ones
            ret.AddRange(WorkGroupsSettings.GetSettings.AllWorkTypes
                .Where(wt => wt.visible && !group.Items.Contains(wt)).OrderByDescending(wt => wt.naturalPriority)
                .Select(wt => new FloatMenuOption($"DISABLED | {wt.labelShort}", () => group.Items.Add(wt))));

            return ret;
        }
    }
}
