using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Xpo;
using System;

namespace SenDev.Xaf.ApplicationServerHosting
{
    class DebugTimeDatabaseUpdater : MarshalByRefObject
    {
        internal void UpdateDatabaseWithNewApplication(DirectConnectionInfo connectionInfo, Type applicationType)
        {
            using (var updateApplication = (XafApplication)Activator.CreateInstance(applicationType))
            {
                updateApplication.ConnectionString = connectionInfo.ConnectionString;
                if (connectionInfo.Provider != null)
                {
                    updateApplication.CreateCustomObjectSpaceProvider += (s, e) => 
                        e.ObjectSpaceProvider = new XPObjectSpaceProvider(connectionInfo.Provider);
                }

                updateApplication.DatabaseVersionMismatch += (s, e) =>
                {
                    e.Updater.Update();
                    e.Handled = true;
                };
                updateApplication.Setup();
                updateApplication.CheckCompatibility();
            }
        }
    }
}