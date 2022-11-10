using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace LazerArmory
{
    public static class Logger
    {
        public static string CurrentLogLocation()
        {
            var loc = System.IO.Path.GetDirectoryName(System.AppDomain.CurrentDomain.BaseDirectory);
            var now = DateTime.Now;
            var stampDate = now.ToString("yyyy-MM-dd");
            var file = Path.Combine(loc, "Logs", "log-" + stampDate + ".txt");
            return file;
        }

        private static void CreateLogFileIfNeeded()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(CurrentLogLocation()));
            if (!File.Exists(CurrentLogLocation()))
                File.Create(CurrentLogLocation()).Close();
        }

        public static void Log(string msg)
        {
            var now = DateTime.Now;
            var stampTime = now.ToString("HH:mm:ss");
            CreateLogFileIfNeeded();
            try
            {
                File.AppendAllLines(CurrentLogLocation(), new string[] { "[" + stampTime + "] " + msg });
            }
            catch { }
        }

        public static void Error(string msg)
        {
            Log("ERROR: " + msg);
            MessageBox.Show(msg + "\n\nSee log for more information:\n" + CurrentLogLocation(), "Lazer Kittens' Armory - Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        public static void Info(string msg)
        {
            Log("INFO: " + msg);
            MessageBox.Show(msg + "", "Lazer Kittens' Armory - Info", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        public static bool Ask(string msg)
        {
            Log("ASK: " + msg);
            var res = MessageBox.Show(msg, "Lazer Kittens' Armory - Info", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (res == MessageBoxResult.Yes)
            {
                Log("ASK RESULT: Yes");
                return true;
            }
            else
            {
                Log("ASK RESULT: No");
                return false;
            }
        }
    }
}
