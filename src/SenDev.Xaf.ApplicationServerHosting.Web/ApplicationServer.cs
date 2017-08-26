using DevExpress.ExpressApp.Security;
using DevExpress.ExpressApp.Security.ClientServer;
using DevExpress.ExpressApp.Security.ClientServer.Wcf;
using DevExpress.ExpressApp.Security.Strategy;
using DevExpress.ExpressApp.Xpo;
using DevExpress.Persistent.Base;
using DevExpress.Xpo;
using DevExpress.Xpo.DB;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.ServiceModel;
using System.ServiceModel.Activation;
using DevExpress.Data.Filtering;
using DevExpress.ExpressApp.Utils;
using DevExpress.ExpressApp.Web;
using DevExpress.Xpo.Helpers;
using System.Collections.Concurrent;
using System.Reflection;
using DevExpress.Xpo.Metadata;
using System.Runtime.Serialization.Formatters.Binary;
using DevExpress.Xpo.Metadata;

namespace SenDev.Xaf.ApplicationServerHosting.Web
{
    [AspNetCompatibilityRequirements(RequirementsMode = AspNetCompatibilityRequirementsMode.Allowed)]
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single, ConcurrencyMode = ConcurrencyMode.Multiple, MaxItemsInObjectGraph = int.MaxValue, AddressFilterMode = AddressFilterMode.Any)]
    public class ApplicationServer : IWcfSecuredDataServer
    {
        private static bool webApplicationInitialized;
        private static readonly object webApplicationInitLock = new object();
        private WcfSecuredDataServer server;
        private static readonly ConcurrentDictionary<string, Func<IClientInfo, object, object>> customFunctions
            = new ConcurrentDictionary<string, Func<IClientInfo, object, object>>();

        public static event EventHandler<CustomCreateDataStoreEventArgs> CustomCreateDataStore;
        public static event EventHandler<CustomCreateDataServerSecurityEventArgs> CustomCreateDataServerSecurity;
        public static event EventHandler<CustomCreateXPDictionaryEventArgs> CustomCreateXPDictionary;
        public static event EventHandler<CustomCreateDataLayerEventArgs> CustomCreateDataLayer;

        public ApplicationServer()
        {

        }

        static ApplicationServer()
        {
            EnsureValueManager();
            if (System.Diagnostics.Debugger.IsAttached)
                RegisterServerSideMethods(typeof(IApplicationServiceMetadata), typeof(MetadataService));
        }



        public static bool UseThreadSafeDataLayer { get; set; }

        public static void RegisterCustomCommand(string commandName, Func<IClientInfo, object, object> func)
        {
            customFunctions[commandName] = func;
        }

        public SerializableObjectLayerResult<XPObjectStubCollection[]> LoadObjects(IClientInfo clientInfo, XPDictionaryStub dictionary, ObjectStubsQuery[] queries)
        {
            return Server.LoadObjects(clientInfo, dictionary, queries);
        }

        public CommitObjectStubsResult[] CommitObjects(IClientInfo clientInfo, XPDictionaryStub dictionary, XPObjectStubCollection objectsForDelete, XPObjectStubCollection objectsForSave, LockingOption lockingOption)
        {
            return Server.CommitObjects(clientInfo, dictionary, objectsForDelete, objectsForSave, lockingOption);
        }

        public SerializableObjectLayerResult<XPObjectStubCollection[]> GetObjectsByKey(IClientInfo clientInfo, XPDictionaryStub dictionary, GetObjectStubsByKeyQuery[] queries)
        {
            return Server.GetObjectsByKey(clientInfo, dictionary, queries);
        }

        public object[][] SelectData(IClientInfo clientInfo, XPDictionaryStub dictionary, ObjectStubsQuery query, CriteriaOperatorCollection properties, CriteriaOperatorCollection groupProperties, CriteriaOperator groupCriteria)
        {
            return Server.SelectData(clientInfo, dictionary, query, properties, groupProperties, groupCriteria);
        }

        public bool CanLoadCollectionObjects(IClientInfo clientInfo)
        {
            return Server.CanLoadCollectionObjects(clientInfo);
        }

        public SerializableObjectLayerResult<XPObjectStubCollection> LoadCollectionObjects(IClientInfo clientInfo, XPDictionaryStub dictionary, string refPropertyName, XPObjectStub ownerObject)
        {
            return Server.LoadCollectionObjects(clientInfo, dictionary, refPropertyName, ownerObject);
        }

        public PurgeResult Purge(IClientInfo clientInfo)
        {
            return Server.Purge(clientInfo);
        }

        public SerializableObjectLayerResult<object[]> LoadDelayedProperties(IClientInfo clientInfo, XPDictionaryStub dictionary, XPObjectStub theObject, string[] props)
        {
            return Server.LoadDelayedProperties(clientInfo, dictionary, theObject, props);
        }

        public SerializableObjectLayerResult<object[]> LoadDelayedProperties(IClientInfo clientInfo, XPDictionaryStub dictionary, XPObjectStubCollection objects, string property)
        {
            return Server.LoadDelayedProperties(clientInfo, dictionary, objects, property);
        }

        public bool IsParentObjectToSave(IClientInfo clientInfo, XPDictionaryStub dictionary, XPObjectStub theObject)
        {
            return Server.IsParentObjectToSave(clientInfo, dictionary, theObject);
        }

        public bool IsParentObjectToDelete(IClientInfo clientInfo, XPDictionaryStub dictionary, XPObjectStub theObject)
        {
            return Server.IsParentObjectToDelete(clientInfo, dictionary, theObject);
        }

        public SerializableObjectLayerResult<XPObjectStubCollection> GetParentObjectsToSave(IClientInfo clientInfo)
        {
            return Server.GetParentObjectsToSave(clientInfo);
        }

        public SerializableObjectLayerResult<XPObjectStubCollection> GetParentObjectsToDelete(IClientInfo clientInfo)
        {
            return Server.GetParentObjectsToDelete(clientInfo);
        }

        public string[] GetParentTouchedClassInfos(IClientInfo clientInfo)
        {
            return Server.GetParentTouchedClassInfos(clientInfo);
        }

        public void CreateObjectType(IClientInfo clientInfo, string assemblyName, string typeName)
        {
            Server.CreateObjectType(clientInfo, assemblyName, typeName);
        }

        public object Do(IClientInfo clientInfo, string command, object args)
        {
            Func<IClientInfo, object, object> customCommand;
            if (customFunctions.TryGetValue(command, out customCommand))
                return customCommand(clientInfo, args);
            else
                return Server.Do(clientInfo, command, args);
        }

        public void FinalizeSession(IClientInfo clientInfo)
        {
            Server.FinalizeSession(clientInfo);
        }

        public void Logon(IClientInfo clientInfo)
        {
            Server.Logon(clientInfo);
        }

        public void Logoff(IClientInfo clientInfo)
        {
            Server.Logoff(clientInfo);
        }

        public bool IsGranted(IClientInfo clientInfo, IPermissionRequest permissionRequest)
        {
            try
            {
                return Server.IsGranted(clientInfo, permissionRequest);
            }
            catch (Exception) //Workaround for a DX bug
            {
                return true;
            }
        }

        public IList<bool> IsGranted(IClientInfo clientInfo, IList<IPermissionRequest> permissionRequests)
        {
            Guard.ArgumentNotNull(permissionRequests, "permissionRequests");
            try
            {
                return Server.IsGranted(clientInfo, permissionRequests);
            }
            catch (Exception) //Workaround for a DX bug
            {
                List<bool> res = new List<bool>(permissionRequests.Count);
                for (int i = 0; i < permissionRequests.Count; i++)
                {
                    res.Add(true);
                }

                return res;
            }
        }

        public object GetUserId(IClientInfo clientInfo)
        {
            return Server.GetUserId(clientInfo);
        }

        public string GetUserName(IClientInfo clientInfo)
        {
            return Server.GetUserName(clientInfo);
        }

        public object GetLogonParameters(IClientInfo clientInfo)
        {
            return Server.GetLogonParameters(clientInfo);
        }

        public bool GetNeedLogonParameters(IClientInfo clientInfo)
        {
            return Server.GetNeedLogonParameters(clientInfo);
        }

        public bool GetIsLogoffEnabled(IClientInfo clientInfo)
        {
            return Server.GetIsLogoffEnabled(clientInfo);
        }

        public string GetUserTypeName(IClientInfo clientInfo)
        {
            return Server.GetUserTypeName(clientInfo);
        }

        public string GetRoleTypeName(IClientInfo clientInfo)
        {
            return Server.GetRoleTypeName(clientInfo);
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

        internal static WebApplication CreateWebApplicationInstance()
        {
            return (WebApplication)Activator.CreateInstance(WebApplicationType);
        }

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

        public static void EnsureValueManager()
        {
            var valueManagerType = typeof(HybridValueManager<>).GetGenericTypeDefinition();
            if (ValueManager.ValueManagerType != valueManagerType)
                ValueManager.ValueManagerType = valueManagerType;
        }

        private static IDataStore dataStore;
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
            CustomCreateDataLayerEventArgs args = new CustomCreateDataLayerEventArgs();
            CustomCreateDataLayer?.Invoke(null, args);

            return args.DataLayer ?? (UseThreadSafeDataLayer ?
                            (IDataLayer)new ThreadSafeDataLayer(CreateDictionary(), dataStore) :
                            new SimpleDataLayer(CreateDictionary(), dataStore));
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

            IDisposable[] objectsToDispose;
            return new MSSqlProviderFactory().CreateProviderFromString(GetConnectionString(), AutoCreateOption.SchemaAlreadyExists, out objectsToDispose);
        }

        private static ISecuredSerializableObjectLayer CreateDefaultSecuredSerializableObjectLayer(IDataLayer dataLayer, IRequestSecurityStrategyProvider securityStrategyProvider)
        {
            return new SecuredSerializableObjectLayer(dataLayer, securityStrategyProvider, false);
        }

        private static IServerSecurity CreateDefaultServerSecurity(IRequestSecurityStrategyProvider securityStrategyProvider)
        {
            return new ServerSecurity(securityStrategyProvider);
        }


        protected static string ConnectionStringName { get; set; } = "ConnectionString";

        private WcfSecuredDataServer Server
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

        internal static string GetConnectionString()
        {
            return ConfigurationManager.ConnectionStrings[ConnectionStringName].ConnectionString;
        }

        public static void RegisterServerSideMethods(Type methodsInterface, Type implementor)
        {

            if (methodsInterface == null)
                throw new ArgumentNullException(nameof(methodsInterface));

            if (implementor == null)
                throw new ArgumentNullException(nameof(implementor));

            foreach (var methodInfo in methodsInterface.GetMethods(BindingFlags.Public | BindingFlags.Instance))
            {
                RegisterCustomCommand(methodsInterface.FullName + methodInfo.Name, (ci, p) => ExecuteServerSideMethod(implementor, methodInfo, p));
            }
        }

        private static object ExecuteServerSideMethod(Type implementorType, MethodInfo methodInfo, object parameter)
        {
            object[] parameters;
            bool binarySerialization = methodInfo.DeclaringType.GetCustomAttribute<BinarySerializationAttribute>() != null;
            if (parameter != null && binarySerialization)
                parameters = SerializationUtils.BinaryDeserialize<object[]>((byte[])parameter);
            else
                parameters = parameter as object[];

            if (parameters == null) parameters = new[] { parameter };
            var result = methodInfo.Invoke(Activator.CreateInstance(implementorType, GetConnectionString()), parameters);
            return binarySerialization ? SerializationUtils.BinarySerialize(result) : result;
        }

    }
}