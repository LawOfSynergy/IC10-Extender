using Assets.Scripts.Objects.Electrical;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IC10_Extender
{
    public abstract class Operation
    {
        public string OpCode { get; private set; }
        /// <summary>
        /// must inlcude the opcode itself. e.g. `move r0 1` has an ArgCount of 3
        /// </summary>
        public int ArgCount {  get; private set; }

        public Operation(string opcode, int argCount)
        {
            OpCode = opcode;
            ArgCount = argCount;
        }

        //create a publicly accessable class for this, so that add-ons don't have to publicize the core dll
        public abstract class Instance : ProgrammableChip._Operation
        {
            protected Instance(ProgrammableChip chip, int lineNumber) : base(chip, lineNumber) { }
        }

        public abstract Instance Create(ProgrammableChip chip, int lineNumber, string[] source);
    }
}
