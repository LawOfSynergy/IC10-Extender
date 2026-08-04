using System;
using System.Runtime.CompilerServices;
using Assets.Scripts.Objects.Electrical;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using HarmonyLib.Tools;
using IC10_Extender.Compat;
using IC10_Extender.Operations;
using IC10_Extender.Preprocess;
using static IC10_Extender.Compat.ConfigExtensions;

[assembly: InternalsVisibleTo("IC10Extender.Tests")]

namespace IC10_Extender
{

    [BepInPlugin("net.lawofsynergy.stationeers.ic10e", "IC10 Extender", "0.2.1.0")]
    public class Plugin : BaseUnityPlugin
    {
        
        public static ManualLogSource InstanceLogger { get; private set; }
        public static Plugin Instance { get; private set; }

        public static ILogger Log = new FallbackLogger(() => InstanceLogger, LogLevel.Info);

        public ConfigEntry<bool> AddPreprocessorVariants { get; private set; }
        public ConfigEntry<bool> LabelsCanShareLine { get; private set; }

        public static ConstantCompatibility Common { get; } = new ConstantCompatibility(true);
        public static ConstantCompatibility AfterStrings { get; } = new ConstantCompatibility(typeof(ProgrammableChip).GetMethod("PackAscii6", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static) != null);
        public static ConstantCompatibility BeforeStrings { get; } = new ConstantCompatibility(!AfterStrings.Condition);
        public static CompatibilityCheck PreprocessorVariantsEnabled { get; internal set; }
        public static CompatibilityCheck PreprocessorVariantsDisabled { get; internal set; }
        public static CompatibilityCheck LabelsCanShareLineEnabled { get; internal set; }
        public static CompatibilityCheck LabelsCanShareLineDisabled { get; internal set; }

        public Plugin()
        {
            if (Instance != null)
            {
                throw new InvalidOperationException("Duplicate instance of IC10 Extender detected!");
            }

            InstanceLogger = Logger;
            Instance = this;

            InitConfig();

            //initialize compatability checks
            Log.Info("Initializing Compatability Flags");

            PreprocessorVariantsEnabled = AddPreprocessorVariants.IsEnabled();
            PreprocessorVariantsDisabled = AddPreprocessorVariants.IsDisabled();
            LabelsCanShareLineEnabled = LabelsCanShareLine.IsEnabled();
            LabelsCanShareLineDisabled = LabelsCanShareLine.IsDisabled();
        }
        private void InitConfig()
        {
            Log.Info("Loading Config");

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
                    "If true, a label can be followed by a statement on the same line. If false, the vanilla behavior of a label having to take up a whole line is used.",
                    tags: WithTags(1, "Labels Can Share Line", false)
                )
            );
        }

        void Awake()
        {
            Log.Info("Loading Mod");

            try
            {
                Log.Info("Loading Harmony Patches");
                var harmony = new Harmony("com.lawofsynergy.stationeers.ic10e");
                HarmonyFileLog.Enabled = true;
                harmony.PatchAll(typeof(CommonPatches));
                Log.Info("Patch succeeded");
            }
            catch (Exception e)
            {
                Log.Info("Patch Failed");
                Log.Info(e.ToString());
            }

            DefaultPreprocessors.Register();
            IC10Extender.Register(new ThrowOperation());
        }
    }
}
