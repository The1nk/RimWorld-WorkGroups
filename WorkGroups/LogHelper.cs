using Verse;

namespace The1nk.WorkGroups
{
    public static class LogHelper {
        private const string Prefix = "[The1nk.WorkGroups]";

        public static void Verbose(string message) {
            if (WorkGroupsSettings.GetSettings.VerboseLogging)
                Log.Message($"{Prefix} V {message}", true);
        }
        
        public static void Info(string message) {
            Log.Message($"{Prefix} I {message}");
        }

        public static void Warning(string message) {
            Log.Warning($"{Prefix} W {message}");
        }

        public static void Error(string message) {
            Log.Warning($"{Prefix} E {message}");
        }
    }
}
