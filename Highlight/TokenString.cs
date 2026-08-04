using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Assets.Scripts.Util;
using IC10_Extender;
using IC10_Extender.Variables;
using static IC10_Extender.Highlight.TokenString;

namespace IC10_Extender.Highlight
{
    /// <summary>
    /// Represents a contiguous range in the original (unmodified) string.
    /// </summary>
    public readonly struct Segment
    {
        public int StartIndex { get; }
        public int Length { get; }
        public int EndIndex => StartIndex + Length;

        public Segment(int startIndex, int length)
        {
            StartIndex = startIndex;
            Length = length;
        }

        public override string ToString() => $"[{StartIndex}..{EndIndex})";
    }

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
        /// root's <see cref="StartIndex"/> so that sub-nodes can still be located within the source
        /// string. Sub-node ordering within a virtual token is positional in the parent's
        /// <see cref="Children"/> array, not by <see cref="StartIndex"/>.</para>
        /// </summary>
        int StartIndex { get; }

        /// <summary>
        /// The position of this token in the contiguous display string produced by
        /// <see cref="ITokenizedString{T}.ToString(Func{T,string})"/>.
        /// <para>For virtual sub-nodes, returns the root's <see cref="AdjustedStartIndex"/>.
        /// The setter throws for virtual sub-nodes since only <see cref="TokenizedString{T,TVirtual}.ReNumber"/>
        /// should assign this value, and it only operates on root-level tokens.</para>
        /// </summary>
        int AdjustedStartIndex { get; }

        /// <summary>
        /// Number of characters this token covers. For virtual tokens this reflects only the
        /// current live (non-removed) leaf content and changes as sub-nodes are removed or replaced.
        /// </summary>
        int Length { get; }

        /// <summary>
        /// The text content. For mapped tokens this is immutable after construction. For virtual
        /// tokens this is the concatenation of all non-removed leaf descendants and changes as
        /// sub-nodes are removed.
        /// </summary>
        string Text { get; }

        bool IsRemoved { get; }
        bool IsConsumed { get; }
        bool IsUnconsumed { get; }

        bool IsSplit { get; }

        /// <summary>
        /// The direct children produced by <see cref="Split"/>. Includes removed children — this is 
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
        /// The original-string segments this token covers.
        /// <para>For mapped tokens: one segment per source fragment in original-string order,
        /// non-overlapping and sorted by <see cref="Segment.StartIndex"/>.</para>
        /// <para>For virtual root tokens: the <see cref="OriginalSegments"/> of the mapped token
        /// that was replaced, preserved for positional lookup via
        /// <see cref="ITokenizedString{T}.AtIndex"/>. Note that <see cref="Text"/> no longer
        /// corresponds to these segments character-for-character.</para>
        /// <para>For virtual sub-nodes: always empty — sub-nodes have no original-string
        /// mapping.</para>
        /// </summary>
        IReadOnlyList<Segment> OriginalSegments { get; }
    
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
        /// is removed. The virtual root inherits the mapped token's <see cref="OriginalSegments"/>
        /// for positional lookup purposes; <see cref="Text"/> will no longer correspond to those
        /// segments character-for-character.
        /// </summary>
        TVirtual Replace(string replacement);
    }

    /// <summary>
    /// Read-only and segment-based access to a tokenized string. Implemented by all
    /// <see cref="TokenizedString{T,TVirtual}"/> variants including <see cref="StyledString"/>.
    /// Does not expose search or structural mutation operations other than <see cref="FromSegments"/>— those are on
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
        IToken FromSegments(IEnumerable<Segment> segments);

        /// <summary>
        /// Returns the token (consumed or unconsumed) whose <see cref="IToken{T}.OriginalSegments"/>
        /// contain <paramref name="index"/>. Returns <c>default</c> if no token covers that position.
        /// </summary>
        IToken AtIndex(int index);

        /// <summary>
        /// Returns the token whose contiguous display position contains <paramref name="index"/>.
        /// Returns <c>default</c> if no token covers that position.
        /// </summary>
        IToken AtAdjustedIndex(int index);

        string Remainder { get; }

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
    /// <see cref="StyledString"/> intentionally does <em>not</em> implement this interface — styled
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
        T First(string token, int startAt = 0, bool useTokenBounds = true);

        /// <summary>
        /// Walks the combined token sequence (unconsumed ∪ consumed) in <see cref="IToken.StartIndex"/>
        /// order. Returns an exact-match consumed token if one exists at the candidate position;
        /// otherwise builds a new token from the unconsumed collection. You may need to walk the
        /// object tree to get to the leaf tokens for further processing.
        /// </summary>
        IToken Locate(string token, int startAt = 0, bool useTokenBounds = true);
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

        public readonly string Original;

        protected readonly List<T> unconsumed = new List<T>();
        protected readonly List<IToken> consumed = new List<IToken>();

        /// <summary>
        /// factory: (source, original, text, startIndex, segments) -> T
        /// </summary>
        /// <remarks>Metadata outside of tokenization (e.g. styling) is expected to be carried over or not as makes sense for the extending subclass.</remarks>
        protected readonly Func<TokenizedString<T, TVirtual>, T, string, int, IEnumerable<Segment>, T> tokenFactory;

        /// <summary>
        /// factory: (source, original, text) -> TVirtual
        /// </summary>
        /// <remarks>
        /// <para>If original is a TokenBase, the virtual token will inherit its StartIndex and OriginalSegments, and Parent will remain null.
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
        /// Cache of tokens keyed by their <see cref="TokenBase.OriginalSegments"/> serialised as
        /// a canonical string. Populated on every token creation via <see cref="RegisterToken"/>
        /// and consulted by <see cref="FromSegments"/> before attempting to build from unconsumed
        /// text. Allows <see cref="FromSegments"/> to retrieve already-consumed or split tokens
        /// by position rather than by text, making it immune to text replacement and splits.
        /// </summary>
        private readonly Dictionary<string, IToken> segmentCache = new Dictionary<string, IToken>();

        public TokenizedString(
            string fullString,
            Func<TokenizedString<T, TVirtual>, T, string, int, IEnumerable<Segment>, T> tokenFactory,
            Func<TokenizedString<T, TVirtual>, IToken, string, TVirtual> virtualFactory)
        {
            this.tokenFactory = tokenFactory ?? throw new ArgumentNullException(nameof(tokenFactory));
            this.virtualFactory = virtualFactory ?? throw new ArgumentNullException(nameof(virtualFactory));
            unconsumed.Add(tokenFactory(this, null, fullString, 0, new List<Segment>() { new Segment(0, fullString.Length)}));
        }

        /// <summary>
        /// Registers a token in the segment cache. Called by <see cref="TokenBase"/> and
        /// <see cref="VirtualTokenBase"/> constructors so every token is reachable by segments
        /// from the moment of creation.
        /// </summary>
        internal void RegisterToken(IToken token)
        {
            var key = SegmentKey(token.OriginalSegments);
            if (!string.IsNullOrEmpty(key))
                segmentCache[key] = token;
        }

        private static string SegmentKey(IReadOnlyList<Segment> segments)
        {
            if (segments == null || segments.Count == 0) return null;
            return string.Join("|", segments.Select(s => $"{s.StartIndex}:{s.Length}"));
        }

        protected void UpdateTo(List<T> unconsumed = null, List<IToken> consumed = null)
        {
            if (unconsumed != null)
            {
                this.unconsumed.Clear();
                this.unconsumed.AddRange(unconsumed.OrderBy(t => t.StartIndex).ToList());
            }

            if (consumed != null)
            {
                this.consumed.Clear();
                this.consumed.AddRange(consumed.OrderBy(t => t.StartIndex).ToList());
            }
        }

        protected void AddConsumed(params IToken[] tokens)
        {
            consumed.AddRange(tokens);
            consumed.Sort((a, b) => a.StartIndex.CompareTo(b.StartIndex));
        }

        protected void AddUnconsumed(params T[] tokens)
        {
            unconsumed.AddRange(tokens);
            unconsumed.Sort((a, b) => a.StartIndex.CompareTo(b.StartIndex));
        }

        /// <summary>
        /// Recomputes <see cref="TokenBase.AdjustedStartIndex"/> for all tokens (mapped and virtual
        /// roots) based on <see cref="TokenBase.StartIndex"/> order. Virtual sub-nodes are not
        /// present in either collection and are not numbered directly; they delegate to their root.
        /// </summary>
        protected void ReNumber()
        {
            int index = 0;
            var sorted = unconsumed.Cast<IToken>().Concat(consumed).OrderBy(t => t.StartIndex).ToList();
            foreach (IToken token in sorted)
            {
                if (token is TokenBase mapped) { 
                    mapped.AdjustedStartIndex = index;
                    index += mapped.Length;
                }
                if (token is VirtualTokenBase virtualRoot)
                {
                    virtualRoot.AdjustedStartIndex = index;
                    index += virtualRoot.Length;
                }
            }
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

        protected bool Search<Type>(
            List<Type> domain,
            string token,
            int index,
            int startAt,
            bool containsConsumed,
            bool useTokenBounds,
            bool inSpan,
            out int resultStartIndex,
            out List<Tuple<string, Type>> results
        )
            where Type : IToken
        {
            Log.Debug($"Search(token:\"{token}\", index:{index}, startAt:{startAt}, inSpan:{inSpan}");

            if (domain == null) throw new ArgumentNullException(nameof(domain));

            if (token == null) throw new ArgumentNullException(nameof(token));
            if (string.IsNullOrEmpty(token))
            {
                results = new List<Tuple<string, Type>>();
                resultStartIndex = -1;
                return false;
            }

            if (index < 0) throw new ArgumentOutOfRangeException(nameof(index));
            if (index >= domain.Count) //end of list, hard fail
            {
                Log.Debug("End of list, hard fail");
                results = new List<Tuple<string, Type>>();
                resultStartIndex = -1;
                return false;
            }

            if (startAt < 0) throw new ArgumentOutOfRangeException(nameof(startAt), "Cannot be negative");

            if (startAt > 0)
            {
                var startToken = domain.Where(t => t.StartIndex < startAt).Last();
                if (index < startToken.StartIndex)
                {
                    Log.Debug($"Seeking to {startAt}");
                    return Search(domain, token, startToken.StartIndex, startAt, containsConsumed, useTokenBounds, inSpan, out resultStartIndex, out results);
                }
                startAt = startAt > domain[index].StartIndex ? startAt - startToken.StartIndex : 0;
            }

            if(containsConsumed && consumed.Contains(domain[index]))
            {
                if (inSpan)
                { 
                    //continue to next non-consumed token, since we're in a span and consumed tokens can't be part of a span match
                    Log.Debug("In span and token is consumed, skipping to next non-consumed token");
                    return Search(domain, token, index + 1, startAt, containsConsumed, useTokenBounds, inSpan, out resultStartIndex, out results);
                }

                if (domain[index].Text == token)
                {
                    //exact match, return immediately with this token as the only result
                    Log.Debug("Exact match with consumed token, returning immediately");
                    results = new List<Tuple<string, Type>>() { new Tuple<string, Type>(domain[index].Text, domain[index]) };
                    resultStartIndex = domain[index].StartIndex;
                    return true;
                }

                // consumed tokens (including virtual roots) are never eligible for partial or
                // substring matches since they cannot be restructured by Split() or Remove()
                // without breaking invariants.
                Log.Debug("Consumed token is not an exact match, skipping");
                return Search(domain, token, index + 1, startAt, containsConsumed, useTokenBounds, inSpan, out resultStartIndex, out results);
            }

            var pattern = Compile(token, useTokenBounds && !inSpan, useTokenBounds);

            for (var result = pattern.Match(domain[index].Text, startAt); result.Success; result = pattern.Match(domain[index].Text, result.Index + 1))
            {
                var match = result.Groups["match"];

                Log.Debug($"Match(Value:{match.Value}, Index:{match.Index}, Length:{match.Length})");
                if (match.Index != 0) //doesn't start at beginning of token
                {
                    Log.Debug("Doesn't start at beginning of token");
                    if (inSpan) //then not a contiguous match, return false
                    {
                        Log.Debug("Not a contiguous match, returning false");
                        results = new List<Tuple<string, Type>>();
                        resultStartIndex = -1;
                        return false;
                    }

                    // else new start of match, starting in middle of token
                }

                if (match.Length == token.Length) // full match
                {
                    Log.Debug("Full match");
                    results = new List<Tuple<string, Type>>() { new Tuple<string, Type>(match.Value, domain[index]) };
                    resultStartIndex = domain[index].StartIndex + match.Index;
                    return true;
                }

                // else partial match only
                Log.Debug("Partial match only");
                if (match.Index + match.Length != domain[index].Length) //does not run up to end
                {
                    Log.Debug("Does not run to end of token");
                    if (inSpan) // partial match ends without continuing to next token. Return span failure
                    {
                        Log.Debug("Partial match ends without continuing to next token");
                        results = new List<Tuple<string, Type>>();
                        resultStartIndex = -1;
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
                var spanSuccess = Search(domain, token.Substring(match.Length), index + 1, startAt, containsConsumed, useTokenBounds, true, out resultStartIndex, out results);
                if (spanSuccess)
                {
                    Log.Debug("Span successful");
                    results.Add(new Tuple<string, Type>(match.Value, domain[index]));
                    resultStartIndex = domain[index].StartIndex + match.Index; //override with earlier start index
                    return true;
                }

                //else span unsuccessful, continue to next potential match
                Log.Debug("Span unsuccessful. Continuing to next potential match");
                continue;
            }

            //potential matches exhausted, recursing on next token
            Log.Debug("Exhausted potential matches in token. Recursing on next token");
            return Search(domain, token, index + 1, startAt, containsConsumed, useTokenBounds, false, out resultStartIndex, out results);
        }

        protected T BuildToken(string token, int resultStartIndex, List<Tuple<string, T>> results)
        {
            var segments = new List<Segment>(results.Count);
            foreach (var tuple in results)
            {
                (string match, T t) = tuple;
                int offsetInToken = t.Length - match.Length;
                segments.Add(new Segment(t.StartIndex + offsetInToken, match.Length));
                if (t.Text == match) t.Remove();
                else t.Split(match)
                        .First(frag => frag.Text == match)
                        .Remove();
            }
            var result = tokenFactory(this, null, token, resultStartIndex, segments);
            AddUnconsumed(result);
            return result;
        }

        public T First(string token, int startAt = 0, bool useTokenBounds = true)
        {
            GuardTokenized();

            if (Search(unconsumed, token, 0, startAt, false, useTokenBounds, false, out var resultStartIndex, out var results))
            {
                results.Reverse();
                return BuildToken(token, resultStartIndex, results);
            }
            return default;
        }

        public IToken Locate(string token, int startAt = 0, bool useTokenBounds = true)
        {
            GuardTokenized();

            var allByPosition = consumed.Concat(unconsumed.Cast<IToken>())
                .Where(t => t.StartIndex >= startAt)
                .OrderBy(t => t.StartIndex)
                .ToList();

            if (Search(allByPosition, token, 0, startAt, true, useTokenBounds, false, out var resultStartIndex, out var results))
            {
                results.Reverse();
                var firstMatch = results.FirstOrDefault();
                if (firstMatch != null && consumed.Contains(firstMatch.Item2) && firstMatch.Item1 == token)
                {
                    return firstMatch.Item2;
                }
                if (results.Count > 0)
                {
                    return BuildToken(token, resultStartIndex, results.Select(t => new Tuple<string, T>(t.Item1, (T)t.Item2)).ToList());
                }
            }
            return default;
        }

        public IToken AtIndex(int index)
        {
            return unconsumed.Cast<IToken>().Concat(consumed)
                .FirstOrDefault(t => t.OriginalSegments.Any(s => s.StartIndex <= index && index < s.EndIndex));
        }

        public IToken AtAdjustedIndex(int index)
        {
            return unconsumed.Cast<IToken>().Concat(consumed)
                .FirstOrDefault(t => t.AdjustedStartIndex <= index && index < t.AdjustedStartIndex + t.Length);
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
        public IToken FromSegments(IEnumerable<Segment> segments)
        {
            if (segments == null) throw new ArgumentNullException(nameof(segments));

            var segList = segments.OrderBy(s => s.StartIndex).ToList();
            if (segList.Count == 0) throw new ArgumentException("Segments cannot be empty", nameof(segments));
            if (segList.Any(s => s.Length == 0)) throw new ArgumentException($"Segments cannot have zero length: {segList.First(s => s.Length == 0)}", nameof(segments));

            // consult the cache
            var key = SegmentKey(segList);
            if (segmentCache.TryGetValue(key, out var cached))
                return cached;

            // fallback to building the token
            Dictionary<T, List<Segment>> mappings = new Dictionary<T, List<Segment>>();
            for (int i = 0; i < segList.Count; i++)
            {
                var segment = segList[i];

                if (segment.StartIndex < 0 || segment.EndIndex > Original.Length)
                    throw new ArgumentException(
                        $"Segment {segment} is out of bounds of the original string (length {Original.Length}).",
                        nameof(segments));

                if (i > 0)
                {
                    var prev = segList[i - 1];
                    if (prev.EndIndex > segment.StartIndex)
                        throw new ArgumentException(
                            $"Segments {prev} and {segment} overlap.",
                            nameof(segments));
                }

                var covering = unconsumed.FirstOrDefault(
                    t => t.OriginalSegments.Any(
                        s => s.StartIndex <= segment.StartIndex && s.EndIndex >= segment.EndIndex
                    )
                );

                if (covering == null)
                {
                    if (consumed.Any(
                            t => t.OriginalSegments.Any(
                                s => s.StartIndex <= segment.StartIndex && s.EndIndex >= segment.EndIndex
                            )
                        ))
                        throw new InvalidOperationException(
                            $"Segment {segment} is covered by a consumed (mutated) token. " +
                            $"Acquire the consumed token directly and use Split() to specify segmentation.");
                    else
                        throw new InvalidOperationException(
                            $"Segment {segment} is not covered by any token. It may have been removed.");
                }

                if (!mappings.ContainsKey(covering)) mappings[covering] = new List<Segment>();
                mappings[covering].Add(segment);
            }

            foreach (var kvp in mappings)
            {
                var token = kvp.Key;
                var fragments = kvp.Value.Select(s => Original.Substring(s.StartIndex, s.Length)).ToArray();
                foreach (var fragment in token.Split(fragments))
                {
                    if (fragments.Any(s => s == fragment.Text)) fragment.Remove();
                }
            }

            var result = tokenFactory(
                this,
                null,
                string.Join("", segList.Select(seg => Original.Substring(seg.StartIndex, seg.Length))),
                segList.First().StartIndex,
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

            return Consumed;
        }

        protected string ToString(List<IToken> tokens, Func<IToken, string> formatter)
        {
            var sorted = tokens.ToList();
            StringBuilder sb = new StringBuilder();
            foreach (T token in sorted)
            {
                sb.Append(formatter(token));
            }
            return sb.ToString();
        }

        public string ToString(Func<IToken, string> formatter)
        {
            return ToString(unconsumed.Cast<IToken>().Concat(consumed).OrderBy(t => t.StartIndex).ToList(), formatter);
        }

        public override string ToString()
        {
            return ToString(RawFormat);
        }

        public string Remainder => ToString(unconsumed.Cast<IToken>().ToList(), t => t.Text);

        public class TokenBase : IToken<T, TVirtual>
        {
            public TokenizedString<T, TVirtual> Source { get; private set; }
            /// <summary>
            /// The position of this token in the <em>original</em> string. Never changes after
            /// construction. For span-tokens covering multiple original ranges, this is the
            /// start of the first segment. Use <see cref="OriginalSegments"/> for complete coverage.
            /// </summary>
            public int StartIndex { get; private set; }

            private int adjustedStartIndex;

            /// <summary>
            /// The position of this token in the contiguous display string. Recomputed by
            /// <see cref="TokenizedString{T,TVirtual}.ReNumber"/> after every mutating operation.
            /// </summary>
            public int AdjustedStartIndex
            {
                get
                {
                    GuardNotRemoved();
                    return adjustedStartIndex;
                }
                set
                {
                    GuardNotRemoved();
                    adjustedStartIndex = value;
                }
            }

            public int Length { get; private set; }

            /// <summary>
            /// The text content of this token. Immutable after construction for mapped tokens.
            /// </summary>
            public string Text { get; private set; }

            /// <summary>
            /// The original-string segments this token covers. For simple tokens always a single
            /// segment. For span-tokens one entry per source fragment.
            /// </summary>
            public IReadOnlyList<Segment> OriginalSegments { get; }

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

            public TokenBase(TokenizedString<T, TVirtual> source, IToken parent, string text, int startIndex, IEnumerable<Segment> segments)
            {
                Source = source ?? throw new ArgumentNullException(nameof(source));
                Parent = parent;
                Text = text;
                Length = text.Length;

                var segList = segments != null
                    ? segments.OrderBy(s => s.StartIndex).ToList()
                    : new List<Segment> { new Segment(startIndex, text.Length) };

                OriginalSegments = segList.AsReadOnly();
                StartIndex = segList[0].StartIndex;
                adjustedStartIndex = StartIndex;

                source.RegisterToken(this as T);
            }

            public override string ToString()
            {
                var segStr = string.Join(",", OriginalSegments.Select(s => s.ToString()));
                return $"{nameof(TokenBase)}(Source:{Source.GetType().Name}, Text:\"{Text}\", StartIndex:{StartIndex}, AdjustedStartIndex:{AdjustedStartIndex}, Length:{Length}, Segments:[{segStr}])";
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
            /// Returns the sub-segments covering [textOffset, textOffset+length) within this
            /// token's Text, mapping through OriginalSegments for span-tokens.
            /// </summary>
            protected List<Segment> SliceSegments(int textOffset, int length)
            {
                var result = new List<Segment>();
                int remaining = length;
                int cursor = 0;

                foreach (var seg in OriginalSegments)
                {
                    int segEnd = cursor + seg.Length;
                    int overlapStart = Math.Max(cursor, textOffset);
                    int overlapEnd = Math.Min(segEnd, textOffset + length);

                    if (overlapStart < overlapEnd)
                    {
                        int offsetInSeg = overlapStart - cursor;
                        result.Add(new Segment(seg.StartIndex + offsetInSeg, overlapEnd - overlapStart));
                        remaining -= overlapEnd - overlapStart;
                        if (remaining <= 0) break;
                    }

                    cursor = segEnd;
                }

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
                        int preStart = StartIndex + consumedLength;
                        newTokens.Add(Source.tokenFactory(Source, this as T, workingText.Substring(0, fragmentIndex), preStart, SliceSegments(consumedLength, fragmentIndex)));
                    }

                    int fragStart = StartIndex + consumedLength + fragmentIndex;
                    newTokens.Add(Source.tokenFactory(Source, this as T, fragment, fragStart, SliceSegments(consumedLength + fragmentIndex, fragment.Length)));
                    consumedLength += fragmentIndex + fragment.Length;
                    workingText = workingText.Substring(fragmentIndex + fragment.Length);
                }

                if (workingText.Length > 0)
                {
                    int tailStart = StartIndex + consumedLength;
                    newTokens.Add(Source.tokenFactory(Source, this as T, workingText, tailStart, SliceSegments(consumedLength, workingText.Length)));
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
            /// <see cref="StartIndex"/> and <see cref="OriginalSegments"/> from this token for
            /// positional lookup. The virtual root and all children are inherently considered consumed.
            /// </summary>
            public TVirtual Replace(string replacement)
            {
                GuardNotRemoved();
                GuardExistsInSource();
                if (string.IsNullOrEmpty(replacement)) throw new ArgumentException("Replacement cannot be null or empty. Call Remove() instead", nameof(replacement));

                var virtualToken = Source.virtualFactory(Source, this as T, replacement);
                // Remove this mapped token and add the virtual root to consumed so that
                // Search/Locate can find it via exact-match and AtIndex can resolve it.
                var consumedList = Source.consumed.Where(t => !ReferenceEquals(t, this)).ToList();
                var unconsumedList = Source.unconsumed.Where(t => !ReferenceEquals(t, this)).ToList();
                Source.UpdateTo(unconsumedList, consumedList);
                removed = true;

                Source.AddConsumed(virtualToken as T);
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
        /// <see cref="OriginalSegments"/> for positional lookup.
        /// <para>
        /// A virtual token is a <em>root</em> when it exists directly in the source string's
        /// <see cref="TokenizedString{T,TVirtual}.consumed"/> collection, or a <em>sub-node</em>
        /// when held within another virtual token's internal child tree.
        /// </para>
        /// <para><b>Text / Length:</b> Computed from live (non-removed) leaf descendants.</para>
        /// <para><b>StartIndex / AdjustedStartIndex:</b> Sub-nodes delegate to the root's values
        /// since sub-node ordering is positional within <see cref="Children"/>, not by index.
        /// The <c>AdjustedStartIndex</c> setter throws for sub-nodes.</para>
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
            private readonly int startIndex;

            /// <summary>
            /// For root nodes: the original-string position of the replaced mapped token.
            /// For sub-nodes: delegates to the root's value.
            /// </summary>
            public int StartIndex
            {
                get
                {
                    if (IsRoot) return startIndex;
                    return Parent.StartIndex;
                }
            }

            private int adjustedStartIndex;

            /// <summary>
            /// For root nodes: the contiguous display-string position, set by
            /// <see cref="TokenizedString{T,TVirtual}.ReNumber"/>.
            /// For sub-nodes: delegates to the root's value. The setter throws for sub-nodes.
            /// </summary>
            public int AdjustedStartIndex
            {
                get
                {
                    if (IsRoot)
                    {
                        GuardNotRemoved();
                        return adjustedStartIndex;
                    }
                    return Parent.AdjustedStartIndex;
                }
                set
                {
                    if (!IsRoot) throw new InvalidOperationException(
                        "AdjustedStartIndex cannot be set on virtual sub-nodes. ReNumber operates on root nodes only.");
                    GuardNotRemoved();
                    adjustedStartIndex = value;
                }
            }

            private string text;
            /// <summary>
            /// The live text of this node: concatenation of all non-removed leaf descendants.
            /// Returns <see cref="string.Empty"/> if removed or if all leaves are removed.
            /// </summary>
            public string Text
            {
                get
                {
                    if (removed) return string.Empty;
                    return IsSplit ? string.Join("", LivingChildren.Select(c => c.Text)) : text;
                }
            }

            /// <summary>
            /// The number of live (non-removed) characters in this node's subtree.
            /// </summary>
            public int Length => IsSplit ? LivingChildren.Sum(c => c.Length) : text.Length;

            private IReadOnlyList<Segment> originalSegments;
            /// <summary>
            /// For root nodes: the <see cref="TokenBase.OriginalSegments"/> of the replaced mapped
            /// token, preserved so that <see cref="TokenizedString{T,TVirtual}.AtIndex"/> can still
            /// resolve original-string positions to this virtual token. <see cref="Text"/> does not
            /// correspond to these segments character-for-character.
            /// For sub-nodes: always empty.
            /// </summary>
            public IReadOnlyList<Segment> OriginalSegments => originalSegments ?? Parent.originalSegments;

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
            /// A live recursive view of the non-removed leaf nodes of this subtree — nodes with no
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
                startIndex = original.StartIndex;
                adjustedStartIndex = original.AdjustedStartIndex;
                originalSegments = original.OriginalSegments;

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
                startIndex = parent.StartIndex;
                originalSegments = parent.OriginalSegments;
                adjustedStartIndex = -1;
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
            /// This operation is only valid on leaf nodes. Sets this node's internal text.
            /// throws if removed or not a leaf node.
            /// </summary>
            /// <param name="text"></param>
            /// <returns></returns>
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
                GuardNotRemoved();
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
                (source, original, text, startIndex, segments) =>
                    new Token(
                        source as TokenString ?? throw new ArgumentException("Must be TokenString", nameof(source)),
                        original, text, startIndex, segments),
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
            public Token(TokenString source, Token original, string text, int startIndex, IEnumerable<Segment> segments)
                : base(source, original, text, startIndex, segments) { }
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