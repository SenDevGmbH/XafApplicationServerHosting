using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;

namespace SenDev.Xaf.ApplicationServerHosting.Web
{
    class MetadataService : IApplicationServiceMetadata
    {
        public MetadataService(string connectionString)
        {
        }

        public DirectConnectionInfo GetDirectConnectionInfo()
        {
            DirectConnectionInfo result = new DirectConnectionInfo { ConnectionString = ApplicationServer.GetConnectionString() };
            if (ApplicationServer.WebApplicationType != null)
            {
                using (var application = ApplicationServer.CreateWebApplicationInstance())
                {
                    application.ConnectionString = ApplicationServer.GetConnectionString();
                    result.Provider = (application as IMixedDataStoreProviderApplication)?.CreateMixedDataStoreProvider();
                }
            }

            return result;
        }
    }
}
