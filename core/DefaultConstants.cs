using Assets.Scripts.Objects.Electrical;
using System.Linq;

namespace IC10_Extender
{
    internal static class DefaultConstants
    {
        internal static void RegisterAll()
        {
            ProgrammableChip.AllConstants.ToList().ForEach(c =>
            {
                IC10Extender.Register(c);
            });
        }
    }
}
