﻿using System;
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
        public static BasicHttpBinding CreateBasicBinding(Uri uri)
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


            SetMaxReaderQuotas(binding.ReaderQuotas);

            if (IsHttps(uri))
            {
                binding.Security.Mode = BasicHttpSecurityMode.Transport;
            }
            return binding;
        }

        private static void SetMaxReaderQuotas(XmlDictionaryReaderQuotas quotas)
        {
            quotas.MaxArrayLength =
                            quotas.MaxBytesPerRead =
                            quotas.MaxDepth =
                            quotas.MaxNameTableCharCount =
                            quotas.MaxStringContentLength = int.MaxValue;
        }

        public static bool IsHttps(Uri uri)
        {
            return uri != null && string.Equals(uri.Scheme, "https", StringComparison.OrdinalIgnoreCase);
        }

        public static Binding CreateBinaryEncodedBinding(Uri uri)
        {
            return CreateBinaryEncodedBinding(uri, AuthenticationSchemes.Anonymous);
        }

        public static Binding CreateBinaryEncodedBinding(Uri uri, AuthenticationSchemes authenticationScheme)
        {
            var binding = new CustomBinding
            {
                CloseTimeout = TimeSpan.FromMinutes(1),
                ReceiveTimeout = TimeSpan.FromMinutes(10),
                SendTimeout = TimeSpan.FromMinutes(5),
            };
            BinaryMessageEncodingBindingElement binaryEncodingElement = new BinaryMessageEncodingBindingElement() { CompressionFormat = CompressionFormat.GZip };
            SetMaxReaderQuotas(binaryEncodingElement.ReaderQuotas);
            binding.Elements.Add(binaryEncodingElement);
            binding.Elements.Add(IsHttps(uri) ? CreateTransportElement<HttpsTransportBindingElement>(authenticationScheme) :
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
