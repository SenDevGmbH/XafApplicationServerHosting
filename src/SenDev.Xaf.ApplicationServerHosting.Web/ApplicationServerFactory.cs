using DevExpress.ExpressApp.Security.ClientServer.Wcf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.ServiceModel.Activation;
using System.ServiceModel.Channels;
using System.Web;

namespace SenDev.Xaf.ApplicationServerHosting.Web
{
    public class ApplicationServerFactory : ServiceHostFactoryBase
    {
        protected override Binding CreateBinding(Uri uri)
        {
            return BindingFactory.CreateBinaryEncodedBinding(uri);
        }

        protected override Type ContractType
        {
            get { return typeof(IWcfSecuredDataServer); }
        }

    }
}