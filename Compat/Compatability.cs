using Assets.Scripts.Objects.Electrical;
using System;

namespace IC10_Extender.Compat
{
    public class Compatability
    {
        private bool condition;

        private Compatability(bool condition)
        {
            this.condition = condition;
        }

        public bool Accept() { return condition; }

        public static implicit operator Func<bool>(Compatability compat)
        {
            return compat.Accept;
        }

        public static implicit operator Compatability(Func<bool> condition)
        {
            return new Compatability(condition == null ? true : condition());
        }

        // Instances
        public static readonly Compatability Common = new Compatability(true);
        public static readonly Compatability AfterStrings = new Compatability(typeof(ProgrammableChip).GetMethod("PackAscii6", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static) != null);
        public static readonly Compatability BeforeStrings = new Compatability(!AfterStrings.condition);
    }
}
