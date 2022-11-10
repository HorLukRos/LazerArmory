using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace LazerArmory
{
    public partial class App : Application
    {
        public new void Run()
        {
            // Updater is not async! First do that, then other things.
            AppController.RunUpdater();

            AddonController.StartPinging();
            AddonController.EnableUpload();
            WowController.HookCharacterSaves();
            AppController.GetSettings(); // This will install auto-run if first time.
            AddonController.InstallAddonIfNeeded();

            base.Run();
        }
    }
}
