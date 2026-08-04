using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using IC10_Extender.Preprocessors;

namespace IC10_Extender.Preprocess
{
    public class RegexPreprocessor : Preprocessor
    {
        public static readonly Func<Line, string> Remainder = line => line.Raw.Remainder;
        public static readonly Func<Line, string> Full = line => line.Raw.ToString();

        public readonly struct Result
        {
            public bool Continue { get; }
            public int NextIndex { get; }

            public Result(bool proceed, int nextIndex)
            {
                Continue = proceed;
                NextIndex = nextIndex;
            }
        }

        private readonly Func<Line, string> source;
        private readonly Func<Regex> pattern;
        private readonly Func<CompileContext, Line, Match, bool, Result> consumer;


        public RegexPreprocessor(
            string simpleName,
            int priority,
            Func<Regex> pattern,
            Func<Line, string> source,
            Func<CompileContext, Line, Match, bool, Result> consumer
        )
            : base(simpleName, priority)
        {
            this.source = source ?? throw new ArgumentNullException(nameof(source));
            this.pattern = pattern ?? throw new ArgumentNullException(nameof(pattern));
            this.consumer = consumer ?? throw new ArgumentNullException(nameof(consumer));
        }

        public RegexPreprocessor(
            string simpleName,
            int priority,
            Regex pattern,
            Func<Line, string> source,
            Func<CompileContext, Line, Match, bool, Result> consumer
        ) : this(simpleName, priority, () => pattern, source, consumer)
        {
            if (pattern == null) throw new ArgumentNullException(nameof(pattern));
        }

        public RegexPreprocessor(
            string simpleName,
            int priority,
            string helpEntryName,
            string helpEntryDescription,
            Func<Regex> pattern,
            Func<Line, string> source,
            Func<CompileContext, Line, Match, bool, Result> consumer
        )
            : base(simpleName, priority, helpEntryName, helpEntryDescription)
        {
            this.source = source ?? throw new ArgumentNullException(nameof(source));
            this.pattern = pattern ?? throw new ArgumentNullException(nameof(pattern));
            this.consumer = consumer ?? throw new ArgumentNullException(nameof(consumer));
        }

        public RegexPreprocessor(
            string simpleName,
            int priority,
            string helpEntryName,
            string helpEntryDescription,
            Regex pattern,
            Func<Line, string> source,
            Func<CompileContext, Line, Match, bool, Result> consumer
        ) : this(simpleName, priority, helpEntryName, helpEntryDescription, () => pattern, source, consumer)
        {
            if (pattern == null) throw new ArgumentNullException(nameof(pattern));
        }


        public override IEnumerable<Edit> ProcessLine(CompileContext context, List<Line> lines, Line line, bool write = true)
        {
            var pattern = this.pattern() ?? throw new NullReferenceException("supplier returned a null Regex");
            string text = source(line);

            Match match = pattern.Match(text);
            Result result = new Result(true, 0);
            while (match.Success && (result = consumer(context, line, match, write)).Continue)
            {
                match = pattern.Match(source(line), result.NextIndex);
            }
            return NoChange;
        }
    }
}
