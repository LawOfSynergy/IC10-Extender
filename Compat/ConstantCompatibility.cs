namespace IC10_Extender.Compat
{
    public class ConstantCompatibility : CompatibilityCheck
    {
        public bool Condition { get; private set; }
        public ConstantCompatibility(bool condition) {
            Condition = condition;
        }

        public override bool Accept() => Condition;
        public override void OnFail() { }
    }
}
