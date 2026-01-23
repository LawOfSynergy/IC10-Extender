namespace IC10_Extender.Wrappers.Variable
{
    public interface IVariable
    {
        bool TryParseAliasAsJumpTagValue(out int value, bool throwException = true);
    }
}
