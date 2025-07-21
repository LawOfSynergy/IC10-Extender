using Assets.Scripts;
using Assets.Scripts.Objects.Electrical;
using Assets.Scripts.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using UnityEngine;

namespace IC10_Extender
{
    public abstract class ExtendedOpCode
    {
        public string OpCode { get; private set; }

        private HelpReference helpPage;
        
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

        public virtual string Description()
        {
            return "";
        }

        public HelpReference InitHelpPage(ScriptHelpWindow window)
        {
            Debug.Log($"Initializing HelpPage for {OpCode}");
            Plugin.Logger.LogInfo($"Initializing HelpPage for {OpCode}");
            try
            {
                helpPage = UnityEngine.Object.Instantiate(window.ReferencePrefab, window.FunctionTransform);
                helpPage.Setup(
                    $"<color={Color()}>{OpCode}</color> {CommandExample("yellow", 0)}",
                    Description(),
                    window.DefaultItemImage,
                    HelpReference.INSTRUCTION_STRING,
                    Animator.StringToHash("opcode"),
                    HelpReference.CommandHash
                );
                Debug.Log($"Initializing HelpPage for {OpCode}");
                Plugin.Logger.LogDebug($"Initialized HelpPage: {helpPage}");
                return helpPage;
            }
            catch (Exception e)
            {
                Plugin.Logger.LogError(e);
                return helpPage;
            }
        }

        public HelpReference HelpPage()
        {
            return helpPage;
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
