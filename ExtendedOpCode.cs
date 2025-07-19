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
        /// The help string for each parameter of this command in the order they appear (excluding the opcode itself).
        /// currentArgCount can be ignored unless you are dealing with varargs.
        /// </summary>
        /// <returns></returns>
        public abstract HelpString[] Params(int currentArgCount);
        
        /// <summary>
        /// The color to use for syntax highlighting. 
        /// </summary>
        /// <returns>A keyword (default "yellow") or a hex code of the format #RRGGBBAA (TODO fact-check?)</returns>
        public virtual string Color()
        {
            return "yellow";
        }

        public string CommandExample(string color, int spaceCount)
        {
            var args = Params(spaceCount);
            var result = "";
            if (args == null) return result;
            for (int i = spaceCount; i < args.Length; i++)
            {
                result += args[i] + " ";
            }
            return string.Format("<color={1}>{0}</color>", result.TrimEnd(), color);
        }
    }
}
