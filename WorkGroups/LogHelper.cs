using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace The1nk.WorkGroups
{
    public static class LogHelper {
        private const string Prefix = "[The1nk.WorkGroups]";

        public static void Info(string message) {
            Log.Message($"{Prefix} I {message}");
        }

        public static void Verbose(string message) {
            if (WorkGroupsSettings.GetSettings.VerboseLogging)
                Log.Message($"{Prefix} V {message}", true);
        }
    }
}
