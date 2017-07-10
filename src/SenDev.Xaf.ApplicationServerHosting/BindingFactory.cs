using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.Text;
using System.Xml;

namespace SenDev.Xaf.ApplicationServerHosting
{
    public static class BindingFactory
    {
        public static BasicHttpBinding CreateBasicBinding(Uri[] addresses)
        {

            BasicHttpBinding binding = new BasicHttpBinding()
            {
                CloseTimeout = TimeSpan.FromMinutes(1),
                ReceiveTimeout = TimeSpan.FromMinutes(10),
                SendTimeout = TimeSpan.FromMinutes(5),
                AllowCookies = false,
                BypassProxyOnLocal = false,
                HostNameComparisonMode = HostNameComparisonMode.StrongWildcard,
                MaxBufferSize = int.MaxValue,
                MaxBufferPoolSize = 524288,
                MaxReceivedMessageSize = int.MaxValue,
                MessageEncoding = WSMessageEncoding.Text,
                TextEncoding = Encoding.UTF8,
                TransferMode = TransferMode.Buffered,
                UseDefaultWebProxy = true
            };


            var quotas = binding.ReaderQuotas;
            quotas.MaxArrayLength =
                quotas.MaxBytesPerRead =
                quotas.MaxDepth =
                quotas.MaxNameTableCharCount =
                quotas.MaxStringContentLength = int.MaxValue;

            if (IsHttps(addresses))
            {
                binding.Security.Mode = BasicHttpSecurityMode.Transport;
            }
            return binding;
        }

        public static bool IsHttps(Uri[] addresses)
        {
            return addresses != null && addresses.All(a => string.Equals(a.Scheme, "https", StringComparison.OrdinalIgnoreCase));
        }

        public static Binding CreateBinaryEncodedBinding(Uri[] addresses)
        {
            return CreateBinaryEncodedBinding(addresses, AuthenticationSchemes.Anonymous);
        }

        public static Binding CreateBinaryEncodedBinding(Uri[] addresses, AuthenticationSchemes authenticationScheme)
        {
            var binding = new CustomBinding
            {
                CloseTimeout = TimeSpan.FromMinutes(1),
                ReceiveTimeout = TimeSpan.FromMinutes(10),
                SendTimeout = TimeSpan.FromMinutes(5),
            };
            binding.Elements.Add(new BinaryMessageEncodingBindingElement() { CompressionFormat = CompressionFormat.GZip });
            binding.Elements.Add(IsHttps(addresses) ? CreateTransportElement<HttpsTransportBindingElement>(authenticationScheme) :
                CreateTransportElement<HttpTransportBindingElement>(authenticationScheme));

            return binding;
        }

        private static T CreateTransportElement<T>(AuthenticationSchemes authenticationScheme) where T: HttpTransportBindingElement, new()
        {
            return new T()
            {
                AllowCookies = false,
                BypassProxyOnLocal = false,
                HostNameComparisonMode = HostNameComparisonMode.StrongWildcard,
                MaxBufferSize = int.MaxValue,
                MaxBufferPoolSize = 524288,
                MaxReceivedMessageSize = int.MaxValue,
                TransferMode = TransferMode.Buffered,
                UseDefaultWebProxy = true,
                AuthenticationScheme = authenticationScheme
            };
        }
    }
}
