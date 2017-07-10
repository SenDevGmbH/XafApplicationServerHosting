using DevExpress.Xpo;

namespace SenDev.Xaf.ApplicationServerHosting
{
    class ObjectsCacheKey
    {
        private readonly string className;
        private readonly bool selectDeleted;
        internal ObjectsCacheKey(ObjectStubsQuery query)
        {
            className = query.ClassInfo.ClassName;
            selectDeleted = query.SelectDeleted;
        }

        public override int GetHashCode()
        {
            return SenDev.Xaf.ApplicationServerHosting.Win.Utils.AccumulateHashCodes(className, selectDeleted);
        }

        public override bool Equals(object obj)
        {
            var other = obj as ObjectsCacheKey;
            if (other == null) return false;
            return className.Equals(other.className) && selectDeleted == other.selectDeleted;
        }
    }
}