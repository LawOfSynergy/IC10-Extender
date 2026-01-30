namespace IC10_Extender.Compat
{
    public class ConstantCompatability : CompatabilityCheck
    {
        public bool Condition { get; private set; }
        public ConstantCompatability(bool condition) {
            Condition = condition;
        }

        public override bool Accept() => Condition;
        public override void OnFail() { }
    }
}
