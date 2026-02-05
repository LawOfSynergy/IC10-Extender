using Assets.Scripts;
using Assets.Scripts.Objects.Electrical;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using HarmonyLib.Tools;
using IC10_Extender.Compat;
using IC10_Extender.Operations;
using System;
using System.Collections.Generic;
using static IC10_Extender.Compat.ConfigExtensions;

namespace IC10_Extender
{

    [BepInPlugin("net.lawofsynergy.stationeers.ic10e", "IC10 Extender", "0.2.1.0")]
    public class Plugin : BaseUnityPlugin
    {
        public static new ManualLogSource Logger { get; private set; }
        public static Plugin Instance { get; private set; }

        public ConfigEntry<bool> AddPreprocessorVariants { get; private set; }
        public ConfigEntry<bool> LabelsCanShareLine { get; private set; }

        public readonly ConstantCompatibility Common;
        public readonly ConstantCompatibility AfterStrings;
        public readonly ConstantCompatibility BeforeStrings;
        public readonly CompatibilityCheck PreprocessorVariantsEnabled;
        public readonly CompatibilityCheck PreprocessorVariantsDisabled;
        public readonly CompatibilityCheck LabelsCanShareLineEnabled;
        public readonly CompatibilityCheck LabelsCanShareLineDisabled;

        public Plugin()
        {
            if (Instance != null)
            {
                throw new InvalidOperationException("Duplicate instance of IC10 Extender detected!");
            }

            Logger = base.Logger;
            Instance = this;

            InitConfig();

            //initialize compatability checks
            UnityEngine.Debug.Log("Initializing Compatability Flags");
            Logger.LogInfo("Initializing Compatability Flags");

            Common = new ConstantCompatibility(true);
            AfterStrings = new ConstantCompatibility(typeof(ProgrammableChip).GetMethod("PackAscii6", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static) != null);
            BeforeStrings = new ConstantCompatibility(!AfterStrings.Condition);
            PreprocessorVariantsEnabled = AddPreprocessorVariants.IsEnabled();
            PreprocessorVariantsDisabled = AddPreprocessorVariants.IsDisabled();
            LabelsCanShareLineEnabled = LabelsCanShareLine.IsEnabled();
            LabelsCanShareLineDisabled = LabelsCanShareLine.IsDisabled();
        }

        private void InitConfig()
        {
            UnityEngine.Debug.Log("Loading Config");
            Logger.LogInfo("Loading Config");

            AddPreprocessorVariants = Config.Bind(
                "Features",
                "AddPreprocessorVariants",
                true,
                new ConfigDescription(
                    "If true, adds variants of existing preprocessors (such as HASH(...) vs RHASH(...)) that replace the original source text with the calculated value. "
                    + "The original text is not preserved on chip reload when using these variants.",
                    tags: WithTags(0, "Add Preprocessor Variants")
                )
            );

            LabelsCanShareLine = Config.Bind(
                "Features",
                "LabelsCanShareLine",
                false,
                new ConfigDescription(
                    "If true, a label can be followed by a statement on the same line. If false, the vanilla behavior of a label having to take up a whole line is be used.",
                    tags: WithTags(1, "LabelsCanShareLine", false)
                )
            );
        }

        private void InitCompatabilityChecks()
        {

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
