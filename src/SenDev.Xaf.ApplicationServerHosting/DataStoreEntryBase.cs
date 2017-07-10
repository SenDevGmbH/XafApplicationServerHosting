using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SenDev.Xaf.ApplicationServerHosting
{
    [Serializable]
    abstract class DataStoreEntryBase
    {
        protected DataStoreEntryBase(string prefix, bool removePrefix, DataStoreMode mode)
        {
            Prefix = prefix;
            DeletePrefix = removePrefix;
            Mode = mode;
        }

        public string Prefix { get; }

        public bool DeletePrefix { get; }

        public DataStoreMode Mode { get; }

    }
}
