using Assets.Scripts.Objects.Electrical;

namespace IC10_Extender.Wrappers.Variable
{
    public class Variable : ProgrammableChip._Operation.Variable, IVariable
    {
        protected Variable(ProgrammableChip chip, int lineNumber, string code, InstructionInclude propertiesToUse, bool throwException = true)
            : base(chip, lineNumber, code, propertiesToUse, throwException) { }

        protected int GetRegisterIndex(string rCode, out int recurseCount, bool throwException = true)
        {
            return _GetRegisterIndex(rCode, out recurseCount, throwException);
        }

        protected int GetDeviceIndex(string dCode, out int recurseCount, bool throwException = true)
        {
            return _GetDeviceIndex(dCode, out recurseCount, throwException);
        }

        protected int GetNetworkIndex(string dCode, bool throwException = true)
        {
            return _GetNetworkIndex(dCode, throwException);
        }

        public new bool TryParseAliasAsJumpTagValue(out int value, bool throwException = true)
        {
            return base.TryParseAliasAsJumpTagValue(out value, throwException);
        }
    }
}
