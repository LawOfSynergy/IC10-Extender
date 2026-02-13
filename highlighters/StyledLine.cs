using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace IC10_Extender.Highlighters
{
    public class Theme : IDictionary<string, string>
    {
        public static readonly Theme Vanilla = new Theme
        (
            "#A0A0A0",
            "#0080FFFF",
            "#20B2AA",
            "#00FFEC",
            "#585858FF",
            "#808080",
            "white",
            "yellow",
            "orange",
            "orange",
            "green",
            "purple",
            "red",
            "grey",
            "darkgrey"
        );

        private Dictionary<string, string> colors;

        public string Macro => this[nameof(Macro)];
        public string Register => this[nameof(Register)];
        public string Number => this[nameof(Number)];
        public string Network => this[nameof(Network)];
        public string Comment => this[nameof(Comment)];
        public string Help => this[nameof(Help)];
        public string String => this[nameof(String)];
        public string OpCode => this[nameof(OpCode)];
        public string LogicType => this[nameof(LogicType)];
        public string LogicSlotType => this[nameof(LogicSlotType)];
        public string Device => this[nameof(Device)];
        public string Jump => this[nameof(Jump)];
        public string Error => this[nameof(Error)];
        public string Description => this[nameof(Description)];
        public string Example => this[nameof(Example)];

        public ICollection<string> Keys => ((IDictionary<string, string>)colors).Keys;

        public ICollection<string> Values => ((IDictionary<string, string>)colors).Values;

        public int Count => ((ICollection<KeyValuePair<string, string>>)colors).Count;

        public bool IsReadOnly => false;

        public string this[string key] { 
            get => colors[key];
            set => Add(key, value);
        }

        public Theme(
            string macro,
            string register,
            string number,
            string network,
            string comment,
            string help,
            string str,
            string opCode,
            string logicType,
            string logicSlotType,
            string device,
            string jump,
            string error,
            string description,
            string example,
            Dictionary<string, string> additional = null
        )
        {
            this[nameof(Macro)] = macro;
            this[nameof(Register)] = register;
            this[nameof(Number)] = number;
            this[nameof(Network)] = network;
            this[nameof(Comment)] = comment;
            this[nameof(Help)] = help;
            this[nameof(String)] = str;
            this[nameof(OpCode)] = opCode;
            this[nameof(LogicType)] = logicType;
            this[nameof(LogicSlotType)] = logicSlotType;
            this[nameof(Device)] = device;
            this[nameof(Jump)] = jump;
            this[nameof(Error)] = error;
            this[nameof(Description)] = description;
            this[nameof(Example)] = example;
        }

        public bool ContainsKey(string key)
        {
            return colors.ContainsKey(key);
        }

        public void Add(string key, string value)
        {
            if (ContainsKey(key)) throw new ArgumentException($"{key} already exists in this theme"); //avoid accidental overriding
            colors.Add(key, value);
        }

        public void Override(string key, string value)
        {
            colors.Add(key, value);
        }

        public bool Remove(string key)
        {
            throw new InvalidOperationException("Entires cannot be removed from a theme");
        }

        public bool TryGetValue(string key, out string value)
        {
            return colors.TryGetValue(key, out value);
        }

        public void Add(KeyValuePair<string, string> item)
        {
            Add(item.Key, item.Value);
        }

        public void Override(KeyValuePair<string, string> item)
        {
            Override(item.Key, item.Value);
        }

        public void Clear()
        {
            throw new InvalidOperationException("Themes cannot be cleared");
        }

        public bool Contains(KeyValuePair<string, string> item)
        {
            return colors.Contains(item);
        }

        public void CopyTo(KeyValuePair<string, string>[] array, int arrayIndex)
        {
            ((ICollection<KeyValuePair<string, string>>)colors).CopyTo(array, arrayIndex);
        }

        public bool Remove(KeyValuePair<string, string> item)
        {
            return ((ICollection<KeyValuePair<string, string>>)colors).Remove(item);
        }

        public IEnumerator<KeyValuePair<string, string>> GetEnumerator()
        {
            return colors.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable)colors).GetEnumerator();
        }
    }

    public class StyledLine
    {
        public readonly Theme Theme;
        public readonly string Original;
        private readonly List<Token> remainder = new List<Token>();
        
        private readonly List<Token> Tokens = new List<Token>();

        public StyledLine(Theme theme, string original)
        {
            Theme = theme;
            Original = original;
            remainder.Add(new Token(this, original, 0, original.Length, null));
        }

        public void Consume(string token, string color)
        {
            if(TryConsume(token, color, 0, false, out var before, out var after, out var consumed, out var remaining))
            {
                Tokens.AddRange(consumed);
                remainder.Clear();
                remainder.AddRange(remaining);
                if(before.HasValue) remainder.Add(before.Value);
                if(after.HasValue) remainder.Add(after.Value);

                Tokens.Sort((a, b) => a.StartIndex.CompareTo(b.StartIndex));
                remainder.Sort((a, b) => a.StartIndex.CompareTo(b.StartIndex));
            }
        }

        public void ConsumeAll(string token, string color)
        {
            while (TryConsume(token, color, 0, false, out var before, out var after, out var consumed, out var remaining))
            {
                Tokens.AddRange(consumed);
                remainder.Clear();
                remainder.AddRange(remaining);
                if (before.HasValue) remainder.Add(before.Value);
                if (after.HasValue) remainder.Add(after.Value);
            }
            Tokens.Sort((a, b) => a.StartIndex.CompareTo(b.StartIndex));
            remainder.Sort((a, b) => a.StartIndex.CompareTo(b.StartIndex));
        }

        private bool TryConsume(string token, string color, int tokenIndex, bool inMatch, out Token? before, out Token? after, out List<Token> consumedTokens, out List<Token> remaining)
        {
            if (tokenIndex < 0) throw new ArgumentOutOfRangeException(nameof(tokenIndex));
            if (token == null) throw new ArgumentNullException(nameof(token));
            if (token.Length == 0) throw new ArgumentException("token must have at least one character");
            if (color == null) throw new ArgumentNullException(nameof(color));

            before = null;
            after = null;

            if (tokenIndex >= remainder.Count) //end of list, hard fail
            {
                consumedTokens = new List<Token>();
                remaining = remainder.ToList();
                return false;
            }

            var len = token.Length;
            var pattern = compile(token);

            var result = pattern.Match(remainder[tokenIndex].Text).Groups[0];

            if (result.Length == 0) // no match
            {
                if (!inMatch) //start again from next token, since we still have full token list
                {
                    return TryConsume(token, color, tokenIndex + 1, inMatch, out before, out after, out consumedTokens, out remaining);
                }

                //this attempt failed. backtrack and try again.
                consumedTokens = new List<Token>();
                remaining = remainder.ToList();
                return false;
            }

            if (result.Index > 0) // if partial match does not start at beginning
            {
                if(inMatch) // not a contiguous match
                {
                    consumedTokens = new List<Token>();
                    remaining = remainder.ToList();
                    return false;
                } else // start of new match, with before component
                {
                    var beforeText = remainder[tokenIndex].Text.Substring(0, result.Index);
                    before = new Token(this, beforeText, remainder[tokenIndex].StartIndex, beforeText.Length, null);
                }
            }

            if (result.Length == token.Length) // full match
            {
                if (result.Index + result.Length < remainder[tokenIndex].Length) //with after component
                {
                    var afterText = remainder[tokenIndex].Text.Substring(result.Index + result.Length);
                    after = new Token(this, afterText, remainder[tokenIndex].StartIndex + result.Index + result.Length, afterText.Length, null);
                }

                consumedTokens = new List<Token>
                {
                    new Token(this, result.Value, remainder[tokenIndex].StartIndex + result.Index, result.Length, color)
                };

                remaining = remainder.ToList();
                remaining.Remove(remainder[tokenIndex]);
                return true;
            }

            // else partial match, continue to next token
            if (!TryConsume(token.Substring(result.Length), color, tokenIndex + 1, true, out before, out after, out consumedTokens, out remaining))
            { //partial match did not complete
                if (inMatch) //backtrack
                {
                    before = null;
                    after = null;
                    consumedTokens = new List<Token>();
                    remaining = remainder.ToList();
                    return false; 
                }

                //else try to start new match from next component
                return TryConsume(token, color, tokenIndex + 1, false, out before, out after, out consumedTokens, out remaining);
            }

            //partial match completed, add this partial segment and remove from remaining
            consumedTokens.Add(new Token(this, result.Value, remainder[tokenIndex].StartIndex, result.Length, color));
            remaining.Remove(remainder[tokenIndex]);

            return true;
        }

        private Regex compile(string token)
        {
            string pattern = ""; //abcde
            foreach (var c in token.Reverse())
            {
                pattern = $"({c}{pattern})?";
            }
            return new Regex(pattern, RegexOptions.ExplicitCapture);
        }

        private string Concat(List<Token> tokens)
        {
            var result = "";
            tokens.ForEach(x => result += x);
            return result;
        }

        public string Remainder()
        {
            return Concat(remainder);
        }


        public List<Token> GetTokens()
        {
            return Tokens.ToList();
        }

        public string ToVanillaString()
        {
            return null; //TODO format the string with <color=value>text</color> format
        }
    }

    public readonly struct Token
    {
        public readonly StyledLine Parent;
        public readonly string Text;
        public readonly int StartIndex;
        public readonly int Length;
        public readonly string Color;

        public Token(StyledLine parent, string text, int startIndex, int length, string color)
        {
            Parent = parent;
            Text = text;
            StartIndex = startIndex;
            Length = length;
            Color = color;
        }
    }
}
