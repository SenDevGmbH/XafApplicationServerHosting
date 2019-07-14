using System;
using System.Runtime.Serialization;
using System.ServiceModel.Description;
using System.Xml;
using System.Linq;
using DevExpress.ExpressApp.MiddleTier;
using DevExpress.ExpressApp;
using System.Collections.Concurrent;
using System.Reflection;

namespace SenDev.Xaf.ApplicationServerHosting
{

    public class XafDataContractResolver : DataContractResolver
    {
        private string xmlNamespace;
        private static readonly ConcurrentDictionary<string, Assembly> assembliesByNamespace = new ConcurrentDictionary<string, Assembly>();


        public XafDataContractResolver(string xmlNamespace)
        {
            this.xmlNamespace = xmlNamespace;
        }

        public XafDataContractResolver()
            : this("http://schemas.sendev.de/")
        {

        }
        public override Type ResolveName(string typeName, string typeNamespace, Type declaredType,
                                         DataContractResolver knownTypeResolver)
        {
            if (declaredType == null)
                throw new ArgumentNullException(nameof(declaredType));
            if (knownTypeResolver == null)
                throw new ArgumentNullException(nameof(knownTypeResolver));

            var type = knownTypeResolver.ResolveName(typeName, typeNamespace, declaredType, knownTypeResolver);
            if (type != null)
                return type;

            string dotNetNamespace;
            int slashIndex = typeNamespace.LastIndexOf("/", StringComparison.Ordinal);
            if (slashIndex > 0 && slashIndex < typeNamespace.Length - 1)
                dotNetNamespace = typeNamespace.Substring(slashIndex + 1) + ".";
            else
                dotNetNamespace = string.Empty;

            return
                Type.GetType(typeName)
                ?? XafTypesInfo.Instance.FindTypeInfo(dotNetNamespace + typeName)?.Type
                ?? GuessTypeByNamespace(dotNetNamespace, typeName);

        }

        private Type GuessTypeByNamespace(string dotnetNamespace, string typeName)
        {
            return GetAssembly(dotnetNamespace)?.GetType(typeName);
        }


        private static Assembly GetAssembly(string dotnetNamespace)
        {
            if (string.IsNullOrWhiteSpace(dotnetNamespace)) return null;

            Assembly result;
            if (assembliesByNamespace.TryGetValue(dotnetNamespace, out result))
                return result;

            var parts = dotnetNamespace.Split('.');
            for (int i = parts.Length; i > 0; i--)
            {
                string assemblyName = string.Join(".", parts, 0, i);

#pragma warning disable CS0618 // Type or member is obsolete
                result = Assembly.LoadWithPartialName(assemblyName);
#pragma warning restore CS0618 // Type or member is obsolete


                if (result!=null)
                {
                    assembliesByNamespace[dotnetNamespace] = result;
                    return result;
                }
            }

            return null;
        }
        public override bool TryResolveType(Type type, Type declaredType, DataContractResolver knownTypeResolver,
                                            out XmlDictionaryString typeName,
                                            out XmlDictionaryString typeNamespace)
        {

            if (type == null)
                throw new ArgumentNullException(nameof(type));
            if (declaredType == null)
                throw new ArgumentNullException(nameof(declaredType));
            if (knownTypeResolver == null)
                throw new ArgumentNullException(nameof(knownTypeResolver));

            if (knownTypeResolver.TryResolveType(type, declaredType, knownTypeResolver, out typeName, out typeNamespace))
                return true;

            if (type.IsPrimitive && declaredType == typeof(object))
            {
                return knownTypeResolver.TryResolveType(type, type, knownTypeResolver, out typeName, out typeNamespace);
            }

            XmlDictionary dict = new XmlDictionary();

            typeNamespace = dict.Add(xmlNamespace);
            typeName = dict.Add(type.AssemblyQualifiedName);

            return true;
        }

        public static void AddToEndpoint(ServiceEndpoint endpoint)
        {
            if (endpoint == null)
                throw new ArgumentNullException(nameof(endpoint));

            foreach (var operation in endpoint.Contract.Operations)
            {
                
                for (;;)
                {
                    var behavior = operation.Behaviors.OfType<DataContractSerializerOperationBehavior>().FirstOrDefault();
                    if (behavior == null) break;
                    operation.Behaviors.Remove(behavior);
                }
                operation.Behaviors.Remove<SetMaxItemsInObjectGraphAttribute>();
                operation.Behaviors.Add(new XafDataContractSerializerOperationBehavior(operation));
            }
        }
    }


}