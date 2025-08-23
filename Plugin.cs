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
    [BepInPlugin("net.lawofsynergy.stationeers.ic10e", "IC10 Extender", "0.0.6.1")]
    public class Plugin : BaseUnityPlugin
    {
        public static new ManualLogSource Logger;
        //The particular check for this is arbitrary, as long as we are checking for *something* that exists in the beta branch but not release.
        public static readonly bool IsBeta = typeof(ProgrammableChip).GetMethod("PackAscii6", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static) != null; 

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
                harmony.PatchAll(IsBeta ? typeof(BetaPatches) : typeof(ReleasePatches));

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
