using DevExpress.Xpo;

namespace SenDev.Xaf.ApplicationServerHosting
{

    class ObjectCacheInfo
    {
        internal SerializableObjectLayerResult<XPObjectStubCollection[]> Result { get; set; }
        internal bool Cachable { get; set; }
    }
}