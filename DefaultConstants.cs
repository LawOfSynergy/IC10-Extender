using Assets.Scripts.Objects.Electrical;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Assets.Scripts.Objects.Electrical.ProgrammableChip;

namespace IC10_Extender
{
    public static class DefaultConstants
    {
        public static void Register()
        {
            IC10Extender.Register(new Constant("nan", "A constant representing 'not a number'. This constant technically provides a 'quiet' NaN, a signal NaN from some instructions will result in an exception and halt execution", double.NaN, false));
            IC10Extender.Register(new Constant("pinf", "A constant representing a positive infinite value", double.PositiveInfinity, false));
            IC10Extender.Register(new Constant("ninf", "A constant representing a negative infinite value", double.NegativeInfinity, false));
            IC10Extender.Register(new Constant("pi", "A constant representing ratio of the circumference of a circle to its diameter, provided in double precision", Math.PI));
            IC10Extender.Register(new Constant("deg2rad", "Degrees to radians conversion constant", Math.PI / 180.0));
            IC10Extender.Register(new Constant("rad2deg", "Radians to degrees conversion constant", 57.295780181884766));
            IC10Extender.Register(new Constant("epsilon", "A constant representing the smallest value representable in double precision", double.Epsilon, false));
        }
    }
}
