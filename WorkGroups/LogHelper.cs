using System.IO;
using System.Reflection;
using HugsLib;
using HugsLib.Utils;
using RimWorld;
using Verse;

namespace The1nk.WorkGroups
{
    public class LogHelper {
        public static void SetLogger(ModLogger logger) => _logger = logger;
        private static ModLogger _logger;
        private static StreamWriter _sw;

        static LogHelper() {
            var path = GenFilePaths.SaveDataFolderPath;
            var filename = "WorkGroups.log";
            var filenamePrev = "WorkGroups-prev.log";

            if (File.Exists(System.IO.Path.Combine(path, filename))) {
                if (File.Exists(System.IO.Path.Combine(path, filenamePrev)))
                    File.Delete(System.IO.Path.Combine(path, filenamePrev));
                File.Move(System.IO.Path.Combine(path, filename), System.IO.Path.Combine(path, filenamePrev));
            }

            _sw = new StreamWriter(System.IO.Path.Combine(path, filename), false);
        }

        public static void Verbose(string message) {
            if (!WorkGroupsSettings.GetSettings().VerboseLogging)
                return;

            message = "Verbose\t" + message;
            _logger?.Message(message);
            _sw?.WriteLine(message);
            _sw?.Flush();
        }
        
        public static void Info(string message) {
            message = "Info\t" + message;

            _logger?.Message(message);
            _sw?.WriteLine(message);
            _sw?.Flush();
        }

        public static void Warning(string message) {
            message = "Warn\t" + message;
            
            _logger?.Warning(message);
            _sw?.WriteLine(message);
            _sw?.Flush();
        }

        public static void Error(string message) {
            message = "Error\t" + message;

            _logger?.Error(message);
            _sw?.WriteLine(message);
            _sw?.Flush();
        }
    }
}
