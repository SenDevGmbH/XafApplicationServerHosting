using System;
using System.Configuration;
using DevExpress.ExpressApp.Security;
using DevExpress.ExpressApp.Security.ClientServer;
using DevExpress.ExpressApp.Security.ClientServer.Wcf;
using DevExpress.ExpressApp.Security.Strategy;
using DevExpress.ExpressApp.Web;
using DevExpress.ExpressApp.Xpo;
using DevExpress.Persistent.Base;
using DevExpress.Xpo;
using DevExpress.Xpo.DB;
using DevExpress.Xpo.Metadata;

namespace SenDev.Xaf.ApplicationServerHosting.Web
{
    public abstract class ApplicationServerBase
    {
        private static bool webApplicationInitialized;
        private WcfSecuredDataServer server;

        private static IDataStore dataStore;
        private static readonly object webApplicationInitLock = new object();


        public static event EventHandler<CustomCreateDataStoreEventArgs> CustomCreateDataStore;
        public static event EventHandler<CustomCreateDataServerSecurityEventArgs> CustomCreateDataServerSecurity;
        public static event EventHandler<CustomCreateDataLayerEventArgs> CustomCreateDataLayer;
        public static event EventHandler<CustomCreateXPDictionaryEventArgs> CustomCreateXPDictionary;


        public static bool UseThreadSafeDataLayer { get; set; }

        public static Func<AuthenticationBase> AuthenticationCreator
        {
            get;
            set;
        }

        public static Type UserType
        {
            get;
            set;
        }

        public static bool UseDataStorePool { get; set; } = true;

        public static Type RoleType
        {
            get;
            set;
        }

        public static Type WebApplicationType
        {
            get;
            set;
        }


        public static string ConnectionStringName { get; set; } = "ConnectionString";

        protected WcfSecuredDataServer Server
        {
            get
            {
                if (server == null)
                {
                    server = new WcfSecuredDataServer(CreateDataServer());
                }
                return server;
            }
        }


        private static void EnsureWebApplicationInitialized()
        {
            if (!webApplicationInitialized && WebApplicationType != null)
            {
                lock (webApplicationInitLock)
                {
                    if (!webApplicationInitialized)
                    {
                        EnsureValueManager();
                        using (WebApplication application = CreateWebApplicationInstance())
                        {
                            EnsureValueManager();
                            webApplicationInitialized = true;
                            application.ConnectionString = GetConnectionString();
                            application.Setup();
                        }
                    }
                }
            }
        }

        public static void EnsureValueManager()
        {
            var valueManagerType = typeof(HybridValueManager<>).GetGenericTypeDefinition();
            if (ValueManager.ValueManagerType != valueManagerType)
                ValueManager.ValueManagerType = valueManagerType;
        }
        private static IDataServer CreateDataServer()
        {
            EnsureWebApplicationInitialized();
            QueryRequestSecurityStrategyHandler securityProviderHandler = delegate ()
            {
                AuthenticationBase authentication;
                if (AuthenticationCreator != null)
                    authentication = AuthenticationCreator();
                else
                    authentication = new AuthenticationStandard();
                var security = CreateDataServerSecurity(authentication);
                SecurityHelper.AttachRequestProcessors(security);
                return security;
            };

            dataStore = dataStore ?? CreateDataStore();
            IDataLayer dataLayer = CreateDataLayer();

            var securityStrategyProvider = new CachingRequestSecurityStrategyProvider(new SecuredDataServer.RequestSecurityStrategyProvider(dataLayer, securityProviderHandler));
            IServerSecurity serverSecurity = CreateDefaultServerSecurity(securityStrategyProvider);
            ISecuredSerializableObjectLayer objectLayer = CreateDefaultSecuredSerializableObjectLayer(dataLayer, securityStrategyProvider);
            return new SecuredDataServer(serverSecurity, objectLayer);
        }

        private static IDataLayer CreateDataLayer()
        {
            XPDictionary dictionary = CreateDictionary();
            CustomCreateDataLayerEventArgs args = new CustomCreateDataLayerEventArgs(dictionary, dataStore);
            CustomCreateDataLayer?.Invoke(null, args);

            return args.DataLayer ?? (UseThreadSafeDataLayer ?
                            (IDataLayer)new ThreadSafeDataLayer(dictionary, dataStore) :
                            new SimpleDataLayer(dictionary, dataStore));
        }

        private static DevExpress.Xpo.Metadata.XPDictionary CreateDictionary()
        {
            CustomCreateXPDictionaryEventArgs args = new CustomCreateXPDictionaryEventArgs();
            CustomCreateXPDictionary?.Invoke(null, args);
            return args.Dictionary ?? XpoTypesInfoHelper.GetXpoTypeInfoSource().XPDictionary;
        }


        private static IDataServerSecurity CreateDataServerSecurity(AuthenticationBase authentication)
        {
            CustomCreateDataServerSecurityEventArgs args = new CustomCreateDataServerSecurityEventArgs
            {
                UserType = UserType ?? typeof(SecuritySystemUser),
                RoleType = RoleType ?? typeof(SecuritySystemRole),
                Authentication = authentication
            };

            CustomCreateDataServerSecurity?.Invoke(null, args);

            return args.Security ?? new SecurityStrategyComplex(args.UserType, args.RoleType, authentication);
        }

        private static IDataStore CreateDataStore()
        {

            var handler = CustomCreateDataStore;
            if (handler != null)
            {
                CustomCreateDataStoreEventArgs args = new CustomCreateDataStoreEventArgs();

                handler(null, args);
                if (args.DataStore != null)
                    return args.DataStore;
            }

            string connectionString = GetConnectionString();
            if (UseDataStorePool)
                connectionString = XpoDefault.GetConnectionPoolString(connectionString);

            return XpoDefault.GetConnectionProvider(connectionString, AutoCreateOption.SchemaAlreadyExists);
        }

        private static ISecuredSerializableObjectLayer CreateDefaultSecuredSerializableObjectLayer(IDataLayer dataLayer, IRequestSecurityStrategyProvider securityStrategyProvider)
        {
            return new SecuredSerializableObjectLayer(dataLayer, securityStrategyProvider, false);
        }

        private static IServerSecurity CreateDefaultServerSecurity(IRequestSecurityStrategyProvider securityStrategyProvider)
        {
            return new ServerSecurity(securityStrategyProvider);
        }

        internal static string GetConnectionString()
        {
            return ConfigurationManager.ConnectionStrings[ConnectionStringName].ConnectionString;
        }

        internal static WebApplication CreateWebApplicationInstance()
        {
            return (WebApplication)Activator.CreateInstance(WebApplicationType);
        }

    }
}