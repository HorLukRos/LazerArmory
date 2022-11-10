using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.IO;
using ProtoBuf;

namespace LazerArmory
{
    public static class WowController
    {
        private static Action<string> Log = Logger.Log;
        private static string _installLocation = null;
        private static string[] _accountLocations = null;
        private static string[] _pingFileLocations = null;

        public delegate void ExportHandler(object sender, CharacterRecord[] e);
        public static event ExportHandler OnExport;

        public static string ClassicAddonsLocation()
        {
            return Path.Combine(ClassicInstallLocation(), "Interface\\AddOns");
        }

        public static string ClassicInstallLocation()
        {
            if (!string.IsNullOrEmpty(_installLocation))
                return _installLocation;

            Log("Searching for Classic installation folder.");

            // First check if the path is in the config file or not.
            if (string.IsNullOrEmpty(AppController.GetSettings().wowPath))
            {
                Log("Nothing in config - searching ourselves.");
                string dbfile = System.IO.Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                "Battle.net\\Agent\\product.db");

                Log("Opening product file at: " + dbfile);

                var stream = new FileStream(dbfile, FileMode.Open);
                var db = Serializer.Deserialize<ProductDb>(stream);
                stream.Close();

                Log("Protobuf deserialization done.");

                if (db == null || db.products == null)
                {
                    Logger.Error("Couldn't deserialize Battle.net products!");
                    return null;
                }

                Log("Found product list, searching for classic.");

                foreach (var p in db.products)
                {
                    if (p.name == "wow_classic" && p.client.name == "_classic_")
                    {
                        var path = Path.Combine(p.client.location, "_classic_");
                        Log("Classic found: " + path);
                        _installLocation = path;
                        AppController.SetWowPath(path);
                        return path;
                    }
                }

                Logger.Error("WoW Classic not found in installed Battle.net products!");
                return null;
            }
            else
            {
                Log("Returning from config: " + AppController.GetSettings().wowPath);
                return AppController.GetSettings().wowPath;
            }

            
        }

        public static string[] AccountLocations()
        {
            if (_accountLocations != null)
                return _accountLocations;
            Log("Searching for all account files.");

            var root = Path.Combine(ClassicInstallLocation(), "WTF\\Account");
            var dirs = Directory.GetDirectories(root);

            if (dirs == null || dirs.Length == 0)
            {
                Logger.Error("No WoW Classic accounts found in: " + root);
                return new string[] { };
            }

            List<string> arr = new List<string>();

            for (var i = 0; i < dirs.Length; i++)
            {
                var name = Path.GetFileName(dirs[i]);
                if (name == "SavedVariables")
                    continue;
                arr.Add(dirs[i]);
                Log("Found account: " + dirs[i]);
            }

            _accountLocations = arr.ToArray();
            return _accountLocations;
        }

        public static string[] PingFileLocations()
        {
            if (_pingFileLocations != null)
                return _pingFileLocations;

            Log("Searching for all ping files.");

            var dirs = (string[])AccountLocations().Clone();
            for (var i = 0; i < dirs.Length; i++)
            {
                dirs[i] = Path.Combine(dirs[i], "SavedVariables\\LazerArmory.lua");
            }

            Log("Found ping locations: total " + dirs.Length);

            _pingFileLocations = dirs;

            return dirs;
        }

        public class CharacterRecord
        {
            public string Name { get; set; }
            public string Realm { get; set; }
            public string ExportedFile { get; set; }
            public DateTime Updated { get; set; }
        }

        static FileSystemWatcher watcher = null;
        public static void HookCharacterSaves()
        {
            var roots = AccountLocations();
            foreach (var acc in roots)
            {
                watcher = new FileSystemWatcher(acc); ;
                watcher.NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.CreationTime | NotifyFilters.Size; ;
                watcher.Changed += Watcher_Changed;
                watcher.Created += Watcher_Created;
                watcher.Renamed += Watcher_Renamed;
                watcher.Filter = "*.lua";
                watcher.IncludeSubdirectories = true;
                watcher.EnableRaisingEvents = true;
                Log("Hooked system watcher for: " + acc);
            }
            UploadChanges();
        }

        private static void Watcher_Renamed(object sender, RenamedEventArgs e)
        {
            UploadChanges();
        }

        private static void Watcher_Created(object sender, FileSystemEventArgs e)
        {
            UploadChanges();
        }
       
        private static void Watcher_Changed(object sender, FileSystemEventArgs e)
        {
            UploadChanges();
        }

        private static void UploadChanges()
        {
            Log("Triggered Upload in WoW Controller class.");
            var chars = ReadAllCharacters();
            if (chars != null && chars.Length > 0)
                OnExport?.Invoke(null, chars);
        }

        public static CharacterRecord[] ReadAllCharacters()
        {
            Log("Searching for all characters.");

            var accounts = AccountLocations();
            List<CharacterRecord> arr = new List<CharacterRecord>();
            for (var i = 0; i < accounts.Length; i++)
            {
                var dirs = Directory.GetDirectories(accounts[i]);

                if (dirs == null || dirs.Length == 0)
                    continue;

                for (var j = 0; j < dirs.Length; j++)
                {
                    var realm = Path.GetFileName(dirs[j]);
                    if (realm == "SavedVariables")
                        continue;

                    var chars = Directory.GetDirectories(dirs[j]);

                    for (var k = 0; k < chars.Length; k++)
                    {
                        var name = Path.GetFileName(chars[k]);
                        var path = Path.Combine(chars[k], "SavedVariables\\LazerArmory.lua");

                        if (File.Exists(path))
                        {
                            // We found an export. Check if this character exsits already and if yes, check what
                            // export is the newest. This shouldn't happen, but it can if the user has some
                            // backups of their account config or something. It happened to me so let's handle it.

                            var updated = File.GetLastWriteTime(path);
                            var old = arr.FirstOrDefault(x => x.Name == name && x.Realm == realm);
                            if (old != default(CharacterRecord))
                            {
                                // There is a character with this name / realm.
                                if (old.Updated < updated)
                                {
                                    // And it's old. Rewrite it.
                                    old.ExportedFile = path;
                                    Log("Rewritten character: " + name + " - " + realm + ": " + path);
                                }
                            }
                            else
                            {
                                // No character with this name / realm.
                                CharacterRecord rec = new CharacterRecord
                                {
                                    Name = name,
                                    Realm = realm,
                                    Updated = updated,
                                    ExportedFile = path
                                };
                                arr.Add(rec);
                                Log("Found character: " + name + " - " + realm + ": " + path);
                            }
                        }
                    }
                }
            }

            return arr.ToArray();
        }
    }
}
