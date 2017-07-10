using DevExpress.ExpressApp.Security.ClientServer;
using System;
using DevExpress.ExpressApp.Security;

namespace SenDev.Xaf.ApplicationServerHosting.Web
{
    public class CustomCreateDataServerSecurityEventArgs : EventArgs
    {
        public Type UserType
        {
            get;
            internal set;
        }

        public Type RoleType
        {
            get;
            internal set;
        }

        public IDataServerSecurity Security
        {
            get;
            set;
        }

        public AuthenticationBase Authentication
        {
            get;
            internal set;
        }
    }
}