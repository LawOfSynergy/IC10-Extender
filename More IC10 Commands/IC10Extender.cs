using Assets.Scripts.Objects.Electrical;
using Assets.Scripts.Objects.Motherboards;
using Assets.Scripts.Objects.Pipes;
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

                Transpilers.Execute(harmony);

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
        private static Dictionary<string, ExtendedOpCode> operations = new Dictionary<string, ExtendedOpCode>();

        public static void Register(ExtendedOpCode op)
        {
            operations.Add(op.OpCode, op);
        }

        public static void Deregister(ExtendedOpCode op)
        {
            operations.Remove(op.OpCode);
        }

        public static ProgrammableChip._Operation LoadOpCode(ProgrammableChip chip, string lineOfCode, int lineNumber, string[] source)
        {
            Plugin.Logger.LogInfo($"Loading operation for command `{source[0]}`");
            ExtendedOpCode op = operations.TryGetValue(source[0], out ExtendedOpCode value) ? value : null;

            if (op == null) {
                Plugin.Logger.LogInfo("Operation not found. Reverting to vanilla lookup");
                return null;
            }

            op.Accept(lineNumber, source);
            
            Plugin.Logger.LogInfo($"Operation found.");
            return op.Wrapped(chip, lineNumber, source);
        }

        public static bool HasOpCode(string token) { return operations.ContainsKey(token); }
    }

    public class ThrowOperation : ExtendedOpCode
    {
        protected ManualLogSource Logger;
        public ThrowOperation() : base("error") {
            Logger = Plugin.GetLogger();
        }

        public override void Accept(int lineNumber, string[] source)
        {
            if (source.Length > 2) throw new ProgrammableChipException(ICExceptionType.IncorrectArgumentCount, lineNumber);
        }

        public override Operation Create(ChipWrapper chip, int lineNumber, string[] source)
        {
            return new ThrowInstance(chip, lineNumber, source);
        }

        public class ThrowInstance : Operation
        {
            protected readonly DoubleValueVariable ErrorCode;

            public ThrowInstance(ChipWrapper chip, int lineNumber, string[] source) : base(chip, lineNumber)
            {
                if (source.Length == 2)
                {
                    ErrorCode = new DoubleValueVariable(chip.chip, lineNumber, source[1], InstructionInclude.MaskDoubleValue);
                }
            }

            public override int Execute(int index)
            {
                if (ErrorCode != null)
                {
                    DeviceIndexVariable deviceIndex = new DeviceIndexVariable(Chip.chip, LineNumber, "db", InstructionInclude.MaskDeviceIndex, false);
                    int selfIndex = deviceIndex.GetVariableIndex(AliasTarget.Device, true);
                    ILogicable self = Chip.CircuitHousing.GetLogicableFromIndex(selfIndex, deviceIndex.GetNetworkIndex());
                    double errorCode = ErrorCode.GetVariableValue(AliasTarget.Register);
                    if (!self.CanLogicWrite(LogicType.Setting))
                    {
                        throw new ProgrammableChipException(ICExceptionType.IncorrectLogicType, LineNumber);
                    }
                    self.SetLogicValue(LogicType.Setting, errorCode);
                }
                throw new ProgrammableChipException(ICExceptionType.Unknown, LineNumber);
            }
        }
    }
}
