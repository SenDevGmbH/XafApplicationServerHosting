using DevExpress.Data.Filtering;
using DevExpress.ExpressApp.Security;
using DevExpress.ExpressApp.Security.ClientServer;
using DevExpress.ExpressApp.Security.ClientServer.Wcf;
using DevExpress.Xpo;
using DevExpress.Xpo.Helpers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Principal;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.Threading;

namespace SenDev.Xaf.ApplicationServerHosting
{
    public class OptimizedSecuredDataServerClient : ClientBase<IWcfSecuredDataServer>, IDataServer
    {
        public OptimizedSecuredDataServerClient(Binding binding, EndpointAddress remoteAddress) : base(binding, remoteAddress)
        {
            ClientCredentials.Windows.AllowedImpersonationLevel = TokenImpersonationLevel.Impersonation;
        }

        private T ExecuteWithLog<T>(Func<T> operation, string logText, [CallerMemberName] string methodName = null)
        {

            Stopwatch sw = new Stopwatch();
            sw.Start();
            T result = operation();
            sw.Stop();
            int objectsCount = GetObjectsCount(result);
            Trace.TraceInformation($"{sw.Elapsed} ({objectsCount}): Service call to {methodName}({logText})");
            return result;
        }

        private int GetObjectsCount<T>(T result)
        {
            var loadObjectsResult = result as SerializableObjectLayerResult<XPObjectStubCollection[]>;

            if (loadObjectsResult != null)
            {
                return loadObjectsResult.Result.Select(c => c.Count).Sum();
            }

            var loadCollectionObjectsResult = result as  SerializableObjectLayerResult<XPObjectStubCollection>;
            if (loadCollectionObjectsResult != null)
                return loadCollectionObjectsResult.Result.Count;

            return 0;
        }

        public bool CanLoadCollectionObjects(IClientInfo clientInfo) => true;


        private string GetQueryDebugString(ObjectStubsQuery query)
        {
            if (query == null) return string.Empty;
            return $"{query.ClassInfo.ClassName}({query.Criteria})";
        }
        #region ISecuredSerializableObjectLayer2
        public SerializableObjectLayerResult<XPObjectStubCollection[]> LoadObjects(IClientInfo clientInfo, XPDictionaryStub dictionary, ObjectStubsQuery[] queries)
        {
            return LoadObjectCache.LoadFromCache(clientInfo, dictionary, queries,
                () => ExecuteWithLog(() => Channel.LoadObjects(clientInfo, dictionary, queries),
                $" - ClientId: { clientInfo.ClientId };   Query(1 of {queries.Length})={GetQueryDebugString(queries.FirstOrDefault())}"));
        }
        public CommitObjectStubsResult[] CommitObjects(IClientInfo clientInfo, XPDictionaryStub dictionary, XPObjectStubCollection objectsForDelete, XPObjectStubCollection objectsForSave, LockingOption lockingOption)
        {
            return ExecuteWithLog(
                    () => Channel.CommitObjects(clientInfo, dictionary, objectsForDelete, objectsForSave, lockingOption),
                          $" - ClientId: { clientInfo.ClientId }; objectsForDelete.Count: {objectsForDelete.Count}; objectsForSave.Count: {objectsForSave.Count}; lockingOption: {lockingOption.ToString()}");
        }
        public SerializableObjectLayerResult<XPObjectStubCollection[]> GetObjectsByKey(IClientInfo clientInfo, XPDictionaryStub dictionary, GetObjectStubsByKeyQuery[] queries)
        {
            return ExecuteWithLog(() => Channel.GetObjectsByKey(clientInfo, dictionary, queries),
                $" - ClientId: {clientInfo.ClientId};  queries.Length: {queries.Length}");
        }
        public object[][] SelectData(IClientInfo clientInfo, XPDictionaryStub dictionary, ObjectStubsQuery query, CriteriaOperatorCollection properties, CriteriaOperatorCollection groupProperties, CriteriaOperator groupCriteria)
        {
            return ExecuteWithLog(() => Channel.SelectData(clientInfo, dictionary, query, properties, groupProperties, groupCriteria),
                               $" - ClientId: {clientInfo.ClientId};  query.ClassInfo.ClassName: {query.ClassInfo.ClassName};" +
                               $"groupProperties.Count: {groupProperties?.Count}; groupsCriteria: {groupCriteria}");
        }
        public SerializableObjectLayerResult<XPObjectStubCollection> LoadCollectionObjects(IClientInfo clientInfo, XPDictionaryStub dictionary, string refPropertyName, XPObjectStub ownerObject)
        {
            return ExecuteWithLog(() => Channel.LoadCollectionObjects(clientInfo, dictionary, refPropertyName, ownerObject), $" - {ownerObject.ClassName}.{refPropertyName}");
        }
        public PurgeResult Purge(IClientInfo clientInfo)
        {
            return ExecuteWithLog(() => Channel.Purge(clientInfo), $" - clientId: {clientInfo.ClientId}");
        }
        public SerializableObjectLayerResult<object[]> LoadDelayedProperties(IClientInfo clientInfo, XPDictionaryStub dictionary, XPObjectStub theObject, string[] props)
        {
            return ExecuteWithLog(() => Channel.LoadDelayedProperties(clientInfo, dictionary, theObject, props),
                 $" - clientId: {clientInfo.ClientId};  object.ClassName: {theObject.ClassName}; Properties.Length: {props.Length}");
        }
        public SerializableObjectLayerResult<object[]> LoadDelayedProperties(IClientInfo clientInfo, XPDictionaryStub dictionary, XPObjectStubCollection objects, string property)
        {
            return ExecuteWithLog(() => Channel.LoadDelayedProperties(clientInfo, dictionary, objects, property),
                   $" - clientId: {clientInfo.ClientId};  objects.Count: {objects.Count}; propName: {property}");
        }
        public bool IsParentObjectToSave(IClientInfo clientInfo, XPDictionaryStub dictionary, XPObjectStub theObject)
        {
            return false;
        }
        public bool IsParentObjectToDelete(IClientInfo clientInfo, XPDictionaryStub dictionary, XPObjectStub theObject)
        {
            return false;
        }
        public SerializableObjectLayerResult<XPObjectStubCollection> GetParentObjectsToSave(IClientInfo clientInfo)
        {
            return CreateEmptyResult();
        }

        private static SerializableObjectLayerResult<XPObjectStubCollection> CreateEmptyResult()
        {
            return new SerializableObjectLayerResult<XPObjectStubCollection> { Result = new XPObjectStubCollection() };
        }

        public SerializableObjectLayerResult<XPObjectStubCollection> GetParentObjectsToDelete(IClientInfo clientInfo)
        {
            return CreateEmptyResult();
        }
        public string[] GetParentTouchedClassInfos(IClientInfo clientInfo)
        {
            return ExecuteWithLog(() => Channel.GetParentTouchedClassInfos(clientInfo),
                $" - clientId: {clientInfo.ClientId}");
        }

        public void CreateObjectType(IClientInfo clientInfo, string assemblyName, string typeName)
        {
            ExecuteWithLog<object>(() =>
            {
                Channel.CreateObjectType(clientInfo, assemblyName, typeName);
                return null;
            }, $" - clientId: {clientInfo.ClientId}; assemblyName: {assemblyName}; typeName: {typeName}");
        }
        public object Do(IClientInfo clientInfo, string command, object args)
        {
            return ExecuteWithLog(() => Channel.Do(clientInfo, command, args), $" - clientId: {clientInfo.ClientId}; command: {command}; argsType: {args?.GetType()?.Name}");
        }
        public void FinalizeSession(IClientInfo clientInfo)
        {
            ExecuteWithLog<object>(() =>
            {
                Channel.FinalizeSession(clientInfo);
                return null;
            }, $" - clientId: {clientInfo.ClientId}");
        }

        #endregion

        #region IServerSecurity
        public void Logon(IClientInfo clientInfo)
        {
            ExecuteWithLog<object>(() =>
            {
                Channel.Logon(clientInfo);
                return null;
            }, $" - clientId: {clientInfo.ClientId}");
        }
        public void Logoff(IClientInfo clientInfo)
        {
            ExecuteWithLog<object>(() =>
            {
                Channel.Logoff(clientInfo);
                return null;
            }, $" - clientId: {clientInfo.ClientId}");
        }
        public bool IsGranted(IClientInfo clientInfo, IPermissionRequest permissionRequest)
        {
            return ExecuteWithLog(() => Channel.IsGranted(clientInfo, permissionRequest),
                $" - clientId: {clientInfo.ClientId}; permissionRequest: {permissionRequest}");
        }
        public IList<bool> IsGranted(IClientInfo clientInfo, IList<IPermissionRequest> permissionRequests)
        {
            return ExecuteWithLog(() => Channel.IsGranted(clientInfo, permissionRequests), $" (man req) - clientId: {clientInfo.ClientId}; requestsCount: {permissionRequests.Count}");
        }
        public object GetUserId(IClientInfo clientInfo)
        {
            return ExecuteWithLog(() => Channel.GetUserId(clientInfo), $" - clientId: {clientInfo.ClientId}");
        }
        public string GetUserName(IClientInfo clientInfo)
        {
            return ExecuteWithLog(() => Channel.GetUserName(clientInfo), $" - clientId: {clientInfo.ClientId}");
        }
        public object GetLogonParameters(IClientInfo clientInfo)
        {
            return ExecuteWithLog(() => Channel.GetLogonParameters(clientInfo), $" - clientId: {clientInfo.ClientId}");
        }
        public bool GetNeedLogonParameters(IClientInfo clientInfo)
        {
            return ExecuteWithLog(() => Channel.GetNeedLogonParameters(clientInfo), $" - clientId: {clientInfo.ClientId}");
        }
        public bool GetIsLogoffEnabled(IClientInfo clientInfo)
        {
            return ExecuteWithLog(() => Channel.GetIsLogoffEnabled(clientInfo), $" - clientId: {clientInfo.ClientId}");
        }
        public Type GetUserType(IClientInfo clientInfo)
        {
            string moduleType = base.Channel.GetUserTypeName(clientInfo);
            return string.IsNullOrEmpty(moduleType) ? null : Type.GetType(moduleType);
        }
        public Type GetRoleType(IClientInfo clientInfo)
        {
            string roleType = base.Channel.GetRoleTypeName(clientInfo);
            return string.IsNullOrEmpty(roleType) ? null : Type.GetType(roleType);
        }
        #endregion
    }
}