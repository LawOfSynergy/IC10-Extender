using Assets.Scripts;
using Assets.Scripts.Objects.Electrical;
using System;
using System.Collections.Generic;
using System.Linq;

namespace IC10_Extender
{
    public abstract class ExtendedOpCode
    {
        public string OpCode { get; private set; }
        
        public ExtendedOpCode(string opcode)
        {
            OpCode = opcode;
        }

        //throw ProgrammableChipException if the input is not acceptable, e.g. if the number of args doesn't match
        public abstract void Accept(int lineNumber, string[] source);
        public abstract Operation Create(ChipWrapper chip, int lineNumber, string[] source);

        /// <summary>
        /// The color to use for syntax highlighting. 
        /// </summary>
        /// <returns>A keyword (default "yellow") or a hex code of the format #RRGGBBAA (TODO fact-check?)</returns>
        public virtual string Color()
        {
            return "yellow";
        }
    }
}
