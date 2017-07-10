using System;
using System.Runtime.Serialization;
using System.ServiceModel.Description;

namespace SenDev.Xaf.ApplicationServerHosting
{
    public class XafDataContractSerializerOperationBehavior : DataContractSerializerOperationBehavior
    {
        public XafDataContractSerializerOperationBehavior(OperationDescription operationDescription)
            : base(operationDescription)
        {

        }

        public override XmlObjectSerializer CreateSerializer(Type type, System.Xml.XmlDictionaryString name, System.Xml.XmlDictionaryString ns, System.Collections.Generic.IList<Type> knownTypes)
        {
            return new DataContractSerializer(type, name, ns, knownTypes, int.MaxValue, false, true, null, new XafDataContractResolver());
        }
    }
}
