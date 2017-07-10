using DevExpress.Xpo;
using System;

namespace SenDev.Xaf.ApplicationServerHosting.Web
{
    public class CustomCreateDataLayerEventArgs : EventArgs
    {
        public IDataLayer DataLayer { get; set; }
    }
}