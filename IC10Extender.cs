using Assets.Scripts;
using Assets.Scripts.Objects.Electrical;
using Assets.Scripts.Objects.Motherboards;
using Assets.Scripts.Objects.Pipes;
using Assets.Scripts.UI;
using Assets.Scripts.Util;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using HarmonyLib.Tools;
using Reagents;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;
using Util.Commands;
using static Assets.Scripts.Objects.Electrical.ProgrammableChipException;

namespace IC10_Extender
{
    public class IC10Extender
    {
        private static readonly Dictionary<string, ExtendedOpCode> opcodes = new Dictionary<string, ExtendedOpCode>();

        public static Dictionary<string, ExtendedOpCode> OpCodes => new Dictionary<string, ExtendedOpCode>(opcodes);

        public static void Register(ExtendedOpCode op)
        {
            try { 
                Plugin.Logger.LogInfo($"Registering opcode \"{op.OpCode}\"");
                opcodes.Add(op.OpCode, op);
            } catch (Exception ex){
                Plugin.Logger.LogError(ex);
            }
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
            try
            {
                return new OperationWrapper(op.Create(new ChipWrapper(chip), lineNumber, source));
            } 
            catch (Exception ex)
            {
                switch (ex)
                {
                    case ProgrammableChipException _: throw ex;
                    default:
                        Plugin.Logger.LogError(ex);
                        throw new ProgrammableChipException(ICExceptionType.Unknown, lineNumber);
                }
            }
        }

        public static void ShowHelpPage(string opcode)
        {
            Plugin.Logger.LogInfo($"Searching for help page for {opcode}");
            if(opcodes.TryGetValue(opcode, out ExtendedOpCode value))
            {
                Plugin.Logger.LogInfo($"Showing help page for {opcode}");
                value.HelpPage().SetVisible(true);
            }
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

    public class HelpPageRegister
    {
        private static List<HelpReference> pages = new List<HelpReference>();

        public static void Init(List<HelpReference> init)
        {
            try
            {
                init.AddRange(pages);
            }
            catch (Exception ex)
            {
                Plugin.Logger.LogError(ex);
            }
        }

        public static void Add(HelpReference helpPage)
        {
            pages.Add(helpPage);
        }
    }
}
