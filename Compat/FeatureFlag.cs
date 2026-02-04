using BepInEx.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IC10_Extender.Compat
{
    public class FeatureFlag<T>
    {
        private readonly ConfigEntry<T> value;
        private T initialValue;
        private readonly bool requireRestartOnChange;

        private bool requiresRestart = false;

        public event Action<FeatureFlag<T>> OnValueChanged;

        /**
         * The current value of this feature flag. If this flag requires a restart to take effect, this will return the initial value until a restart is performed.
         * You can set this value, and it will flag if a restart is required and store the pending value.
         */
        public T Value
        {
            get => requireRestartOnChange ? initialValue : value.Value;
            set
            {
                if (requireRestartOnChange && !EqualityComparer<T>.Default.Equals(initialValue, value))
                {
                    requiresRestart = true;
                }

                this.value.Value = value;
                if (!requiresRestart) { OnValueChanged(this); }
            }
            
        }

        /**
         * If this feature flag requires a restart to take effect, this returns what the value will be after restart.
         * Otherwise, it returns the same as Value.
         */
        public T PendingValue => value.Value;

        public bool RequireRestartOnChange => requireRestartOnChange;
        public bool RequiresRestart => requiresRestart;

        public FeatureFlag(ConfigEntry<T> value, bool requireRestartOnChange = false)
        {
            this.value = value;
            initialValue = value.Value;
            this.requireRestartOnChange = requireRestartOnChange;
        }
    }
}
