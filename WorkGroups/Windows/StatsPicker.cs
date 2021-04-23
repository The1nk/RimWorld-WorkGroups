using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using UnityEngine;
using Verse;

namespace The1nk.WorkGroups.Windows
{
    public class StatsPicker : Window {
        private IList<StatDef> _highStats;
        private IList<StatDef> _lowStats;
        private IList<StatDef> _allStats;

        private Vector2 _scroll1;
        private Vector2 _scroll2;
        private Vector2 _scroll3;

        private enum WhichList {
            HighStats,
            AllStats,
            LowStats
        }

        public override Vector2 InitialSize => new Vector2(800, 650);

        public StatsPicker(IList<StatDef> highStats, IList<StatDef> lowStats) {
            _highStats = highStats;
            _lowStats = lowStats;

            this.doCloseX = true;
            this.absorbInputAroundWindow = true;
            RefreshAllStats();
        }

        private void RefreshAllStats() {
            _allStats = WorkGroupsSettings.GetSettings().AllStatDefs
                .Where(s => !_highStats.Contains(s) && !_lowStats.Contains(s)).OrderBy(s => s.LabelForFullStatListCap)
                .ToList();
        }

        public override void DoWindowContents(Rect inRect) {
            var r1 = inRect.GetInnerRect();
            r1.width = 230f;
            r1.height = 550;

            var header = new Rect(r1);
            r1.y += 50;
            header.height = 50;
            header.x += 30;

            // High Stats
            Widgets.Label(header, "HighStats".Translate());
            TooltipHandler.TipRegion(header, "HighStatsTip".Translate());
            DrawStatsList(r1, _highStats, ref _scroll1, WhichList.HighStats);
            r1.x += 232;
            header.x += 232;

            // All Stats
            Widgets.Label(header, "AllStats".Translate());
            TooltipHandler.TipRegion(header, "AllStatsTip".Translate());
            DrawStatsList(r1, _allStats, ref _scroll2, WhichList.AllStats);
            r1.x += 232;
            header.x += 232;

            // Low Stats
            Widgets.Label(header, "LowStats".Translate());
            TooltipHandler.TipRegion(header, "LowStatsTip".Translate());
            DrawStatsList(r1, _lowStats, ref _scroll3, WhichList.LowStats);
        }

        private void DrawStatsList(Rect rect, IList<StatDef> stats, ref Vector2 scrollPosition, WhichList whichList) {
            var listCopy = stats.OrderBy(s => s.LabelForFullStatListCap).ToList();
            var innerRect = rect.GetInnerRect();
            innerRect.height = (stats.Count() + 1) * Verse.Text.LineHeight;

            var newLoc = innerRect.GetInnerRect();
            newLoc.height = Verse.Text.LineHeight;
            newLoc.width -= 20;
            newLoc.x += 30;

            var btn1 = new Rect(newLoc.x - 30, newLoc.y, 20, newLoc.height);
            var btn2 = new Rect(newLoc.x + newLoc.width + 10, newLoc.y, 20, newLoc.height);

            Widgets.BeginScrollView(rect, ref scrollPosition, innerRect);

            foreach (var stat in listCopy) {
                Widgets.Label(newLoc, stat.LabelForFullStatListCap);
                TooltipHandler.TipRegion(newLoc, stat.description);

                switch (whichList) {
                    case WhichList.HighStats:
                        if (Widgets.ButtonText(btn2, ">")) {
                            stats.Remove(stat);
                            RefreshAllStats();
                        }
                        break;
                    case WhichList.AllStats:
                        if (Widgets.ButtonText(btn1, "<")) {
                            _highStats.Add(stat);
                            RefreshAllStats();
                        }

                        if (Widgets.ButtonText(btn2, ">")) {
                            _lowStats.Add(stat);
                            RefreshAllStats();
                        }

                        break;
                    case WhichList.LowStats:
                        if (Widgets.ButtonText(btn1, "<")) {
                            stats.Remove(stat);
                            RefreshAllStats();
                        }

                        break;
                }

                newLoc.y += Verse.Text.LineHeight;
                btn1.y = newLoc.y;
                btn2.y = newLoc.y;
            }

            Widgets.EndScrollView();
        }
    }
}
