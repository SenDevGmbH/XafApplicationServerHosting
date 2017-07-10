using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SenDev.Xaf.ApplicationServerHosting
{
    [BinarySerialization]
    public interface IApplicationServiceMetadata
    {
        DirectConnectionInfo GetDirectConnectionInfo();
    }
}
