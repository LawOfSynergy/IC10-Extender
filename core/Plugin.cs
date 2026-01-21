using Assets.Scripts.Objects.Electrical;
using BepInEx;
using BepInEx.Harmony;
using BepInEx.Logging;
using HarmonyLib;
using HarmonyLib.Tools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace IC10_Extender
{

    [BepInPlugin("net.lawofsynergy.stationeers.ic10e", "IC10 Extender", "0.2.1.0")]
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

            UnityEngine.Debug.Log("Loading Mod");
            Logger.LogInfo("Loading Mod");
            try
            {
                UnityEngine.Debug.Log("Loading Harmony Patches");
                Logger.LogInfo("Loading Harmony Patches");
                var harmony = new Harmony("com.lawofsynergy.stationeers.ic10e");
                HarmonyFileLog.Enabled = true;
                harmony.PatchAll(typeof(CommonPatches));

                UnityEngine.Debug.Log("Patch succeeded");
                Logger.LogInfo("Patch succeeded");
            }
            catch (Exception e)
            {
                UnityEngine.Debug.Log("Patch Failed");
                UnityEngine.Debug.Log(e.ToString());
                Logger.LogInfo("Patch Failed");
                Logger.LogInfo(e.ToString());
            }

            DefaultPreprocessors.Register();
            IC10Extender.Register(new ThrowOperation());
        }
    }
}
