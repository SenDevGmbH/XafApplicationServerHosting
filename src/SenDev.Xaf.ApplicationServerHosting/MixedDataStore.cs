using DevExpress.Xpo.DB;
using DevExpress.Xpo.Helpers;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics.Contracts;
using DevExpress.Data.Filtering;

namespace SenDev.Xaf.ApplicationServerHosting
{
    public class MixedDataStore : ICommandChannel, ISqlDataStore, IDisposable
    {

        private class DataStoreEntry
        {

            internal DataStoreEntry(IDataStore dataStore, DataStoreMode mode, string prefix, bool deletePrefix)
            {
                DataStore = dataStore;
                Mode = mode;
                Prefix = prefix;
                DeletePrefix = deletePrefix;
            }
            internal IDataStore DataStore { get; }

            internal DataStoreMode Mode { get; }

            internal string Prefix { get; }

            internal bool DeletePrefix { get; }
        }


        private class ClonedDbTable : DBTable
        {
            internal ClonedDbTable(DBTable table, string prefix)
            {
                Prefix = prefix;
                Columns.AddRange(table.Columns);
                ForeignKeys.AddRange(table.ForeignKeys);
                Indexes.AddRange(table.Indexes);
                IsView = table.IsView;
                PrimaryKey = table.PrimaryKey;
            }

            public string Prefix { get; }

        }

        private readonly List<DataStoreEntry> entries = new List<DataStoreEntry>();


        public MixedDataStore(IDataStore dataStore, DataStoreMode mode)
        {
            AddDataStore(dataStore, mode, string.Empty, false);
        }
        public void AddDataStore(IDataStore dataStore, DataStoreMode mode, string prefix, bool deletePrefix)
        {
            entries.Add(new DataStoreEntry(dataStore, mode, prefix, deletePrefix));
        }

        private IDataStore MainDataStore => entries.First().DataStore;
        public AutoCreateOption AutoCreateOption => MainDataStore.AutoCreateOption;

        public IDbConnection Connection => (MainDataStore as ISqlDataStore)?.Connection ?? throw new NotSupportedException();

        public IDbCommand CreateCommand() => (MainDataStore as ISqlDataStore)?.CreateCommand() ?? throw new NotSupportedException();

        public object Do(string command, object args) => ((ICommandChannel)MainDataStore).Do(command, args);

        public ModificationResult ModifyData(params ModificationStatement[] dmlStatements)
        {
            if (dmlStatements == null)
                throw new ArgumentNullException(nameof(dmlStatements));


            List<ParameterValue> identities = new List<ParameterValue>();
            foreach (var statement in dmlStatements)
            {
                var entry = GetDataStoreEntry(statement, DataStoreMode.ReadWrite);
                RemovePrefix(statement, entry.Prefix);
                identities.AddRange(entry.DataStore.ModifyData(statement).Identities);

            }

            return new ModificationResult(identities.ToArray());
        }



        private void RemovePrefix(JoinNode node, string prefix)
        {
            if (!string.IsNullOrEmpty(prefix))
                RemovePrefix(node, prefix, new Dictionary<DBTable, DBTable>());
        }

        private void RemovePrefix(JoinNode node, string prefix, Dictionary<DBTable, DBTable> clonedTables)
        {
            if (node.Table.Name.StartsWith(prefix, StringComparison.Ordinal))
            {
                DBTable cloned;
                if (!clonedTables.TryGetValue(node.Table, out cloned))
                {
                    cloned = RemovePrefix(node.Table, prefix);
                    clonedTables[node.Table] = cloned;
                    node.Table = cloned;
                }
            }
            foreach (var subNode in node.SubNodes)
                RemovePrefix(subNode, prefix, clonedTables);
        }

        private static DBTable RemovePrefix(DBTable table, string prefix)
        {
            DBTable cloned = new ClonedDbTable(table, prefix);
            cloned.Name = table.Name.Substring(prefix.Length);
            return cloned;
        }

        private static DBTable[] GetTables(params BaseStatement[] statements)
        {
            HashSet<DBTable> tables = new HashSet<DBTable>();
            foreach (BaseStatement statement in statements)
            {
                List<JoinNode> nodes;
                List<CriteriaOperator> criteria;
                statement.CollectJoinNodesAndCriteria(out nodes, out criteria);
                foreach (JoinNode node in nodes)
                {
                    DBTable table = node.Table;
                    if (table != null)
                    {
                        DBProjection projection = node.Table as DBProjection;
                        if (projection == null)
                        {
                            tables.Add(table);
                        }
                        else
                        {
                            foreach (var tbl in GetTables(projection.Projection))
                            {
                                tables.Add(table);
                            }
                        }
                    }
                }
            }

            return tables.ToArray();
        }

        private DataStoreEntry GetDataStoreEntry(BaseStatement statement, DataStoreMode mode)
        {
            if (entries.Count == 1) return entries[0];
            var tables = GetTables(statement);

            var dataStores = tables.Select(GetDataStoreEntry).Distinct().ToArray();
            if (dataStores.Length == 1)
            {
                var result = dataStores[0];
                if (result.Mode < mode)
                    throw new InvalidOperationException($"Mode {mode} not allowed on data store {result.DataStore}");
                return result;
            }

            string joinedNames = string.Join(", ", tables.Select(t => t.Name));

            if (dataStores.Length == 0)
                throw new InvalidOperationException($"No data stores found for tables: {joinedNames}");

            throw new InvalidOperationException($"Multiple data stores found in sinlge statement. Tables: {joinedNames}");

        }

        private DataStoreEntry GetDataStoreEntry(DBTable table)
        {
            ClonedDbTable cloned = table as ClonedDbTable;
            if (cloned != null)
                return entries.FirstOrDefault(e => e.Prefix == cloned.Prefix);
            else
                return entries.FirstOrDefault(e => !string.IsNullOrEmpty(e.Prefix) && table.Name.StartsWith(e.Prefix, StringComparison.Ordinal)) ?? entries[0];
        }

        public SelectedData SelectData(params SelectStatement[] selects)
        {
            if (selects == null)
                throw new ArgumentNullException(nameof(selects));

            List<SelectStatementResult> results = new List<SelectStatementResult>();
            foreach (var statement in selects)
            {
                var entry = GetDataStoreEntry(statement, DataStoreMode.ReadOnly);
                RemovePrefix(statement, entry.Prefix);
                results.AddRange(entry.DataStore.SelectData(statement).ResultSet);
            }

            return new SelectedData(results.ToArray());

        }

        public UpdateSchemaResult UpdateSchema(bool dontCreateIfFirstTableNotExist, params DBTable[] tables)
        {
            foreach (var entry in entries.Where(e => e.Mode >= DataStoreMode.SchemaUpdate))
            {
                var filteredTables = tables.Where(t => GetDataStoreEntry(t) == entry).Select(t => RemovePrefix(t, entry.Prefix)).ToArray();
                if (entry.DataStore.UpdateSchema(dontCreateIfFirstTableNotExist, filteredTables) == UpdateSchemaResult.FirstTableNotExists)
                    return UpdateSchemaResult.FirstTableNotExists;
            }

            return UpdateSchemaResult.SchemaExists;

        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (disposing)
            {
                foreach (var disposable in entries.Select(e => e.DataStore).OfType<IDisposable>())
                    disposable.Dispose();
            }
        }

        ~MixedDataStore()
        {
            Dispose(false);
        }
    }
}
