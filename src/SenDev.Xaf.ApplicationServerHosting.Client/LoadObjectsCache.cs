using System;
using DevExpress.ExpressApp.Security.ClientServer;
using DevExpress.Xpo;
using System.Collections.Concurrent;

namespace SenDev.Xaf.ApplicationServerHosting
{

    public static class LoadObjectCache
    {
        private static ConcurrentDictionary<string, ObjectCacheInfo> cacheInfos = new ConcurrentDictionary<string, ObjectCacheInfo>();
        internal static SerializableObjectLayerResult<XPObjectStubCollection[]> LoadFromCache(IClientInfo clientInfo, XPDictionaryStub dictionary, ObjectStubsQuery[] queries, 
            Func<SerializableObjectLayerResult<XPObjectStubCollection[]>> getDataFunc)
        {
            if (queries.Length != 1) return getDataFunc();
            var query = queries[0];
            if (!ReferenceEquals(query.Criteria, null) || query.SkipSelectedRecords > 0 || query.TopSelectedRecords > 0 ||
                (query.Sorting != null && query.Sorting.Count > 0)) return getDataFunc();

            ObjectCacheInfo cacheInfo;
            var key = new ObjectsCacheKey(query);
            if (cacheInfos.TryGetValue(query.ClassInfo.ClassName, out cacheInfo))
            {
                if (cacheInfo.Cachable)
                {
                    if (cacheInfo.Result != null)
                        return cacheInfo.Result;
                    else
                        return cacheInfo.Result = getDataFunc();
                }
            }

            return getDataFunc();
        }

        public static void AddCachableType(Type type)
        {
            cacheInfos[type.FullName] = new ObjectCacheInfo { Cachable = true };
        }
    }
}