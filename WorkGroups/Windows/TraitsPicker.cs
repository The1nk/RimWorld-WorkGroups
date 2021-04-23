using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using UnityEngine;
using Verse;

namespace The1nk.WorkGroups.Windows {
    public class TraitsPicker : Window {
        private IList<Trait> _mustHave;
        private IList<Trait> _wantToHave;
        private IList<Trait> _cantHave;
        private IList<Trait> _allTraits;

        private Vector2 _scroll1;
        private Vector2 _scroll2;
        private Vector2 _scroll3;
        private Vector2 _scroll4;

        private enum WhichList {
            MustHave,
            WantToHave,
            CantHave,
            All
        }

        public override Vector2 InitialSize => new Vector2(1000, 650);

        public TraitsPicker(IList<Trait> mustHaves, IList<Trait> wantToHaves, IList<Trait> cantHaves) {
            _mustHave = mustHaves;
            _wantToHave = wantToHaves;
            _cantHave = cantHaves;

            if (_mustHave == null)
                _mustHave = new List<Trait>();
            if (_wantToHave == null)
                _wantToHave = new List<Trait>();
            if (_cantHave == null)
                _cantHave = new List<Trait>();

            this.doCloseX = true;
            this.absorbInputAroundWindow = true;
            RefreshAllTraits();
        }

        private void RefreshAllTraits() {
            _allTraits = WorkGroupsSettings.GetSettings().AllTraits
                .Where(t => !_mustHave.Contains(t) && !_wantToHave.Contains(t) && !_cantHave.Contains(t)).ToList();
        }

        public override void DoWindowContents(Rect inRect) {
            var r1 = inRect.GetInnerRect();
            r1.width = 230f;
            r1.height = 550;

            var header = new Rect(r1);
            r1.y += 50;
            header.height = 50;
            header.x += 30;

            // Must Haves
            Widgets.Label(header, "MustHaves".Translate());
            TooltipHandler.TipRegion(header, "MustHavesTip".Translate());
            DrawTraitsList(r1, _mustHave, ref _scroll1, WhichList.MustHave);
            r1.x += 232;
            header.x += 232;

            // Want To Haves
            Widgets.Label(header, "WantToHaves".Translate());
            TooltipHandler.TipRegion(header, "WantToHavesTip".Translate());
            DrawTraitsList(r1, _wantToHave, ref _scroll2, WhichList.WantToHave);
            r1.x += 232;
            header.x += 232;

            // Can't Haves
            Widgets.Label(header, "CantHaves".Translate());
            TooltipHandler.TipRegion(header, "CantHavesTip".Translate());
            DrawTraitsList(r1, _cantHave, ref _scroll3, WhichList.CantHave);
            r1.x += 232;
            header.x += 232;

            // All traits
            Widgets.Label(header, "AllTraits".Translate());
            TooltipHandler.TipRegion(header, "AllTraitsTip".Translate());
            DrawTraitsList(r1, _allTraits, ref _scroll4, WhichList.All);
        }

        private void DrawTraitsList(Rect rect, IList<Trait> stats, ref Vector2 scrollPosition, WhichList whichList) {
            var listCopy = stats.ToList();
            var innerRect = rect.GetInnerRect();
            innerRect.height = (stats.Count() + 1) * Verse.Text.LineHeight;

            var newLoc = innerRect.GetInnerRect();
            newLoc.height = Verse.Text.LineHeight;
            newLoc.width -= 20;
            newLoc.x += 30;

            var btn2 = new Rect(newLoc.x + newLoc.width + 10, newLoc.y, 20, newLoc.height);

            Widgets.BeginScrollView(rect, ref scrollPosition, innerRect);

            foreach (var stat in listCopy) {
                Widgets.Label(newLoc, stat.CurrentData.label);
                TooltipHandler.TipRegion(newLoc, stat.CurrentData.description);

                switch (whichList) {
                    case WhichList.MustHave:
                    case WhichList.WantToHave:
                    case WhichList.CantHave:
                        if (Widgets.ButtonText(btn2, "-")) {
                            stats.Remove(stat);
                            RefreshAllTraits();
                        }

                        break;
                    case WhichList.All:
                        if (Widgets.ButtonText(btn2, "+")) {
                            var lst = GetFloatOptions(stat);
                            if (lst.Any())
                                Find.WindowStack.Add(new FloatMenu(lst));
                            RefreshAllTraits();
                        }

                        break;
                }

                newLoc.y += Verse.Text.LineHeight;
                btn2.y = newLoc.y;
            }

            Widgets.EndScrollView();
        }

        private List<FloatMenuOption> GetFloatOptions(Trait trait) {
            var ret = new List<FloatMenuOption>();

            ret.Add(new FloatMenuOption("MustHaves".Translate(), () => {
                _mustHave.Add(trait);
                RefreshAllTraits();
            }));
            ret.Add(new FloatMenuOption("WantToHaves".Translate(), () => {
                _wantToHave.Add(trait);
                RefreshAllTraits();
            }));
            ret.Add(new FloatMenuOption("CantHaves".Translate(), () => {
                _cantHave.Add(trait);
                RefreshAllTraits();
            }));

            return ret;
        }
    }
}