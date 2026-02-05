using BepInEx.Configuration;
using System;
using System.Collections.Generic;

namespace IC10_Extender.Compat
{
    public static class ConfigExtensions
    {
        public const string Order = "Order";
        public const string DisplayName = "DisplayName";
        public const string RequireRestart = "RequireRestart";
        public const string Visible = "Visible";

        public static CompatibilityCheck Equals<T>(this ConfigEntry<T> feature, T value, Action onFail = null)
        {
            return new CustomCheck(() => EqualityComparer<T>.Default.Equals(feature.Value, value), onFail);
        }

        public static CompatibilityCheck NotEquals<T>(this ConfigEntry<T> feature, T value, Action onFail = null)
        {
            return new CustomCheck(() => !EqualityComparer<T>.Default.Equals(feature.Value, value), onFail);
        }

        public static CompatibilityCheck IsEnabled(this ConfigEntry<bool> feature, Action onFail = null)
        {
            return new CustomCheck(() => feature.Value, onFail);
        }

        public static CompatibilityCheck IsDisabled(this ConfigEntry<bool> feature, Action onFail = null)
        {
            return new CustomCheck(() => !feature.Value, onFail);
        }

        public static CompatibilityCheck HasFlag<T>(this ConfigEntry<T> feature, T flag, Action onFail) where T : Enum
        {
            return new CustomCheck(() => feature.Value.HasFlag(flag), onFail);
        }

        public static CompatibilityCheck ExcludesFlag<T>(this ConfigEntry<T> feature, T flag, Action onFail) where T : Enum
        {
            return new CustomCheck(() => !feature.Value.HasFlag(flag), onFail);
        }

        public static object[] WithTags(int order, string displayName, bool requireRestart = true, bool visible = true)
        {
            return new object[] {
                new KeyValuePair<string, int>(Order, order),
                new KeyValuePair<string, string>(DisplayName, displayName),
                new KeyValuePair<string, bool>(RequireRestart, requireRestart),
                new KeyValuePair<string, bool>(Visible, visible)
            };
        }
    }
}
