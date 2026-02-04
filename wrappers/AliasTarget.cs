using System;

namespace IC10_Extender.Wrappers
{
    [Flags]
    public enum AliasTarget
    {
        None = 0,
        Register = 1,
        Device = 2,
        Network = 4,
        All = 0xFFFFFFF, 
    }
}
