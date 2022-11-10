using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LazerArmory
{
    public class VersionDescription
    {
        public class App
        {
            public string version { get; set; }
            public string link { get; set; }
        }

        public class Addon
        {
            public int version { get; set; }
            public string link { get; set; }
        }

        public App app { get; set; }
        public Addon addon { get; set; }
    }
}
