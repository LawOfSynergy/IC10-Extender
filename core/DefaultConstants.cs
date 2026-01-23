using Assets.Scripts.Objects.Electrical;
using System.Linq;

namespace IC10_Extender
{
    public static class DefaultConstants
    {
        public static void RegisterAll()
        {
            ProgrammableChip.AllConstants.ToList().ForEach(c =>
            {
                IC10Extender.Register(c);
            });
        }
    }
}
