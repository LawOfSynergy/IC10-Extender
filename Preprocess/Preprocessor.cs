using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Assets.Scripts.Objects;
using Assets.Scripts.Objects.Electrical;
using Assets.Scripts.UI;
using Assets.Scripts.Vehicles;
using Mono.Cecil.Cil;
using UnityEngine;
using UnityEngine.TextCore.Text;

namespace IC10_Extender.Preprocessors
{
    public abstract class Preprocessor
    {
        public const int PriorityLineCountEdits = 10; //e.g. includes
        public const int PriorityComment = 20;
        public const int PriorityRawLiteral = 29; //e.g. $$, %%, rhash(), rstring()
        public const int PriorityLiteral = 30; //e.g. $, %, hash(), string()
        public const int PriorityKeywordFinder = 40; //e.g. jump, alias, define

        public static readonly Edit[] NoChange = new Edit[0];
        
        /**
         * A line insertion or deletion requested by a preprocessor.
         *
         * Edits are collected across the preprocessor's entire pass and applied only after it
         * completes, so emitting one does not disturb the pass in progress. Once a pass yields any
         * insertion or deletion, every line is Reset() and the compile recurses, which re-runs every
         * preprocessor from the start against the modified line list.
         */
        public readonly struct Edit
        {
            public readonly List<Line> Deletions;
            public readonly Tuple<Line, List<Line>> Insertions; //<insertAfter, <Insertions>> if insertAfter is null, will insert at the beginning of the script

            /**
             * Full constructor, covers deletions and insertions
             */
            public Edit(List<Line> deletions = null, Tuple<Line, List<Line>> insertions = null)
            {
                Deletions = deletions;
                Insertions = insertions;
            }

            /**
             * Delete current line
             */
            public Edit(Line currentLine) : this(new List<Line> { currentLine }) {}
        }

        public static ILogger Log = new FallbackLogger(() => Plugin.InstanceLogger, LogLevel.Info);

        private readonly bool hasHelp;
        protected HelpReference helpPage;

        public string SimpleName { get; private set; }
        public int Priority { get; private set; }
        private readonly string helpEntryName;
        private readonly string helpEntryDescription;

        public Preprocessor(string simpleName, int priority)
        {
            SimpleName = simpleName;
            Priority = priority;
            hasHelp = false;
        }

        public Preprocessor(string simpleName, int priority, string helpEntryName, string helpEntryDescription)
        {
            SimpleName = simpleName;
            Priority = priority;
            hasHelp = true;
            this.helpEntryName = helpEntryName;
            this.helpEntryDescription = helpEntryDescription;
        }

        /// <summary>
        /// Preprocess the current line, with a copy of the list of lines to allow look-ahead or look-behind.
        /// 
        /// Subclasses should apply styling to Original regardless of whether there is anything to process in the
        /// actual Raw text.
        /// 
        /// Line.Raw is the canonical representation of the line during compilation and is what should be checked when determining what actions
        /// to take (if any)
        /// 
        /// </summary>
        /// <param name="context"></param>
        /// <param name="lines"></param>
        /// <param name="currentLine"></param>
        /// <returns>
        ///     An enumerable of deletions and insertions to be made to the current script.
        ///     If null, will be defaulted to Preprocessor.NoChange
        ///     If non-empty, changes will be applied immediately to the list for the current processing run.
        ///     Then, when this preprocessor has finished all lines of the current run, a new compilation will be kicked
        ///     off from the beginning, with all of the line insertion and removals kept, but all non-permanant line changes reset
        ///     to their original state for re-processing.
        /// </returns>
        public abstract IEnumerable<Edit> ProcessLine(CompileContext context, List<Line> lines, Line currentLine, bool write = true);

        #region help page
        public bool HasHelpPage => hasHelp;

        public HelpReference HelpPage()
        {
            return helpPage;
        }

        public virtual void InitHelpPage(ScriptHelpWindow window)
        {
            if (!hasHelp) return;

            Log.Info($"Initializing HelpPage for {SimpleName}");
            try
            {
                helpPage = UnityEngine.Object.Instantiate(window.ReferencePrefab, window.FunctionTransform);
                helpPage.Setup(
                    helpEntryName,
                    helpEntryDescription,
                    window.DefaultItemImage,
                    "Macro",
                    Animator.StringToHash(helpEntryName),
                    HelpReference.InstructionHash
                );
                Log.Debug($"Initialized HelpPage for {SimpleName}");
                window._helpReferences.Add(helpPage);
            }
            catch (Exception e)
            {
                e.Print(Log);
            }
        }
        #endregion help page
    }
}
