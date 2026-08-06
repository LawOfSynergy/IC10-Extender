using System;

namespace IC10_Extender.Highlight
{
    /// <summary>
    /// Marker interface for the phantom types that tag a coordinate space. Constrains
    /// <see cref="Index{TSpace}"/>, <see cref="Offset{TSpace}"/> and <see cref="Segment{TSpace}"/>
    /// so that only the declared spaces can be used.
    /// </summary>
    public interface ICoordinateSpace { }

    /// <summary>
    /// Absolute positions in the immutable source string. Provenance: where text came from.
    /// Never shifts.
    /// </summary>
    public struct OriginalSpace : ICoordinateSpace { }

    /// <summary>
    /// Positions in the current contiguous rendered string: the concatenation of all live tokens
    /// in Start order. Layout: where text currently appears. Recomputed after every mutation.
    /// </summary>
    public struct AdjustedSpace : ICoordinateSpace { }

    /// <summary>
    /// Positions within a single token's own text. Always relative to a specific token; the type
    /// does not record which one.
    /// </summary>
    public struct TextSpace : ICoordinateSpace { }

    /// <summary>
    /// Positions in the unconsumed text remaining to be processed: the concatenation of the
    /// unconsumed tokens, which is what <see cref="ITokenizedString{T,TVirtual}.Remainder"/> returns.
    /// Shifts as tokens are consumed, so a value must not be held across a mutation.
    /// </summary>
    public struct RemainderSpace : ICoordinateSpace { }

    /// <summary>
    /// A position in <typeparamref name="TSpace"/>. Distinct from <see cref="Offset{TSpace}"/>:
    /// adding two positions is meaningless and does not compile. Subtracting two positions yields
    /// the <see cref="Offset{TSpace}"/> between them.
    /// </summary>
    public readonly struct Index<TSpace> : IEquatable<Index<TSpace>>, IComparable<Index<TSpace>>
        where TSpace : ICoordinateSpace
    {
        public static readonly Index<TSpace> Zero = new Index<TSpace>(0);

        /// <summary>
        /// No position in this space: not found, or outside the string. Reported rather than thrown,
        /// so reading a position is always safe; operations that require one reject it.
        /// </summary>
        public static readonly Index<TSpace> Missing = new Index<TSpace>(-1);

        /// <summary>
        /// The position falls inside replacement text, which has no character-level counterpart in
        /// the space asked about. <see cref="IToken.Provenance"/> answers at token granularity.
        /// </summary>
        public static readonly Index<TSpace> Virtual = new Index<TSpace>(-2);

        /// <summary>
        /// The position falls in a region that was removed, so it exists in the source but has no
        /// place in the layout.
        /// </summary>
        public static readonly Index<TSpace> Removed = new Index<TSpace>(-3);

        /// <summary>
        /// True for a real position. Every "no position" value is negative, so prefer this over
        /// comparing against a particular sentinel.
        /// <para>Which sentinels an operation can actually return depends on the question asked, and
        /// the value cannot tell you which question produced it. See the operation's own
        /// documentation, and use <see cref="IsMissing"/>, <see cref="IsVirtual"/> and
        /// <see cref="IsRemoved"/> to branch on the reason.</para>
        /// </summary>
        public bool IsResolved => Value >= 0;

        /// <summary>Not found, or outside the string.</summary>
        public bool IsMissing => Value == Missing.Value;

        /// <summary>Inside replacement text, which has no counterpart in the space asked about.</summary>
        public bool IsVirtual => Value == Virtual.Value;

        /// <summary>In a region that was removed, so it has no place in the layout.</summary>
        public bool IsRemoved => Value == Removed.Value;

        public readonly int Value;

        public Index(int value)
        {
            Value = value;
        }

        public static explicit operator Index<TSpace>(int value) => new Index<TSpace>(value);
        public static explicit operator int(Index<TSpace> index) => index.Value;

        public static Index<TSpace> operator +(Index<TSpace> position, Offset<TSpace> displacement)
            => new Index<TSpace>(position.Value + displacement.Value);

        public static Index<TSpace> operator +(Offset<TSpace> displacement, Index<TSpace> position)
            => new Index<TSpace>(position.Value + displacement.Value);

        public static Index<TSpace> operator -(Index<TSpace> position, Offset<TSpace> displacement)
            => new Index<TSpace>(position.Value - displacement.Value);

        public static Offset<TSpace> operator -(Index<TSpace> to, Index<TSpace> from)
            => new Offset<TSpace>(to.Value - from.Value);

        public static bool operator ==(Index<TSpace> a, Index<TSpace> b) => a.Value == b.Value;
        public static bool operator !=(Index<TSpace> a, Index<TSpace> b) => a.Value != b.Value;
        public static bool operator <(Index<TSpace> a, Index<TSpace> b) => a.Value < b.Value;
        public static bool operator >(Index<TSpace> a, Index<TSpace> b) => a.Value > b.Value;
        public static bool operator <=(Index<TSpace> a, Index<TSpace> b) => a.Value <= b.Value;
        public static bool operator >=(Index<TSpace> a, Index<TSpace> b) => a.Value >= b.Value;

        /// <summary>An extent in this index's space, for building a range from here.</summary>
        public Offset<TSpace> By(int extent) => new Offset<TSpace>(extent);

        /// <summary>The range starting here and running for <paramref name="extent"/>.</summary>
        public Segment<TSpace> Spanning(Offset<TSpace> extent) => new Segment<TSpace>(this, extent);

        /// <summary>The range starting here and running for <paramref name="extent"/> characters.</summary>
        public Segment<TSpace> Spanning(int extent) => new Segment<TSpace>(this, new Offset<TSpace>(extent));

        /// <summary>The range from here up to, but not including, <paramref name="endExclusive"/>.</summary>
        public Segment<TSpace> Through(Index<TSpace> endExclusive) => Segment<TSpace>.FromBounds(this, endExclusive);

        /// <summary>Falls before the segment starts.</summary>
        public bool IsBefore(Segment<TSpace> segment) => this < segment.Start;

        /// <summary>Falls at or past the segment's exclusive end.</summary>
        public bool IsAfter(Segment<TSpace> segment) => this >= segment.End;

        public bool IsWithin(Segment<TSpace> segment) => segment.Contains(this);

        public bool Equals(Index<TSpace> other) => Value == other.Value;
        public override bool Equals(object obj) => obj is Index<TSpace> other && Equals(other);
        public override int GetHashCode() => Value;
        public int CompareTo(Index<TSpace> other) => Value.CompareTo(other.Value);
        public override string ToString() => Value.ToString();
    }

    /// <summary>
    /// A displacement within <typeparamref name="TSpace"/>, including extents and lengths.
    /// Space-bound because displacements are not interchangeable across spaces: advancing three
    /// characters of a token's text can advance more than three in original coordinates when the
    /// token's segments are disjoint.
    /// </summary>
    public readonly struct Offset<TSpace> : IEquatable<Offset<TSpace>>, IComparable<Offset<TSpace>>
        where TSpace : ICoordinateSpace
    {
        public static readonly Offset<TSpace> Zero = new Offset<TSpace>(0);

        public readonly int Value;

        public Offset(int value)
        {
            Value = value;
        }

        public static explicit operator Offset<TSpace>(int value) => new Offset<TSpace>(value);
        public static explicit operator int(Offset<TSpace> offset) => offset.Value;

        /// <summary>
        /// Reinterprets this extent in another space. Valid because character counts correspond
        /// one-to-one across spaces, so an extent is the same magnitude in each.
        /// <para>Deliberately absent from <see cref="Index{TSpace}"/>: positions do not carry across
        /// spaces and must be converted through the owning token or string.</para>
        /// </summary>
        public Offset<TOther> In<TOther>() where TOther : ICoordinateSpace => new Offset<TOther>(Value);

        public static Offset<TSpace> operator +(Offset<TSpace> a, Offset<TSpace> b)
            => new Offset<TSpace>(a.Value + b.Value);

        public static Offset<TSpace> operator -(Offset<TSpace> a, Offset<TSpace> b)
            => new Offset<TSpace>(a.Value - b.Value);

        public static Offset<TSpace> operator -(Offset<TSpace> offset)
            => new Offset<TSpace>(-offset.Value);

        public static bool operator ==(Offset<TSpace> a, Offset<TSpace> b) => a.Value == b.Value;
        public static bool operator !=(Offset<TSpace> a, Offset<TSpace> b) => a.Value != b.Value;
        public static bool operator <(Offset<TSpace> a, Offset<TSpace> b) => a.Value < b.Value;
        public static bool operator >(Offset<TSpace> a, Offset<TSpace> b) => a.Value > b.Value;
        public static bool operator <=(Offset<TSpace> a, Offset<TSpace> b) => a.Value <= b.Value;
        public static bool operator >=(Offset<TSpace> a, Offset<TSpace> b) => a.Value >= b.Value;

        /// <summary>The range of this extent, anchored at <paramref name="start"/>.</summary>
        public Segment<TSpace> From(Index<TSpace> start) => new Segment<TSpace>(start, this);

        public bool Equals(Offset<TSpace> other) => Value == other.Value;
        public override bool Equals(object obj) => obj is Offset<TSpace> other && Equals(other);
        public override int GetHashCode() => Value;
        public int CompareTo(Offset<TSpace> other) => Value.CompareTo(other.Value);
        public override string ToString() => Value.ToString();
    }

    /// <summary>
    /// A contiguous range in <typeparamref name="TSpace"/>: a <see cref="Index{TSpace}"/> anchor
    /// plus an <see cref="Offset{TSpace}"/> extent. <see cref="EndIndex"/> is exclusive.
    /// </summary>
    public readonly struct Segment<TSpace> : IEquatable<Segment<TSpace>>
        where TSpace : ICoordinateSpace
    {
        public readonly Index<TSpace> Start;
        public readonly Offset<TSpace> Length;

        /// <summary>Exclusive.</summary>
        public Index<TSpace> End => Start + Length;

        public Segment(Index<TSpace> start, Offset<TSpace> length)
        {
            Start = start;
            Length = length;
        }

        /// <summary>Builds a segment from an inclusive start and an exclusive end.</summary>
        public static Segment<TSpace> FromBounds(Index<TSpace> start, Index<TSpace> endExclusive)
            => new Segment<TSpace>(start, endExclusive - start);

        /// <summary>False when built on a sentinel start, which makes the whole range meaningless.</summary>
        public bool IsResolved => Start.IsResolved;

        public bool Contains(Index<TSpace> index) => Start <= index && index < End;

        public bool Contains(Segment<TSpace> other) => Start <= other.Start && other.End <= End;

        public bool Overlaps(Segment<TSpace> other) => Start < other.End && other.Start < End;

        public static bool operator ==(Segment<TSpace> a, Segment<TSpace> b) => a.Equals(b);
        public static bool operator !=(Segment<TSpace> a, Segment<TSpace> b) => !a.Equals(b);

        public bool Equals(Segment<TSpace> other) => Start == other.Start && Length == other.Length;
        public override bool Equals(object obj) => obj is Segment<TSpace> other && Equals(other);
        public override int GetHashCode() => unchecked((Start.Value * 397) ^ Length.Value);
        public override string ToString() => $"[{Start}..{End})";
    }

    /// <summary>
    /// A string whose positions are known to be in <typeparamref name="TSpace"/>. Declaring the space
    /// once at the source lets every index and extent derived from it carry that space without a cast.
    /// <para>Operations that rebase positions (<see cref="Substring(Segment{TSpace})"/>, case
    /// conversion) return <see cref="TextSpace"/>, since the result's positions are its own rather
    /// than the parent's. Operations that discard positions entirely return <see cref="string"/>.</para>
    /// <para>Converts implicitly to <see cref="string"/> for interop. Note that extension methods do
    /// not flow through user-defined conversions, so <see cref="string"/> extensions are not
    /// available on this type without unwrapping first.</para>
    /// </summary>
    public readonly struct CoordString<TSpace> : IEquatable<CoordString<TSpace>>
        where TSpace : ICoordinateSpace
    {
        public readonly string Value;

        public CoordString(string value)
        {
            Value = value;
        }

        public static implicit operator string(CoordString<TSpace> coord) => coord.Value;

        public Offset<TSpace> Length => (Offset<TSpace>)Value.Length;

        /// <summary>The whole string as a range, for containment checks against it.</summary>
        public Segment<TSpace> Bounds => new Segment<TSpace>(Index<TSpace>.Zero, Length);

        /// <summary>A position in this string's space. The space is inferred from this instance.</summary>
        public Index<TSpace> At(int position) => (Index<TSpace>)position;

        /// <summary>An extent in this string's space. The space is inferred from this instance.</summary>
        public Offset<TSpace> By(int extent) => (Offset<TSpace>)extent;

        public char this[Index<TSpace> position] => Value[position.Value];
        public char this[int position] => Value[position];

        #region slicing, which rebases into a new local space
        public CoordString<TextSpace> Substring(Segment<TSpace> range)
            => new CoordString<TextSpace>(Value.Substring(range.Start.Value, range.Length.Value));

        public CoordString<TextSpace> Substring(Index<TSpace> start, Offset<TSpace> length)
            => new CoordString<TextSpace>(Value.Substring(start.Value, length.Value));

        public CoordString<TextSpace> Substring(Index<TSpace> start, int length)
            => new CoordString<TextSpace>(Value.Substring(start.Value, length));

        public CoordString<TextSpace> Substring(int start, Offset<TSpace> length)
            => new CoordString<TextSpace>(Value.Substring(start, length.Value));

        public CoordString<TextSpace> Substring(int start, int length)
            => new CoordString<TextSpace>(Value.Substring(start, length));

        public CoordString<TextSpace> Substring(Index<TSpace> start)
            => new CoordString<TextSpace>(Value.Substring(start.Value));

        public CoordString<TextSpace> Substring(int start)
            => new CoordString<TextSpace>(Value.Substring(start));
        #endregion slicing

        #region searching, which yields positions in this string's space
        /// <summary>Returns <see cref="Index{TSpace}.Missing"/> when not found.</summary>
        public Index<TSpace> IndexOf(string value) => (Index<TSpace>)Value.IndexOf(value);
        public Index<TSpace> IndexOf(char value) => (Index<TSpace>)Value.IndexOf(value);
        public Index<TSpace> IndexOfAny(char[] anyOf) => (Index<TSpace>)Value.IndexOfAny(anyOf);
        public Index<TSpace> LastIndexOf(string value) => (Index<TSpace>)Value.LastIndexOf(value);
        public Index<TSpace> LastIndexOf(char value) => (Index<TSpace>)Value.LastIndexOf(value);
        public Index<TSpace> LastIndexOfAny(char[] anyOf) => (Index<TSpace>)Value.LastIndexOfAny(anyOf);
        #endregion searching

        #region predicates
        public bool Contains(string value) => Value.Contains(value);
        public bool Contains(CoordString<TSpace> value) => Value.Contains(value.Value);
        public bool StartsWith(string value) => Value.StartsWith(value);
        public bool StartsWith(CoordString<TSpace> value) => Value.StartsWith(value.Value);
        public bool EndsWith(string value) => Value.EndsWith(value);
        public bool EndsWith(CoordString<TSpace> value) => Value.EndsWith(value.Value);
        #endregion predicates

        #region whole-string transforms, which preserve positions in a new local space
        public CoordString<TextSpace> ToLower() => new CoordString<TextSpace>(Value.ToLower());
        public CoordString<TextSpace> ToUpper() => new CoordString<TextSpace>(Value.ToUpper());
        public CoordString<TextSpace> ToLowerInvariant() => new CoordString<TextSpace>(Value.ToLowerInvariant());
        public CoordString<TextSpace> ToUpperInvariant() => new CoordString<TextSpace>(Value.ToUpperInvariant());
        #endregion whole-string transforms

        /// <summary>Pieces start at differing offsets, so they carry no space.</summary>
        public string[] Split(params char[] separator) => Value.Split(separator);
        public string[] Split(char[] separator, StringSplitOptions options) => Value.Split(separator, options);

        public static bool operator ==(CoordString<TSpace> a, CoordString<TSpace> b) => a.Equals(b);
        public static bool operator !=(CoordString<TSpace> a, CoordString<TSpace> b) => !a.Equals(b);

        public bool Equals(CoordString<TSpace> other) => Value == other.Value;
        public override bool Equals(object obj) => obj is CoordString<TSpace> other && Equals(other);
        public override int GetHashCode() => Value?.GetHashCode() ?? 0;
        public override string ToString() => Value;
    }

    public static class CoordStringExtensions
    {
        /// <summary>Declares which coordinate space this string's positions are in.</summary>
        public static CoordString<TSpace> In<TSpace>(this string value) where TSpace : ICoordinateSpace
            => new CoordString<TSpace>(value);
    }
}
