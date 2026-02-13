using Assets.Scripts.UI;
using IC10_Extender.Highlighters;
using System;
using System.Linq;
using UnityEngine;

namespace IC10_Extender.Operations
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

        /**
         * throw ProgrammableChipException or ExtendedPCException if the input is not acceptable, e.g. if the number of args doesn't match
         */
        public abstract void Accept(int lineNumber, string[] source);

        /// <summary>
        /// Create an instance of the Operation represented by this ExtendedOpCode for use at runtime.
        /// </summary>
        /// <param name="chip"></param>
        /// <param name="lineNumber"></param>
        /// <param name="source"></param>
        /// <returns></returns>
        public abstract Operation Create(ChipWrapper chip, int lineNumber, string[] source);

        /// <summary>
        /// The help string for each parameter of this command in the order they appear (excluding the opcode itself).
        /// </summary>
        /// <returns></returns>
        public abstract HelpString[] Params();

        [Obsolete("Use VarArgParams() instead")]
        public virtual HelpString? VarArgParam() { return null; }

        public virtual HelpString[] VarArgParams() { 
            #pragma warning disable CS0618 // Wrap the obsolete single nullable vararg param into an array compatible with this new contract
            var vararg = VarArgParam();
            #pragma warning restore CS0618 // Type or member is obsolete
            return vararg.HasValue ? new HelpString[] { vararg.Value } : null;
        }

        /// <summary>
        /// The color to use for syntax highlighting. 
        /// </summary>
        /// <returns>A keyword (default "yellow") or a hex code of the format #RRGGBBAA (TODO fact-check?)</returns>
        public virtual string Color()
        {
            return Theme.Vanilla.OpCode;
        }

        /// <summary>
        /// The description of this command for the help page.
        /// </summary>
        /// <returns></returns>
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
                    $"<color={Color()}>{OpCode}</color> {CommandExample()}",
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

        public string CommandExample(int spaceCount = 0, string overrideColor = null)
        {
            var paramList = Params();
            var varArgs = VarArgParams();
            var result = "";

            if (paramList == null && varArgs == null) return result;

            var args = (paramList ?? new HelpString[0]).Skip(spaceCount);

            if (varArgs != null && varArgs.Length > 0)
            {
                var varargSkip = Math.Max(0, (spaceCount - paramList.Length) % varArgs.Length);
                args = args.Concat(varArgs.Skip(varargSkip));
            }

            foreach (var arg in args)
            {
                result += (overrideColor != null ? arg.Strip() : arg) + " ";
            }
            return overrideColor == null ? result.TrimEnd() : string.Format("<color={1}>{0}</color>", result.TrimEnd(), overrideColor);
        }
    }
}
