using Assets.Scripts.Objects.Electrical;
using System;

namespace IC10_Extender.Compat
{
    public abstract class CompatabilityCheck
    {
        public abstract bool Accept();
        public abstract void OnFail();

        public static implicit operator Func<bool>(CompatabilityCheck compat)
        {
            return compat.Accept;
        }

        // Instances
        public static readonly ConstantCompatability Common = new ConstantCompatability(true);
        public static readonly ConstantCompatability AfterStrings = new ConstantCompatability(typeof(ProgrammableChip).GetMethod("PackAscii6", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static) != null);
        public static readonly ConstantCompatability BeforeStrings = new ConstantCompatability(!AfterStrings.Condition);
    }
}
