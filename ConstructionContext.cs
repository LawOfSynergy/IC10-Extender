using Assets.Scripts.Objects.Electrical;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static IC10_Extender.PreprocessorOperation;

namespace IC10_Extender
{
    public static class ConstructionContext
    {
        private static readonly Dictionary<ProgrammableChip, Dictionary<int, Line>> context = new Dictionary<ProgrammableChip, Dictionary<int, Line>>();

        public static void Store(ProgrammableChip chip, IEnumerable<Line> lines)
        {
            context.Add(chip, lines.ToDictionary(line => (int)line.OriginatingLineNumber));
        }

        public static Line Get(ProgrammableChip chip, int lineNumber) {
            Line result = context[chip][lineNumber];
            context[chip].Remove(lineNumber);
            if (context[chip].Count == 0) context.Remove(chip);
            return result;
        }

        public static void Clear(ProgrammableChip chip)
        {
            if(context.ContainsKey(chip)) context.Remove(chip);
        }
    }
}
