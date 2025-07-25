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

        protected HelpReference helpPage;
        public bool Deprecated { get; private set; }
        
        public ExtendedOpCode(string opcode, bool deprecated = false)
        {
            OpCode = opcode;
            Deprecated = deprecated;
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

        public virtual void InitHelpPage(ScriptHelpWindow window)
        {
            Debug.Log($"Initializing HelpPage for {OpCode}");
            Plugin.Logger.LogInfo($"Initializing HelpPage for {OpCode}");
            try
            {
                helpPage = UnityEngine.Object.Instantiate(window.ReferencePrefab, window.FunctionTransform);
                helpPage.Setup(
                    $"<color={Color()}>{OpCode}</color> {CommandExample(0)}",
                    Description(),
                    window.DefaultItemImage,
                    HelpReference.INSTRUCTION_STRING,
                    Animator.StringToHash(OpCode),
                    HelpReference.CommandHash
                );
                Debug.Log($"Initializing HelpPage for {OpCode}");
                Plugin.Logger.LogDebug($"Initialized HelpPage: {helpPage}");
                window._helpReferences.Add(helpPage);
            }
            catch (Exception e)
            {
                Plugin.Logger.LogError(e);
            }
        }

        public HelpReference HelpPage()
        {
            return helpPage;
        }

        public string CommandExample(int spaceCount, string overrideColor = null)
        {
            var args = Params(spaceCount);
            var result = "";
            if (args == null) return result;
            for (int i = spaceCount; i < args.Length; i++)
            {
                result += (overrideColor != null ? args[i].Strip() : args[i]) + " ";
            }
            return overrideColor == null ? result.TrimEnd() : string.Format("<color={1}>{0}</color>", result.TrimEnd(), overrideColor);
        }
    }
}
