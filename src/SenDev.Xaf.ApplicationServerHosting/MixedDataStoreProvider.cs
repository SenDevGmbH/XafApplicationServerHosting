﻿using DevExpress.ExpressApp.Xpo;
using DevExpress.Xpo;
using DevExpress.Xpo.DB;
using System;
using System.Collections.Generic;
using System.Configuration;

namespace SenDev.Xaf.ApplicationServerHosting
{
    [Serializable]
    public class MixedDataStoreProvider : IXpoDataStoreProvider
    {

        [Serializable]
        private class ConnectionStringEntry : DataStoreEntryBase
        {
            internal ConnectionStringEntry(string connectionString, string prefix, bool removePrefix,
                DataStoreMode mode) : base(prefix, removePrefix, mode)
            {
                ConnectionString = connectionString;
            }
            public string ConnectionString { get; }
        }

        private List<ConnectionStringEntry> entries = new List<ConnectionStringEntry>();
        public MixedDataStoreProvider(string connectionString)
        {
            ConnectionString = connectionString;
        }

        public void AddConnectionString(string connectionString, string prefix, bool removePrefix, DataStoreMode mode)
        {
            entries.Add(new ConnectionStringEntry(connectionString, prefix, removePrefix, mode));
        }
        public string ConnectionString
        {
            get;
        }

        public IDataStore CreateSchemaCheckingStore(out IDisposable[] disposableObjects)
        {
            return CreateStoreCore(out disposableObjects);
        }

        public IDataStore CreateUpdatingStore(bool allowUpdateSchema, out IDisposable[] disposableObjects)
        {
            return CreateStoreCore(out disposableObjects);
        }

        public IDataStore CreateWorkingStore(out IDisposable[] disposableObjects)
        {
            return CreateStoreCore(out disposableObjects);

        }

        private IDataStore CreateStoreCore(out IDisposable[] disposableObjects)
        {
            disposableObjects = new IDisposable[0];
            var mixedDataStore = new MixedDataStore(
                XpoDefault.GetConnectionProvider(ConnectionString, AutoCreateOption.DatabaseAndSchema),
                DataStoreMode.SchemaUpdate);

            Dictionary<string, IDataStore> dataStores = new Dictionary<string, IDataStore>();
            foreach (var entry in entries)
            {
                string connectionString = entry.ConnectionString;
                if (!dataStores.TryGetValue(connectionString, out var dataStore))
                {
                    dataStores[connectionString] = dataStore = XpoDefault.GetConnectionProvider(connectionString,
                                        entry.Mode == DataStoreMode.SchemaUpdate ? AutoCreateOption.DatabaseAndSchema : AutoCreateOption.SchemaAlreadyExists);
                }
                mixedDataStore.AddDataStore(dataStore,
                    entry.Mode, entry.Prefix, entry.DeletePrefix);
            }

            return mixedDataStore;
        }
    }
}
