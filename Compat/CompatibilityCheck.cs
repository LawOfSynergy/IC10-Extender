using Assets.Scripts.Objects.Electrical;
using System;

namespace IC10_Extender.Compat
{
    public abstract class CompatibilityCheck
    {
        public abstract bool Accept();
        public abstract void OnFail();

        public static implicit operator Func<bool>(CompatibilityCheck compat)
        {
            return compat.Accept;
        }
    }
}
