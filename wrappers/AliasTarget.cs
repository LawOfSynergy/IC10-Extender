using System;

namespace IC10_Extender
{
    [Flags]
    public enum AliasTarget
    {
        None = 0,
        Register = 1,
        Device = 2,
        Network = 4,
        All = 268435455, // 0x0FFFFFFF
    }
}
