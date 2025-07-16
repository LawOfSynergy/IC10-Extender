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
    public class IC10Extender
    {
        private static Dictionary<string, ExtendedOpCode> opcodes = new Dictionary<string, ExtendedOpCode>();

        public static Dictionary<string, ExtendedOpCode> OpCodes => new Dictionary<string, ExtendedOpCode>(opcodes);

        public static void Register(ExtendedOpCode op)
        {
            opcodes.Add(op.OpCode, op);
        }

        public static void Deregister(ExtendedOpCode op)
        {
            opcodes.Remove(op.OpCode);
        }

        public static ProgrammableChip._Operation LoadOpCode(ProgrammableChip chip, string lineOfCode, int lineNumber, string[] source)
        {
            ExtendedOpCode op = opcodes.TryGetValue(source[0], out ExtendedOpCode value) ? value : null;

            if (op == null) {
                return null;
            }

            op.Accept(lineNumber, source);
            
            return new OperationWrapper(op.Create(new ChipWrapper(chip), lineNumber, source));
        }

        public static bool HasOpCode(string token) { return opcodes.ContainsKey(token); }

        //private wrapper classes to transition between private internals and our public api
        private class OperationWrapper : ProgrammableChip._Operation
        {
            private readonly Operation op;
            public OperationWrapper(Operation op) : base(op.Chip.chip, op.LineNumber)
            {
                this.op = op;
            }

            public override int Execute(int index)
            {
                return op.Execute(index);
            }
        }
    }
}
