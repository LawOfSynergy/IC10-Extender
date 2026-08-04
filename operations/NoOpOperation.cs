using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IC10_Extender.Variables;

namespace IC10_Extender.Operations
{
    public class NoOpOperation : Operation
    {
        public NoOpOperation(Line line) : base(null, line, null, null) { }

        public override int Execute(int index)
        {
            return index + 1;
        }
    }
}
