using Assets.Scripts.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IC10_Extender.Core
{
    internal class HelpWindow
    {
        private readonly Dictionary<string, HelpReference> preprocessorPages = new Dictionary<string, HelpReference>();
        private readonly Dictionary<string, HelpReference> opcodePages = new Dictionary<string, HelpReference>();
        private readonly Dictionary<string, HelpReference> constantPages = new Dictionary<string, HelpReference>();
        private readonly Dictionary<string, HelpReference> untrackedPages = new Dictionary<string, HelpReference>();
    }
}
