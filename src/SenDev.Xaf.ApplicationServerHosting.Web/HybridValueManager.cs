using DevExpress.ExpressApp.Web;
using DevExpress.Persistent.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;

namespace SenDev.Xaf.ApplicationServerHosting.Web
{
    public class HybridValueManager<TValue> : ASPSessionValueManagerBase, IValueManager<TValue>
    {
        private readonly string valueManagerKey;
        private IValueManager<TValue> sessionValueManager;
        private readonly IValueManager<TValue> serviceValueManager;
        public HybridValueManager(string key)
        {
            valueManagerKey = key;
            serviceValueManager = new MultiThreadValueManager<TValue>();

            EnsureSessionValueManager();
        }

        private void EnsureSessionValueManager()
        {
            if (sessionValueManager == null && HttpContext.Current?.Session != null)
                sessionValueManager = new ASPSessionValueManager<TValue>(valueManagerKey);
        }


        private IValueManager<TValue> UnderlyingManager
        {
            get
            {
                if (HttpContext.Current?.Session != null)
                {
                    EnsureSessionValueManager();
                    return sessionValueManager;
                }
                else
                    return serviceValueManager;
            }
        }




        public bool CanManageValue
        {
            get { return UnderlyingManager.CanManageValue; }
        }

        public TValue Value
        {
            get
            {
                return UnderlyingManager.Value;
            }
            set
            {
                UnderlyingManager.Value = value;
            }
        }

        public void Clear()
        {
            UnderlyingManager.Clear();
        }
    }
}
