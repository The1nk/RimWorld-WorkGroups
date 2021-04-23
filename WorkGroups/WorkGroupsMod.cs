using System.Reflection;
using HugsLib;
using Verse;

namespace The1nk.WorkGroups {
    public class WorkGroupsMod : ModBase {
        public override string ModIdentifier => "The1nk.WorkGroups";

        public static WorkGroupsMod Mod { get; private set; }


        public override void Initialize() {
            base.Initialize();

            LogHelper.SetLogger(this.Logger);
            Mod = this;

            //LogHelper.Info("WorkGroups v" + Assembly.GetExecutingAssembly().GetName().Version.ToString());
        }
    }
}