using System;
using System.Linq;
using Assets.Scripts.UI;
using IC10_Extender.Highlight;
using IC10_Extender.Variables;
using UnityEngine;

namespace IC10_Extender.Operations
{
    public abstract class ExtendedOpCode
    {
        public static readonly ILogger Log = new FallbackLogger(() => Plugin.InstanceLogger, LogLevel.Info);

        public Signature Signature { get; private set; }
        public bool Deprecated { get; private set; }

        public string OpCode => Signature.Name;
        public Style Style => Signature.Style.Value;

        private readonly bool hasHelp;
        public string Description { get; protected set; }
        public HelpReference HelpPage { get; protected set; }

        public ExtendedOpCode(Signature signature, string description, bool deprecated = false)
        {
            Signature = signature;
            Deprecated = deprecated;
            hasHelp = !deprecated;
            Description = description;

            signature.Style = signature.Style ?? (deprecated ? Theme.Default.DeprecatedOpCode : Theme.Default.OpCode);
        }

        public ExtendedOpCode(Signature signature, bool deprecated = false)
        {
            Signature = signature;
            Deprecated = deprecated;
            hasHelp = false;
        }

        /// <summary>
        /// This method is where validation of the tokens during compilation occurs. By default it automatically
        /// validates that the number of tokens is within the acceptable range based on the signature this opcode was
        /// created with. Implementors may override this method to provide additional validation when necessary prior to binding.
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="line"></param>
        /// <param name="tokens"></param>
        /// <returns></returns>
        public virtual BoundSignature Compile(CompileContext ctx, Line line, IToken[] tokens)
        {
            return Signature.Bind(ctx, line, tokens);
        }

        public abstract Operation Create(CompileContext ctx, Line line, BoundSignature signature, ChipWrapper chip);


        public virtual void InitHelpPage(ScriptHelpWindow window)
        {
            if (!hasHelp) return;

            Log.Info($"Initializing HelpPage for {OpCode}");
            try
            {
                HelpPage = UnityEngine.Object.Instantiate(window.ReferencePrefab, window.FunctionTransform);
                HelpPage.Setup(
                    $"<color={Signature.Style.Value.Foreground}>{OpCode}</color> {Signature.CommandExample(Theme.Default)}",
                    Description,
                    window.DefaultItemImage,
                    HelpReference.INSTRUCTION_STRING,
                    Animator.StringToHash(OpCode),
                    HelpReference.CommandHash
                );
                Log.Debug($"Initialized HelpPage: {HelpPage}");
                window._helpReferences.Add(HelpPage);
            }
            catch (Exception e)
            {
                e.Print(Log);
            }
        }
    }
}
