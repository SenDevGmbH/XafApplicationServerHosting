using DevExpress.Xpo.Metadata;
using System;

namespace SenDev.Xaf.ApplicationServerHosting.Web
{
    public class CustomCreateXPDictionaryEventArgs : EventArgs
    {
        public XPDictionary Dictionary { get; set; }
    }
}