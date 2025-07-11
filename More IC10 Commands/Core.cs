using Assets.Scripts.Objects.Electrical;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Assets.Scripts.Objects.Electrical.ProgrammableChipException;

namespace More_IC10_Commands
{
    public class Core
    {
        private Core() { } //static class, no public constructor

        public static void init()
        {
            var harmony = new Harmony("com.lawofsynergy.stationeers.ic10ext");
            harmony.PatchAll();
        }

        private static Dictionary<string, Operation> operations = new Dictionary<string, Operation>();

        public static void Register(Operation op)
        {
            operations.Add(op.OpCode, op);
        }

        public static void Deregister(Operation op)
        {
            operations.Remove(op.OpCode);
        }

        public static Operation.Instance? LoadExtension(ProgrammableChip chip, string lineOfCode, int lineNumber, string[] source)
        {
            Operation? op =  operations.TryGetValue(source[0], out Operation? value) ? value : null;
            
            if (op == null) 
                return null;
            
            if (source.Length != op.ArgCount)
            {
                throw new ProgrammableChipException(ICExceptionType.IncorrectArgumentCount, lineNumber);
            }
            return op.Create(chip, lineNumber, source);
        }
    }
}
