using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;

namespace The1nk.WorkGroups.Models {
    public class ScoreCard {
        public List<ScoreCardEntry> Entries;

        public ScoreCard() {
            Entries = new List<ScoreCardEntry>();
        }

        public void CalculateFinalModifiers() {
            var stats = Entries.SelectMany(e => e.Stats.Select(s => new {s.Stat, s.IsLowStat})).Distinct();

            foreach (var stat in stats) {
                var relevant =
                    Entries.SelectMany(e => e.Stats.Where(s => s.Stat == stat.Stat).Select(s => s.StatValue));
                var benchmark = (stat.IsLowStat) ? relevant.Min() : relevant.Max();
                if (!stat.IsLowStat)
                    Entries.ForEach(
                        e => e.FinalModifier *= ((e.Stats.Single(s => s.Stat == stat.Stat).StatValue / benchmark)));
                else
                    Entries.ForEach(
                        e => e.FinalModifier *= (benchmark / (e.Stats.Single(s => s.Stat == stat.Stat).StatValue)));
            }
        }
    }

    public class ScoreCardEntry {
        public PawnWithWorkgroups Pawn;
        public float Skill;
        public List<ScoreCardEntryStat> Stats;
        public float FinalModifier = 1;
        public bool HasSomeWantedTraits;

        public float FinalScore => Skill * FinalModifier;

        public ScoreCardEntry() {
            this.Stats = new List<ScoreCardEntryStat>();
        }
    }

    public class ScoreCardEntryStat {
        public StatDef Stat;
        public float StatValue;
        public bool IsLowStat;
    }
}
