using Assets.Scripts.UI;
using IC10_Extender.Highlighters;
using System;
using UnityEngine;

namespace IC10_Extender.Preprocessors
{
    public abstract class Preprocessor
    {
        protected HelpReference helpPage;

        public abstract string SimpleName { get; }
        public abstract string HelpEntryName { get; }
        public abstract string HelpEntryDescription { get; }

        public abstract PreprocessorOperation Create(ChipWrapper chip);
        public virtual SyntaxHighlighter Highlighter()
        {
            return new SyntaxHighlighter();
        }

        public virtual void InitHelpPage(ScriptHelpWindow window)
        {
            Debug.Log($"Initializing HelpPage for {SimpleName}");
            Plugin.Logger.LogInfo($"Initializing HelpPage for {SimpleName}");
            try
            {
                helpPage = UnityEngine.Object.Instantiate(window.ReferencePrefab, window.FunctionTransform);
                helpPage.Setup(
                    HelpEntryName,
                    HelpEntryDescription,
                    window.DefaultItemImage,
                    "Macro",
                    Animator.StringToHash(HelpEntryName),
                    HelpReference.InstructionHash
                );
                Debug.Log($"Initialized HelpPage for {SimpleName}");
                Plugin.Logger.LogDebug($"Initialized HelpPage for {SimpleName}");
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
    }
}
