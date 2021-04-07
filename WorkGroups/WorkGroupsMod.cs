using System.Reflection;
using Verse;

namespace The1nk.WorkGroups {
    public class WorkGroupsMod : Mod {
        public WorkGroupsMod(ModContentPack content) : base(content) {
            LogHelper.Info("WorkGroups v" + Assembly.GetExecutingAssembly().GetName().Version.ToString());
        }
    }
}