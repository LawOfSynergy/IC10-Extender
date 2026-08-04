using System;
using System.Collections.Generic;
using System.Linq;
using Assets.Scripts.Objects.Electrical;
using Assets.Scripts.UI;
using IC10_Extender.Highlight;
using IC10_Extender.operations;
using IC10_Extender.Preprocessors;
using IC10_Extender.Variables;
using IC10_Extender.Wrappers;
using MonoMod.Utils;
using static IC10_Extender.Preprocessors.Preprocessor;

namespace IC10_Extender
{
    public class CompileContext
    {
        public static readonly ILogger Log = new FallbackLogger(() => Plugin.InstanceLogger, LogLevel.Info);

        public const int DefaultStackPointer = 16;
        public const int DefaultReturnAddress = 17;

        private static readonly Dictionary<object, CompileContext> contexts = new Dictionary<object, CompileContext>();

        private readonly Dictionary<string, object> data = new Dictionary<string, object>();

        public Dictionary<string, int> JumpTags => (Dictionary<string, int>)data[nameof(JumpTags)];
        public Dictionary<string, double> Defines => (Dictionary<string, double>)data[nameof(Defines)];
        public Dictionary<string, AliasValue> Aliases => (Dictionary<string, AliasValue>)data[nameof(Aliases)];
        public List<Line> OriginalLines => (List<Line>)data[nameof(OriginalLines)];
        public List<Line> ProcessedLines => (List<Line>)data[nameof(ProcessedLines)];
        internal Dictionary<int, Line> Construction {
            get
            {
                if (!data.ContainsKey(nameof(Construction))) data[nameof(Construction)] = ProcessedLines.ToDictionary(l => (int)l.LineNumber, l => l);
                return (Dictionary<int, Line>)data[nameof(Construction)];
            }
        }
        public Dictionary<Line, BoundSignature> Operations => (Dictionary<Line, BoundSignature>)data[nameof(Operations)];
        public Dictionary<Line, List<Tuple<int, ProgrammableChipException>>> Errors => (Dictionary<Line, List<Tuple<int, ProgrammableChipException>>>)data[nameof(Errors)];

        public CompileContext()
        {
            data.Add(nameof(JumpTags), new Dictionary<string, int>());
            data.Add(nameof(Defines), new Dictionary<string, double>());
            data.Add(nameof(Aliases), new Dictionary<string, AliasValue>());
            Aliases.Add("db", new AliasValue(AliasTarget.Device, int.MaxValue));
            Aliases.Add("sp", new AliasValue(AliasTarget.Register, DefaultStackPointer));
            Aliases.Add("ra", new AliasValue(AliasTarget.Register, DefaultReturnAddress));
            data.Add(nameof(Operations), new Dictionary<Line, BoundSignature>());
            data.Add(nameof(Errors), new Dictionary<Line, List<Tuple<int, ProgrammableChipException>>>());
        }

        public void Add(string key, object value) { data[key] = value; }

        public T Get<T>(string key) { return (T)data[key]; }

        /**
         * column is the start index within line.Display of the exception
         */
        public void AddError(Line line, int column, ProgrammableChipException ex)
        {
            if (line == null) throw new ArgumentNullException(nameof(line));
            if (ex == null) throw new ArgumentNullException(nameof(ex));

            if (!Errors.ContainsKey(line)) Errors.Add(line, new List<Tuple<int, ProgrammableChipException>>());
            Errors[line].Add(new Tuple<int, ProgrammableChipException>(column, ex));
        }

        public static void Store(object key, CompileContext context)
        {
            contexts.Add(key, context);
        }

        public static CompileContext Get(object key)
        {
            return contexts[key];
        }

        public static void Remove(object key)
        {
            contexts.Remove(key);
        }

        public static CompileContext Compile(string script)
        {
            return Compile(
                script.Split('\n').Select((line, index) => new Line(line, index)).ToList(),
                script.Split('\n').Select((line, index) => new Line(line, index)).ToList()
            );
        }

        private static CompileContext Compile(List<Line> originalLines, List<Line> currentLines)
        {
            var context = new CompileContext();
            context.data[nameof(OriginalLines)] = originalLines.ToList();

            var script = currentLines.ToList();

            //preprocessing
            var preprocessors = IC10Extender.Preprocessors;
            foreach (var processor in preprocessors) {
                List<Line> visited = new List<Line>();
                bool recurse = false;

                for (int i = 0; i < script.Count;)
                {
                    var currentLine = script[i];
                    visited.Add(currentLine);
                    var edits = processor.ProcessLine(context, script, currentLine, true) ?? NoChange;

                    if (edits.Count() > 0)
                    {
                        var renumber = false;
                        foreach (var edit in edits)
                        {
                            if (edit.Insertions != null)
                            {
                                (Line index, List<Line> inserts) = edit.Insertions;

                                if (index != null && !script.Contains(index)) throw new KeyNotFoundException("index must exist in the list");

                                if (inserts.Count() > 0) {
                                    recurse = true;
                                    renumber = true;
                                    script.InsertRange(index == null ? 0 : script.IndexOf(index)+1, inserts);
                                }
                            }

                            if (edit.Deletions != null && edit.Deletions.Count() > 0)
                            {
                                recurse = true;
                                renumber = true;
                                foreach (var delete in edit.Deletions)
                                {
                                    script.Remove(delete);
                                }
                            }
                        }

                        if (renumber)
                        {
                            int newIndex = 0;
                            foreach (Line l in script)
                            {
                                l.LineNumber = newIndex++;
                            }
                        }

                        // find most recent index that is still in the list
                        i = visited.Max(line => script.IndexOf(line));
                    }
                    // go to next index. if edited and no previously visited lines are in the list (-1) will start from beginning (0)
                    i++;
                }

                if (recurse)
                {
                    script.ForEach(l => l.Reset());
                    return Compile(originalLines, script);
                }
            }
            context.data[nameof(ProcessedLines)] = script.ToList();

            //highlight original lines
            foreach (Preprocessor processor in preprocessors)
            {
                foreach (var line in originalLines)
                {
                    processor.ProcessLine(context, originalLines, line, false);
                }
            }

            //opcode resolution
            foreach (var line in script)
            {
                // Tokenize() finalizes preprocessing on Raw, splitting remaining unconsumed text by
                // whitespace. Whitespace-only tokens are filtered out before opcode resolution.
                var tokens = line.Raw.Tokenize()
                    .Where(t => !string.IsNullOrWhiteSpace(t.Text))
                    .ToArray();

                if (tokens.Length == 0) continue;

                Func<CompileContext, Line, IToken[], BoundSignature> compiler;

                var opcode = IC10Extender.OpCode(tokens[0].Text);
                Style? style;
                if (opcode != null)
                {
                    compiler = opcode.Compile;
                    style = opcode.Style;
                }
                else if (VanillaOpCode.Signatures.ContainsKey(tokens[0].Text))
                {
                    // capture tokens[0].Text for the lambda — tokens[0] itself is a struct-backed
                    // reference that remains stable, but capturing the string is clearer.
                    var opcodeText = tokens[0].Text;
                    compiler = (ctx, l, t) => VanillaOpCode.Signatures[opcodeText].Bind(ctx, l, t);
                    style = VanillaOpCode.Signatures[tokens[0].Text].Style;
                }
                else
                {
                    //unknown opcode — style the remainder as an error and continue.
                    //LoC creation falls back to vanilla on absence of an entry in context.Operations.
                    line.ConsumeRemainder(Theme.Default.Error.WithTooltip(new HelpString(((StyledString)line.Display).Theme, "Unrecognized instruction")));
                    continue;
                }

                try
                {
                    context.Operations.Add(line, compiler(context, line, tokens.Skip(1).ToArray()));
                }
                catch (Exception ex)
                {
                    context.AddError(line, 0, ex.Wrap(line.LineNumber));
                    line.ConsumeRemainder(((StyledString)line.Display).Theme.Error.WithError(ex));
                }
            }

            return context; //end of compilation. Next step is Write()
        }

        public static void Render(CompileContext context, InputSourceCode window)
        {
            for (int i = 0; i < window.LinesOfCode.Count; i++)
            {
                window.LinesOfCode[i].FormattedText.text = ((StyledString)context.ProcessedLines[i].Original).ToString(StyledString.VanillaFormat);
                window.LinesOfCode[i].IsVoidLine = string.IsNullOrWhiteSpace(window.LinesOfCode[i].FormattedText.text);
            }
        }

        public static void Write(CompileContext context, ProgrammableChip chip)
        {
            if (chip == null) throw new ArgumentNullException(nameof(chip));
            if (context == null) throw new ArgumentNullException(nameof(context));

            //force construction of new wrapper, invalidate old wrappers
            var wrapper = chip.Wrap(true);

            //update aliases to what is actually on the chip, in-case some other mod changes these
            context.Aliases["sp"] = new AliasValue(AliasTarget.Register, wrapper.StackPointerIndex);
            context.Aliases["ra"] = new AliasValue(AliasTarget.Register, wrapper.ReturnAddressIndex);

            //set defines, aliases, and jump tags
            wrapper.Defines = context.Defines;
            wrapper.Aliases = context.Aliases;
            wrapper.JumpTags = context.JumpTags;

            //clear errors, stack pointer, and NextAddress
            wrapper.ClearException();
            wrapper.Registers[wrapper.StackPointerIndex] = 0.0;
            wrapper.NextAddr = 0;

            //set source code
            chip.SourceCode = new AsciiString(string.Join("\n", context.ProcessedLines.Select(line => line.Display.ToString(TokenString.RawFormat)).ToArray()));

            //prepare for LineOfCode construction
            contexts.Add(chip, context);

            //construct LineOfCode instances
            chip._LinesOfCode.Clear();
            context.ProcessedLines.ForEach(line =>
            {
                try
                {
                    chip._LinesOfCode.Add(new ProgrammableChip._LineOfCode(chip, line.Raw.ToString(TokenString.RawFormat), line.LineNumber));
                }
                catch (Exception ex)
                {
                    Log.Error(ex.ToString());
                    context.AddError(line, 0, ex.Wrap(line.LineNumber));
                }
            });
            context.data.Remove(nameof(context.Construction));

            //clean up LineOfCode construction
            contexts.Remove(chip);

            //store the first compile exception if any exist
            var exception = context.Errors.SelectMany(kvp => kvp.Value.Select(tuple => tuple.Item2)).FirstOrDefault();
            wrapper.CompileException = exception;
        }
    }
}
