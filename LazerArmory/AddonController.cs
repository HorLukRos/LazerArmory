using System;
using System.Windows.Threading;
using System.IO;
using System.Collections.Generic;
using System.Net.Http;
using System.Net;
using Newtonsoft.Json;
using System.Windows;

namespace LazerArmory
{
    public static class AddonController
    {
        static bool isPinging = false;
        static Action<string> Log = Logger.Log;

        static string pingContent = "\r\nLazerArmoryPing = {ping}\r\n";

        const string UPLOAD_URL = "http://lazer-kittens.fun/armory/api/character.php?API_KEY=12e245e43a857524404982bcf4b7110a";

        public delegate void UploadStarted(object sender, object e);
        public static event UploadStarted OnUploadStarted;

        public delegate void UploadEnded(object sender, object e);
        public static event UploadEnded OnUploadEnded;

        public static void StartPinging()
        {
            if (isPinging)
                return;
            isPinging = true;

            Log("Starting pinging process.");

            DispatcherTimer pingTimer = new DispatcherTimer();
            pingTimer.Interval = TimeSpan.FromSeconds(60 * 5);
            pingTimer.Tick += PingTimer_Tick;
            pingTimer.Start();
            Ping();
        }

        private static void PingTimer_Tick(object sender, EventArgs e)
        {
            Ping();
        }

        static long EpochTime()
        {
            DateTime foo = DateTime.Now;
            long unixTime = ((DateTimeOffset)foo).ToUnixTimeSeconds();
            return unixTime;
        }

        public static void Ping()
        {
            var paths = WowController.PingFileLocations();
            if (paths == null)
                return;
            foreach (var path in paths)
            {
                if (!File.Exists(path))
                    File.Create(path).Close();
                File.WriteAllText(path, pingContent.Replace("{ping}", EpochTime().ToString()));
            }
        }

        public static void EnableUpload()
        {
            WowController.OnExport += WowController_OnExport;
        }

        static List<WowController.CharacterRecord[]> uploadQueue = new List<WowController.CharacterRecord[]>();
        static volatile bool uploading = false;
        static System.Timers.Timer waitAfterExport = null;
        private static void WowController_OnExport(object sender, WowController.CharacterRecord[] characters)
        {
            uploadQueue.Add(characters);
            if (waitAfterExport != null)
                waitAfterExport.Stop();
            waitAfterExport = new System.Timers.Timer();
            waitAfterExport.Interval = 500;
            waitAfterExport.Elapsed += WaitAfterExport_Elapsed;
            waitAfterExport.Start();
        }

        private static void WaitAfterExport_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            if (waitAfterExport != null)
                waitAfterExport.Stop();
            waitAfterExport = null;
            UploadQueue();
        }

        static void UploadQueue()
        {
            if (uploading)
                return;

            uploading = true;

            while (uploadQueue.Count > 0)
            {
                // We only want to upload the newest addition.
                var list = uploadQueue[uploadQueue.Count - 1];
                uploadQueue.Clear(); // It can be re-populated before we finish. That's the reason for the while-loop.
                Upload(list);
            }

            uploading = false;
            OnUploadEnded?.Invoke(null, null);
        }

        class GearSavedRecord
        {
            public string saved { get; set; }
        }
        static Dictionary<string, DateTime> savedCache = new Dictionary<string, DateTime>();

        private static readonly HttpClient client = new HttpClient();
        public static WowController.CharacterRecord LastSavedCharacter = null;

        static void Upload(WowController.CharacterRecord[] characters)
        {
            foreach (var character in characters)
            {
                var state = AppController.IsCharacterEnabled(character.Name, character.Realm);
                if (!state.HasValue)
                {
                    AppController.SetCharacterEnabled(character.Name, character.Realm,
                        Logger.Ask("New character: " + character.Name + " (" + character.Realm + ")\n\n" +
                        "Allow auto syncing to Lazer Kittens' Armory?\n\n" +
                        "This can be changed later in options."));
                    state = AppController.IsCharacterEnabled(character.Name, character.Realm);
                }

                if (state.HasValue && state.Value)
                {
                    var nicename = character.Name + " (" + character.Realm + ")";
                    string json = AppController.LUAtoJSON(File.ReadAllText(character.ExportedFile), character.Realm);
                    var savedObj = JsonConvert.DeserializeObject<GearSavedRecord>(json);
                    var saved = DateTime.Parse("20" + savedObj.saved);
                    if (savedCache.ContainsKey(nicename) && savedCache[nicename] >= saved.AddSeconds(-1))
                    {
                        // We already saved this character.
                        Log("Character already up to date - ignoring: " + nicename);
                    }
                    else
                    {
                        savedCache[nicename] = saved;
                        OnUploadStarted?.Invoke(null, null);
                        Log("Uploading character: " + nicename + " from " + character.ExportedFile);

                        var httpWebRequest = (HttpWebRequest)WebRequest.Create(UPLOAD_URL);
                        httpWebRequest.ContentType = "application/json";
                        httpWebRequest.Method = "POST";

                        using (var streamWriter = new StreamWriter(httpWebRequest.GetRequestStream()))
                        {
                            streamWriter.Write(json);
                        }

                        var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();

                        using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
                        {
                            var result = streamReader.ReadToEnd();
                            Log("Response: " + httpResponse.StatusCode.ToString() + " - " + result);
                        }

                        LastSavedCharacter = character;
                    }
                }
                else
                {
                    Log("Ignoring character: " + character.Name + " (" + character.Realm + ")");
                }
            }
        }

        private static void CopyFilesRecursively(string sourcePath, string targetPath)
        {
            //Now Create all of the directories
            foreach (string dirPath in Directory.GetDirectories(sourcePath, "*", SearchOption.AllDirectories))
            {
                Directory.CreateDirectory(dirPath.Replace(sourcePath, targetPath));
            }

            //Copy all the files & Replaces any files with the same name
            foreach (string newPath in Directory.GetFiles(sourcePath, "*.*", SearchOption.AllDirectories))
            {
                File.Copy(newPath, newPath.Replace(sourcePath, targetPath), true);
            }
        }

        public static int GetAddonVersion()
        {
            var loc = WowController.ClassicAddonsLocation();
            var dir = Path.Combine(loc, "LazerArmory");
            if (!Directory.Exists(dir))
                return 0;

            var file = Path.Combine(dir, "LazerArmory.lua");
            if (!File.Exists(file))
                return 0;

            string s = File.ReadAllText(file);
            string x = "local VERSION = ";
            int i = s.IndexOf(x);
            if (i == -1)
                return 0;
            s = s.Substring(i + x.Length);
            i = s.IndexOf(';');
            s = s.Substring(0, i);

            int o = 0;
            bool ok = int.TryParse(s, out o);
            if (ok) return o; else return 0;
        }

        public static void InstallAddonIfNeeded(bool force=false)
        {
            var loc = WowController.ClassicAddonsLocation();
            var dir = Path.Combine(loc, "LazerArmory");
            if (Directory.Exists(dir) && !force)
                return;
            var from = Path.Combine(AppController.AppBaseFolder(), "Addon\\LazerArmory");

            Log("Installing addon from: " + from + " to: " + dir);
            Directory.CreateDirectory(dir);
            CopyFilesRecursively(from, dir);
        }
    }
}
