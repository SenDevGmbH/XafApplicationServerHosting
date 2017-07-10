using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SenDev.Xaf.ApplicationServerHosting
{
    [Serializable]
    public class DirectConnectionInfo
    {
        public string ConnectionString { get; set; }
        public MixedDataStoreProvider Provider { get; set; }
    }
}
