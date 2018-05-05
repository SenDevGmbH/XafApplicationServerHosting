using System;
using System.Configuration;
using System.Diagnostics;
using System.Linq;
using System.ServiceModel;
using System.ServiceModel.Channels;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Security.ClientServer;

namespace SenDev.Xaf.ApplicationServerHosting
{
    public class ApplicationServerClient
    {
        private const string updateAppDomainName = "UpdateDatabaseDomain";

        public bool InitializeApplication(XafApplication winApplication, EndpointAddress endpointAddress, Binding binding)
        {
            System.Net.ServicePointManager.Expect100Continue = false;

            var clientDataServer = new OptimizedSecuredDataServerClient(binding, endpointAddress);
            ServerSecurityClient.CanUseCache = true;
            var securityClient = CreateServerSecurityClient(clientDataServer);
            securityClient.IsSupportChangePassword = true;
            XafDataContractResolver.AddToEndpoint(clientDataServer.ChannelFactory.Endpoint);
            winApplication.CreateCustomObjectSpaceProvider += (sender, e) =>
            {
                XafApplication application = (XafApplication)sender;
                e.ObjectSpaceProviders.Add(CreateObjectSpaceProvider(clientDataServer, securityClient));
                e.ObjectSpaceProviders.Add(CreateNonPersistentObjectSpaceProvider(application));

            };
            winApplication.Security = securityClient;
            if (DatabaseUpdateMode != DatabaseUpdateHandlerMode.None)
                winApplication.DatabaseVersionMismatch += Application_DatabaseVersionMismatch;

                return true;
        }

        protected virtual NonPersistentObjectSpaceProvider CreateNonPersistentObjectSpaceProvider(XafApplication application)
        {
            return new NonPersistentObjectSpaceProvider(application.TypesInfo, null);
        }

        public DatabaseUpdateHandlerMode DatabaseUpdateMode { get; set; } = DatabaseUpdateHandlerMode.WhenDebugging;
        private void Application_DatabaseVersionMismatch(object sender, DevExpress.ExpressApp.DatabaseVersionMismatchEventArgs e)
        {
            if (DatabaseUpdateMode == DatabaseUpdateHandlerMode.Always || System.Diagnostics.Debugger.IsAttached)
            {
                var application = (XafApplication)sender;
                if (string.IsNullOrWhiteSpace(application.ConnectionString))
                    UpdateLocalDatabaseInDebugMode(application);
                else
                    e.Updater.Update();
                e.Handled = true;

            }
            else
            {
                string message = "The application cannot connect to the specified database, " +
                    "because the database doesn't exist, its version is older " +
                    "than that of the application or its schema does not match " +
                    "the ORM data model structure. To avoid this error, use one " +
                    "of the solutions from the https://www.devexpress.com/kb=T367835 KB Article.";

                if (e.CompatibilityError != null && e.CompatibilityError.Exception != null)
                {
                    message += "\r\n\r\nInner exception: " + e.CompatibilityError.Exception.Message;
                }
                throw new InvalidOperationException(message);
            }
        }

        private static void UpdateLocalDatabaseInDebugMode(XafApplication application)
        {
            if (Debugger.IsAttached)
            {

                var connectionInfo = GetDirectConnectionInfo(application);
                var updateDomain = AppDomain.CreateDomain(updateAppDomainName);
                try
                {
                    var updater = updateDomain.CreateObject<DebugTimeDatabaseUpdater>();
                    updater.UpdateDatabaseWithNewApplication(connectionInfo, application.GetType());
                }
                finally
                {
                    AppDomain.Unload(updateDomain);
                }
            }
        }

        private static DirectConnectionInfo GetDirectConnectionInfo(XafApplication application)
        {
            var provider = application.ObjectSpaceProviders.OfType<DataServerObjectSpaceProvider>().FirstOrDefault();
            using (var os = provider.CreateUpdatingObjectSpace(false))
            {
                return os.GetServerSideMethods<IApplicationServiceMetadata>().GetDirectConnectionInfo();
            }

        }



        protected virtual IObjectSpaceProvider CreateObjectSpaceProvider(OptimizedSecuredDataServerClient clientDataServer, ServerSecurityClient securityClient)
        {
            return new DataServerObjectSpaceProvider(clientDataServer, securityClient);
        }


        protected virtual IClientInfoFactory CreateClientInfoFactory() => new ClientInfoFactory();

        protected virtual ServerSecurityClient CreateServerSecurityClient(OptimizedSecuredDataServerClient clientDataServer)
        {
            return new ServerSecurityClient(clientDataServer, CreateClientInfoFactory());
        }

        public bool InitializeApplication(XafApplication application)
        {
            string connectionString = ConfigurationManager.ConnectionStrings["ConnectionString"].ConnectionString;
            var uri = TryCreateUri(connectionString);
            if (uri == null)
                return false;

            return InitializeApplication(application, new EndpointAddress(uri), BindingFactory.CreateBinaryEncodedBinding(new[] { uri }));
        }

        private static Uri TryCreateUri(string connectionString)
        {
            try
            {
                return new Uri(connectionString);
            }
            catch (UriFormatException)
            {
                return null;
            }
        }
    }

}