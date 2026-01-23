namespace IC10_Extender.Wrappers.Variable
{
    public interface IIndexVariable : IVariable
    {
        int GetVariableIndex(AliasTarget aliasTarget, bool throwError = true);
    }
}
