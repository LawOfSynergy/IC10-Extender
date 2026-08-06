using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace IC10_Extender.Highlight
{
    public class Theme : IDictionary<string, Style>
    {
        public static readonly Theme Default = new Theme
        (
            // Only the required keys. Integrations are free to add any optional keys, or their own custom keys.
            //This is the minimum set of entries required to create a custom theme.
            new Dictionary<string, Style>()
            {
                // data types
                { nameof(String), new Style(nameof(String), null, "white") },
                { nameof(Number), new Style(nameof(Number), null, "#20B2AA") },
                { nameof(Enum), new Style(nameof(Enum), null, "orange") },

                // drcode parts
                { nameof(Device), new Style(nameof(Device), null, "green") },
                { nameof(Network), new Style(nameof(Network), null, "#00FFEC") },
                { nameof(Register), new Style(nameof(Register), null, "#0080FFFF") },

                // structural
                { nameof(Macro), new Style(nameof(Macro), null, "#A0A0A0") },
                { nameof(OpCode), new Style(nameof(OpCode), null, "yellow") },
                { nameof(DeprecatedOpCode), new Style(nameof(DeprecatedOpCode), null, "yellow", strikeThrough:true) },
                { nameof(Jump), new Style(nameof(Jump), null, "purple") },

                // error
                { nameof(Error), new Style(nameof(Error), null, "red") },

                // general text
                { nameof(Help), new Style(nameof(Help), null, "#808080") },
                { nameof(Comment), new Style(nameof(Comment), null, "#585858FF") },
                { nameof(Description), new Style(nameof(Description), null, "grey") },
                { nameof(Example), new Style(nameof(Example), null, "#585858FF", italic:true) },
            }
        );

        #region data types
        #region required
        public Style String => this[nameof(String)];
        public Style Number => this[nameof(Number)];
        public Style Enum => this[nameof(Enum)];
        public Style Jump => this[nameof(Jump)];
        #endregion required

        #region optional
        public Style Alias => this[nameof(Alias), nameof(String)];
        public Style Constant => this[nameof(Constant), nameof(String)];
        public Style Define => this[nameof(Define), nameof(String)];
        public Style Double => this[nameof(Double), nameof(Number)];
        public Style Long => this[nameof(Long), nameof(Number)];
        public Style Integer => this[nameof(Integer), nameof(Number)];
        public Style Short => this[nameof(Short), nameof(Number)];
        public Style Byte => this[nameof(Byte), nameof(Number)];
        public Style RefId => this[nameof(RefId), nameof(Number)];
        public Style DeviceHash => this[nameof(DeviceHash), nameof(Number)];
        public Style NameHash => this[nameof(NameHash), nameof(Number)];
        public Style SlotIndex => this[nameof(SlotIndex), nameof(Number)];
        public Style LogicType => this[nameof(LogicType), nameof(Enum)].WithLink("LogicType{0}");
        public Style LogicSlotType => this[nameof(LogicSlotType), nameof(Enum)].WithLink("LogicSlotType{0}");
        public Style BatchMode => this[nameof(BatchMode), nameof(Enum)];
        public Style ReagentMode => this[nameof(ReagentMode), nameof(Enum)].WithLink("Reagent{0}");
        #endregion optional
        #endregion data types

        #region drcode parts 
        public Style Device => this[nameof(Device)];
        public Style Network => this[nameof(Network)];
        public Style Register => this[nameof(Register)];
        #endregion drcode parts

        #region structural
        #region required
        public Style Macro => this[nameof(Macro)];
        public Style OpCode => this[nameof(OpCode)];
        public Style DeprecatedOpCode => this[nameof(DeprecatedOpCode)];
        #endregion required

        #region optional
        public Style Or => this[nameof(Or), nameof(Comment)];
        public Style VarName => this[nameof(VarName), nameof(String)];
        public Style VarParentheses => this[nameof(VarParentheses), nameof(String)];
        public Style VarBrackets => this[nameof(VarBrackets), nameof(String)];
        public Style AliasKey => this[nameof(AliasKey), nameof(String)];
        public Style DefineKey => this[nameof(DefineKey), nameof(String)];
        #endregion optional
        #endregion structural

        #region error
        public Style Error => this[nameof(Error)];
        public Style Warning => this[nameof(Warning), nameof(Error)];
        #endregion error

        #region general text
        public Style Help => this[nameof(Help)];
        public Style Comment => this[nameof(Comment)];
        public Style Description => this[nameof(Description)];
        public Style Example => this[nameof(Example)];
        #endregion general text

        public ICollection<string> Keys => styles.Keys;

        public ICollection<Style> Values => styles.Values;

        public int Count => styles.Count;

        public bool IsReadOnly => false;

        private readonly Dictionary<string, Style> styles;

        /// <summary>
        /// tries key. Throws KeyNotFoundException, ArgumentNullException
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public Style this[string key] { 
            get => styles[key];
            set => Add(key, value);
        }

        /// <summary>
        /// tries key, fallback key. Throws KeyNotFoundExecption, ArgumentNullException
        /// </summary>
        /// <param name="key"></param>
        /// <param name="fallbackKey"></param>
        /// <returns></returns>
        public Style this[string key, string fallbackKey]
        {
            get
            {
                if (styles.ContainsKey(key)) return styles[key];
                var fallback = styles[fallbackKey];
                return new Style(fallback, key, fallbackKey);
            }
        }

        /// <summary>
        /// tries lookup by other.Key, other.FallbackKey, identity
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public Style this[Style other]
        {
            get {
                if (!styles.TryGetValue(other.Key, out Style result))
                {
                    if (other.FallbackKey == null)
                        throw new KeyNotFoundException(
                            $"Style \"{other.Key}\" declares no fallback key, marking it a root key, but this theme " +
                            $"does not define it. Root keys are required in every theme; all other keys must declare " +
                            $"a fallback key (repeating the key itself is valid).");

                    if (!styles.TryGetValue(other.FallbackKey, out result))
                    {
                        return other;
                    }
                }
                return new Style(result, other.Key, other.FallbackKey, link:other.Link, error:other.Error, tooltip:other.Tooltip);
            }
        }

        /// <summary>
        /// if null, returns null. Otherwise tries lookup by other.Key, other.FallbackKey, identity
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public Style? this[Style? other]
        {
            get
            {
                if (!other.HasValue) return null;
                return this[other.Value];
            }
        }

        public Theme(
            Dictionary<string, Style> styles
        )
        {
            this.styles = styles ?? throw new ArgumentNullException(nameof(styles));
        }

        public bool ContainsKey(string key)
        {
            return styles.ContainsKey(key);
        }

        public void Add(string key, Style value)
        {
            if (ContainsKey(key)) throw new ArgumentException($"{key} already exists in this theme"); //avoid accidental overriding
            styles.Add(key, value);
        }

        public void Add(KeyValuePair<string, Style> item)
        {
            Add(item.Key, item.Value);
        }

        public void AddAll(Dictionary<string, Style> additional)
        {
            foreach (var kvp in additional)
            {
                Add(kvp.Key, kvp.Value);
            }
        }

        public void Override(string key, Style value)
        {
            styles.Add(key, value);
        }

        public void Override(KeyValuePair<string, Style> item)
        {
            Override(item.Key, item.Value);
        }

        public void OverrideAll(Dictionary<string, Style> additional)
        {
            foreach (var kvp in additional)
            {
                Override(kvp.Key, kvp.Value);
            }
        }

        public bool Remove(string key)
        {
            throw new InvalidOperationException("Entires cannot be removed from a theme");
        }

        public bool TryGetValue(string key, out Style value)
        {
            return styles.TryGetValue(key, out value);
        }

        public void Clear()
        {
            throw new InvalidOperationException("Themes cannot be cleared");
        }

        public bool Contains(KeyValuePair<string, Style> item)
        {
            return styles.Contains(item);
        }

        public void CopyTo(KeyValuePair<string, Style>[] array, int arrayIndex)
        {
            ((ICollection<KeyValuePair<string, Style>>)styles).CopyTo(array, arrayIndex);
        }

        public bool Remove(KeyValuePair<string, Style> item)
        {
            return ((ICollection<KeyValuePair<string, Style>>)styles).Remove(item);
        }

        public IEnumerator<KeyValuePair<string, Style>> GetEnumerator()
        {
            return styles.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable)styles).GetEnumerator();
        }
    }
}
