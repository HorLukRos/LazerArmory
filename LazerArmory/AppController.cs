using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace LazerArmory
{
    public static class AppController
    {
        public const string APP_VERSION = "1.4.0"; // change this to 1.4.0!
        public const string LAST_VERSION_URL = "http://lazer-kittens.fun/armory/version.json";

        public static string LUAtoJSON(string lua, string realm)
        {
            // This is extremely naive and bad way of doing this, but it should be enough for our use-case.

            // Get rid of WoW comments of elements:
            for (var x = 1; x <= 1000; x++)
                lua = lua.Replace(", -- [" + x + "]", ",");

            // Change all nils to nulls:
            lua = lua.Replace("nil,", "null,");

            // Replace initial [ everywhere:
            lua = lua.Replace("[\"", "\"");

            // Replace the ending ] everywhere and make = into :
            lua = lua.Replace("\"] =", "\" :");

            // Make specs into arrays:
            lua = lua.Replace("\"Spec1\" : {", "\"Spec1\" : [");
            lua = lua.Replace("\"Spec2\" : {", "\"Spec2\" : [");

            var i = lua.IndexOf("\"Spec1\" : [") + 12;
            var s = lua.Substring(i);
            var j = s.IndexOf("}") + i;
            lua = lua.Substring(0, j) + "]" + lua.Substring(j + 1);

            i = lua.IndexOf("\"Spec2\" : [") + 12;
            s = lua.Substring(i);
            j = s.IndexOf("}") + i;
            lua = lua.Substring(0, j) + "]" + lua.Substring(j + 1);

            // Get rid of the prefix:
            lua = lua.Replace("LazerArmoryGear = ", "");

            // Fix attributes:
            var attributes = new string[] { "Name", "Saved", "Spec1", "Spec2", "Archive", "Guild", "Level", "Desc1", "Desc2", "Profs" };
            foreach (var a in attributes)
                lua = lua.Replace("\"" + a + "\"", "\"" + a.ToLower() + "\"");
            lua = lua.Replace("Class", "classId");
            lua = lua.Replace("GuildRank", "guildRank");
            lua = lua.Replace("ProfRecipes", "profRecipes" );

            var professions = new string[] { "Herbalism", "Mining", "Skinning", "Alchemy", "Blacksmithing", "Enchanting", "Engineering", "Inscription", "Jewelcrafting", "Leatherworking", "Tailoring", "Archeology", "Cooking", "Fishing", "First Aid", "Lockpicking", "Riding", "Runeforging", "Poisons" };
            foreach (var a in professions)
            {
                lua = lua.Replace("\""+a+"\" : {", "\""+a+"\" : [");

                var sf = "\"" + a + "\" : [";
                var ix = lua.IndexOf(sf);
                if (ix > -1)
                {
                    var iy = ix + sf.Length + 1;
                    var sx = lua.Substring(ix);
                    var jx = sx.IndexOf("}") + ix;
                    lua = lua.Substring(0, jx) + "]" + lua.Substring(jx + 1);
                }

                lua = lua.Replace("\"" + a + "\"", "\"" + a.ToLower() + "\"");
            }
            lua = lua.Replace("first aid", "firstAid");

            // Get rid of the commas at the end of arrays:
            var tab = "										";
            while(tab != "")
            {
                tab = tab.Substring(0, tab.Length - 1);
                lua = lua.Replace(",\r\n"+tab+"}", "\r\n"+tab+"}");
                lua = lua.Replace(",\r\n"+tab+"]", "\r\n"+tab+"]");
                lua = lua.Replace(",\r\n"+tab+"}", "\r\n"+tab+"]");
                lua = lua.Replace(",\r\n"+tab+"]", "\r\n"+tab+"}");
                lua = lua.Replace(",\n" + tab + "}", "\n" + tab + "}");
                lua = lua.Replace(",\n" + tab + "]", "\n" + tab + "]");
                lua = lua.Replace(",\n" + tab + "}", "\n" + tab + "]");
                lua = lua.Replace(",\n" + tab + "]", "\n" + tab + "}");
            }

            // Add realm:
            lua = lua.Replace("\"name\"", "\"realm\":\""+ realm +"\",\r\n  \"name\"");

            return lua;
        }

        static Settings _settings = null;

        public static string AppBaseFolder()
        {
            return System.IO.Path.GetDirectoryName(System.AppDomain.CurrentDomain.BaseDirectory);
        }

        static string SettingsFilePath()
        {            
            var file = Path.Combine(AppBaseFolder(), "settings.json");
            return file;
        }

        public static string VersionBeforeThisStartup { get; set; } = null;

        public static Settings GetSettings()
        {
            if (_settings == null)
            {

                if (File.Exists(SettingsFilePath()))
                    _settings = JsonConvert.DeserializeObject<Settings>(File.ReadAllText(SettingsFilePath()));
                else
                {
                    _settings = new Settings();
                    EnableAutoStartup();
                }
                VersionBeforeThisStartup = _settings.appVersion;
                if (_settings.appVersion != APP_VERSION)
                {
                    _settings.appVersion = APP_VERSION;
                    SaveSettings();
                    // Force the installation of a new addon because we have a new version of the app.
                    AddonController.InstallAddonIfNeeded(true);
                }
            }
            return _settings;
        }

        public static void SetCharacterEnabled(string name, string realm, bool enabled)
        {
            var c = GetSettings().characters.FirstOrDefault(x => x.name == name && x.realm == realm);
            if (c == default(Character))
            {
                Character nc = new Character { name = name, realm = realm, enabled = enabled };
                GetSettings().characters.Add(nc);
            }
            else
            {
                c.enabled = enabled;
            }

            SaveSettings();
        }

        public static void SetWowPath(string path)
        {
            GetSettings().wowPath = path;
            SaveSettings();
        }

        public static string RootPath()
        {
            return System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
        }

        public static string ExePath()
        {
            var file = Path.Combine(RootPath(), "LazerArmory.exe");
            return file;
        }

        public static void EnableAutoStartup()
        {
            Microsoft.Win32.RegistryKey key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);
            key.SetValue("LazerArmory", ExePath());
            GetSettings().runAtStartup = true;
            SaveSettings();
        }

        public static void DisableAutoStartup()
        {
            Microsoft.Win32.RegistryKey key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);            
            key.DeleteValue("LazerArmory");
            GetSettings().runAtStartup = false;
            SaveSettings();
        }

        static void SaveSettings()
        {
            var str = JsonConvert.SerializeObject(GetSettings(), Formatting.Indented);
            if (!File.Exists(SettingsFilePath()))
                File.Create(SettingsFilePath()).Close();
            File.WriteAllText(SettingsFilePath(), str);
        }

        public static bool? IsCharacterEnabled(string name, string realm)
        {
            var c = GetSettings().characters.FirstOrDefault(x => x.name == name && x.realm == realm);
            if (c == default(Character))
                return null;
            return c.enabled;
        }

        public static void CopyFilesRecursivelyForBackup(DirectoryInfo source, DirectoryInfo target, bool moveInstead)
        {
            foreach (DirectoryInfo dir in source.GetDirectories())
                if (dir.Name != "NewVersion" && dir.Name != "Logs" && dir.Name != "OldVersionBackup")
                    CopyFilesRecursivelyForBackup(dir, target.CreateSubdirectory(dir.Name), moveInstead);
            foreach (FileInfo file in source.GetFiles())
                if (file.Name != "NEW_VERSION.zip" && file.Name != "settings.json")
                    if (moveInstead)
                        file.MoveTo(Path.Combine(target.FullName, file.Name));
                    else
                        file.CopyTo(Path.Combine(target.FullName, file.Name));
        }

        public static void RunUpdater(bool ShowNoNewVersionInfo = false)
        {
            // First we need to check if we are in the installation step:
            if (Path.GetFileName(RootPath()) == "NewVersion" && File.Exists(Path.Combine(RootPath(), "NEW_VERSION.zip")))
            {
                // This is the step in between - we just copy the file to the old location - one level above, and run the final app there.
                try
                {
                    // Delete the last backup if there is any:
                    var dir = Path.Combine(RootPath(), "..\\OldVersionBackup");
                    if (Directory.Exists(dir))
                        Directory.Delete(dir, true);
                    Directory.CreateDirectory(dir);

                    // Make a backup by moving everything in the folder above except for our own folder:
                    CopyFilesRecursivelyForBackup(new DirectoryInfo(Path.Combine(RootPath(), "..")), new DirectoryInfo(dir), true);

                    // Now copy everything from current folder to root:
                    CopyFilesRecursivelyForBackup(new DirectoryInfo(RootPath()), new DirectoryInfo(Path.Combine(RootPath(), "..")), false);
                }
                catch (Exception ex) { MessageBox.Show(ex.Message, "Lazer Kittens' Armory", MessageBoxButton.OK, MessageBoxImage.Error); }

                MessageBox.Show("Installation complete! Lazer Kittens' Armory will start now. Enjoy!", "Meow!", MessageBoxButton.OK, MessageBoxImage.Information);
                Process.Start(Path.Combine(RootPath(), "..\\LazerArmory.exe"));
                System.Windows.Application.Current.Shutdown();
            }
            else
            {
                // Now the actual rutine for the updater:
                try
                {
                    // We might be in the final step of the installation - remove NewVersion if necessary:
                    var dir2 = Path.Combine(RootPath(), "NewVersion");
                    if (Directory.Exists(dir2))
                        Task.Run(() => { Thread.Sleep(1000); Directory.Delete(dir2, true); });

                    // Load latest versions:
                    var newest = LoadLastVersion();
                    if (newest == null) return;

                    // First check if there is a new app (with addon included):
                    var myAppVersion = new Version(APP_VERSION);
                    var latestAppVersion = new Version(newest.app.version);
                    var result = latestAppVersion.CompareTo(myAppVersion);
                    if (result > 0)
                    {
                        // There is a new version of app!
                        var res = MessageBox.Show("Huray! New version of Lazer Kittens' Armory app is available!\n\nDownload and install?", "Meow!", MessageBoxButton.YesNo, MessageBoxImage.Question);
                        if (res == MessageBoxResult.Yes)
                        {
                            // Create a directory for our download:
                            var dir = Path.Combine(RootPath(), "NewVersion");
                            if (Directory.Exists(dir))
                                Directory.Delete(dir, true);
                            var file = Path.Combine(dir, "NEW_VERSION.zip");
                            Directory.CreateDirectory(dir);

                            // Download .ZIP file:
                            using (var client = new WebClient())
                            {
                                client.DownloadFile(newest.app.link, file);
                            }

                            // Extract the .ZIP file:
                            ZipFile.ExtractToDirectory(file, dir);

                            // Shutdown the app, the new one will proceed with installation:
                            MessageBox.Show("Download complete. New version of Lazer Kittens' Armory will be installed now.", "Meow!", MessageBoxButton.OK, MessageBoxImage.Information);
                            Process.Start(Path.Combine(dir, "LazerArmory.exe"));
                            System.Windows.Application.Current.Shutdown();
                        }
                        else
                            MessageBox.Show("*Sad kitten noises* :-(", "Meow!", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else
                    {
                        // The app is newest, but there still could be a new addon:
                        var myAddonVersion = AddonController.GetAddonVersion();
                        var newestAddonVersion = newest.addon.version;
                        if (newestAddonVersion > myAddonVersion)
                        {
                            // There is a newer addon, but not a newer app. Just download the addon.
                            var res = MessageBox.Show("Huray! New version of Lazer Kittens' Armory game addon is available!\n\nDownload and rewrite the old one?", "Meow!", MessageBoxButton.YesNo, MessageBoxImage.Question);
                            if (res == MessageBoxResult.Yes)
                            {
                                // Download .ZIP file:
                                var file = Path.Combine(RootPath(), "NEW_ADDON.zip");
                                using (var client = new WebClient())
                                {
                                    client.DownloadFile(newest.addon.link, file);
                                }

                                // Remove old addon:
                                var addonDir = Path.Combine(RootPath(), "Addon");
                                if (Directory.Exists(addonDir))
                                    Directory.Delete(addonDir, true);
                                Directory.CreateDirectory(addonDir);

                                // Extract the .ZIP file:
                                ZipFile.ExtractToDirectory(file, Path.Combine(RootPath(), "Addon"));

                                // Delete .ZIP:
                                File.Delete(file);

                                // Complete the installation:
                                AddonController.InstallAddonIfNeeded(true);
                                MessageBox.Show("Done. You are now using the latest version of Lazer Kittens' Armory. Enjoy!", "Meow!", MessageBoxButton.OK, MessageBoxImage.Information);
                            }
                            else
                                MessageBox.Show("*Sad kitten noises* :-(", "Meow!", MessageBoxButton.OK, MessageBoxImage.Information);
                        }

                        if (ShowNoNewVersionInfo)
                            MessageBox.Show("You are using the newest version of Lazer Kittens' Armory.", "Meow!", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
                catch (Exception ex) { MessageBox.Show(ex.Message, "Lazer Kittens' Armory", MessageBoxButton.OK, MessageBoxImage.Error); }
            }
        }

        public static VersionDescription LoadLastVersion()
        {
            try
            {
                Logger.Log("Loading last version from " + LAST_VERSION_URL);
                using (WebClient wc = new WebClient())
                {
                    var str = wc.DownloadString(LAST_VERSION_URL);
                    return JsonConvert.DeserializeObject<VersionDescription>(str);
                }
            }
            catch (Exception ex) { Logger.Log("Could not load last version: " + ex.Message); }
            return null;
        }
    }
}
