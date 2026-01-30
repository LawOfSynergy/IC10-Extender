using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IC10_Extender
{
    public static class Colors
    {
        public const string MACRO = "#A0A0A0";
        public const string REGISTER = "#0080FFFF";
        public const string NUMBER = "#20B2AA";
        public const string NETWORK = "#00FFEC";
        public const string COMMENT = "#585858FF";
        public const string HELP = "#808080";
        public const string STRING = "white";
        public const string COMMAND = "yellow";
        public const string LOGICTYPE = "orange";
        public const string DEVICE = "green";
        public const string JUMP = "purple";
        public const string ERROR = "red";
        public const string DESCRIPTION = "grey";
        public const string EXAMPLE = "darkgrey";

        internal static void Register()
        {
            IC10Extender.RegisterColor("colormacro", MACRO);
            IC10Extender.RegisterColor("colorregister", REGISTER);
            IC10Extender.RegisterColor("colornumber", NUMBER);
            IC10Extender.RegisterColor("colornetwork", NETWORK);
            IC10Extender.RegisterColor("colorcomment", COMMENT);
            IC10Extender.RegisterColor("colorhelp", HELP);
        }
    }
}
