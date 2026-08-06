using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using IC10_Extender;
using IC10_Extender.Variables;
using static IC10_Extender.Highlight.TokenString;

namespace IC10_Extender.Highlight
{
    public interface IFormattableString<T>
    {
        string ToString(Func<T, string> formatter);
    }

    /// <summary>
    /// Represents a token in a <see cref="TokenizedString{T,TVirtual}"/>.
    /// </summary>
    /// <remarks>
    /// <para><b>Text immutability:</b> For mapped tokens (<see cref="TokenizedString{T,TVirtual}.TokenBase"/>),
    /// <see cref="Text"/> is immutable after construction. For virtual tokens
    /// (<see cref="TokenizedString{T,TVirtual}.VirtualTokenBase"/>), <see cref="Text"/> is computed
    /// from the current live leaf descendants and changes as sub-nodes are removed. Callers must
    /// not cache <see cref="Text"/> across mutations when holding a <c>TVirtual</c> reference.</para>
    /// <para><b>Consume() on virtual sub-nodes:</b> Calling <see cref="Consume"/> on a virtual
    /// sub-node marks only that sub-node as consumed locally within the virtual tree. It does not
    /// propagate to the virtual root or the source string's consumed collection. To test whether all
    /// parts of a virtual token have been claimed use
    /// <c>virtualToken.Leaves.All(l =&gt; l.IsConsumed)</c>.</para>
    /// </remarks>
    public interface IToken
    {
        /// <summary>
        /// The position of this token in the original string.
        /// <para>For virtual sub-nodes (nodes that are not the root of a virtual tree), returns the
        /// root's <see cref="Start"/> so that sub-nodes can still be located within the source
        /// string. Sub-node ordering within a virtual token is positional in the parent's
        /// <see cref="Children"/> array, not by <see cref="Start"/>.</para>
        /// </summary>
        Index<OriginalSpace> Start { get; }

        /// <summary>
        /// The position of this token in the contiguous display string produced by
        /// <see cref="ITokenizedString{T}.ToString(Func{T,string})"/>.
        /// <para>For virtual sub-nodes, returns the root's <see cref="AdjustedStart"/>.
        /// The setter throws for virtual sub-nodes since only <see cref="TokenizedString{T,TVirtual}.ReNumber"/>
        /// should assign this value, and it only operates on root-level tokens.</para>
        /// <para><see cref="Index{TSpace}.Removed"/> once the token is removed, and
        /// <see cref="Index{TSpace}.Missing"/> for a virtual sub-node whose root has not been
        /// numbered. <see cref="Index{TSpace}.Virtual"/> is not reachable here: a virtual token does
        /// have a layout position, it just has no source counterpart.</para>
        /// </summary>
        Index<AdjustedSpace> AdjustedStart { get; }

        /// <summary>
        /// Number of characters this token covers. For virtual tokens this reflects only the
        /// current live (non-removed) leaf content and changes as sub-nodes are removed or replaced.
        /// </summary>
        Offset<TextSpace> Length { get; }

        /// <summary>
        /// The text content. For mapped tokens this is immutable after construction. For virtual
        /// tokens this is the concatenation of all non-removed leaf descendants and changes as
        /// sub-nodes are removed.
        /// </summary>
        CoordString<TextSpace> Text { get; }

        bool IsRemoved { get; }
        bool IsConsumed { get; }
        bool IsUnconsumed { get; }

        bool IsSplit { get; }

        /// <summary>
        /// The direct children produced by <see cref="Split"/>. Includes removed children; this is
        /// an immutable snapshot of the split operation. Empty if never split.
        /// </summary>
        IToken[] Children { get; }
        IToken[] Leaves { get; }

        /// <summary>
        /// For tokens created through First() or FromSegments() or root virtual tokens created through Replace(), this will be null.
        /// Otherwise, this is the parent token from which this token was split.
        /// </summary>
        IToken Parent { get; }

        /// <summary>
        /// The provenance of this token: which original-string ranges it is a transform of.
        /// <para>For mapped tokens: one segment per source fragment in original-string order,
        /// non-overlapping and sorted by <see cref="Segment{TSpace}.Start"/>. These correspond to
        /// <see cref="Text"/> character-for-character, so <see cref="MapToOriginal"/> is defined.</para>
        /// <para>For virtual tokens: the provenance of the mapped token that was replaced, shared by
        /// every node in the virtual tree and inherited by sub-nodes. <see cref="Text"/> does
        /// <em>not</em> correspond to it character-for-character, so this answers "where did this come
        /// from", not "which source character is this", so <see cref="MapToOriginal"/> throws.</para>
        /// </summary>
        IReadOnlyList<Segment<OriginalSpace>> Provenance { get; }

        /// <summary>
        /// Projects a range of this token's own text onto the original ranges it covers. Returns more
        /// than one segment when this token's <see cref="Provenance"/> are disjoint and the
        /// range straddles a gap.
        /// <para>Throws <see cref="InvalidOperationException"/> for virtual tokens, which have no
        /// character-level provenance. A walk that must ask every token where it came from should use
        /// <see cref="Provenance"/> instead, which is total and never throws.</para>
        /// </summary>
        IReadOnlyList<Segment<OriginalSpace>> MapToOriginal(Segment<TextSpace> range);

        /// <summary>
        /// Projects an original range onto this token's text. One-to-one: text is contiguous, so a
        /// covered original range always maps to a single text range even when it straddles a gap.
        /// <para>Throws <see cref="InvalidOperationException"/> if the range is not fully covered by
        /// this token, or for virtual tokens.</para>
        /// </summary>
        Segment<TextSpace> MapToText(Segment<OriginalSpace> range);

        /// <summary>
        /// Projects a range of this token's text onto the rendered string. Total for live tokens,
        /// virtual roots included, since adjusted space is the concatenation of token texts.
        /// </summary>
        Segment<AdjustedSpace> MapToAdjusted(Segment<TextSpace> range);

        /// <summary>
        /// Projects a range of the rendered string onto this token's text.
        /// <para>Throws <see cref="InvalidOperationException"/> if the range is not wholly within
        /// this token.</para>
        /// </summary>
        Segment<TextSpace> MapToText(Segment<AdjustedSpace> range);
    }

    public interface IToken<out T, out TVirtual> : IToken
        where T : IToken
        where TVirtual : IToken
    {
        /// <summary>
        /// Splits the token into fragments. For mapped tokens the parent is removed from the source
        /// collection and replaced by the children. For virtual tokens the parent remains in the
        /// source collection as the authoritative entry; children are held internally and never
        /// added to source collections directly.
        /// </summary>
        T[] Split(params string[] fragments);

        /// <summary>
        /// Marks the token as consumed.
        /// <para>For mapped tokens: moves the token from the unconsumed to the consumed collection
        /// in the source string.</para>
        /// <para>For virtual root tokens: marks the root as consumed within the source string's
        /// consumed collection (no-op if already consumed).</para>
        /// <para>For virtual sub-nodes: marks only the sub-node as consumed locally within the
        /// virtual tree. Does not propagate to the root or source collections.</para>
        /// </summary>
        T Consume();

        /// <summary>
        /// Removes the token.
        /// <para>For mapped tokens and virtual root tokens: removes from the source string's
        /// collections entirely and triggers <see cref="TokenizedString{T,TVirtual}.ReNumber"/>.</para>
        /// <para>For virtual sub-nodes: marks the node removed within the virtual tree only; stays
        /// in <see cref="Children"/> as a historical snapshot. If this was the last live leaf, the
        /// removal propagates up the virtual tree. If propagation reaches the root the root is
        /// removed from the source string entirely.</para>
        /// </summary>
        T Remove();

        /// <summary>
        /// Replaces this mapped token with a virtual root token carrying the replacement text.
        /// The virtual root is added to the source string's consumed collection. The mapped token
        /// is removed. The virtual root inherits the mapped token's <see cref="Provenance"/>
        /// for positional lookup purposes; <see cref="Text"/> will no longer correspond to those
        /// segments character-for-character.
        /// </summary>
        TVirtual Replace(string replacement);
    }

    /// <summary>
    /// Read-only and segment-based access to a tokenized string. Implemented by all
    /// <see cref="TokenizedString{T,TVirtual}"/> variants including <see cref="StyledString"/>.
    /// Does not expose search or structural mutation operations other than <see cref="FromSegments"/>; those are on
    /// <see cref="IPreprocessableString{T,TVirtual}"/>.
    /// </summary>
    public interface ITokenizedString<out T, out TVirtual> : IFormattableString<IToken>
        where T : IToken<T, TVirtual>
        where TVirtual : IToken<TVirtual, TVirtual>
    {

        /// <summary>
        /// Looks up a token by its original-string segments, or creates a new token if not found.
        /// This action is mutating if a new token is created, as it reorganizes the string to form the text into a token.
        /// </summary>
        /// <param name="segments"></param>
        /// <returns></returns>
        IToken FromSegments(IEnumerable<Segment<OriginalSpace>> segments);

        /// <summary>
        /// Returns the token (consumed or unconsumed) whose <see cref="IToken{T}.Provenance"/>
        /// contain <paramref name="index"/>. Returns <c>default</c> if no token covers that position.
        /// </summary>
        IToken TokenAt(Index<OriginalSpace> index);

        /// <summary>
        /// Where a source position currently appears in the layout string.
        /// <para>All three sentinels are reachable:
        /// <see cref="Index{TSpace}.Missing"/> when the position lies outside the source string,
        /// <see cref="Index{TSpace}.Removed"/> when the region was removed, and
        /// <see cref="Index{TSpace}.Virtual"/> when it sits under a replacement and so has no
        /// character-precise layout position.</para>
        /// </summary>
        Index<AdjustedSpace> MapToAdjusted(Index<OriginalSpace> index);

        /// <summary>
        /// Which source position a layout position came from.
        /// <para>Returns <see cref="Index{TSpace}.Missing"/> when the position lies outside the
        /// layout string, and <see cref="Index{TSpace}.Virtual"/> when it falls inside replacement
        /// text. <see cref="Index{TSpace}.Removed"/> is <em>not</em> reachable here: removed text is
        /// absent from the layout, so there is no layout position to ask about.</para>
        /// <para>For a token-granular answer in the virtual case, use <see cref="TokenAt"/> and read
        /// the token's <see cref="IToken.Provenance"/>.</para>
        /// </summary>
        Index<OriginalSpace> MapToOriginal(Index<AdjustedSpace> index);

        /// <summary>
        /// Returns the token whose contiguous display position contains <paramref name="index"/>.
        /// Returns <c>default</c> if no token covers that position.
        /// </summary>
        IToken TokenAt(Index<AdjustedSpace> index);

        /// <summary>The unconsumed text remaining to be processed.</summary>
        CoordString<RemainderSpace> Remainder { get; }

        /// <summary>
        /// The layout string: every live token in <see cref="IToken.Start"/> order. The untyped
        /// form is <see cref="object.ToString()"/>.
        /// </summary>
        CoordString<AdjustedSpace> Render { get; }

        IReadOnlyList<T> Unconsumed { get; }
        IReadOnlyList<IToken> Consumed { get; }
        IReadOnlyList<T> Mapped { get; }
        IReadOnlyList<TVirtual> Virtual { get; }

        /// <summary>
        /// Finalises the token state, splits remaining unconsumed tokens by whitespace, adds them to
        /// consumed, and returns <see cref="ITokenizedString{T, TVirtual}.Consumed"/>.
        /// subsequent calls short circuit to return <see cref="ITokenizedString{T, TVirtual}.Consumed"/>.
        /// </summary>
        IReadOnlyList<IToken> Tokenize();
    }

    /// <summary>
    /// Extends <see cref="ITokenizedString{T,TVirtual}"/> with search and structural preprocessing
    /// operations. Implemented by <see cref="TokenString"/> and <see cref="TokenizedString{T,TVirtual}"/>
    /// subclasses used as <c>Raw</c> or <c>ResetRaw</c> in <see cref="Line"/>.
    /// <para>
    /// <see cref="StyledString"/> intentionally does <em>not</em> implement this interface. Styled
    /// strings are rendering records, not preprocessing targets. Use <see cref="ITokenizedString{T,TVirtual}.FromSegments"/>
    /// for all synchronisation from a Raw anchor into a styled string.
    /// </para>
    /// </summary>
    public interface IPreprocessableString<out T, out TVirtual> : ITokenizedString<T, TVirtual>
        where T : IToken<T, TVirtual>
        where TVirtual : IToken<TVirtual, TVirtual>
    {
        /// <summary>
        /// Finds the first occurrence of <paramref name="token"/> among unconsumed tokens,
        /// isolates it as a new token, and returns it. This is always a fresh unconsumed token.
        /// </summary>
        T First(string token, Index<RemainderSpace> startAt = default, bool useTokenBounds = true);

        /// <summary>
        /// Walks the combined token sequence (unconsumed ∪ consumed) in <see cref="IToken.Start"/>
        /// order. Returns an exact-match consumed token if one exists at the candidate position;
        /// otherwise builds a new token from the unconsumed collection. You may need to walk the
        /// object tree to get to the leaf tokens for further processing.
        /// </summary>
        IToken Locate(string token, Index<AdjustedSpace> startAt = default, bool useTokenBounds = true);
    }

    /// <summary>
    /// A string partitioned into mapped tokens (<typeparamref name="T"/>) with valid original-string
    /// segment mappings, and virtual tokens (<typeparamref name="TVirtual"/>) produced by
    /// <see cref="TokenBase.Replace"/> and subsequent TVirtual token interactions that carry replacement 
    /// text with no character-level original-string mapping.
    /// </summary>
    public class TokenizedString<T, TVirtual> : IPreprocessableString<T, TVirtual>
        where T : TokenizedString<T, TVirtual>.TokenBase
        where TVirtual : TokenizedString<T, TVirtual>.VirtualTokenBase
    {
        public const string TokenStartBoundary = @"(?<=^|\s+)";
        public const string TokenEndBoundary = @"(?=\s+|$)";


        public static readonly ILogger Log = new FallbackLogger(() => Plugin.InstanceLogger, LogLevel.Info);
        public static readonly Func<IToken, string> RawFormat = t => t.Text;

        public readonly CoordString<OriginalSpace> Original;

        protected readonly List<T> unconsumed = new List<T>();
        protected readonly List<IToken> consumed = new List<IToken>();

        /// <summary>
        /// factory: (source, original, text, start, segments) -> T
        /// </summary>
        /// <remarks>Metadata outside of tokenization (e.g. styling) is expected to be carried over or not as makes sense for the extending subclass.</remarks>
        protected readonly Func<TokenizedString<T, TVirtual>, T, string, Index<OriginalSpace>, IEnumerable<Segment<OriginalSpace>>, T> tokenFactory;

        /// <summary>
        /// factory: (source, original, text) -> TVirtual
        /// </summary>
        /// <remarks>
        /// <para>If original is a TokenBase, the virtual token will inherit its Start and Provenance, and Parent will remain null.
        /// Otherwise, the created token will have Parent = original, and its fields will be computed or delegated to parent.
        /// Text and Length will be from the provided text, unless the token itself becomes a parent, at which point, the fields will be computed.</para>
        /// <para>Metadata outside of tokenization (e.g. styling) is expected to be carried over or not as makes sense for the extending subclass.</para>
        /// </remarks>
        protected readonly Func<TokenizedString<T, TVirtual>, IToken, string, TVirtual> virtualFactory;

        private bool _tokenized = false;

        public bool IsTokenized => _tokenized;

        public IReadOnlyList<T> Unconsumed => unconsumed.AsReadOnly();
        public IReadOnlyList<IToken> Consumed => consumed.AsReadOnly();
        public IReadOnlyList<T> Mapped => consumed.Where(t => t is T).Cast<T>().ToList().AsReadOnly();
        public IReadOnlyList<TVirtual> Virtual => consumed.Where(t => t is TVirtual).Cast<TVirtual>().ToList().AsReadOnly();

        /// <summary>
        /// Cache of tokens keyed by their <see cref="TokenBase.Provenance"/> serialised as
        /// a canonical string. Populated on every token creation via <see cref="RegisterToken"/>
        /// and consulted by <see cref="FromSegments"/> before attempting to build from unconsumed
        /// text. Allows <see cref="FromSegments"/> to retrieve already-consumed or split tokens
        /// by position rather than by text, making it immune to text replacement and splits.
        /// </summary>
        private readonly Dictionary<string, IToken> segmentCache = new Dictionary<string, IToken>();

        public TokenizedString(
            string fullString,
            Func<TokenizedString<T, TVirtual>, T, string, Index<OriginalSpace>, IEnumerable<Segment<OriginalSpace>>, T> tokenFactory,
            Func<TokenizedString<T, TVirtual>, IToken, string, TVirtual> virtualFactory)
        {
            Original = (fullString ?? throw new ArgumentNullException(nameof(fullString))).In<OriginalSpace>();
            this.tokenFactory = tokenFactory ?? throw new ArgumentNullException(nameof(tokenFactory));
            this.virtualFactory = virtualFactory ?? throw new ArgumentNullException(nameof(virtualFactory));
            unconsumed.Add(tokenFactory(this, null, fullString, Index<OriginalSpace>.Zero, new List<Segment<OriginalSpace>>() { new Segment<OriginalSpace>(Index<OriginalSpace>.Zero, (Offset<OriginalSpace>)fullString.Length) }));
        }

        /// <summary>
        /// Registers a token in the segment cache. Called by <see cref="TokenBase"/> and
        /// <see cref="VirtualTokenBase"/> constructors so every token is reachable by segments
        /// from the moment of creation.
        /// </summary>
        internal void RegisterToken(IToken token)
        {
            var key = SegmentKey(token.Provenance);
            if (!string.IsNullOrEmpty(key))
                segmentCache[key] = token;
        }

        private static string SegmentKey(IReadOnlyList<Segment<OriginalSpace>> segments)
        {
            if (segments == null || segments.Count == 0) return null;
            return string.Join("|", segments.Select(s => $"{s.Start}:{s.Length}"));
        }

        protected void UpdateTo(List<T> unconsumed = null, List<IToken> consumed = null)
        {
            if (unconsumed != null)
            {
                this.unconsumed.Clear();
                this.unconsumed.AddRange(unconsumed.OrderBy(t => t.Start).ToList());
            }

            if (consumed != null)
            {
                this.consumed.Clear();
                this.consumed.AddRange(consumed.OrderBy(t => t.Start).ToList());
            }
        }

        protected void AddConsumed(params IToken[] tokens)
        {
            consumed.AddRange(tokens);
            consumed.Sort((a, b) => a.Start.CompareTo(b.Start));
        }

        protected void AddUnconsumed(params T[] tokens)
        {
            unconsumed.AddRange(tokens);
            unconsumed.Sort((a, b) => a.Start.CompareTo(b.Start));
        }

        /// <summary>
        /// Recomputes <see cref="TokenBase.AdjustedStart"/> for all tokens (mapped and virtual
        /// roots) based on <see cref="TokenBase.Start"/> order. Virtual sub-nodes are not
        /// present in either collection and are not numbered directly; they delegate to their root.
        /// </summary>
        protected void ReNumber()
        {
            var index = Index<AdjustedSpace>.Zero;
            var sorted = unconsumed.Cast<IToken>().Concat(consumed).OrderBy(t => t.Start).ToList();
            foreach (IToken token in sorted)
            {
                if (token is TokenBase mapped) {
                    mapped.AdjustedStart = index;
                    index = index + mapped.Length.In<AdjustedSpace>();
                }
                if (token is VirtualTokenBase virtualRoot)
                {
                    virtualRoot.AdjustedStart = index;
                    index = index + virtualRoot.Length.In<AdjustedSpace>();
                }
            }
        }

        /// <summary>
        /// Character counts correspond one-to-one between a token's text and the spaces derived from
        /// it, so an extent is the same magnitude in each.
        /// </summary>
        protected static Offset<TextSpace> AsText(Offset<OriginalSpace> extent) => (Offset<TextSpace>)extent.Value;
        protected static Offset<OriginalSpace> AsOriginal(Offset<TextSpace> extent) => (Offset<OriginalSpace>)extent.Value;
        protected static Offset<AdjustedSpace> AsAdjusted(Offset<TextSpace> extent) => (Offset<AdjustedSpace>)extent.Value;
        protected static Offset<TextSpace> AsText(Offset<AdjustedSpace> extent) => (Offset<TextSpace>)extent.Value;

        protected static void GuardWithinText(Segment<TextSpace> range, Offset<TextSpace> textLength)
        {
            if (range.Start < Index<TextSpace>.Zero || range.End > Index<TextSpace>.Zero + textLength)
                throw new ArgumentOutOfRangeException(
                    nameof(range),
                    $"Text range {range} lies outside this token (length {textLength}).");
        }

        /// <summary>
        /// Rejects layout-space mapping for a token that has no layout position.
        /// </summary>
        protected static void GuardPositioned(Index<AdjustedSpace> adjustedStart)
        {
            if (!adjustedStart.IsResolved)
                throw new InvalidOperationException(
                    "This token has no position in the layout string, so it cannot be mapped to or " +
                    "from adjusted space. It has been removed, or its root has not been numbered.");
        }

        protected void GuardTokenized()
        {
            if (_tokenized)
                throw new InvalidOperationException(
                    "Tokenize() has already been called. First(), Locate(), and FromSegments() are not valid after tokenization.");
        }

        protected Regex Compile(string token, bool includeTokenStartBoundary, bool includeTokenEndBoundary)
        {
            string pattern = ""; //abcde
            var reversed = token.Reverse();
            foreach (var c in reversed.Take(token.Length - 1))
            {
                pattern = $"({Regex.Escape(c.ToString())}{pattern})?";
            }
            pattern = $"{Regex.Escape(reversed.Last().ToString())}{pattern}";
            pattern = $"{(includeTokenStartBoundary ? TokenStartBoundary : "")}(?<match>{pattern}){(includeTokenEndBoundary ? TokenEndBoundary : "")}";
            return new Regex(pattern, RegexOptions.ExplicitCapture);
        }

        /// <summary>
        /// One token's contribution to a match: the matched text, the token it came from, and where
        /// within that token's text it starts. The offset is what lets <see cref="BuildToken"/> ask
        /// the token to map the range, rather than inferring a position from lengths.
        /// </summary>
        protected readonly struct MatchFragment<TToken> where TToken : IToken
        {
            public readonly string Text;
            public readonly TToken Token;
            public readonly Index<TextSpace> At;

            public MatchFragment(string text, TToken token, Index<TextSpace> at)
            {
                Text = text;
                Token = token;
                At = at;
            }

            public Segment<TextSpace> Range => At.Spanning(Text.Length);
        }

        /// <summary>
        /// <paramref name="startAt"/> is an offset into the concatenation of <paramref name="domain"/>,
        /// so <typeparamref name="TSpace"/> is whichever space that concatenation defines:
        /// <see cref="RemainderSpace"/> for the unconsumed domain used by <see cref="First"/>,
        /// <see cref="AdjustedSpace"/> for the live domain used by <see cref="Locate"/>.
        /// <paramref name="index"/> is a list index into <paramref name="domain"/>, not a position.
        /// </summary>
        protected bool Search<Type, TSpace>(
            List<Type> domain,
            string token,
            int index,
            Index<TSpace> startAt,
            bool containsConsumed,
            bool useTokenBounds,
            bool inSpan,
            out List<MatchFragment<Type>> results
        )
            where Type : IToken
            where TSpace : ICoordinateSpace
        {
            Log.Debug($"Search(token:\"{token}\", index:{index}, startAt:{startAt}, inSpan:{inSpan}");

            if (domain == null) throw new ArgumentNullException(nameof(domain));

            if (token == null) throw new ArgumentNullException(nameof(token));
            if (string.IsNullOrEmpty(token))
            {
                results = new List<MatchFragment<Type>>();
                return false;
            }

            if (index < 0) throw new ArgumentOutOfRangeException(nameof(index));
            if (index >= domain.Count) //end of list, hard fail
            {
                Log.Debug("End of list, hard fail");
                results = new List<MatchFragment<Type>>();
                return false;
            }

            if (startAt < Index<TSpace>.Zero) throw new ArgumentOutOfRangeException(nameof(startAt), "Cannot be negative");

            var searchFrom = Offset<TextSpace>.Zero;
            if (startAt > Index<TSpace>.Zero)
            {
                // startAt is an offset into the concatenation of domain, so accumulate extents to
                // find which entry holds it and where within that entry's text it falls.
                var cursor = Index<TSpace>.Zero;
                var startTokenIndex = domain.Count;
                var withinToken = Offset<TSpace>.Zero;
                for (int i = 0; i < domain.Count; i++)
                {
                    var end = cursor + domain[i].Length.In<TSpace>();
                    if (startAt < end)
                    {
                        startTokenIndex = i;
                        withinToken = startAt - cursor;
                        break;
                    }
                    cursor = end;
                }

                if (startTokenIndex >= domain.Count)
                {
                    Log.Debug("startAt is past the end of the domain");
                    results = new List<MatchFragment<Type>>();
                    return false;
                }

                if (index < startTokenIndex)
                {
                    Log.Debug($"Seeking to {startAt}");
                    return Search(domain, token, startTokenIndex, startAt, containsConsumed, useTokenBounds, inSpan, out results);
                }

                if (index == startTokenIndex) searchFrom = withinToken.In<TextSpace>();
            }

            if(containsConsumed && consumed.Contains(domain[index]))
            {
                if (inSpan)
                { 
                    //continue to next non-consumed token, since we're in a span and consumed tokens can't be part of a span match
                    Log.Debug("In span and token is consumed, skipping to next non-consumed token");
                    return Search(domain, token, index + 1, startAt, containsConsumed, useTokenBounds, inSpan, out results);
                }

                if (domain[index].Text == token)
                {
                    //exact match, return immediately with this token as the only result
                    Log.Debug("Exact match with consumed token, returning immediately");
                    results = new List<MatchFragment<Type>>() { new MatchFragment<Type>(domain[index].Text, domain[index], Index<TextSpace>.Zero) };
                    return true;
                }

                // consumed tokens (including virtual roots) are never eligible for partial or
                // substring matches since they cannot be restructured by Split() or Remove()
                // without breaking invariants.
                Log.Debug("Consumed token is not an exact match, skipping");
                return Search(domain, token, index + 1, startAt, containsConsumed, useTokenBounds, inSpan, out results);
            }

            var pattern = Compile(token, useTokenBounds && !inSpan, useTokenBounds);

            for (var result = pattern.Match(domain[index].Text, searchFrom.Value); result.Success; result = pattern.Match(domain[index].Text, result.Index + 1))
            {
                var match = result.Groups["match"];

                Log.Debug($"Match(Value:{match.Value}, Index:{match.Index}, Length:{match.Length})");
                if (match.Index != 0) //doesn't start at beginning of token
                {
                    Log.Debug("Doesn't start at beginning of token");
                    if (inSpan) //then not a contiguous match, return false
                    {
                        Log.Debug("Not a contiguous match, returning false");
                        results = new List<MatchFragment<Type>>();
                        return false;
                    }

                    // else new start of match, starting in middle of token
                }

                if (match.Length == token.Length) // full match
                {
                    Log.Debug("Full match");
                    results = new List<MatchFragment<Type>>() { new MatchFragment<Type>(match.Value, domain[index], (Index<TextSpace>)match.Index) };
                    return true;
                }

                // else partial match only
                Log.Debug("Partial match only");
                if (match.Index + match.Length != domain[index].Length.Value) //does not run up to end
                {
                    Log.Debug("Does not run to end of token");
                    if (inSpan) // partial match ends without continuing to next token. Return span failure
                    {
                        Log.Debug("Partial match ends without continuing to next token");
                        results = new List<MatchFragment<Type>>();
                        return false;
                    }
                    else // partial match ends, but since not in span, we can continue to next potential match
                    {
                        Log.Debug("Partial match ends outside of span. Continue to next potential match");
                        continue;
                    }
                }

                //else runs up to end, recurse to next token, starting span
                Log.Debug("Runs up to end of token, starting span");
                var spanSuccess = Search(domain, token.Substring(match.Length), index + 1, startAt, containsConsumed, useTokenBounds, true, out results);
                if (spanSuccess)
                {
                    Log.Debug("Span successful");
                    results.Add(new MatchFragment<Type>(match.Value, domain[index], (Index<TextSpace>)match.Index));
                    return true;
                }

                //else span unsuccessful, continue to next potential match
                Log.Debug("Span unsuccessful. Continuing to next potential match");
                continue;
            }

            //potential matches exhausted, recursing on next token
            Log.Debug("Exhausted potential matches in token. Recursing on next token");
            return Search(domain, token, index + 1, startAt, containsConsumed, useTokenBounds, false, out results);
        }

        protected T BuildToken(string token, List<MatchFragment<T>> results)
        {
            var segments = new List<Segment<OriginalSpace>>(results.Count);
            foreach (var fragment in results)
            {
                // Ask the token to map its own text range. A flat offset cannot express a match that
                // straddles a gap between disjoint provenance segments.
                segments.AddRange(fragment.Token.MapToOriginal(fragment.Range));

                if (fragment.Token.Text == fragment.Text) fragment.Token.Remove();
                else fragment.Token.Split(fragment.Text)
                        .First(frag => frag.Text == fragment.Text)
                        .Remove();
            }
            var result = tokenFactory(this, null, token, segments[0].Start, segments);
            AddUnconsumed(result);
            return result;
        }

        public T First(string token, Index<RemainderSpace> startAt = default, bool useTokenBounds = true)
        {
            GuardTokenized();

            if (Search(unconsumed, token, 0, startAt, false, useTokenBounds, false, out var results))
            {
                results.Reverse();
                return BuildToken(token, results);
            }
            return default;
        }

        public IToken Locate(string token, Index<AdjustedSpace> startAt = default, bool useTokenBounds = true)
        {
            GuardTokenized();

            // Not filtered by startAt: Search resolves it by walking the domain, and removing
            // leading entries would shift the concatenation that startAt is an offset into.
            var allByPosition = consumed.Concat(unconsumed.Cast<IToken>())
                .OrderBy(t => t.Start)
                .ToList();

            if (Search(allByPosition, token, 0, startAt, true, useTokenBounds, false, out var results))
            {
                results.Reverse();
                if (results.Count == 0) return default;

                var firstMatch = results[0];
                if (consumed.Contains(firstMatch.Token) && firstMatch.Text == token)
                {
                    return firstMatch.Token;
                }
                return BuildToken(token, results
                    .Select(f => new MatchFragment<T>(f.Text, (T)f.Token, f.At))
                    .ToList());
            }
            return default;
        }

        public IToken TokenAt(Index<OriginalSpace> index)
        {
            return unconsumed.Cast<IToken>().Concat(consumed)
                .FirstOrDefault(t => t.Provenance.Any(s => s.Contains(index)));
        }

        public Index<AdjustedSpace> MapToAdjusted(Index<OriginalSpace> index)
        {
            var token = TokenAt(index);
            if (token == null)
            {
                // In the source but covered by nothing live means the region was removed. Removed
                // tokens leave both collections, so TokenAt cannot report them directly.
                return Original.Bounds.Contains(index)
                    ? Index<AdjustedSpace>.Removed
                    : Index<AdjustedSpace>.Missing;
            }

            if (token is VirtualTokenBase) return Index<AdjustedSpace>.Virtual;

            return token.MapToAdjusted(token.MapToText(index.Spanning(1))).Start;
        }

        public Index<OriginalSpace> MapToOriginal(Index<AdjustedSpace> index)
        {
            var token = TokenAt(index);
            if (token == null) return Index<OriginalSpace>.Missing;
            if (token is VirtualTokenBase) return Index<OriginalSpace>.Virtual;

            return token.MapToOriginal(token.MapToText(index.Spanning(1)))[0].Start;
        }

        public IToken TokenAt(Index<AdjustedSpace> index)
        {
            return unconsumed.Cast<IToken>().Concat(consumed)
                .FirstOrDefault(t => t.AdjustedStart <= index
                                  && index < t.AdjustedStart + t.Length.In<AdjustedSpace>());
        }

        /// <summary>
        /// First consults the cache for a token with exactly these segments. If not found, walks the combined token sequence
        /// to build a new token from the unconsumed tokens. This makes FromSegments immune to text replacement and token splits, 
        /// though callers may need to traverse the tree for leaf-level tokens.
        /// </summary>
        /// <param name="segments"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="ArgumentException"></exception>
        /// <exception cref="InvalidOperationException"></exception>
        public IToken FromSegments(IEnumerable<Segment<OriginalSpace>> segments)
        {
            GuardTokenized();
            if (segments == null) throw new ArgumentNullException(nameof(segments));

            var segList = segments.OrderBy(s => s.Start).ToList();
            if (segList.Count == 0) throw new ArgumentException("Segments cannot be empty", nameof(segments));
            if (segList.Any(s => s.Length == Offset<OriginalSpace>.Zero)) throw new ArgumentException($"Segments cannot have zero length: {segList.First(s => s.Length == Offset<OriginalSpace>.Zero)}", nameof(segments));

            // consult the cache
            var key = SegmentKey(segList);
            if (segmentCache.TryGetValue(key, out var cached))
                return cached;

            // fallback to building the token
            Dictionary<T, List<Segment<OriginalSpace>>> mappings = new Dictionary<T, List<Segment<OriginalSpace>>>();
            for (int i = 0; i < segList.Count; i++)
            {
                var segment = segList[i];

                if (!Original.Bounds.Contains(segment))
                    throw new ArgumentException(
                        $"Segment {segment} is out of bounds of the original string (length {Original.Length}).",
                        nameof(segments));

                if (i > 0)
                {
                    var prev = segList[i - 1];
                    if (prev.Overlaps(segment))
                        throw new ArgumentException(
                            $"Segments {prev} and {segment} overlap.",
                            nameof(segments));
                }

                var covering = unconsumed.FirstOrDefault(
                    t => t.Provenance.Any(
                        s => s.Contains(segment)
                    )
                );

                if (covering == null)
                {
                    if (consumed.Any(
                            t => t.Provenance.Any(
                                s => s.Contains(segment)
                            )
                        ))
                        throw new InvalidOperationException(
                            $"Segment {segment} is covered by a consumed (mutated) token. " +
                            $"Acquire the consumed token directly and use Split() to specify segmentation.");
                    else
                        throw new InvalidOperationException(
                            $"Segment {segment} is not covered by any token. It may have been removed.");
                }

                if (!mappings.ContainsKey(covering)) mappings[covering] = new List<Segment<OriginalSpace>>();
                mappings[covering].Add(segment);
            }

            foreach (var kvp in mappings)
            {
                var token = kvp.Key;
                var fragments = kvp.Value.Select(s => Original.Substring(s).Value).ToArray();
                foreach (var fragment in token.Split(fragments))
                {
                    if (fragments.Any(s => s == fragment.Text)) fragment.Remove();
                }
            }

            var result = tokenFactory(
                this,
                null,
                string.Join("", segList.Select(seg => Original.Substring(seg.Start.Value, seg.Length.Value))),
                segList.First().Start,
                segList
            );
            AddUnconsumed(result);
            return result;
        }

        public IReadOnlyList<IToken> Tokenize()
        {
            if (_tokenized) return Consumed;

            var tokens = Remainder.Split(" \t".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);

            List<IToken> newTokens = new List<IToken>();
            foreach (var token in tokens)
            {
                newTokens.Add(First(token));
            }
            AddConsumed(newTokens.ToArray());
            ReNumber();

            _tokenized = true;

            return Consumed;
        }

        protected string ToString(List<IToken> tokens, Func<IToken, string> formatter)
        {
            var sorted = tokens.ToList();
            StringBuilder sb = new StringBuilder();
            foreach (IToken token in sorted)
            {
                sb.Append(formatter(token));
            }
            return sb.ToString();
        }

        public string ToString(Func<IToken, string> formatter)
        {
            return ToString(unconsumed.Cast<IToken>().Concat(consumed).OrderBy(t => t.Start).ToList(), formatter);
        }

        public override string ToString()
        {
            return Render;
        }

        /// <summary>
        /// The layout string: every live token, consumed and unconsumed, in <see cref="IToken.Start"/>
        /// order. The untyped form is <see cref="ToString()"/>.
        /// </summary>
        public CoordString<AdjustedSpace> Render => ToString(RawFormat).In<AdjustedSpace>();

        public CoordString<RemainderSpace> Remainder =>
            ToString(unconsumed.Cast<IToken>().ToList(), t => t.Text).In<RemainderSpace>();

        public class TokenBase : IToken<T, TVirtual>
        {
            public TokenizedString<T, TVirtual> Source { get; private set; }
            /// <summary>
            /// The position of this token in the <em>original</em> string. Never changes after
            /// construction. For span-tokens covering multiple original ranges, this is the
            /// start of the first segment. Use <see cref="Provenance"/> for complete coverage.
            /// </summary>
            public Index<OriginalSpace> Start { get; private set; }

            private Index<AdjustedSpace> adjustedStart;

            /// <summary>
            /// The position of this token in the contiguous display string. Recomputed by
            /// <see cref="TokenizedString{T,TVirtual}.ReNumber"/> after every mutating operation.
            /// </summary>
            public Index<AdjustedSpace> AdjustedStart
            {
                get
                {
                    return removed ? Index<AdjustedSpace>.Removed : adjustedStart;
                }
                set
                {
                    GuardNotRemoved();
                    adjustedStart = value;
                }
            }

            public Offset<TextSpace> Length { get; private set; }

            /// <summary>
            /// The text content of this token. Immutable after construction for mapped tokens.
            /// </summary>
            public CoordString<TextSpace> Text { get; private set; }

            /// <summary>
            /// The original-string segments this token covers. For simple tokens always a single
            /// segment. For span-tokens one entry per source fragment.
            /// </summary>
            public IReadOnlyList<Segment<OriginalSpace>> Provenance { get; }

            private bool removed = false;
            public bool IsRemoved => removed;
            public bool IsConsumed => Source.consumed.Contains(this as T);
            public bool IsUnconsumed => Source.unconsumed.Contains(this as T);

            private T[] children = null;
            public bool IsSplit => children != null;
            public T[] Children => children?.ToArray() ?? Array.Empty<T>();

            IToken[] IToken.Children => Children;

            public T[] Leaves
            {
                get
                {
                    if (removed) return Array.Empty<T>();
                    if (!IsSplit) return new T[] { this as T };
                    var result = new List<T>();
                    foreach (var child in children.Where(t => !t.IsRemoved).ToList())
                    {
                        result.AddRange(child.Leaves);
                    }
                    return result.ToArray();
                }
            }

            IToken[] IToken.Leaves => Leaves.Cast<IToken>().ToArray();
            public IToken Parent { get; private set; }

            public TokenBase(TokenizedString<T, TVirtual> source, IToken parent, string text, Index<OriginalSpace> start, IEnumerable<Segment<OriginalSpace>> segments)
            {
                Source = source ?? throw new ArgumentNullException(nameof(source));
                Parent = parent;
                Text = text.In<TextSpace>();
                Length = (Offset<TextSpace>)text.Length;

                var segList = segments != null
                    ? segments.OrderBy(s => s.Start).ToList()
                    : new List<Segment<OriginalSpace>> { new Segment<OriginalSpace>(start, (Offset<OriginalSpace>)text.Length) };

                Provenance = segList.AsReadOnly();
                Start = segList[0].Start;
                // Provisional until ReNumber runs. Split does not renumber, so children carry this
                // until the next mutation that does.
                adjustedStart = (Index<AdjustedSpace>)Start.Value;

                source.RegisterToken(this as T);
            }

            public override string ToString()
            {
                var segStr = string.Join(",", Provenance.Select(s => s.ToString()));
                return $"{nameof(TokenBase)}(Source:{Source.GetType().Name}, Text:\"{Text}\", Start:{Start}, AdjustedStart:{AdjustedStart}, Length:{Length}, Segments:[{segStr}])";
            }

            /// <summary>
            /// Returns true if in consumed, false if in unconsumed. Throws if in both or neither.
            /// </summary>
            protected bool GuardExistsInSource()
            {
                bool? isConsumed = null;
                if (Source.unconsumed.Contains(this as T)) isConsumed = false;
                if (Source.consumed.Contains(this as T))
                {
                    if (isConsumed.HasValue) throw new InvalidOperationException("Token found in both consumed and unconsumed collections");
                    isConsumed = true;
                }
                if (!isConsumed.HasValue)
                    throw new InvalidOperationException("Token not found in either consumed or unconsumed collections");
                return isConsumed.Value;
            }

            protected void GuardNotRemoved()
            {
                if (removed) throw new InvalidOperationException("Token has been removed");
            }

            /// <summary>
            /// Removes this token from the source collections and optionally adds replacement tokens.
            /// Marks this token removed, preventing future mutating operations.
            /// </summary>
            protected void Update(bool isConsumed, params IToken[] newTokens)
            {
                var consumedList = Source.consumed.Where(t => !ReferenceEquals(t, this)).ToList();
                var unconsumedList = Source.unconsumed.Where(t => !ReferenceEquals(t, this)).ToList();

                if (isConsumed)
                {
                    consumedList.AddRange(newTokens);
                } else
                {
                    unconsumedList.AddRange(newTokens.Cast<T>());
                }
                Source.UpdateTo(unconsumedList, consumedList);
                removed = true;
            }

            /// <summary>
            /// Projects a range of this token's own text onto the original-string ranges it covers.
            /// Returns more than one segment when this token's <see cref="Provenance"/> are
            /// disjoint and the range straddles a gap.
            /// </summary>
            public IReadOnlyList<Segment<OriginalSpace>> MapToOriginal(Segment<TextSpace> range)
            {
                GuardWithinText(range, Length);

                var result = new List<Segment<OriginalSpace>>();
                var remaining = range.Length;
                var cursor = Index<TextSpace>.Zero;

                foreach (var seg in Provenance)
                {
                    var segEnd = cursor + AsText(seg.Length);
                    var overlapStart = cursor > range.Start ? cursor : range.Start;
                    var overlapEnd = segEnd < range.End ? segEnd : range.End;

                    if (overlapStart < overlapEnd)
                    {
                        var covered = overlapEnd - overlapStart;
                        result.Add(new Segment<OriginalSpace>(
                            seg.Start + AsOriginal(overlapStart - cursor),
                            AsOriginal(covered)));
                        remaining = remaining - covered;
                        if (remaining <= Offset<TextSpace>.Zero) break;
                    }

                    cursor = segEnd;
                }

                return result;
            }

            public Segment<TextSpace> MapToText(Segment<OriginalSpace> range)
            {
                var cursor = Index<TextSpace>.Zero;
                var covered = Offset<TextSpace>.Zero;
                Index<TextSpace>? start = null;

                foreach (var seg in Provenance)
                {
                    var overlapStart = seg.Start > range.Start ? seg.Start : range.Start;
                    var overlapEnd = seg.End < range.End ? seg.End : range.End;

                    if (overlapStart < overlapEnd)
                    {
                        if (!start.HasValue) start = cursor + AsText(overlapStart - seg.Start);
                        covered = covered + AsText(overlapEnd - overlapStart);
                    }

                    cursor = cursor + AsText(seg.Length);
                }

                if (!start.HasValue || covered != AsText(range.Length))
                    throw new ArgumentOutOfRangeException(
                        nameof(range),
                        $"Original range {range} is not fully covered by this token ({covered} of {range.Length} characters).");

                return new Segment<TextSpace>(start.Value, covered);
            }

            public Segment<AdjustedSpace> MapToAdjusted(Segment<TextSpace> range)
            {
                GuardPositioned(AdjustedStart);
                GuardWithinText(range, Length);
                return new Segment<AdjustedSpace>(
                    (Index<AdjustedSpace>)AdjustedStart + AsAdjusted(range.Start - Index<TextSpace>.Zero),
                    AsAdjusted(range.Length));
            }

            public Segment<TextSpace> MapToText(Segment<AdjustedSpace> range)
            {
                GuardPositioned(AdjustedStart);
                var result = new Segment<TextSpace>(
                    Index<TextSpace>.Zero + AsText(range.Start - (Index<AdjustedSpace>)AdjustedStart),
                    AsText(range.Length));
                GuardWithinText(result, Length);
                return result;
            }

            public T[] Split(params string[] fragments)
            {
                GuardNotRemoved();
                var isConsumed = GuardExistsInSource();
                if (fragments == null) throw new ArgumentNullException(nameof(fragments));
                if (fragments.Length == 0) throw new ArgumentException("Must have at least one token to split on.", nameof(fragments));

                List<T> newTokens = new List<T>();
                string workingText = Text;
                int consumedLength = 0;

                foreach (var fragment in fragments)
                {
                    if (string.IsNullOrEmpty(fragment)) throw new ArgumentException("Fragments cannot be null or empty", nameof(fragments));

                    var fragmentIndex = workingText.IndexOf(fragment);
                    if (fragmentIndex == -1) throw new ArgumentException($"Fragment \"{fragment}\" not found in token text \"{Text}\"", nameof(fragments));

                    if (fragmentIndex > 0)
                    {
                        var preStart = Start + ((Offset<TextSpace>)consumedLength).In<OriginalSpace>();
                        newTokens.Add(Source.tokenFactory(Source, this as T, workingText.Substring(0, fragmentIndex), preStart, MapToOriginal(new Segment<TextSpace>((Index<TextSpace>)consumedLength, (Offset<TextSpace>)fragmentIndex))));
                    }

                    var fragStart = Start + ((Offset<TextSpace>)(consumedLength + fragmentIndex)).In<OriginalSpace>();
                    newTokens.Add(Source.tokenFactory(Source, this as T, fragment, fragStart, MapToOriginal(new Segment<TextSpace>((Index<TextSpace>)(consumedLength + fragmentIndex), (Offset<TextSpace>)fragment.Length))));
                    consumedLength += fragmentIndex + fragment.Length;
                    workingText = workingText.Substring(fragmentIndex + fragment.Length);
                }

                if (workingText.Length > 0)
                {
                    var tailStart = Start + ((Offset<TextSpace>)consumedLength).In<OriginalSpace>();
                    newTokens.Add(Source.tokenFactory(Source, this as T, workingText, tailStart, MapToOriginal(new Segment<TextSpace>((Index<TextSpace>)consumedLength, (Offset<TextSpace>)workingText.Length))));
                }

                var arr = newTokens.ToArray();
                children = arr;
                Update(isConsumed, arr);

                return arr;
            }

            public T Consume()
            {
                GuardNotRemoved();
                var isConsumed = GuardExistsInSource();
                if (!isConsumed)
                {
                    Source.unconsumed.Remove(this as T);
                    Source.AddConsumed(this as T);
                    return this as T;
                }
                return this as T;
            }

            /// <summary>
            /// Replaces this mapped token with a virtual root token. The mapped token is removed
            /// from source collections. The virtual root is added to consumed, inheriting
            /// <see cref="Start"/> and <see cref="Provenance"/> from this token for
            /// positional lookup. The virtual root and all children are inherently considered consumed.
            /// <para>Structural, so an empty replacement is rejected: it would leave a zero-length
            /// virtual root in the consumed collection. Call <see cref="Remove"/> for that. This is
            /// the opposite of <see cref="VirtualTokenBase.Replace"/>, which edits a leaf's content
            /// and does accept an empty string.</para>
            /// </summary>
            public TVirtual Replace(string replacement)
            {
                GuardNotRemoved();
                GuardExistsInSource();
                if (string.IsNullOrEmpty(replacement)) throw new ArgumentException("Replacement cannot be null or empty. Call Remove() instead", nameof(replacement));

                var virtualToken = Source.virtualFactory(Source, this as T, replacement);
                // Remove this mapped token and add the virtual root to consumed so that
                // Search/Locate can find it via exact-match and TokenAt can resolve it.
                var consumedList = Source.consumed.Where(t => !ReferenceEquals(t, this)).ToList();
                var unconsumedList = Source.unconsumed.Where(t => !ReferenceEquals(t, this)).ToList();
                Source.UpdateTo(unconsumedList, consumedList);
                removed = true;

                // Not `as T`: TVirtual is unrelated to T, so it compiles but yields null.
                Source.AddConsumed(virtualToken);
                Source.ReNumber();
                return virtualToken;
            }

            /// <summary>
            /// Removes this token from source and marks it removed, preventing future operations.
            /// This operation is idempotent, so it will just return itself instead of throwing if called multiple times.
            /// </summary>
            /// <returns></returns>
            public T Remove()
            {
                if(removed) return this as T;
                Update(true);
                Source.ReNumber();
                return this as T;
            }
        }

        /// <summary>
        /// A virtual token produced by <see cref="TokenBase.Replace"/>. Virtual tokens have no
        /// character-level original-string mapping but retain the replaced token's
        /// <see cref="Provenance"/> for positional lookup.
        /// <para>
        /// A virtual token is a <em>root</em> when it exists directly in the source string's
        /// <see cref="TokenizedString{T,TVirtual}.consumed"/> collection, or a <em>sub-node</em>
        /// when held within another virtual token's internal child tree.
        /// </para>
        /// <para><b>Text / Length:</b> Computed from live (non-removed) leaf descendants.</para>
        /// <para><b>Start / AdjustedStart:</b> Sub-nodes delegate to the root's values
        /// since sub-node ordering is positional within <see cref="Children"/>, not by index.
        /// The <c>AdjustedStart</c> setter throws for sub-nodes.</para>
        /// </summary>
        public class VirtualTokenBase : IToken<TVirtual, TVirtual>
        {
            /// <summary>
            /// The source string that owns the root of this virtual tree. Same on all nodes in
            /// the tree.
            /// </summary>
            public TokenizedString<T, TVirtual> Source { get; private set; }

            IToken IToken.Parent => Parent ?? mappedParent;
            private IToken mappedParent;
            /// <summary>
            /// The virtual token that created this node via <see cref="Split"/>. Null on the root.
            /// </summary>
            public TVirtual Parent { get; private set; }

            /// <summary>True if this is the root node (no virtual parent).</summary>
            public bool IsRoot => Parent == null;

            // Root-only positional state.
            private readonly Index<OriginalSpace> start;

            /// <summary>
            /// Where this transform's provenance begins: the original-string position of the mapped
            /// token that was replaced. Every node in a virtual tree reports the same value because
            /// the whole tree is one transform of one source region. This is the value's meaning,
            /// not a delegation shortcut. It does not locate an individual node.
            /// </summary>
            public Index<OriginalSpace> Start
            {
                get
                {
                    if (IsRoot) return start;
                    return Parent.Start;
                }
            }

            private Index<AdjustedSpace> adjustedStart;

            /// <summary>
            /// Where this transform begins in the layout string, set on the root by
            /// <see cref="TokenizedString{T,TVirtual}.ReNumber"/>. As with <see cref="Start"/>,
            /// every node reports the same value because it describes the transform, not the node.
            /// It is <em>not</em> where a sub-node renders. For that, use
            /// <see cref="MapToAdjusted"/>, which resolves a node's own position on demand.
            /// The setter throws for sub-nodes; only roots are numbered.
            /// </summary>
            public Index<AdjustedSpace> AdjustedStart
            {
                get
                {
                    // Before delegating: a removed sub-node must not report its root's position.
                    if (removed) return Index<AdjustedSpace>.Removed;
                    if (IsRoot) return adjustedStart;
                    return Parent.AdjustedStart;
                }
                set
                {
                    if (!IsRoot) throw new InvalidOperationException(
                        "AdjustedStart cannot be set on virtual sub-nodes. ReNumber operates on root nodes only.");
                    GuardNotRemoved();
                    adjustedStart = value;
                }
            }

            private string text;
            /// <summary>
            /// The live text of this node: concatenation of all non-removed leaf descendants.
            /// Returns <see cref="string.Empty"/> if removed or if all leaves are removed.
            /// </summary>
            public CoordString<TextSpace> Text
            {
                get
                {
                    if (removed) return string.Empty.In<TextSpace>();
                    return (IsSplit ? string.Join("", LivingChildren.Select(c => c.Text.Value)) : text).In<TextSpace>();
                }
            }

            /// <summary>
            /// The number of live (non-removed) characters in this node's subtree.
            /// </summary>
            public Offset<TextSpace> Length => IsSplit
                ? (Offset<TextSpace>)LivingChildren.Sum(c => c.Length.Value)
                : (Offset<TextSpace>)text.Length;

            private IReadOnlyList<Segment<OriginalSpace>> originalSegments;
            /// <summary>
            /// The <see cref="TokenBase.Provenance"/> of the replaced mapped token, inherited by every
            /// node in the tree since they are all parts of one transform of one source region, and
            /// preserved so that <see cref="TokenizedString{T,TVirtual}.TokenAt"/> can still resolve
            /// original-string positions to this virtual token. <see cref="Text"/> does not correspond
            /// to these segments character-for-character, which is why <see cref="MapToOriginal"/>
            /// throws. This answers where the tree came from, not which source character a node is.
            /// </summary>
            public IReadOnlyList<Segment<OriginalSpace>> Provenance => originalSegments ?? Parent.originalSegments;

            private bool removed = false;
            public bool IsRemoved => removed;


            /// <summary>
            /// always true for virtual tokens since they are created in the consumed collection and cannot be moved to unconsumed.
            /// </summary>
            public bool IsConsumed => true;

            public bool IsUnconsumed => false;

            
            private TVirtual[] children = null;
            public bool IsSplit => children != null;
            IToken[] IToken.Children => Children;
            /// <summary>
            /// All direct children from the most recent <see cref="Split"/>, including removed
            /// ones. Immutable snapshot of the split operation.
            /// </summary>
            public TVirtual[] Children => children?.ToArray() ?? Array.Empty<TVirtual>();
            public TVirtual[] LivingChildren => children?.Where(c => !c.IsRemoved).ToArray() ?? Array.Empty<TVirtual>();

            /// <summary>
            /// A live recursive view of the non-removed leaf nodes of this subtree: nodes with no
            /// children of their own that have not been removed. Formatters and display consumers
            /// should iterate this rather than <see cref="Children"/>.
            /// </summary>
            public TVirtual[] Leaves
            {
                get
                {
                    if (removed) return Array.Empty<TVirtual>();
                    if (!IsSplit) return new TVirtual[] { this as TVirtual };
                    var result = new List<TVirtual>();
                    foreach (var child in LivingChildren)
                    {
                        result.AddRange(child.Leaves);
                    }
                    return result.ToArray();
                }
            }

            IToken[] IToken.Leaves => Leaves.Cast<IToken>().ToArray();

            /// <summary>Constructs a virtual root node.</summary>
            public VirtualTokenBase(
                TokenizedString<T, TVirtual> source,
                T original,
                string text
            )
            {
                Source = source ?? throw new ArgumentNullException(nameof(source));
                mappedParent = original ?? throw new ArgumentNullException(nameof(original));
                Parent = null;
                this.text = text;
                start = original.Start;
                adjustedStart = original.AdjustedStart;
                originalSegments = original.Provenance;

                source.RegisterToken(this as TVirtual);
            }

            /// <summary>Constructs a virtual sub-node.</summary>
            public VirtualTokenBase(
                TokenizedString<T, TVirtual> source,
                TVirtual parent,
                string text)
            {
                Source = source ?? throw new ArgumentNullException(nameof(source));
                Parent = parent ?? throw new ArgumentNullException(nameof(parent));
                this.text = text;
                start = parent.Start;
                originalSegments = parent.Provenance;
                adjustedStart = Index<AdjustedSpace>.Missing;
            }

            public override string ToString()
            {
                return Text;
            }

            protected void GuardNotRemoved()
            {
                if (removed) throw new InvalidOperationException("Virtual token has been removed");
            }

            /// <summary>
            /// Not defined for virtual tokens: replacement text has no character-level correspondence
            /// to the original. <see cref="Provenance"/> answers the provenance question instead.
            /// </summary>
            public IReadOnlyList<Segment<OriginalSpace>> MapToOriginal(Segment<TextSpace> range)
                => throw new InvalidOperationException(
                    "Virtual tokens carry replacement text with no character-level mapping to the " +
                    "original string. Use Provenance for the provenance of this virtual tree.");

            /// <summary>
            /// Not defined for virtual tokens: replacement text has no character-level correspondence
            /// to the original. <see cref="Provenance"/> answers the provenance question instead.
            /// </summary>
            public Segment<TextSpace> MapToText(Segment<OriginalSpace> range)
                => throw new InvalidOperationException(
                    "Virtual tokens carry replacement text with no character-level mapping to the " +
                    "original string. Use Provenance for the provenance of this virtual tree.");

            public Segment<AdjustedSpace> MapToAdjusted(Segment<TextSpace> range)
            {
                GuardPositioned(AdjustedStart);
                GuardWithinText(range, Length);
                return new Segment<AdjustedSpace>(
                    (Index<AdjustedSpace>)AdjustedStart
                        + AsAdjusted(OffsetWithinRoot() + (range.Start - Index<TextSpace>.Zero)),
                    AsAdjusted(range.Length));
            }

            public Segment<TextSpace> MapToText(Segment<AdjustedSpace> range)
            {
                GuardPositioned(AdjustedStart);
                var result = new Segment<TextSpace>(
                    Index<TextSpace>.Zero
                        + AsText(range.Start - (Index<AdjustedSpace>)AdjustedStart)
                        - OffsetWithinRoot(),
                    AsText(range.Length));
                GuardWithinText(result, Length);
                return result;
            }

            /// <summary>
            /// This node's position within its root's live text, resolved on demand. Zero for a root.
            /// Not cached: an edit earlier in the tree invalidates every later node's offset.
            /// </summary>
            private Offset<TextSpace> OffsetWithinRoot()
            {
                if (IsRoot) return Offset<TextSpace>.Zero;

                var anchor = Leaves.FirstOrDefault();
                if (anchor == null) throw new InvalidOperationException(
                    "This virtual node has no live leaves, so it has no position within its root.");

                var node = this;
                while (!node.IsRoot) node = node.Parent;

                var accumulated = Offset<TextSpace>.Zero;
                foreach (var leaf in node.Leaves)
                {
                    if (ReferenceEquals(leaf, anchor)) return accumulated;
                    accumulated = accumulated + (Offset<TextSpace>)leaf.Length;
                }

                throw new InvalidOperationException(
                    "This virtual node is not reachable from its root's live leaves.");
            }

            /// <summary>
            /// Splits this virtual node into children. The parent node stays in the source
            /// collection (if root) or the virtual tree (if sub-node). Children are held
            /// internally and never added directly to source collections.
            /// </summary>
            public TVirtual[] Split(params string[] fragments)
            {
                GuardNotRemoved();
                if (fragments == null) throw new ArgumentNullException(nameof(fragments));
                if (fragments.Length == 0) throw new ArgumentException("Must have at least one token to split on.", nameof(fragments));

                List<TVirtual> newChildren = new List<TVirtual>();
                string workingText = text;
                int consumedLength = 0;

                foreach (var fragment in fragments)
                {
                    if (string.IsNullOrEmpty(fragment)) throw new ArgumentException("Fragments cannot be null or empty", nameof(fragments));

                    var fragmentIndex = workingText.IndexOf(fragment);
                    if (fragmentIndex == -1) throw new ArgumentException($"Fragment \"{fragment}\" not found in virtual token text \"{text}\"", nameof(fragments));

                    if (fragmentIndex > 0)
                        newChildren.Add(Source.virtualFactory(Source, this as IToken, workingText.Substring(0, fragmentIndex)));

                    newChildren.Add(Source.virtualFactory(Source, this as IToken, fragment));

                    consumedLength += fragmentIndex + fragment.Length;
                    workingText = workingText.Substring(fragmentIndex + fragment.Length);
                }

                if (workingText.Length > 0)
                    newChildren.Add(Source.virtualFactory(Source, this as IToken, workingText));

                children = newChildren.ToArray();

                // Root stays in source; ReNumber because Length may have effectively changed
                // if any children will later be removed.
                if (IsRoot) Source.ReNumber();

                return children;
            }

            /// <summary>
            /// This is a no-op for virtual nodes, as they are already consumed.
            /// will still throw if removed, as a fail-fast mechanism for the caller
            /// </summary>
            public TVirtual Consume()
            {
                GuardNotRemoved();
                return this as TVirtual;
            }

            /// <summary>
            /// Sets this leaf's stored text. Only valid on leaf nodes; throws if removed or split.
            /// <para>Unlike <see cref="TokenBase.Replace"/>, which is structural and rejects an empty
            /// replacement, this is content editing and accepts one. A blanked leaf is not a removed
            /// leaf: it stays in <see cref="Leaves"/>, contributes zero characters, does not trigger
            /// upward removal propagation, and can be repopulated by calling this again.</para>
            /// </summary>
            public TVirtual Replace(string text)
            {
                GuardNotRemoved();
                if (IsSplit) throw new InvalidOperationException("Node must be a leaf");
                this.text = text;
                return this as TVirtual;
            }

            /// <summary>
            /// Removes this virtual node.
            /// <list type="bullet">
            /// <item><b>Root:</b> removed from source consumed collection; <see cref="TokenizedString{T,TVirtual}.ReNumber"/> called.</item>
            /// <item><b>Sub-node:</b> marked removed within virtual tree; remains in <see cref="Children"/>
            /// as history. If this was the last live leaf, removal propagates up. If propagation
            /// reaches the root, the root is removed from source entirely.</item>
            /// </list>
            /// </summary>
            public TVirtual Remove()
            {
                // Idempotent, matching TokenBase.Remove.
                if (removed) return this as TVirtual;
                MarkRemoved();

                if (IsRoot)
                {
                    var consumedList = Source.consumed.Where(t => !ReferenceEquals(t, this)).ToList();
                    Source.UpdateTo(null, consumedList);
                }
                else
                {
                    if (Parent.Leaves.Length == 0)
                    {
                        Parent.Remove();
                    }
                }
                Source.ReNumber();
                return this as TVirtual;
            }

            //marks self as removed, then recurses on all children.
            private void MarkRemoved()
            {
                removed = true;
                foreach (var child in Children)
                {
                    child.MarkRemoved();
                }
            }
        }
    }

    public class TokenString : TokenizedString<Token, VirtualToken>
    {
        public TokenString(string fullString)
            : base(
                fullString,
                (source, original, text, start, segments) =>
                    new Token(
                        source as TokenString ?? throw new ArgumentException("Must be TokenString", nameof(source)),
                        original, text, start, segments),
                (source, original, text) =>
                {
                    var ts = source as TokenString ?? throw new ArgumentException("Expected TokenString instance", nameof(source));
                    if (original is Token mapped) return new VirtualToken(ts, mapped, text);
                    if (original is VirtualToken node) return new VirtualToken(ts, node, text);
                    throw new ArgumentException("Unrecognized type", nameof(original));
                })
        {
        }

        public class Token : TokenBase, IToken<Token, VirtualToken>, IToken
        {
            public Token(TokenString source, Token original, string text, Index<OriginalSpace> start, IEnumerable<Segment<OriginalSpace>> segments)
                : base(source, original, text, start, segments) { }
        }

        public class VirtualToken : VirtualTokenBase, IToken<VirtualToken, VirtualToken>, IToken
        {
            public VirtualToken(TokenString source, Token original, string text)
                : base(source, original, text) { }

            // Sub-node constructor
            public VirtualToken(TokenString source, VirtualToken parent, string text)
                : base(source, parent, text) { }
        }
    }
}