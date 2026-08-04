using System.Collections.Generic;
using System.Linq;
using Assets.Scripts.Objects.Electrical;
using Assets.Scripts.Util;
using IC10_Extender.Compat;
using IC10_Extender.Operations;
using IC10_Extender.Preprocessors;

namespace IC10_Extender
{
    public delegate void PreExecute(Operation op);
    public delegate void PostExecute(Operation op, ref int index);
    /** Called when a chip wrapper is deleted, whether due to destruction or recompilation. The ChipWrapper passed in is no longer valid after this call.*/
    public delegate void OnDelete(ChipWrapper chip);
    /** Called when a chip's runtime state is reset, e.g. on putting into or taking out of a circuit housing*/
    public delegate void OnReset(ChipWrapper chip);
    public static class IC10Extender
    {
        public static readonly ILogger Log = new FallbackLogger(() => Plugin.InstanceLogger, LogLevel.Info);

        private static readonly Dictionary<string, ExtendedOpCode> opcodes = new Dictionary<string, ExtendedOpCode>();
        private static readonly List<Preprocessor> preprocessors = new List<Preprocessor>();
        private static readonly Dictionary<string, string> colors = new Dictionary<string, string>();
        public static PreExecute PreExecute = NoOpPreExecute;
        public static PostExecute PostExecute = NoOpPostExecute;
        public static OnDelete OnDelete = NoOpOnDelete;
        public static OnReset OnReset = NoOpOnReset;

        public static Dictionary<string, ExtendedOpCode> OpCodes => new Dictionary<string, ExtendedOpCode>(opcodes);
        public static List<Preprocessor> Preprocessors => preprocessors.OrderBy(p => p.Priority).ThenBy(p => p.SimpleName).ToList();
        public static Dictionary<string, ProgrammableChip.Constant> Constants => ProgrammableChip.AllConstants.ToDictionary(c => c.Literal, c => c);
        public static Dictionary<string, double> ConstantValues => ProgrammableChip.AllConstants.ToDictionary(c => c.Literal, c => c.Value);

        public static void Register(ExtendedOpCode op, params CompatibilityCheck[] requires)
        {
            foreach (var accept in requires)
            {
                if (!accept.Accept())
                {
                    accept.OnFail();
                    return;
                }
            }
            
            Log.Info($"Registering opcode \"{op.OpCode}\"");
            opcodes.Add(op.OpCode, op);
        }

        //an index can be provided in case execution order needs to be modified. Defaults to adding to the end of the list (executes last)
        public static void Register(Preprocessor preprocessor, params CompatibilityCheck[] requires)
        {
            foreach (var accept in requires)
            {
                if (!accept.Accept())
                {
                    accept.OnFail();
                    return;
                }
            }

            Log.Info($"Registering preprocessor \"{preprocessor.SimpleName}\"");
            preprocessors.Add(preprocessor);
        }

        public static void Register(ProgrammableChip.Constant constant, params CompatibilityCheck[] requires)
        {
            foreach (var accept in requires)
            {
                if (!accept.Accept())
                {
                    accept.OnFail();
                    return;
                }
            }

            Log.Info($"Registering constant \"{constant.Literal}\"");
            ProgrammableChip.AllConstants = ProgrammableChip.AllConstants.Append(constant).ToArray();
        }

        public static ExtendedOpCode OpCode(string name)
        {
            return opcodes.GetValueOrDefault(name);
        }

        public static bool HasOpCode(string name) => opcodes.ContainsKey(name);

        public static void NoOpPreExecute(Operation op) { }
        public static void NoOpPostExecute(Operation op, ref int index) { }
        public static void NoOpOnDelete(ChipWrapper chip) { }

        public static void NoOpOnReset(ChipWrapper chip) { }
    }
}
