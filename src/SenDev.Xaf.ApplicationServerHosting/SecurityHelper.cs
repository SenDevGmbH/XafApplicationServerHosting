using DevExpress.ExpressApp.Security;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SenDev.Xaf.ApplicationServerHosting
{
    public static class SecurityHelper
    {
        public static void AttachRequestProcessors(ISecurityStrategyBase security)
        {
            var strategy = security as SecurityStrategy;
            if (strategy != null)
            {
                strategy.CustomizeRequestProcessors += (s2, e2) =>
                {

                };
            }
        }
    }
}
