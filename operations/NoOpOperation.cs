using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IC10_Extender.Operations
{
    public class NoOpOperation : Operation
    {
        public NoOpOperation(ChipWrapper chip, Line line) : base(chip, line)
        {
        }

        public override int Execute(int index)
        {
            return index + 1;
        }
    }
}
