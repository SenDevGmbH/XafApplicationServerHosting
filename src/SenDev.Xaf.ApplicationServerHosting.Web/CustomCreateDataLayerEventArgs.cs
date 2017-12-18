using DevExpress.Xpo;
using DevExpress.Xpo.Metadata;
using System;

namespace SenDev.Xaf.ApplicationServerHosting.Web
{
    public class CustomCreateDataLayerEventArgs : EventArgs
    {
        public CustomCreateDataLayerEventArgs(XPDictionary dictionary)
        {
            Dictionary = dictionary;
        }
        public IDataLayer DataLayer { get; set; }

        public XPDictionary Dictionary { get; }
    }
}