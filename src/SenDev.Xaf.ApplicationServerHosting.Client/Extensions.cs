using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Xpo;
using DevExpress.Xpo.Helpers;
using System;

namespace SenDev.Xaf.ApplicationServerHosting
{
    public static class Extensions
    {
        public static T GetServerSideMethods<T>(this ICommandChannel commandChannel)
        {
            return (T)new RemoteCommandProxy(commandChannel, typeof(T)).GetTransparentProxy();
        }

        public static T GetServerSideMethods<T>(this IObjectSpace objectSpace)
        {
            return ((XPObjectSpace)objectSpace).Session.GetServerSideMethods<T>();
        }
        public static T CreateObject<T>(this AppDomain domain) where T : MarshalByRefObject
        {
            Type type = typeof(T);
            return (T)domain.CreateInstanceAndUnwrap(type.Assembly.FullName, type.FullName);
        }
    }
}