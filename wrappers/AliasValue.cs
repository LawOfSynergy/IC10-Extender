using Assets.Scripts.Objects.Electrical;

namespace IC10_Extender.Wrappers
{
    public readonly struct AliasValue
    {
        public readonly AliasTarget Target;
        public readonly int Index;

        public AliasValue(AliasTarget target, int index)
        {
            Target = target;
            Index = index;
        }

        public static implicit operator ProgrammableChip._AliasValue(AliasValue self) => new ProgrammableChip._AliasValue((ProgrammableChip._AliasTarget)self.Target, self.Index);
        public static implicit operator AliasValue(ProgrammableChip._AliasValue self) => new AliasValue((AliasTarget)self.Target, self.Index);
    }
}
