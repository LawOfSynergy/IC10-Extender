using Assets.Scripts.Objects.Electrical;
using BepInEx;

using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;
using static Assets.Scripts.Objects.Electrical.ProgrammableChipException;

namespace IC10_Extender
{
    [BepInPlugin("net.lawofsynergy.stationeers.ic10e", "IC10 Extender", "0.0.1.0")]
    public class Plugin : BaseUnityPlugin
    {
        public void Log(string line)
        {
            Logger.LogInfo("[IC10Extender]: " + line);
        }

        void Awake()
        {
            Log("Loading Mod");
            try
            {
                Log("Loading Harmony Patches");
                //    Harmony.DEBUG = true;
                var harmony = new Harmony("com.lawofsynergy.stationeers.ic10e");
                //var tmp = typeof(ProgrammableChip).GetNestedTypes(BindingFlags.NonPublic)
                var targetClass = typeof(ProgrammableChip).GetNestedType("_LineOfCode", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
                var target = targetClass.GetConstructor(BindingFlags.Public | BindingFlags.Instance, null, new Type[] { typeof(ProgrammableChip), typeof(string), typeof(int) }, null);
                var transpiler = typeof(PCTranspiler).GetMethod("Transpile");
                Log($"Target Class: {targetClass?.Name ?? "null"}\nTarget: {target?.ToString() ?? "null"}\nTranspiler: {transpiler?.ToString() ?? "null"}");

                harmony.Patch(
                    target,
                    null,
                    null,
                    new HarmonyMethod(transpiler)
                );
                harmony.PatchAll();
                Log("Patch succeeded");
            }
            catch (Exception e)
            {
                Log("Patch Failed");
                Log(e.ToString());
            }

            //IC10Extender.Register(new ThrowOperation());
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
            Operation op = operations.TryGetValue(source[0], out Operation value) ? value : null;

            if (op == null)
                return null;

            if (source.Length != op.ArgCount)
            {
                throw new ProgrammableChipException(ICExceptionType.IncorrectArgumentCount, lineNumber);
            }
            return op.Create(chip, lineNumber, source);
        }
    }

    public class ThrowOperation : Operation
    {
        public ThrowOperation() : base("t", 1) { }

        public override Instance Create(ProgrammableChip chip, int lineNumber, string[] source)
        {
            return new ThrowInstance(chip, lineNumber, source);
        }

        public class ThrowInstance : Operation.Instance
        {
            public ThrowInstance(ProgrammableChip chip, int lineNumber, string[] source) : base(chip, lineNumber) { }

            public override int Execute(int index)
            {
                throw new ProgrammableChipException(ICExceptionType.None, _LineNumber);
            }
        }
    }
}
