using System;

namespace IC10_Extender.Compat
{
    public class CustomCheck : CompatabilityCheck
    {
        private Func<bool> accept;
        private Action onFail;

        public CustomCheck(Func<bool> accept, Action onFail = null)
        {
            this.accept = accept ?? throw new ArgumentNullException("accept");
            this.onFail = onFail;
        }

        public override bool Accept()
        {
            return accept();
        }

        public override void OnFail()
        {
            onFail?.Invoke();
        }
    }
}
