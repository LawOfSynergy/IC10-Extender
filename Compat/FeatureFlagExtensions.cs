using System;
using System.Collections.Generic;

namespace IC10_Extender.Compat
{
    public static class FeatureFlagExtensions
    {
        public static CompatabilityCheck Equals<T>(this FeatureFlag<T> feature, T value, Action onFail = null)
        {
            return new CustomCheck(() => EqualityComparer<T>.Default.Equals(feature.Value, value), onFail);
        }

        public static CompatabilityCheck NotEquals<T>(this FeatureFlag<T> feature, T value, Action onFail = null)
        {
            return new CustomCheck(() => !EqualityComparer<T>.Default.Equals(feature.Value, value), onFail);
        }

        public static CompatabilityCheck IsEnabled(this FeatureFlag<bool> feature, Action onFail = null)
        {
            return new CustomCheck(() => feature.Value, onFail);
        }

        public static CompatabilityCheck IsDisabled(this FeatureFlag<bool> feature, Action onFail = null)
        {
            return new CustomCheck(() => !feature.Value, onFail);
        }

        public static CompatabilityCheck HasFlag<T>(this FeatureFlag<T> feature, T flag, Action onFail) where T : Enum
        {
            return new CustomCheck(() => feature.Value.HasFlag(flag), onFail);
        }

        public static CompatabilityCheck ExcludesFlag<T>(this FeatureFlag<T> feature, T flag, Action onFail) where T : Enum
        {
            return new CustomCheck(() => !feature.Value.HasFlag(flag), onFail);
        }
    }
}
