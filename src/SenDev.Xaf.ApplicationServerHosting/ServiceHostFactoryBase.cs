using System;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;
using System.ServiceModel.Activation;
using System.Linq;

namespace SenDev.Xaf.ApplicationServerHosting
{
    public abstract class ServiceHostFactoryBase : ServiceHostFactory
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1804:RemoveUnusedLocals", MessageId = "endpoint")]
        protected override ServiceHost CreateServiceHost(Type serviceType, Uri[] baseAddresses)
        {
            ServiceHost host = new ServiceHost(serviceType, baseAddresses);

            foreach (var address in baseAddresses)
            {
                var binding = CreateBinding(address);
                ServiceEndpoint endpoint = host.AddServiceEndpoint(ContractType, binding, string.Empty);
                XafDataContractResolver.AddToEndpoint(endpoint);
            }
            CustomizeHost(host, baseAddresses);
            return host;
        }

        protected virtual void CustomizeHost(ServiceHost host, Uri[] addresses)
        {
            host.Description.Behaviors.Remove<ServiceDebugBehavior>();
            host.Description.Behaviors.Remove<ServiceAuthorizationBehavior>();
            host.Description.Behaviors.Remove<ServiceMetadataBehavior>();
            bool isHttps = addresses.Any(BindingFactory.IsHttps);
            host.Description.Behaviors.Add(new ServiceMetadataBehavior { HttpGetEnabled = !isHttps, HttpsGetEnabled = isHttps });
            host.Description.Behaviors.Add(new ServiceDebugBehavior { IncludeExceptionDetailInFaults = true });

        }

        protected abstract Type ContractType { get; }

        protected virtual Binding CreateBinding(Uri uri) => BindingFactory.CreateBasicBinding(uri);
    }
}