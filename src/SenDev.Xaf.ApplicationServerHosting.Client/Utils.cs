using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SenDev.Xaf.ApplicationServerHosting.Win
{
    public static class Utils
    {
        internal static int AccumulateHashCodes(params object[] objects)
        {
            if (objects == null) throw new ArgumentNullException(nameof(objects));

            long hashCode = 0;
            foreach (object obj in objects)
                hashCode += (obj != null) ? obj.GetHashCode() : 0;

            return BitConverter.ToInt32(BitConverter.GetBytes(hashCode), 0);
        }

    }
}
