using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using System.ServiceModel;
using System.ServiceModel.Activation;
using DevExpress.Data.Filtering;
using DevExpress.ExpressApp.Security;
using DevExpress.ExpressApp.Security.ClientServer;
using DevExpress.ExpressApp.Security.ClientServer.Wcf;
using DevExpress.ExpressApp.Utils;
using DevExpress.Xpo;
using DevExpress.Xpo.Helpers;

namespace SenDev.Xaf.ApplicationServerHosting.Web
{
    [AspNetCompatibilityRequirements(RequirementsMode = AspNetCompatibilityRequirementsMode.Allowed)]
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single, ConcurrencyMode = ConcurrencyMode.Multiple, MaxItemsInObjectGraph = int.MaxValue, AddressFilterMode = AddressFilterMode.Any)]
    public class ApplicationServer : ApplicationServerBase, IWcfSecuredDataServer
    {
        private static readonly ConcurrentDictionary<string, Func<IClientInfo, object, object>> customFunctions =
                        new ConcurrentDictionary<string, Func<IClientInfo, object, object>>();


        static ApplicationServer()
        {
            EnsureValueManager();
            if (System.Diagnostics.Debugger.IsAttached)
                RegisterServerSideMethods(typeof(IApplicationServiceMetadata), typeof(MetadataService));
        }

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