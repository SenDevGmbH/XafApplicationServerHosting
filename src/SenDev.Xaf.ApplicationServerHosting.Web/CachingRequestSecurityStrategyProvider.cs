using DevExpress.ExpressApp.Security.ClientServer;
using System;
using System.Runtime.Caching;
using System.Text;
using DevExpress.ExpressApp;

namespace SenDev.Xaf.ApplicationServerHosting.Web
{
    public class CachingRequestSecurityStrategyProvider : IRequestSecurityStrategyProvider
    {
        private readonly MemoryCache cache = new MemoryCache("RequestSecurityProviderCache");
        private readonly IRequestSecurityStrategyProvider provider;
        public CachingRequestSecurityStrategyProvider(IRequestSecurityStrategyProvider provider)
        {
            this.provider = provider;
            ApplicationServer.EnsureValueManager();
        }

        private string CreateCacheKey(IClientInfo clientInfo)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(clientInfo.ClientId);
            //Using name of the logon parameters to distiguish between anonymous and UserName / Password logon
            if (clientInfo.LogonParameters != null)
                sb.Append(clientInfo.LogonParameters.GetType().FullName);
            return sb.ToString();
        }

        public IDataServerSecurity CreateAndLogonSecurity(IClientInfo clientInfo)
        {
            ApplicationServer.EnsureValueManager();
            string cacheKey = CreateCacheKey(clientInfo);
            var result = (IDataServerSecurity)cache.Get(cacheKey);
            if (result == null)
            {
                result = provider.CreateAndLogonSecurity(clientInfo);
                cache.Add(cacheKey, result, new CacheItemPolicy{AbsoluteExpiration = DateTime.Now.AddHours(1)});
            }

            SecuritySystem.SetInstance(result);
            return result;
        }
    }
}