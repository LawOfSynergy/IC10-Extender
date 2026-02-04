using Assets.Scripts;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using HarmonyLib.Tools;
using IC10_Extender.Compat;
using IC10_Extender.Operations;
using System;

namespace IC10_Extender
{

    [BepInPlugin("net.lawofsynergy.stationeers.ic10e", "IC10 Extender", "0.2.1.0")]
    public class Plugin : BaseUnityPlugin
    {
        public static new ManualLogSource Logger { get; private set; }
        public static Plugin Instance { get; private set; }

        public FeatureFlag<bool> AddPreprocessorVariants { get; private set; }
        public FeatureFlag<bool> LabelsCanShareLine { get; private set; }

        public Plugin()
        {
            if (Instance != null)
            {
                throw new InvalidOperationException("Duplicate instance of IC10 Extender detected!");
            }

            Logger = base.Logger;
            Instance = this;
        }

        private void InitConfig()
        {
            AddPreprocessorVariants = new FeatureFlag<bool>(
                Config.Bind(
                    "Features",
                    "AddPreprocessorVariants",
                    false,
                    "If true, adds variants of existing preprocessors (such as HASH(...) vs RHASH(...)) that replace the original source text with the calculated value. " +
                    "The original text is not preserved on chip reload when using these variants."
                )
            );

            LabelsCanShareLine = new FeatureFlag<bool>(
                Config.Bind(
                    "Features",
                    "LabelsCanShareLine",
                    false,
                    "If true, a label can be followed by a statement on the same line. If false, the vanilla behavior of a label having to take up a whole line is be used."
                )
            );
        }

        void Awake()
        {
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
            Colors.Register();
        }
    }
}
