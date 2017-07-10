using DevExpress.Utils;
using DevExpress.Xpo.Helpers;
using System;
using System.Reflection;
using System.Runtime.Remoting.Messaging;
using System.Runtime.Remoting.Proxies;

namespace SenDev.Xaf.ApplicationServerHosting
{
    public class RemoteCommandProxy : RealProxy
    {

        private readonly ICommandChannel commandChannel;
        private readonly Type classToProxy;
        private readonly bool binarySerialization;
        public RemoteCommandProxy(ICommandChannel commandChannel, Type classToProxy) : base(classToProxy)
        {
            this.commandChannel = commandChannel;
            this.classToProxy = classToProxy;
            binarySerialization = classToProxy.GetCustomAttribute<BinarySerializationAttribute>() != null;
        }
        public override IMessage Invoke(IMessage msg)
        {
            var methodCall = (IMethodCallMessage)msg;
            var result = commandChannel.Do(classToProxy.FullName + methodCall.MethodName,
                binarySerialization ? (object)SerializationUtils.BinarySerialize(methodCall.InArgs) : methodCall.InArgs);

            if (binarySerialization && result != null)
                result = SerializationUtils.BinaryDeserialize<object>((byte[])result);

            return new ReturnMessage(result, null, 0, null, methodCall);
        }
    }
}