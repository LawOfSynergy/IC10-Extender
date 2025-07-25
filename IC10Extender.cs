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
        private static readonly List<Preprocessor> preprocessors = new List<Preprocessor>();
        private static readonly Dictionary<string, ProgrammableChip.Constant> constants = new Dictionary<string, ProgrammableChip.Constant>();
        private static readonly Dictionary<string, string> colors  = new Dictionary<string, string>();


        public static Dictionary<string, ExtendedOpCode> OpCodes => new Dictionary<string, ExtendedOpCode>(opcodes);
        public static List<Preprocessor> Preprocessors => new List<Preprocessor>(preprocessors);
        public static Dictionary<string, ProgrammableChip.Constant> Constants => new Dictionary<string, ProgrammableChip.Constant>(constants);
        public static Dictionary<string, string> Colors => new Dictionary<string, string>(colors);

        public static void Register(ExtendedOpCode op)
        {
            UnityEngine.Debug.Log($"Registering opcode \"{op.OpCode}\"");
            Plugin.Logger?.LogInfo($"Registering opcode \"{op.OpCode}\"");
            opcodes.Add(op.OpCode, op);
        }

        //an index can be provided in case execution order needs to be modified. Defaults to adding to the end of the list (executes last)
        public static void Register(Preprocessor preprocessor, int index = -1)
        {
            UnityEngine.Debug.Log($"Registering preprocessor \"{preprocessor.SimpleName}\"");
            Plugin.Logger?.LogInfo($"Registering preprocessor \"{preprocessor.SimpleName}\"");
            if (index == -1)
            {
                preprocessors.Add(preprocessor);
            } else {
                preprocessors.Insert(index, preprocessor);
            }
        }

        public static void Register(ProgrammableChip.Constant constant)
        {
            UnityEngine.Debug.Log($"Registering constant \"{constant.Literal}\"");
            Plugin.Logger?.LogInfo($"Registering constant \"{constant.Literal}\"");
            constants.Add(constant.Literal, constant);
        }

        public static void RegisterColor(string name, string value)
        {
            colors.Add(name, value);
        }

        public static ExtendedOpCode OpCode(string name)
        {
            return opcodes.GetValueOrDefault(name);
        }

        public static ProgrammableChip.Constant? Constant(string name)
        {
            if (constants.ContainsKey(name))
            {
                return constants[name];
            }
            else
            {
                return null;
            }
        }

        public static string Color(string name)
        {
            return colors.ContainsKey(name) ? colors[name] : null;
        }

        public static bool HasOpCode(string name) => opcodes.ContainsKey(name);
        public static bool HasConstant(string name) => constants.ContainsKey(name);
        public static bool HasColor(string name) => colors.ContainsKey(name);
    }
}
