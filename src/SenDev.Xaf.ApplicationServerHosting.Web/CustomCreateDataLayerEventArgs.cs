using DevExpress.Xpo;
using DevExpress.Xpo.Metadata;
using System;

namespace SenDev.Xaf.ApplicationServerHosting.Web
{
    public class CustomCreateDataLayerEventArgs : EventArgs
    {
        public CustomCreateDataLayerEventArgs(XPDictionary dictionary, DevExpress.Xpo.DB.IDataStore dataStore)
        {
            Dictionary = dictionary;
            DataStore = dataStore;
        }
        public IDataLayer DataLayer { get; set; }

        public XPDictionary Dictionary { get; }
        public DevExpress.Xpo.DB.IDataStore DataStore { get; }
    }
}