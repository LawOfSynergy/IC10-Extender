using Assets.Scripts.Objects.Electrical;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using HarmonyLib.Tools;
using System;
using System.Collections.Generic;
using System.Reflection;
using static Assets.Scripts.Objects.Electrical.ProgrammableChipException;

namespace IC10_Extender
{
    [BepInPlugin("net.lawofsynergy.stationeers.ic10e", "IC10 Extender", "0.0.1.0")]
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

                //Transpilers.Execute(harmony);
                
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

    public class IC10Extender
    {
        private static Dictionary<string, Operation> operations = new Dictionary<string, Operation>();

        public static void Register(Operation op)
        {
            operations.Add(op.OpCode, op);
        }

        public static void Deregister(Operation op)
        {
            operations.Remove(op.OpCode);
        }

        public static Operation.Instance LoadExtension(ProgrammableChip chip, string lineOfCode, int lineNumber, string[] source)
        {
            Plugin.Logger.LogInfo($"Loading operation for command `{source[0]}`");
            Operation op = operations.TryGetValue(source[0], out Operation value) ? value : null;

            if (op == null) {
                Plugin.Logger.LogInfo("Operation not found. Reverting to vanilla lookup");
                return null;
            }

            if (source.Length != op.ArgCount)
            {
                Plugin.Logger.LogInfo($"Operation found, but argument count mismatched. Received {source.Length}, expected {op.ArgCount}");
                throw new ProgrammableChipException(ICExceptionType.IncorrectArgumentCount, lineNumber);
            }
            Plugin.Logger.LogInfo($"Operation found.");
            return op.Create(chip, lineNumber, source);
        }
    }

    public class ThrowOperation : Operation
    {
        protected ManualLogSource Logger;
        public ThrowOperation() : base("error", 1) {
            Logger = Plugin.GetLogger();
        }

        public override Instance Create(ProgrammableChip chip, int lineNumber, string[] source)
        {
            return new ThrowInstance(chip, lineNumber, source);
        }

        public class ThrowInstance : Operation.Instance
        {
            public ThrowInstance(ProgrammableChip chip, int lineNumber, string[] source) : base(chip, lineNumber) { }

            public override int Execute(int index)
            {
                throw new ProgrammableChipException(ICExceptionType.Unknown, _LineNumber);
            }
        }
    }
}
