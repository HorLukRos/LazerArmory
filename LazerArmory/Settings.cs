using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LazerArmory
{
    public class Character
    {
        public string name { get; set; } = "";
        public string realm { get; set; } = "";
        public bool enabled { get; set; } = true;
    }

    public class Settings
    {
        public List<Character> characters { get; set; } = new List<Character>();
        public bool runAtStartup { get; set; } = true;
        public string wowPath { get; set; } = "";
        public string appVersion { get; set; } = "";
    }
}
