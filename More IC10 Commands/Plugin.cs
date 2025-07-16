using BepInEx.Logging;
using BepInEx;
using HarmonyLib.Tools;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IC10_Extender
{
    [BepInPlugin("net.lawofsynergy.stationeers.ic10e", "IC10 Extender", "0.0.2.0")]
    public class Plugin : BaseUnityPlugin
    {
        public static new ManualLogSource Logger;

        public static ManualLogSource GetLogger()
        {
            return Logger;
        }

        void Awake()
        {
            Plugin.Logger = base.Logger;

            Logger.LogInfo("Loading Mod");
            try
            {
                Logger.LogInfo("Loading Harmony Patches");
                var harmony = new Harmony("com.lawofsynergy.stationeers.ic10e");
                HarmonyFileLog.Enabled = true;
                Patches.Apply(harmony);
                harmony.PatchAll();
                Logger.LogInfo("Patch succeeded");
            }
            catch (Exception e)
            {
                Logger.LogInfo("Patch Failed");
                Logger.LogInfo(e.ToString());
            }

            IC10Extender.Register(new ThrowOperation());
        }
    }
}
