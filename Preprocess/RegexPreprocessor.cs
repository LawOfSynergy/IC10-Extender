using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using IC10_Extender.Highlight;
using IC10_Extender.Preprocessors;

namespace IC10_Extender.Preprocess
{
    /// <summary>
    /// Scans a line with a regex and hands each match to a consumer.
    /// <para><typeparamref name="TSpace"/> is the space of the string being scanned, and it fixes
    /// which anchoring method is valid. <see cref="RemainderSpace"/> scans
    /// <see cref="ITokenizedString{T,TVirtual}.Remainder"/>, which contains only unclaimed text, and
    /// pairs with <see cref="IPreprocessableString{T,TVirtual}.First"/>. <see cref="AdjustedSpace"/>
    /// scans <see cref="ITokenizedString{T,TVirtual}.Render"/>, which includes text other
    /// preprocessors have already claimed, and pairs with
    /// <see cref="IPreprocessableString{T,TVirtual}.Locate"/>. Pairing them the wrong way round does
    /// not compile, because the two methods accept indices in different spaces.</para>
    /// </summary>
    public class RegexPreprocessor<TSpace> : Preprocessor where TSpace : ICoordinateSpace
    {
        /// <summary>
        /// Everything one match hands to the consumer. Bundled so that adding to it later does not
        /// break every consumer's signature.
        /// </summary>
        public readonly struct MatchContext
        {
            public CompileContext Context { get; }
            public Line Line { get; }

            /// <summary>The string the pattern was run against, whose space is <typeparamref name="TSpace"/>.</summary>
            public CoordString<TSpace> Source { get; }
            public Match Match { get; }

            /// <summary>False during the second pass over the unmodified lines, which styles without mutating.</summary>
            public bool Write { get; }

            /// <summary>
            /// Sink for line insertions and deletions. Edits are collected across the whole pass and
            /// applied afterwards, so adding one does not disturb the scan in progress; use
            /// <see cref="Result.Continue"/> to decide whether scanning proceeds.
            /// </summary>
            public ICollection<Edit> Edits { get; }

            /// <summary>Where this match starts, in the source's space.</summary>
            public Index<TSpace> At => Source.At(Match.Index);

            public MatchContext(CompileContext context, Line line, CoordString<TSpace> source, Match match, bool write, ICollection<Edit> edits)
            {
                Context = context;
                Line = line;
                Source = source;
                Match = match;
                Write = write;
                Edits = edits;
            }
        }

        public readonly struct Result
        {
            public bool Continue { get; }
            public Index<TSpace> Next { get; }

            public Result(bool proceed, Index<TSpace> nextIndex)
            {
                Continue = proceed;
                Next = nextIndex;
            }
        }

        private readonly Func<Line, CoordString<TSpace>> source;
        private readonly Func<Regex> pattern;
        private readonly Func<MatchContext, Result> consumer;

        public RegexPreprocessor(
            string simpleName,
            int priority,
            Func<Regex> pattern,
            Func<Line, CoordString<TSpace>> source,
            Func<MatchContext, Result> consumer
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
            Func<Line, CoordString<TSpace>> source,
            Func<MatchContext, Result> consumer
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
            Func<Line, CoordString<TSpace>> source,
            Func<MatchContext, Result> consumer
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
            Func<Line, CoordString<TSpace>> source,
            Func<MatchContext, Result> consumer
        ) : this(simpleName, priority, helpEntryName, helpEntryDescription, () => pattern, source, consumer)
        {
            if (pattern == null) throw new ArgumentNullException(nameof(pattern));
        }

        public override IEnumerable<Edit> ProcessLine(CompileContext context, List<Line> lines, Line line, bool write = true)
        {
            var pattern = this.pattern() ?? throw new NullReferenceException("supplier returned a null Regex");
            var edits = new List<Edit>();

            var text = source(line);
            Match match = pattern.Match(text);
            Result result;
            while (match.Success
                && (result = consumer(new MatchContext(context, line, text, match, write, edits))).Continue)
            {
                text = source(line);
                match = pattern.Match(text, result.Next.Value);
            }

            if (edits.Count == 0) return NoChange;
            return edits;
        }
    }
}
